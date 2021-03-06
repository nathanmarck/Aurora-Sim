/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Collections;
using System.IO;
using System.Reflection;

namespace Aurora.Framework.Servers.HttpServer
{
    public delegate void ReQueuePollServiceItem(PollServiceHttpRequest req);

    public class PollServiceWorkerThread
    {
        private readonly BlockingQueue<PollServiceHttpRequest> m_request;
        private readonly BaseHttpServer m_server;
        private readonly int m_timeout = 250;
        private bool m_running = true;

        public PollServiceWorkerThread(BaseHttpServer pSrv, int pTimeout)
        {
            m_request = new BlockingQueue<PollServiceHttpRequest>();
            m_server = pSrv;
            m_timeout = pTimeout;
        }

        public event ReQueuePollServiceItem ReQueue;

        public void ThreadStart(object o)
        {
            Culture.SetCurrentCulture();
            Run();
        }

        public void Run()
        {
            while (m_running)
            {
                PollServiceHttpRequest req = m_request.Dequeue();
                try
                {
                    if (req.PollServiceArgs.Valid())
                    {
                        if (req.PollServiceArgs.HasEvents(req.RequestID, req.PollServiceArgs.Id))
                        {
                            StreamReader str;
                            try
                            {
                                str = new StreamReader(req.Request.Body);
                            }
                            catch (ArgumentException)
                            {
                                // Stream was not readable means a child agent
                                // was closed due to logout, leaving the
                                // Event Queue request orphaned.
                                continue;
                            }

                            Hashtable responsedata = req.PollServiceArgs.GetEvents(req.RequestID, req.PollServiceArgs.Id,
                                                                                   str.ReadToEnd());
                            var request = new OSHttpRequest(req.HttpContext, req.Request);
                            m_server.MessageHandler.SendGenericHTTPResponse(
                                responsedata,
                                request.MakeResponse(System.Net.HttpStatusCode.OK, "OK"),
                                request
                            );
                        }
                        else
                        {
                            if ((Environment.TickCount - req.RequestTime) > m_timeout)
                            {
                                var request = new OSHttpRequest(req.HttpContext, req.Request);
                                m_server.MessageHandler.SendGenericHTTPResponse(
                                    req.PollServiceArgs.NoEvents(req.RequestID, req.PollServiceArgs.Id),
                                    request.MakeResponse(System.Net.HttpStatusCode.OK, "OK"),
                                    request);
                            }
                            else
                            {
                                ReQueuePollServiceItem reQueueItem = ReQueue;
                                if (reQueueItem != null)
                                    reQueueItem(req);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("Exception in poll service thread: " + e);
                }
            }
        }

        internal void Enqueue(PollServiceHttpRequest pPollServiceHttpRequest)
        {
            m_request.Enqueue(pPollServiceHttpRequest);
        }
    }
}