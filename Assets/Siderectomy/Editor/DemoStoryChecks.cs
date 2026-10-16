using System;
using System.IO;
using Siderectomy;
using UnityEngine;
public static class DemoStoryChecks {
 static void Check(bool v,string msg){if(!v)throw new Exception(msg);}
 public static void Run(){
  var rules=DemoRuleBook.Load();rules.solarStory.enabled=true;
  var s=new DemoSimulation(DemoLevel.Defaults()[3],rules);
  Check(!s.CanApply&&!s.CanIntercept,"剧情前权限错误");
  s.BeginSolarStory();s.Paused=false;double time=s.Time;s.Advance(10);Check(s.Time==time,"对话期间时间未冻结");
  s.NextSolarDialogue();s.ChooseSolarEscape(true);Check(!s.CanIntercept&&!s.CanApply,"A分支权限错误");
  s.Advance(rules.solarStory.secondDelay+.1);s.NextSolarDialogue();Check(s.CanApply&&!s.CanIntercept,"第二轮结束权限错误");
  var restored=DemoSimulation.Restore(s.Capture(),rules);Check(restored.SolarChoice=="A"&&restored.SolarStoryPhase==4&&!restored.CanIntercept,"读档丢失剧情选择");
  s.SetDebugFeatures(true);Check(s.CanIntercept&&s.Unlocked(rules.Find("operation","galaxy")),"调试全开失败");
  s.SetDebugFeatures(false);Check(!s.CanIntercept&&!s.Unlocked(rules.Find("operation","galaxy")),"恢复本关权限失败");
  var b=new DemoSimulation(DemoLevel.Defaults()[3],rules);b.BeginSolarStory();b.NextSolarDialogue();b.ChooseSolarEscape(false);
  Check(b.CanIntercept&&!b.CanApply,"B分支权限错误");b.Paused=false;b.Advance(rules.solarStory.secondDelay+.1);b.NextSolarDialogue();Check(b.CanApply,"B第二轮未开放申请");
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/SolarStory.txt","PASS 两轮剧情、选择后15秒、A/B权限、申请门槛、剧情暂停、存档、调试全开与权限恢复。");Debug.Log("SOLAR_STORY_CHECK_PASS");
 }
 public static void Build(){Run();DemoConfigChecks.Build();}
}
