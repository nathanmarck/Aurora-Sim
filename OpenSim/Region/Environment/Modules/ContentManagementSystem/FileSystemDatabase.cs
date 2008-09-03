﻿#region Header

// FileSystemDatabase.cs
// User: bongiojp 

#endregion Header

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Slash = System.IO.Path;
using System.Reflection;
using System.Xml;

using libsecondlife;

using Nini.Config;

using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Modules.World.Serialiser;
using OpenSim.Region.Environment.Modules.World.Terrain;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Physics.Manager;

using log4net;

using Axiom.Math;

namespace OpenSim.Region.Environment.Modules.ContentManagement
{
    public class FileSystemDatabase : IContentDatabase
    {
        #region Static Fields

        public static float TimeToDownload = 0;
        public static float TimeToSave = 0;
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Static Fields

        #region Fields

        private string m_repodir = null;
        private Dictionary<LLUUID, Scene> m_scenes = new Dictionary<LLUUID, Scene>();
        private Dictionary<LLUUID, IRegionSerialiser> m_serialiser = new Dictionary<LLUUID, IRegionSerialiser>();

        #endregion Fields

        #region Constructors

        public FileSystemDatabase()
        {
        }

        #endregion Constructors

        #region Private Methods

        // called by postinitialise
        private void CreateDirectory()
        {
            string scenedir;
            if (!Directory.Exists(m_repodir))
            	Directory.CreateDirectory(m_repodir);

            foreach (LLUUID region in m_scenes.Keys)
            {
            	scenedir = m_repodir + Slash.DirectorySeparatorChar + region + Slash.DirectorySeparatorChar;
            	if (!Directory.Exists(scenedir))
            		Directory.CreateDirectory(scenedir);
            }
        }

        // called by postinitialise
        private void SetupSerialiser()
        {
            if (m_serialiser.Count == 0)
            	foreach(LLUUID region in m_scenes.Keys)
            		m_serialiser.Add(region,
            		                 m_scenes[region].RequestModuleInterface<IRegionSerialiser>()
            		                 );
        }

        #endregion Private Methods

        #region Public Methods

        public int GetMostRecentRevision(LLUUID regionid)
        {
            return NumOfRegionRev(regionid);
        }

