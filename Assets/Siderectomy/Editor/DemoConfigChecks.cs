using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Siderectomy;
public static class DemoConfigChecks {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static DemoSimulation Sim(DemoRuleBook rules){var l=DemoLevel.Defaults()[3];l.awareness=false;var s=new DemoSimulation(l,rules);s.SolarStoryPhase=4;return s;}
 [MenuItem("裁星计划/运行配置与资源申请验收")]
 public static void Run(){
  var rules=DemoRuleBook.Load();var s=Sim(rules);s.Paused=false;
  var before=(double[])s.Res.Clone();Check(s.Submit("apply","apply"),"申请提交失败");
  s.Advance(rules.application.seconds-.1);Check(s.Res[2]==before[2],"未到期不应拨付");
  s.Advance(.2);
  for(int i=2;i<6;i++)Check(Math.Abs(s.Res[i]-before[i]-rules.application.amounts[i])<.001,"固定拨付错误："+i);
  var first=DemoLevel.Defaults()[0];var a=new DemoSimulation(first,rules);a.Paused=false;Check(!a.Submit("apply","apply"),"第四关前申请不应开放");a.SetDebugFeatures(true);a.Submit("apply","apply");a.SetDebugFeatures(false);a.Advance(rules.application.seconds+.1);Check(a.Res[4]==0&&a.Res[5]==0,"申请绕过关卡解锁");
  var changed=JsonUtility.FromJson<DemoRuleBook>(JsonUtility.ToJson(rules));
  changed.application.seconds=2;changed.application.amounts.info=777;
  changed.nodeTemplates.First(n=>n.type=="军事").hp=222;
  changed.Find("facility","signal").productionRate=3;
  changed.Find("operation","surface").seconds=1;changed.Find("operation","surface").pop=9;
  changed.Find("carrier","dust").flight=2;
  changed.Find("operation","disrupt").duration=7;
  changed.Find("operation","virus").interval=2;changed.Find("operation","virus").ticks=2;changed.Find("operation","virus").effect=5;
  changed.Find("research","calibration").effect=1.4;
  changed.Validate();var t=Sim(changed);t.Paused=false;t.Submit("apply","apply");t.Advance(2.1);
  Check(t.Res[2]==t.Level.info+777,"修改申请配置未生效");
  Check(t.Bodies.SelectMany(b=>b.nodes).Where(n=>n.type=="军事").All(n=>n.max==222),"节点HP未读配置");
  Check(t.InitialNodes==25&&t.Bodies.Where(b=>b.nodes.Count>0).All(b=>b.id=="p2"),"节点分布改变");
  t.Facilities["signal"]=2;Check(t.Production("info")==6,"信息产率未读配置");t.Facilities["signal"]=0;
  t.GlobalProbe=true;t.Find("p2").recon=true;t.Carriers["dust"]=1;t.TargetNode="auto";
  var pop=t.Find("p2").pop;Check(t.Launch("surface","dust"),"打击提交失败");
  Check(t.Missions[0].total==3,"执行或航行时间未读配置");t.Advance(3.1);Check(Math.Abs(pop-t.Find("p2").pop-9)<.001,"人口伤害未读配置");
  t.Carriers["satellite"]=1;Check(t.Launch("disrupt","satellite"),"扰乱提交失败");t.Advance(36.1);Check(t.Find("p2").disruptedUntil>t.Time&&t.Find("p2").disruptedUntil<=t.Time+7.01,"扰乱持续时间错误");
  t.Carriers["dust"]=1;pop=t.Find("p2").pop;Check(t.Launch("virus","dust"),"病毒提交失败");t.Advance(35);
  Check(Math.Abs(pop-t.Find("p2").pop-10)<.001,"病毒周期/次数/效果未读配置");
  t.Submit("research","calibration");t.Advance(46);Check(t.Research["calibration"]==1.4,"研发倍率未读配置");
  Directory.CreateDirectory("Docs/Validation");
  File.WriteAllText("Docs/Validation/ConfigRules.txt","PASS 固定申请、等待时间、未解锁武装过滤、配置变更、节点HP与分布、信息产率、打击伤害与执行时间、载体航行、扰乱持续、病毒周期/次数、研发倍率。");
  Debug.Log("CONFIG_RULES_PASS");
 }
 public static void Build(){
  Run();DemoBuilder.Prepare();
  var report=UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions{
   scenes=UnityEditor.EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
   locationPathName="Builds/WindowsDemo/裁星计划Demo.exe",target=UnityEditor.BuildTarget.StandaloneWindows64,options=UnityEditor.BuildOptions.Development});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("构建失败");
  Debug.Log("CONFIG_BUILD_PASS");
 }
}
