/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Aurora.DataManager.Migration;
using Aurora.Framework;
using MySql.Data.MySqlClient;
using OpenMetaverse;

namespace Aurora.DataManager.MySQL
{
    public class MySQLDataLoader : DataManagerBase
    {
        private string m_connectionString = "";

        public override string Identifier
        {
            get { return "MySQLData"; }
        }

        #region Database

        public override void ConnectToDatabase(string connectionstring, string migratorName, bool validateTables)
        {
            m_connectionString = connectionstring;
            MySqlConnection c = new MySqlConnection(connectionstring);
            int subStrA = connectionstring.IndexOf("Database=");
            int subStrB = connectionstring.IndexOf(";", subStrA);
            string noDatabaseConnector = m_connectionString.Substring(0, subStrA) + m_connectionString.Substring(subStrB+1);

            ExecuteNonQuery(noDatabaseConnector, "create schema IF NOT EXISTS " + c.Database, new Dictionary<string, object>());

            var migrationManager = new MigrationManager(this, migratorName, validateTables);
            migrationManager.DetermineOperation();
            migrationManager.ExecuteOperation();
        }

        public void CloseDatabase(MySqlConnection connection)
        {
            //Interlocked.Decrement (ref m_locked);
            //connection.Close();
            //connection.Dispose();
        }

        public override void CloseDatabase()
        {
            //Interlocked.Decrement (ref m_locked);
            //m_connection.Close();
            //m_connection.Dispose();
        }

        #endregion

        #region Query

        public IDataReader Query(string sql, Dictionary<string, object> parameters)
        {
            try
            {
                MySqlParameter[] param = new MySqlParameter[parameters.Count];
                int i = 0;
                foreach (KeyValuePair<string, object> p in parameters)
                {
                    param[i] = new MySqlParameter(p.Key, p.Value);
                    i++;
                }
                return MySqlHelper.ExecuteReader(m_connectionString, sql, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Query(" + sql + "), " + e);
                return null;
            }
        }

        public void ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            ExecuteNonQuery(m_connectionString, sql, parameters);
        }

        public void ExecuteNonQuery(string connStr, string sql, Dictionary<string, object> parameters)
        {
            try
            {
                MySqlParameter[] param = new MySqlParameter[parameters.Count];
                int i = 0;
                foreach (KeyValuePair<string, object> p in parameters)
                {
                    param[i] = new MySqlParameter(p.Key, p.Value);
                    i++;
                }
                MySqlHelper.ExecuteNonQuery(connStr, sql, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] ExecuteNonQuery(" + sql + "), " + e);
            }
        }

