using Aurora.BotManager;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using LSL_Types = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types;
using System;
namespace Script
{
[Serializable]
public class ScriptClass : Aurora.ScriptEngine.AuroraDotNetEngine.Runtime.ScriptBaseClass
{

public LSL_Types.LSLString origname = new LSL_Types.LSLString("") ;
public System.Collections.IEnumerator default_event_state_entry()
{
    origname = ((ILSL_Api)m_apis["ll"]).llGetObjectName();
    ((ILSL_Api)m_apis["ll"]).llRegionSay(-new LSL_Types.LSLInteger(300), ((ILSL_Api)m_apis["ll"]).llGetObjectDesc());
    ((ILSL_Api)m_apis["ll"]).llSetTimerEvent(new LSL_Types.LSLInteger(30));
    ((ILSL_Api)m_apis["ll"]).llListen(-new LSL_Types.LSLInteger(300), new LSL_Types.LSLString(""), new LSL_Types.LSLString(""), new LSL_Types.LSLString(""));
    yield break;
}
public System.Collections.IEnumerator default_event_listen(LSL_Types.LSLInteger chan, LSL_Types.LSLString name, LSL_Types.LSLString id, LSL_Types.LSLString msg)
{
    if (((LSL_Types.LSLInteger)(msg == new LSL_Types.LSLString("confirm"))))
    {
        ((ILSL_Api)m_apis["ll"]).llSetObjectName(new LSL_Types.LSLString("API Controller"));
        ((ILSL_Api)m_apis["ll"]).llOwnerSay(new LSL_Types.LSLString("API has been activated. Thank you."));
        ((ILSL_Api)m_apis["ll"]).llSetObjectName(origname);
        ((ILSL_Api)m_apis["ll"]).llSetTimerEvent(new LSL_Types.LSLInteger(0));
    }
    yield break;
}
public System.Collections.IEnumerator default_event_link_message(LSL_Types.LSLInteger sender_num, LSL_Types.LSLInteger num, LSL_Types.LSLString apifunction, LSL_Types.LSLString id)
{
    seperated = ((ILSL_Api)m_apis["ll"]).llParseString2List(apifunction, new LSL_Types.list(new LSL_Types.LSLString("|")), new LSL_Types.list());
    if (((LSL_Types.LSLInteger)(apifunction == new LSL_Types.LSLString("lockdown"))))
    {
        ((ILSL_Api)m_apis["ll"]).llRegionSay(-new LSL_Types.LSLInteger(876), new LSL_Types.LSLString("lockdown"));
    }
    else
{
    if (((LSL_Types.LSLInteger)(apifunction == new LSL_Types.LSLString("lockdownoff"))))
    {
        ((ILSL_Api)m_apis["ll"]).llRegionSay(-new LSL_Types.LSLInteger(876), new LSL_Types.LSLString("lockdownoff"));
    }
    else
    {
        ((ILSL_Api)m_apis["ll"]).llOwnerSay(new LSL_Types.LSLString("Incorrect API Function called... Ignoring..."));
    }
}
    yield break;
}
public System.Collections.IEnumerator default_event_timer()
{
    ((ILSL_Api)m_apis["ll"]).llOwnerSay(new LSL_Types.LSLString("FATAL: Cannot connect to server..."));
    ((ILSL_Api)m_apis["ll"]).llSetTimerEvent(new LSL_Types.LSLInteger(0));
    yield break;
}
public override System.Collections.IEnumerator FireEvent (string evName, object[] parameters)
{
if(evName == "default_event_state_entry")
return default_event_state_entry ();
if(evName == "default_event_listen")
return default_event_listen ((LSL_Types.LSLInteger)parameters[0], (LSL_Types.LSLString)parameters[1], (LSL_Types.LSLString)parameters[2], (LSL_Types.LSLString)parameters[3]);
if(evName == "default_event_link_message")
return default_event_link_message ((LSL_Types.LSLInteger)parameters[0], (LSL_Types.LSLInteger)parameters[1], (LSL_Types.LSLString)parameters[2], (LSL_Types.LSLString)parameters[3]);
if(evName == "default_event_timer")
return default_event_timer ();
return null;
}
}
}
