using System;
using System.Linq;
using UnityEngine;
namespace Siderectomy {
 public static class DemoNarrativeSession {
  public static bool TestMode {get{return Environment.GetCommandLineArgs().Any(x=>x.StartsWith("--")&&x.EndsWith("-check")&&x!="--narrative-ui-check"&&x!="--profile-ui-check");}}
  public static void Commit(DemoStoryState state,DemoSnapshot checkpoint=null){
   DemoSession.Ensure();var p=JsonUtility.FromJson<DemoProfile>(JsonUtility.ToJson(DemoSession.Profile));p.story=state;
   if(checkpoint!=null){p.checkpoint=checkpoint;p.hasCheckpoint=true;p.currentLevel=checkpoint.level.number;}
   DemoSaveStore.Save(p);DemoSession.Profile=p;
  }
  public static DemoStoryState Clone(){DemoSession.Ensure();return JsonUtility.FromJson<DemoStoryState>(JsonUtility.ToJson(DemoSession.Profile.story));}
  public static bool OfficePump(){
   DemoSession.Ensure();var state=Clone();if(DemoNarrative.Busy(state))return true;
   string id=state.pending.FirstOrDefault(x=>x.StartsWith("office."));
   if(id==null&&!state.completed.Contains("office.intro")&&DemoSession.Profile.completedLevels.Count==0)id="office.intro";
   if(id==null)return false;
   bool started=DemoNarrative.Start(state,DemoStoryBook.Load(),id);if(started)Commit(state);return started;
  }
  public static void Record(DemoSimulation s,DemoSnapshot checkpoint=null){
   DemoSession.Ensure();var p=JsonUtility.FromJson<DemoProfile>(JsonUtility.ToJson(DemoSession.Profile));
   DemoNarrative.Record(p,s);p.applyCount=Math.Max(p.applyCount,s.ApplyCount);p.secondEnding|=s.SecondEnding;
   if(checkpoint!=null){p.checkpoint=checkpoint;p.hasCheckpoint=true;p.currentLevel=s.Level.number;}
   DemoSaveStore.Save(p);DemoSession.Profile=p;
  }
 }
}