        public override List<string> QueryFullData(string whereClause, string table, string wantedValue)
        {
            IDataReader reader = null;
            List<string> retVal = new List<string>();
            string query = String.Format("select {0} from {1} {2}",
                                         wantedValue, table, whereClause);
            try
            {
                using (reader = Query(query, new Dictionary<string, object>()))
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            retVal.Add(reader.GetString(i));
                        }
                    }
                    return retVal;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] QueryFullData(" + query + "), " + e);
                return null;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        //reader.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[MySQLDataLoader] Query(" + query + "), " + e);
                }
            }
        }

        public override IDataReader QueryData(string whereClause, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} {2}",
                                         wantedValue, table, whereClause);
            return Query(query, new Dictionary<string, object>());
        }

        private static string QueryFilter2Query(QueryFilter filter, out Dictionary<string, object> ps, ref uint j)
        {
            ps = new Dictionary<string,object>();
            Dictionary<string, object>[] pss = {ps};
            string query = "";
            List<string> parts;
            uint i = j;
            bool had = false;
            if (filter.Count > 0)
            {
                query += "(";

                #region equality

                parts = new List<string>();
                foreach(KeyValuePair<string, object> where in filter.andFilters){
                    string key = "?where_AND_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} = {1}", where.Key, key));
                }
                if(parts.Count > 0){
                    query += " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach(KeyValuePair<string, object> where in filter.orFilters){
                    string key = "?where_OR_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} = {1}", where.Key, key));
                }
                if(parts.Count > 0){
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, List<object>> where in filter.orMultiFilters)
                {
                    foreach (object value in where.Value)
                    {
                        string key = "?where_OR_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                        ps[key] = value;
                        parts.Add(string.Format("{0} = {1}", where.Key, key));
                    }
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in filter.andNotFilters)
                {
                    string key = "?where_AND_NOT_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} != {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region LIKE

                parts = new List<string>();
                foreach (KeyValuePair<string, string> where in filter.andLikeFilters)
                {
                    string key = "?where_ANDLIKE_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} LIKE {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, string> where in filter.orLikeFilters)
                {
                    string key = "?where_ORLIKE_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} LIKE {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, List<string>> where in filter.orLikeMultiFilters)
                {
                    foreach (string value in where.Value)
                    {
                        string key = "?where_ORLIKE_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                        ps[key] = value;
                        parts.Add(string.Format("{0} LIKE {1}", where.Key, key));
                    }
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region bitfield &

                parts = new List<string>();
                foreach(KeyValuePair<string, uint> where in filter.andBitfieldAndFilters){
                    string key = "?where_bAND_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} & {1}", where.Key, key));
                }
                if(parts.Count > 0){
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, uint> where in filter.orBitfieldAndFilters)
                {
                    string key = "?where_bOR_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} & {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region greater than

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.andGreaterThanFilters)
                {
                    string key = "?where_gtAND_" + (++i) + where.Key.Replace("`", "").Replace("(","__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} > {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.orGreaterThanFilters)
                {
                    string key = "?where_gtOR_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} > {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.andGreaterThanEqFilters)
                {
                    string key = "?where_gteqAND_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} >= {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region less than

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.andLessThanFilters)
                {
                    string key = "?where_ltAND_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} > {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.orLessThanFilters)
                {
                    string key = "?where_ltOR_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} > {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.andLessThanEqFilters)
                {
                    string key = "?where_lteqAND_" + (++i) + where.Key.Replace("`", "").Replace("(", "__").Replace(")", "");
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} <= {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                foreach(QueryFilter subFilter in filter.subFilters){
                    Dictionary<string, object> sps;
                    query += (had ? " AND" : string.Empty) + QueryFilter2Query(subFilter, out sps, ref i);
                    pss[pss.Length] = sps;
                    if (subFilter.Count > 0)
                    {
                        had = true;
                    }
                }
                query += ")";
            }
            pss.SelectMany(x => x).ToLookup(x=>x.Key, x=>x.Value).ToDictionary(x => x.Key, x=>x.First());
            return query;
        }

        public override List<string> Query(string[] wantedValue, string table, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            string query = string.Format("SELECT {0} FROM {1}", string.Join(", ", wantedValue), table); ;
            Dictionary<string, object> ps = new Dictionary<string,object>();
            List<string> retVal = new List<string>();
            List<string> parts = new List<string>();

            if (queryFilter != null && queryFilter.Count > 0)
            {
                uint j = 0;
                query += " WHERE " + QueryFilter2Query(queryFilter, out ps, ref j);
            }

            if (sort != null && sort.Count > 0)
            {
                parts = new List<string>();
                foreach (KeyValuePair<string, bool> sortOrder in sort)
                {
                    parts.Add(string.Format("`{0}` {1}", sortOrder.Key, sortOrder.Value ? "ASC" : "DESC"));
                }
                query += " ORDER BY " + string.Join(", ", parts.ToArray());
            }

            if(start.HasValue){
                query += " LIMIT " + start.Value.ToString();
                if (count.HasValue)
                {
                    query += ", " + count.Value.ToString();
                }
            }

            IDataReader reader = null;
            int i = 0;
            try
            {
                using (reader = Query(query, ps))
                {
                    while (reader.Read())
                    {
                        for (i = 0; i < reader.FieldCount; i++)
                        {
                            Type r = reader[i].GetType();
                            retVal.Add(r == typeof(DBNull) ? null : reader.GetString(i));
                        }
                    }
                    return retVal;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Query(" + query + "), " + e);
                return null;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        //reader.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[MySQLDataLoader] Query(" + query + "), " + e);
                }
            }
        }

        public override Dictionary<string, List<string>> QueryNames(string[] keyRow, object[] keyValue, string table, string wantedValue)
        {
            IDataReader reader = null;
            Dictionary<string, List<string>> retVal = new Dictionary<string, List<string>>();
            Dictionary<string, object> ps = new Dictionary<string, object>();
            string query = String.Format("select {0} from {1} where ",
                                         wantedValue, table);
            int i = 0;
            foreach (object value in keyValue)
            {
                query += String.Format("{0} = ?{1} and ", keyRow[i], keyRow[i]);
                ps["?" + keyRow[i]] = value;
                i++;
            }
            query = query.Remove(query.Length - 5);

            try
            {
                using (reader = Query(query, ps))
                {
                    while (reader.Read())
                    {
                        for (i = 0; i < reader.FieldCount; i++)
                        {
                            Type r = reader[i].GetType();
                            AddValueToList(ref retVal, reader.GetName(i),
                                           r == typeof (DBNull) ? null : reader[i].ToString());
                        }
                    }
                    return retVal;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] QueryNames(" + query + "), " + e);
                return null;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        //reader.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[MySQLDataLoader] QueryNames(" + query + "), " + e);
                }
            }
        }

        private void AddValueToList(ref Dictionary<string, List<string>> dic, string key, string value)
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, new List<string>());
            }

            dic[key].Add(value);
        }

        #endregion

        #region Update

        public override bool Update(string table, Dictionary<string, object> values, Dictionary<string, int> incrementValues, QueryFilter queryFilter, uint? start, uint? count)
        {
            if ((values == null || values.Count < 1) && (incrementValues == null || incrementValues.Count < 1))
            {
                MainConsole.Instance.Warn("Update attempted with no values");
                return false;
            }

            string query = string.Format("UPDATE {0}", table); ;
            Dictionary<string, object> ps = new Dictionary<string, object>();

            string filter = "";
            if (queryFilter != null && queryFilter.Count > 0)
            {
                uint j = 0;
                filter = " WHERE " + QueryFilter2Query(queryFilter, out ps, ref j);
            }

            List<string> parts = new List<string>();
            if (values != null)
            {
                foreach (KeyValuePair<string, object> value in values)
                {
                    string key = "?updateSet_" + value.Key.Replace("`", "");
                    ps[key] = value.Value;
                    parts.Add(string.Format("{0} = {1}", value.Key, key));
                }
            }
            if (incrementValues != null)
            {
                foreach (KeyValuePair<string, int> value in incrementValues)
                {
                    string key = "?updateSet_increment_" + value.Key.Replace("`", "");
                    ps[key] = value.Value;
                    parts.Add(string.Format("{0} = {0} + {1}", value.Key, key));
                }
            }

            query += " SET " + string.Join(", ", parts.ToArray()) + filter;

            if (start.HasValue)
            {
                query += " LIMIT " + start.Value.ToString();
                if (count.HasValue)
                {
                    query += ", " + count.Value.ToString();
                }
            }

            try
            {
                ExecuteNonQuery(query, ps);
            }
            catch (MySqlException e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Update(" + query + "), " + e);
            }
            return true;
        }

        #endregion

        #region Insert

        public override bool InsertMultiple(string table, List<object[]> values)
        {
            string query = String.Format("insert into {0} select ", table);
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            int i = 0;
            foreach (object[] value in values)
            {
                foreach (object v in value)
                {
                    parameters[Util.ConvertDecString(i)] = v;
                    query += "?" + Util.ConvertDecString(i++) + ",";
                }
                query = query.Remove(query.Length - 1);
                query += " union all select ";
            }
            query = query.Remove(query.Length - (" union all select ").Length);

            try
            {
                ExecuteNonQuery(query, parameters);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Insert(" + query + "), " + e);
            }
            return true;
        }

        public override bool Insert(string table, object[] values)
        {
            string query = String.Format("insert into {0} values (", table);
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            int i = 0;
            foreach (object o in values)
            {
                parameters[Util.ConvertDecString(i)] = o;
                query += "?" + Util.ConvertDecString(i++) + ",";
            }
            query = query.Remove(query.Length - 1);
            query += ")";

            try
            {
                ExecuteNonQuery(query, parameters);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Insert(" + query + "), " + e);
            }
            return true;
        }

        public override bool Insert(string table, string[] keys, object[] values)
        {
            string query = String.Format("insert into {0} (", table);
            Dictionary<string, object> param = new Dictionary<string, object>();

            int i = 0;
            foreach (string key in keys)
            {
                param.Add("?" + key, values[i]);
                query += String.Format("{0},", key);
                i++;
            }
            query = query.Remove(query.Length - 1);
            query += ") values (";

#if (!ISWIN)
            foreach (string key in keys)
                query = query + String.Format("?{0},", key);
#else
            query = keys.Aggregate(query, (current, key) => current + String.Format("?{0},", key));
#endif
            query = query.Remove(query.Length - 1);
            query += ")";

            try
            {
                ExecuteNonQuery(query, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Insert(" + query + "), " + e);
            }
            return true;
        }

        public override bool Replace(string table, string[] keys, object[] values)
        {
            string query = String.Format("replace into {0} (", table);
            Dictionary<string, object> param = new Dictionary<string, object>();

            int i = 0;
            foreach (string key in keys)
            {
                string kkey = key;
                if (key.Contains('`'))
                    kkey = key.Replace("`", ""); //Remove them

                param.Add("?" + kkey, values[i].ToString());
                query += "`" + kkey + "`" + ",";
                i++;
            }
            query = query.Remove(query.Length - 1);
            query += ") values (";

            foreach (string key in keys)
            {
                string kkey = key;
                if (key.Contains('`'))
                    kkey = key.Replace("`", ""); //Remove them
                query += String.Format("?{0},", kkey);
            }
            query = query.Remove(query.Length - 1);
            query += ")";

            try
            {
                ExecuteNonQuery(query, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Replace(" + query + "), " + e);
                return false;
            }
            return true;
        }

        public override bool DirectReplace(string table, string[] keys, object[] values)
        {
            string query = String.Format("replace into {0} (", table);
            Dictionary<string, object> param = new Dictionary<string, object>();

            foreach (string key in keys)
            {
                string kkey = key;
                if (key.Contains('`'))
                    kkey = key.Replace("`", ""); //Remove them

                query += "`" + kkey + "`" + ",";
            }
            query = query.Remove(query.Length - 1);
            query += ") values (";

#if (!ISWIN)
            foreach (object value in values)
                query = query + String.Format("{0},", value.ToString());
#else
            query = values.Aggregate(query, (current, key) => current + String.Format("{0},", key.ToString()));
#endif
            query = query.Remove(query.Length - 1);
            query += ")";

            try
            {
                ExecuteNonQuery(query, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] DirectReplace(" + query + "), " + e);
                return false;
            }
            return true;
        }

        public override bool Insert(string table, object[] values, string updateKey, object updateValue)
        {
            string query = String.Format("insert into {0} VALUES('", table);
            Dictionary<string, object> param = new Dictionary<string, object>();
            int i = 0;
            foreach (object o in values)
            {
                param["?" + Util.ConvertDecString(i)] = o;
                query += "?" + Util.ConvertDecString(i++) + ",";
            }
            query = query.Remove(query.Length - 1);
            query += String.Format(") ON DUPLICATE KEY UPDATE {0} = '{1}'", updateKey, updateValue);
            try
            {
                ExecuteNonQuery(query, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Insert(" + query + "), " + e);
                return false;
            }
            return true;
        }

        #endregion

        #region Delete

        public override bool Delete(string table, string[] keys, object[] values)
        {
            Dictionary<string, object> param = new Dictionary<string, object>();
            string query = "delete from " + table + (keys.Length > 0 ? " WHERE " : "");
            int i = 0;
            foreach (object value in values)
            {
                param["?" + keys[i].Replace("`", "")] = value;
                query += keys[i] + " = ?" + keys[i].Replace("`", "") + " AND ";
                i++;
            }
            if (keys.Length > 0)
                query = query.Remove(query.Length - 5);
            try
            {
                ExecuteNonQuery(query, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Delete(" + query + "), " + e);
                return false;
            }
            return true;
        }

        public override bool DeleteByTime(string table, string key)
        {
            QueryFilter filter = new QueryFilter();
            filter.andLessThanEqFilters["(UNIX_TIMESTAMP(`" + key.Replace("`","") + "`) - UNIX_TIMESTAMP())"] = 0;

            return Delete(table, filter);
        }

        public override bool Delete(string table, QueryFilter queryFilter)
        {
            Dictionary<string, object> ps;
            uint j=0;
            string query = "DELETE FROM " + table + " WHERE " + QueryFilter2Query(queryFilter, out ps, ref j);

            try
            {
                ExecuteNonQuery(query, ps);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Delete(" + query + "), " + e);
                return false;
            }
            return true;
        }

        #endregion

        public override string FormatDateTimeString(int time)
        {
            if (time == 0)
                return "now()";
            return "date_add(now(), interval " + time + " minute)";
        }

        public override string IsNull(string field, string defaultValue)
        {
            return "IFNULL(" + field + "," + defaultValue + ")";
        }

        public override string ConCat(string[] toConcat)
        {
#if (!ISWIN)
            string returnValue = "concat(";
            foreach (string s in toConcat)
                returnValue = returnValue + (s + ",");
#else
            string returnValue = toConcat.Aggregate("concat(", (current, s) => current + (s + ","));
#endif
            return returnValue.Substring(0, returnValue.Length - 1) + ")";
        }

        #region Tables

        public override void CreateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indices)
        {
            table = table.ToLower();
            if (TableExists(table))
            {
                throw new DataManagerException("Trying to create a table with name of one that already exists.");
            }

            IndexDefinition primary = null;
            foreach (IndexDefinition index in indices)
            {
                if (index.Type == IndexType.Primary)
                {
                    primary = index;
                    break;
                }
            }

            List<string> columnDefinition = new List<string>();

            foreach (ColumnDefinition column in columns)
            {
                columnDefinition.Add("`" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type));
            }
            if (primary != null && primary.Fields.Length > 0)
            {
                columnDefinition.Add("PRIMARY KEY (`" + string.Join("`, `", primary.Fields) + "`)");
            }

            List<string> indicesQuery = new List<string>(indices.Length);
            foreach (IndexDefinition index in indices)
            {
                string type = "KEY";
                switch (index.Type)
                {
                    case IndexType.Primary:
                        continue;
                    case IndexType.Unique:
                        type = "UNIQUE";
                        break;
                    case IndexType.Index:
                    default:
                        type = "KEY";
                        break;
                }
                indicesQuery.Add(string.Format("{0}( {1} )", type, "`" + string.Join("`, `", index.Fields) + "`"));
            }

            string query = string.Format("create table " + table + " ( {0} {1}) ", string.Join(", ", columnDefinition.ToArray()), indicesQuery.Count > 0 ? ", " + string.Join(", ", indicesQuery.ToArray()) : string.Empty);

            try
            {
                ExecuteNonQuery(query, new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] CreateTable", e);
            }
        }

        public override void UpdateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indices, Dictionary<string, string> renameColumns)
        {
            table = table.ToLower();
            if (!TableExists(table))
            {
                throw new DataManagerException("Trying to update a table with name of one that does not exist.");
            }

            List<ColumnDefinition> oldColumns = ExtractColumnsFromTable(table);

            Dictionary<string, ColumnDefinition> removedColumns = new Dictionary<string, ColumnDefinition>();
            Dictionary<string, ColumnDefinition> modifiedColumns = new Dictionary<string, ColumnDefinition>();
#if (!ISWIN)
            Dictionary<string, ColumnDefinition> addedColumns = new Dictionary<string, ColumnDefinition>();
            foreach (ColumnDefinition column in columns)
            {
                if (!oldColumns.Contains(column)) addedColumns.Add(column.Name.ToLower(), column);
            }
            foreach (ColumnDefinition column in oldColumns)
            {
                if (!columns.Contains(column))
                {
                    if (addedColumns.ContainsKey(column.Name.ToLower()))
                    {
                        if (column.Name.ToLower() != addedColumns[column.Name.ToLower()].Name.ToLower() || column.Type != addedColumns[column.Name.ToLower()].Type)
                        {
                            modifiedColumns.Add(column.Name.ToLower(), addedColumns[column.Name.ToLower()]);
                        }
                        addedColumns.Remove(column.Name.ToLower());
                    }
                    else
                    {
                        removedColumns.Add(column.Name.ToLower(), column);
                    }
                }
            }
#else
            Dictionary<string, ColumnDefinition> addedColumns = columns.Where(column => !oldColumns.Contains(column)).ToDictionary(column => column.Name.ToLower());
            foreach (ColumnDefinition column in oldColumns.Where(column => !columns.Contains(column)))
            {
                if (addedColumns.ContainsKey(column.Name.ToLower()))
                {
                    if (column.Name.ToLower() != addedColumns[column.Name.ToLower()].Name.ToLower() || column.Type != addedColumns[column.Name.ToLower()].Type)
                    {
                        modifiedColumns.Add(column.Name.ToLower(), addedColumns[column.Name.ToLower()]);
                    }
                    addedColumns.Remove(column.Name.ToLower());
                }
                else{
                    removedColumns.Add(column.Name.ToLower(), column);
                }
            }
#endif


            try
            {
#if (!ISWIN)
                foreach (ColumnDefinition column in addedColumns.Values)
                {
                    string addedColumnsQuery = "add `" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type) + " ";
                    string query = string.Format("alter table " + table + " " + addedColumnsQuery);
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
                foreach (ColumnDefinition column in modifiedColumns.Values)
                {
                    string modifiedColumnsQuery = "modify column `" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type) + " ";
                    string query = string.Format("alter table " + table + " " + modifiedColumnsQuery);
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
                foreach (ColumnDefinition column in removedColumns.Values)
                {
                    string droppedColumnsQuery = "drop `" + column.Name + "` ";
                    string query = string.Format("alter table " + table + " " + droppedColumnsQuery);
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
#else
                foreach (string query in addedColumns.Values.Select(column => "add `" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type) +
                                                                              " ").Select(addedColumnsQuery => string.Format("alter table " + table + " " + addedColumnsQuery)))
                {
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
                foreach (string query in modifiedColumns.Values.Select(column => "modify column `" + column.Name + "` " +
                                                                                 GetColumnTypeStringSymbol(column.Type) + " ").Select(modifiedColumnsQuery => string.Format("alter table " + table + " " + modifiedColumnsQuery)))
                {
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
                foreach (string query in removedColumns.Values.Select(column => "drop `" + column.Name + "` ").Select(droppedColumnsQuery => string.Format("alter table " + table + " " + droppedColumnsQuery)))
                {
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] UpdateTable", e);
            }

            Dictionary<string, IndexDefinition> oldIndicesDict = ExtractIndicesFromTable(table);

            List<string> removeIndices = new List<string>();
            List<string> oldIndexNames = new List<string>(oldIndicesDict.Count);
            List<IndexDefinition> oldIndices = new List<IndexDefinition>(oldIndicesDict.Count);
            List<IndexDefinition> newIndices = new List<IndexDefinition>();

            foreach (KeyValuePair<string, IndexDefinition> oldIndex in oldIndicesDict)
            {
                oldIndexNames.Add(oldIndex.Key);
                oldIndices.Add(oldIndex.Value);
            }
            int i=0;
            foreach(IndexDefinition oldIndex in oldIndices){
                bool found = false;
                foreach (IndexDefinition newIndex in indices)
                {
                    if (oldIndex.Equals(newIndex))
                    {
                        found = true;
                        break;
                    }
                    else
                    {
                        MainConsole.Instance.Info(oldIndex.Type.ToString() + " " + string.Join(", ", oldIndex.Fields) + " does not match new index " + newIndex.Type.ToString() + " " + string.Join(", ", newIndex.Fields));
                    }
                }
                if (!found)
                {
                    removeIndices.Add(oldIndexNames[i]);
                }
                ++i;
            }

            foreach (IndexDefinition newIndex in indices)
            {
                bool found = false;
                foreach (IndexDefinition oldIndex in oldIndices)
                {
                    if (oldIndex.Equals(newIndex))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    newIndices.Add(newIndex);
                }
            }

            foreach (string oldIndex in removeIndices)
            {
                ExecuteNonQuery(string.Format("ALTER TABLE `{0}` DROP INDEX `{1}`", table, oldIndex), new Dictionary<string, object>());
            }
            foreach (IndexDefinition newIndex in newIndices)
            {
                ExecuteNonQuery(string.Format("ALTER TABLE `{0}` ADD {1} (`{2}`)", table, newIndex.Type == IndexType.Primary ? "PRIMARY KEY" : (newIndex.Type == IndexType.Unique ? "UNIQUE" : "INDEX"), string.Join("`, `", newIndex.Fields)), new Dictionary<string,object>());
            }
        }

        public override string GetColumnTypeStringSymbol(ColumnTypes type)
        {
            switch (type)
            {
                case ColumnTypes.Double:
                    return "DOUBLE";
                case ColumnTypes.Integer11:
                    return "int(11)";
                case ColumnTypes.Integer30:
                    return "int(30)";
                case ColumnTypes.UInteger11:
                    return "INT(11) UNSIGNED";
                case ColumnTypes.UInteger30:
                    return "INT(30) UNSIGNED";
                case ColumnTypes.Char36:
                    return "char(36)";
                case ColumnTypes.Char32:
                    return "char(32)";
                case ColumnTypes.Char5:
                    return "char(5)";
                case ColumnTypes.String:
                    return "TEXT";
                case ColumnTypes.String1:
                    return "VARCHAR(1)";
                case ColumnTypes.String2:
                    return "VARCHAR(2)";
                case ColumnTypes.String10:
                    return "VARCHAR(10)";
                case ColumnTypes.String16:
                    return "VARCHAR(16)";
                case ColumnTypes.String30:
                    return "VARCHAR(30)";
                case ColumnTypes.String32:
                    return "VARCHAR(32)";
                case ColumnTypes.String36:
                    return "VARCHAR(36)";
                case ColumnTypes.String45:
                    return "VARCHAR(45)";
                case ColumnTypes.String50:
                    return "VARCHAR(50)";
                case ColumnTypes.String64:
                    return "VARCHAR(64)";
                case ColumnTypes.String128:
                    return "VARCHAR(128)";
                case ColumnTypes.String100:
                    return "VARCHAR(100)";
                case ColumnTypes.String255:
                    return "VARCHAR(255)";
                case ColumnTypes.String512:
                    return "VARCHAR(512)";
                case ColumnTypes.String1024:
                    return "VARCHAR(1024)";
                case ColumnTypes.String8196:
                    return "VARCHAR(8196)";
                case ColumnTypes.Text:
                    return "TEXT";
                case ColumnTypes.MediumText:
                    return "MEDIUMTEXT";
                case ColumnTypes.LongText:
                    return "LONGTEXT";
                case ColumnTypes.Blob:
                    return "blob";
                case ColumnTypes.LongBlob:
                    return "longblob";
                case ColumnTypes.Date:
                    return "DATE";
                case ColumnTypes.DateTime:
                    return "DATETIME";
                case ColumnTypes.Float:
                    return "float";
                case ColumnTypes.TinyInt1:
                    return "TINYINT(1)";
                case ColumnTypes.TinyInt4:
                    return "TINYINT(4)";
                default:
                    throw new DataManagerException("Unknown column type.");
            }
        }

        public override void DropTable(string tableName)
        {
            tableName = tableName.ToLower();
            try
            {
                ExecuteNonQuery(string.Format("drop table {0}", tableName), new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] DropTable", e);
            }
        }

        public override void ForceRenameTable(string oldTableName, string newTableName)
        {
            newTableName = newTableName.ToLower();
            try
            {
                ExecuteNonQuery(string.Format("RENAME TABLE {0} TO {1}", oldTableName, newTableName),
                                new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] ForceRenameTable", e);
            }
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions)
        {
            sourceTableName = sourceTableName.ToLower();
            destinationTableName = destinationTableName.ToLower();
            try
            {
                ExecuteNonQuery(string.Format("insert into {0} select * from {1}", destinationTableName, sourceTableName), new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] CopyAllDataBetweenMatchingTables", e);
            }
        }

        public override bool TableExists(string table)
        {
            IDataReader reader = null;
            List<string> retVal = new List<string>();
            try
            {
                using (reader = Query("show tables", new Dictionary<string, object>()))
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            retVal.Add(reader.GetString(i).ToLower());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] TableExists", e);
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        //reader.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[MySQLDataLoader] TableExists", e);
                }
            }
            return retVal.Contains(table.ToLower());
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            var defs = new List<ColumnDefinition>();
            tableName = tableName.ToLower();
            IDataReader rdr = null;
            try
            {
                rdr = Query(string.Format("desc {0}", tableName), new Dictionary<string, object>());
                while (rdr.Read())
                {
                    var name = rdr["Field"];
                    var pk = rdr["Key"];
                    var type = rdr["Type"];
                    var extra = rdr["Extra"];
                    defs.Add(new ColumnDefinition
                    {
                        Name = name.ToString(),
                        Type = ConvertTypeToColumnType(type.ToString())
                    });
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] ExtractColumnsFromTable", e);
            }
            finally
            {
                try
                {
                    if (rdr != null)
                    {
                        rdr.Close();
                        //rdr.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Debug("[MySQLDataLoader] ExtractColumnsFromTable", e);
                }
            }
            return defs;
        }

        protected override Dictionary<string, IndexDefinition> ExtractIndicesFromTable(string tableName)
        {
            Dictionary<string, IndexDefinition> defs = new Dictionary<string, IndexDefinition>();
            tableName = tableName.ToLower();
            IDataReader rdr = null;
            Dictionary<string, Dictionary<uint, string>> indexLookup = new Dictionary<string, Dictionary<uint, string>>();
            Dictionary<string, bool> indexIsUnique = new Dictionary<string,bool>();

            try
            {
                rdr = Query(string.Format("SHOW INDEX IN {0}", tableName), new Dictionary<string, object>());
                while (rdr.Read())
                {
                    string name = rdr["Column_name"].ToString();
                    bool unique = uint.Parse(rdr["Non_unique"].ToString()) == 0;
                    string index = rdr["Key_name"].ToString();
                    uint sequence = uint.Parse(rdr["Seq_in_index"].ToString());
                    if (indexLookup.ContainsKey(index) == false)
                    {
                        indexLookup[index] = new Dictionary<uint, string>();
                    }
                    indexIsUnique[index] = unique;
                    indexLookup[index][sequence - 1] = name;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] ExtractIndicesFromTable", e);
            }
            finally
            {
                try
                {
                    if (rdr != null)
                    {
                        rdr.Close();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Debug("[MySQLDataLoader] ExtractIndicesFromTable", e);
                }
            }

            foreach (KeyValuePair<string, Dictionary<uint, string>> index in indexLookup)
            {
                index.Value.OrderBy(x=>x.Key);
                defs[index.Key] = new IndexDefinition
                {
                    Fields = index.Value.Values.ToArray<string>(),
                    Type = (indexIsUnique[index.Key] ? (index.Key == "PRIMARY" ? IndexType.Primary : IndexType.Unique) : IndexType.Index)
                };
            }

            return defs;
        }

        private ColumnTypes ConvertTypeToColumnType(string typeString)
        {
            string tStr = typeString.ToLower();
            //we'll base our names on lowercase
            switch (tStr)
            {
                case "double":
                    return ColumnTypes.Double;
                case "int(11)":
                    return ColumnTypes.Integer11;
                case "int(30)":
                    return ColumnTypes.Integer30;
                case "integer":
                    return ColumnTypes.Integer11;
                case "int(11) unsigned":
                    return ColumnTypes.UInteger11;
                case "int(30) unsigned":
                    return ColumnTypes.UInteger30;
                case "integer unsigned":
                    return ColumnTypes.UInteger11;
                case "char(36)":
                    return ColumnTypes.Char36;
                case "char(32)":
                    return ColumnTypes.Char32;
                case "char(5)":
                    return ColumnTypes.Char5;
                case "varchar(1)":
                    return ColumnTypes.String1;
                case "varchar(2)":
                    return ColumnTypes.String2;
                case "varchar(10)":
                    return ColumnTypes.String10;
                case "varchar(16)":
                    return ColumnTypes.String16;
                case "varchar(30)":
                    return ColumnTypes.String30;
                case "varchar(32)":
                    return ColumnTypes.String32;
                case "varchar(36)":
                    return ColumnTypes.String36;
                case "varchar(45)":
                    return ColumnTypes.String45;
                case "varchar(50)":
                    return ColumnTypes.String50;
                case "varchar(64)":
                    return ColumnTypes.String64;
                case "varchar(128)":
                    return ColumnTypes.String128;
                case "varchar(100)":
                    return ColumnTypes.String100;
                case "varchar(255)":
                    return ColumnTypes.String255;
                case "varchar(512)":
                    return ColumnTypes.String512;
                case "varchar(1024)":
                    return ColumnTypes.String1024;
                case "date":
                    return ColumnTypes.Date;
                case "datetime":
                    return ColumnTypes.DateTime;
                case "varchar(8196)":
                    return ColumnTypes.String8196;
                case "text":
                    return ColumnTypes.Text;
                case "mediumtext":
                    return ColumnTypes.MediumText;
                case "longtext":
                    return ColumnTypes.LongText;
                case "float":
                    return ColumnTypes.Float;
                case "blob":
                    return ColumnTypes.Blob;
                case "longblob":
                    return ColumnTypes.LongBlob;
                case "smallint(6)":
                    return ColumnTypes.Integer11;
                case "int(10)":
                    return ColumnTypes.Integer11;
                case "tinyint(1)":
                    return ColumnTypes.TinyInt1;
                case "tinyint(4)":
                    return ColumnTypes.TinyInt4;
            }
            if (tStr.StartsWith("varchar"))
            {
                //... Someone was editing the database
                // Swallow the exception... but set it to the highest setting so we don't break anything
                return ColumnTypes.String8196;
            }
            if (tStr.StartsWith("int"))
            {
                //... Someone was editing the database
                // Swallow the exception... but set it to the highest setting so we don't break anything
                return ColumnTypes.Integer11;
            }
            throw new Exception(
                "You've discovered some type in MySQL that's not reconized by Aurora, please place the correct conversion in ConvertTypeToColumnType. Type: " +
                tStr);
        }

        #endregion

        public override IGenericData Copy()
        {
            return new MySQLDataLoader();
        }
    }
}