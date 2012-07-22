using Aurora.BotManager;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using LSL_Types = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types;
using System;
namespace Script
{
[Serializable]
public class ScriptClass : Aurora.ScriptEngine.AuroraDotNetEngine.Runtime.ScriptBaseClass
{

public LSL_Types.LSLFloat t = new LSL_Types.LSLFloat(0.0) ;
public System.Collections.IEnumerator default_event_state_entry()
{
    ((ILSL_Api)m_apis["ll"]).llVolumeDetect(TRUE);
    yield break;
}
public System.Collections.IEnumerator default_event_touch_start(LSL_Types.LSLInteger number)
{
    t = ((ILSL_Api)m_apis["ll"]).llGetTime();
    yield break;
}
public System.Collections.IEnumerator default_event_touch_end(LSL_Types.LSLInteger number)
{
    endtime = ((ILSL_Api)m_apis["ll"]).llGetTime() - t;
    yield break;
}
public override System.Collections.IEnumerator FireEvent (string evName, object[] parameters)
{
if(evName == "default_event_state_entry")
return default_event_state_entry ();
if(evName == "default_event_touch_start")
return default_event_touch_start ((LSL_Types.LSLInteger)parameters[0]);
if(evName == "default_event_touch_end")
return default_event_touch_end ((LSL_Types.LSLInteger)parameters[0]);
return null;
}
}
}
