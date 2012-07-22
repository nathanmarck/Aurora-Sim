using Aurora.BotManager;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using LSL_Types = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types;
using System;
namespace Script
{
[Serializable]
public class ScriptClass : Aurora.ScriptEngine.AuroraDotNetEngine.Runtime.ScriptBaseClass
{

public System.Collections.IEnumerator default_event_state_entry()
{
    yield return ((ILSL_Api)m_apis["ll"]).llSetTexture(new LSL_Types.LSLString("screen"));
    yield break;
}
public override System.Collections.IEnumerator FireEvent (string evName, object[] parameters)
{
if(evName == "default_event_state_entry")
return default_event_state_entry ();
return null;
}
}
}