        public string GetRegionObjectHeightMap(LLUUID regionid)
        {
            String filename = m_repodir + Slash.DirectorySeparatorChar + regionid +
            	Slash.DirectorySeparatorChar + "heightmap.r32";
            FileStream fs = new FileStream( filename, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            String result = sr.ReadToEnd();
            sr.Close();
            fs.Close();
            return result;
        }

        public string GetRegionObjectHeightMap(LLUUID regionid, int revision)
        {
            String filename = m_repodir + Slash.DirectorySeparatorChar + regionid +
            	Slash.DirectorySeparatorChar + "heightmap.r32";
            FileStream fs = new FileStream( filename, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            String result = sr.ReadToEnd();
            sr.Close();
            fs.Close();
            return result;
        }

        public System.Collections.ArrayList GetRegionObjectXMLList(LLUUID regionid, int revision)
        {
            System.Collections.ArrayList objectList = new System.Collections.ArrayList();
            string filename = m_repodir + Slash.DirectorySeparatorChar + regionid + Slash.DirectorySeparatorChar + 
            	+ revision + Slash.DirectorySeparatorChar + "objects.xml";
            XmlDocument doc = new XmlDocument();
            XmlNode rootNode;
            //int primCount = 0;
            //SceneObjectGroup obj = null;

            if(File.Exists(filename))
            {
                XmlTextReader reader = new XmlTextReader(filename);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                doc.Load(reader);
                reader.Close();
                rootNode = doc.FirstChild;
                foreach (XmlNode aPrimNode in rootNode.ChildNodes)
                {
            		objectList.Add(aPrimNode.OuterXml);
            	}
            	return objectList;
            }
            return null;
        }

        public System.Collections.ArrayList GetRegionObjectXMLList(LLUUID regionid)
        {
            int revision = NumOfRegionRev(regionid);
            m_log.Info("[FSDB]: found revisions:" + revision);
            System.Collections.ArrayList xmlList = new System.Collections.ArrayList();
            string filename = m_repodir + Slash.DirectorySeparatorChar + regionid + Slash.DirectorySeparatorChar + 
            	+ revision + Slash.DirectorySeparatorChar + "objects.xml";
            XmlDocument doc = new XmlDocument();
            XmlNode rootNode;


            m_log.Info("[FSDB]: Checking if " + filename + " exists.");
            if(File.Exists(filename))
            {			
            	Stopwatch x = new Stopwatch();
            	x.Start();
            	
                XmlTextReader reader = new XmlTextReader(filename);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                doc.Load(reader);
                reader.Close();
                rootNode = doc.FirstChild;
            	
                foreach (XmlNode aPrimNode in rootNode.ChildNodes)
            		xmlList.Add(aPrimNode.OuterXml);
            	
            	x.Stop();
            	TimeToDownload += x.ElapsedMilliseconds;
            	m_log.Info("[FileSystemDatabase] Time spent retrieving xml files so far: " + TimeToDownload);
            	
            	return xmlList;
            }
            return null;
        }

        public void Initialise(Scene scene, string dir)
        {
            lock(this)
            {
            	if (m_repodir == null)
            		m_repodir = dir;
            }
            lock(m_scenes)
            	m_scenes.Add(scene.RegionInfo.RegionID, scene);
        }

        public System.Collections.Generic.SortedDictionary<string, string> ListOfRegionRevisions(LLUUID regionid)
        {
            SortedDictionary<string, string> revisionDict = new SortedDictionary<string,string>();

            string scenedir = m_repodir + Slash.DirectorySeparatorChar + regionid + Slash.DirectorySeparatorChar;
            string[] directories = Directory.GetDirectories(scenedir);

            FileStream fs = null;
            StreamReader sr = null;
            String logMessage = "";
            String logLocation = "";
            foreach(string revisionDir in directories)
            {
            	try {
            		logLocation = revisionDir + Slash.DirectorySeparatorChar + "log";
            		fs = new FileStream( logLocation, FileMode.Open);
            		sr = new StreamReader(fs);
            		logMessage = sr.ReadToEnd();
            		sr.Close();
            		fs.Close();
            		revisionDict.Add(revisionDir, logMessage);
            	}
            	catch (Exception)
            	{}
            }

            return revisionDict;
        }

        public int NumOfRegionRev(LLUUID regionid)
        {
            string scenedir = m_repodir + Slash.DirectorySeparatorChar + regionid + Slash.DirectorySeparatorChar;
            m_log.Info("[FSDB]: Reading scene dir: " + scenedir);
            string[] directories = Directory.GetDirectories(scenedir);
            return directories.Length;
        }

        // Run once and only once.
        public void PostInitialise()
        {
            SetupSerialiser();
            	
            	m_log.Info("[FSDB]: Creating repository in " + m_repodir + ".");
            	CreateDirectory();
        }

        public void SaveRegion(LLUUID regionid, string regionName, string logMessage)
        {
            m_log.Info("[FSDB]: ...............................");
            string scenedir = m_repodir + Slash.DirectorySeparatorChar + regionid + Slash.DirectorySeparatorChar;

            m_log.Info("[FSDB]: checking if scene directory exists: " + scenedir);
            if (!Directory.Exists(scenedir))
            	Directory.CreateDirectory(scenedir);

            int newRevisionNum = GetMostRecentRevision(regionid)+1;
            string revisiondir = scenedir + newRevisionNum + Slash.DirectorySeparatorChar;

            m_log.Info("[FSDB]: checking if revision directory exists: " + revisiondir);
            if (!Directory.Exists(revisiondir))
            	Directory.CreateDirectory(revisiondir);

            try {	
            	Stopwatch x = new Stopwatch();
            	x.Start();
            	if (m_scenes.ContainsKey(regionid))
            	{
            		m_serialiser[regionid].SerialiseRegion(m_scenes[regionid], revisiondir);
            	}
            	x.Stop();
            	TimeToSave += x.ElapsedMilliseconds;
            	m_log.Info("[FileSystemDatabase] Time spent serialising regions to files on disk for " + regionName + ": " + x.ElapsedMilliseconds);
            	m_log.Info("[FileSystemDatabase] Time spent serialising regions to files on disk so far: " + TimeToSave);
            }
            catch (Exception e)
            {
            	m_log.ErrorFormat("[FSDB]: Serialisation of region failed: " + e);
            	return;
            }

            try {
            	// Finish by writing log message.
            	FileStream file = new FileStream(revisiondir + "log", FileMode.Create, FileAccess.ReadWrite);
            	StreamWriter sw = new StreamWriter(file);
            	sw.Write(logMessage);
            	sw.Close();
            }
            catch (Exception e)
            {
            	m_log.ErrorFormat("[FSDB]: Failed trying to save log file " + e);
            	return;
            }
        }

        #endregion Public Methods
    }
}