using Aurora.BotManager;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using LSL_Types = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types;
using System;
namespace Script
{
[Serializable]
public class ScriptClass : Aurora.ScriptEngine.AuroraDotNetEngine.Runtime.ScriptBaseClass
{

public System.Collections.IEnumerator initilize()
{
 yield break;
}
public System.Collections.IEnumerator default_event_state_entry()
{
    ((ILSL_Api)m_apis["ll"]).llOwnerSay(new LSL_Types.LSLString("Loading Internal Systems..."));
    licensewait = TRUE;
    yield break;
}
public System.Collections.IEnumerator default_event_link_message(LSL_Types.LSLInteger sender_num, LSL_Types.LSLInteger num, LSL_Types.LSLString msg, LSL_Types.LSLString id)
{
    if (((LSL_Types.LSLInteger)(msg == new LSL_Types.LSLString("licenseconfirm"))))
    {
        string tbeagifhtvt =  "";
        System.Collections.IEnumerator tisiwaggmiy = 
        initilize(
        
        );
        while (true) {
         try {
          if(!tisiwaggmiy.MoveNext())
           break;
          }
         catch(Exception ex) 
          {
          tbeagifhtvt = ex.Message;
          }
         if(tbeagifhtvt != "")
           yield return tbeagifhtvt;
         else if(tisiwaggmiy.Current == null || tisiwaggmiy.Current is DateTime)
           yield return tisiwaggmiy.Current;
         else break;
         }
        ;
    }
    yield break;
}
public override System.Collections.IEnumerator FireEvent (string evName, object[] parameters)
{
if(evName == "initilize")
return initilize ();
if(evName == "default_event_state_entry")
return default_event_state_entry ();
if(evName == "default_event_link_message")
return default_event_link_message ((LSL_Types.LSLInteger)parameters[0], (LSL_Types.LSLInteger)parameters[1], (LSL_Types.LSLString)parameters[2], (LSL_Types.LSLString)parameters[3]);
return null;
}
}
}
