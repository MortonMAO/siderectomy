using System;
using System.Linq;
using UnityEngine;
namespace Siderectomy {
 public sealed partial class DemoMissionView {
  DemoDialogueView narrativeView;string narrativeFingerprint="";public string NarrativeSaveError {get;private set;}
  void BuildNarrative(){
   if(DemoNarrativeSession.TestMode)return;
   narrativeView=gameObject.AddComponent<DemoDialogueView>();
   narrativeView.Initialize(ui,s=>DemoNarrativeSession.Commit(s,CaptureProgress()),()=>false,true);
  }
  bool TickNarrative(){
   if(narrativeView==null)return false;
   try{
    var state=DemoSession.Profile.story;
    // Choice state is durable before the simulation applies it. Reapply idempotently after a reload.
    if(Simulation.Level.number==4&&Simulation.SolarStoryPhase==1&&state.completed.Contains("mission.solar")){
     Simulation.NextSolarDialogue();Simulation.ChooseSolarEscape(DemoNarrative.Has(state,"solar_allow"));
    }
    if(Simulation.Level.number==4&&Simulation.SolarStoryPhase==3&&state.completed.Contains("mission.resources"))Simulation.NextSolarDialogue();
    if(DemoNarrative.Busy(state)){narrativeView.Refresh();return true;}
    string trigger=null;
    if(Simulation.Level.number==4&&Simulation.SolarStoryPhase==1)trigger="mission.solar";
    else if(Simulation.Level.number==4&&Simulation.SolarStoryPhase==3)trigger="mission.resources";
    else if(Simulation.Level.number==6&&!Simulation.Over)trigger="mission.six.safety";
    if(trigger!=null&&state.completed.Contains(trigger))trigger=null;
    if(trigger==null&&Simulation.Level.number<=2&&!Simulation.Over)trigger="tutorial."+Simulation.Level.number+"."+Math.Min(4,Simulation.TutorialStep);
    if(trigger!=null&&!state.completed.Contains(trigger)){
     var copy=DemoNarrativeSession.Clone();if(DemoNarrative.Start(copy,DemoStoryBook.Load(),trigger)){DemoNarrativeSession.Commit(copy,CaptureProgress());narrativeView.Refresh();return true;}
    }
    return false;
   }catch(Exception ex){NarrativeSaveError=ex.Message;Simulation.Paused=true;conversationText.text="剧情保存未完成："+ex.Message;return true;}
  }
  void RecordNarrativeFacts(){
   if(narrativeView==null)return;
   string key=Simulation.RunId+":"+Simulation.Ships.Count+":"+Simulation.Escaped+":"+Simulation.SolarChoice+":"+Simulation.ApplyCount+":"+Simulation.Over;
   if(key==narrativeFingerprint)return;
   try{DemoNarrativeSession.Record(Simulation,Simulation.Over&&Simulation.Won?null:CaptureProgress());narrativeFingerprint=key;NarrativeSaveError=null;}
   catch(Exception ex){NarrativeSaveError=ex.Message;Simulation.Paused=true;conversationText.text="剧情记录保存失败："+ex.Message;}
  }
 }
}
