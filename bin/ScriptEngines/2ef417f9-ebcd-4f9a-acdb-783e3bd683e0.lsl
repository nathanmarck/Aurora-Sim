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
    yield break;
}
public System.Collections.IEnumerator default_event_touch_start(LSL_Types.LSLInteger num)
{
    if (((LSL_Types.LSLInteger)(((ILSL_Api)m_apis["ll"]).llDetectedLinkNumber() == new LSL_Types.LSLInteger(9))))
    {
        ((ILSL_Api)m_apis["ll"]).llOwnerSay(new LSL_Types.LSLString("Implment Off functions here"));
    }
    yield break;
}
public System.Collections.IEnumerator default_event_link_message(LSL_Types.LSLInteger sender_num, LSL_Types.LSLInteger num, LSL_Types.LSLString msg, LSL_Types.LSLString id)
{
    if (((LSL_Types.LSLInteger)( (bool)((((LSL_Types.LSLInteger)(msg == new LSL_Types.LSLString("licenseconfirm")))))) & ((LSL_Types.LSLInteger)((bool)((((LSL_Types.LSLInteger)(licensewait == TRUE))))))))
    {
        string tuslfpmnlsu =  "";
        System.Collections.IEnumerator txkmtjcwhoh = 
        initilize1(
        
        );
        while (true) {
         try {
          if(!txkmtjcwhoh.MoveNext())
           break;
          }
         catch(Exception ex) 
          {
          tuslfpmnlsu = ex.Message;
          }
         if(tuslfpmnlsu != "")
           yield return tuslfpmnlsu;
         else if(txkmtjcwhoh.Current == null || txkmtjcwhoh.Current is DateTime)
           yield return txkmtjcwhoh.Current;
         else break;
         }
        ;
        licensewait = FALSE;
        ((ILSL_Api)m_apis["ll"]).llSetScriptState(new LSL_Types.LSLString("license checker.lsl"), FALSE);
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
return null;
}
}
}
