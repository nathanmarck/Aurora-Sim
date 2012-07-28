using Aurora.BotManager;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using LSL_Types = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types;
using System;
namespace Script
{
[Serializable]
public class ScriptClass : Aurora.ScriptEngine.AuroraDotNetEngine.Runtime.ScriptBaseClass
{

public System.Collections.IEnumerator initilize1()
{
 yield break;
}
public LSL_Types.LSLInteger licensewait = new LSL_Types.LSLInteger(0) ;
public System.Collections.IEnumerator default_event_state_entry()
{
    ((ILSL_Api)m_apis["ll"]).llOwnerSay(new LSL_Types.LSLString("Loading Internal Systems..."));
    licensewait = TRUE;
    ((ILSL_Api)m_apis["ll"]).llSetScriptState(new LSL_Types.LSLString("license checker.lsl"), TRUE);
    ((ILSL_Api)m_apis["ll"]).llResetOtherScript(new LSL_Types.LSLString("license checker.lsl"));
    data = ((ILSL_Api)m_apis["ll"]).llGetNotecardLine(new LSL_Types.LSLString("Settings"), new LSL_Types.LSLInteger(1));
    yield break;
}
public System.Collections.IEnumerator default_event_touch_start(LSL_Types.LSLInteger num)
{
    if (((LSL_Types.LSLInteger)(((ILSL_Api)m_apis["ll"]).llDetectedLinkNumber(new LSL_Types.LSLInteger(0)) == new LSL_Types.LSLInteger(5))))
    {
        ((ILSL_Api)m_apis["ll"]).llOwnerSay(new LSL_Types.LSLString("Implment Off functions here"));
    }
    yield break;
}
public System.Collections.IEnumerator default_event_link_message(LSL_Types.LSLInteger sender_num, LSL_Types.LSLInteger num, LSL_Types.LSLString msg, LSL_Types.LSLString id)
{
    if (((LSL_Types.LSLInteger)( (bool)((((LSL_Types.LSLInteger)(msg == new LSL_Types.LSLString("licenseconfirm")))))) & ((LSL_Types.LSLInteger)((bool)((((LSL_Types.LSLInteger)(licensewait == TRUE))))))))
    {
        string tysxvmyqngf =  "";
        System.Collections.IEnumerator tjdbwmldknn = 
        initilize1(
        
        );
        while (true) {
         try {
          if(!tjdbwmldknn.MoveNext())
           break;
          }
         catch(Exception ex) 
          {
          tysxvmyqngf = ex.Message;
          }
         if(tysxvmyqngf != "")
           yield return tysxvmyqngf;
         else if(tjdbwmldknn.Current == null || tjdbwmldknn.Current is DateTime)
           yield return tjdbwmldknn.Current;
         else break;
         }
        ;
        licensewait = FALSE;
        ((ILSL_Api)m_apis["ll"]).llSetScriptState(new LSL_Types.LSLString("license checker.lsl"), FALSE);
    }
    yield break;
}
public System.Collections.IEnumerator default_event_dataserver(LSL_Types.LSLString queryid, LSL_Types.LSLString setting)
{
    if (((LSL_Types.LSLInteger)( (bool)((((LSL_Types.LSLInteger)(queryid == data))))) & ((LSL_Types.LSLInteger)((bool)((setting = new LSL_Types.LSLString("on")))))))
    {
        ((ILSL_Api)m_apis["ll"]).llSetScriptState(new LSL_Types.LSLString("error handler.lsl"), TRUE);
    }
    else
{
    if (((LSL_Types.LSLInteger)( (bool)((((LSL_Types.LSLInteger)(queryid == data))))) & ((LSL_Types.LSLInteger)((bool)((setting = new LSL_Types.LSLString("off")))))))
    {
        ((ILSL_Api)m_apis["ll"]).llSetScriptState(new LSL_Types.LSLString("error handler.lsl"), FALSE);
    }
}
    yield break;
}
public override System.Collections.IEnumerator FireEvent (string evName, object[] parameters)
{
if(evName == "initilize1")
return initilize1 ();
if(evName == "default_event_state_entry")
return default_event_state_entry ();
if(evName == "default_event_touch_start")
return default_event_touch_start ((LSL_Types.LSLInteger)parameters[0]);
if(evName == "default_event_link_message")
return default_event_link_message ((LSL_Types.LSLInteger)parameters[0], (LSL_Types.LSLInteger)parameters[1], (LSL_Types.LSLString)parameters[2], (LSL_Types.LSLString)parameters[3]);
if(evName == "default_event_dataserver")
return default_event_dataserver ((LSL_Types.LSLString)parameters[0], (LSL_Types.LSLString)parameters[1]);
return null;
}
}
}
