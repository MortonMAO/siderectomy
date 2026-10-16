using System;
using System.IO;
using System.Linq;
using Siderectomy;
using UnityEngine;
using UnityEditor;
public static class DemoModuleChecks {
 static void Check(bool value,string message){if(!value)throw new Exception("MODULE_CHECK: "+message);}
 static DemoSimulation Fixture(int level,DemoRuleBook rules){
  var campaign=Resources.Load<DemoCampaign>("DemoCampaign");var s=new DemoSimulation(campaign.levels.First(l=>l.number==level),rules);
  for(int i=0;i<6;i++)s.Res[i]=10000;s.GlobalProbe=true;foreach(var b in s.Bodies)b.recon=true;s.Research["stealth"]=1;s.Paused=false;
  // Isolate delivery/slot boundaries from autonomous escape while leaving combat counter active.
  s.Level.techRate=0;s.Level.techStart=0;s.Tech=0;s.Level.escapeSeconds=10000;return s;
 }
 static void Make(DemoSimulation s,string kind,string key){
  Check(s.Submit(kind,key),"制造提交失败 "+key);double seconds=s.Rules.Find(kind,key).manufactureSeconds;s.Advance(seconds+.1);
 }
 [MenuItem("裁星计划/运行模块投送验收")]
 public static void Run(){
  var r=DemoRuleBook.Load();
  Check(r.Table("attackModule").Length==5&&r.Table("technologyModule").Length==3,"模块目录数量错误");
  Check(!r.operations.Any(o=>new[]{"galaxy","disrupt","steal","infoVirus","dissolve"}.Contains(o.key)),"旧模块未移除");
  int[] slots={1,2,1,3};for(int i=0;i<4;i++)Check(r.carriers[i].attackSlots==1&&r.carriers[i].technologySlots==slots[i],"载体两类插槽不符");
  var s=Fixture(2,r);s.Research.Clear();
  Check(!s.Submit("technologyModule","stealth")&&s.Jobs.Count==0,"隐形研发前置失效");
  Check(s.Submit("research","stealth"),"研发提交失败");s.Advance(35.1);
  double e=s.Res[0],matter=s.Res[1],info=s.Res[2],arms=s.Res[3];
  Check(s.Submit("technologyModule","stealth"),"科技模块无法制造");
  Check(s.TechnologyModules["stealth"]==0&&s.Jobs.Any(j=>j.kind=="technologyModule"),"制造没有经过队列");
  Check(Math.Abs(e-s.Res[0]-10)<.001&&s.Res[1]==matter&&Math.Abs(info-s.Res[2]-16)<.001&&s.Res[3]==arms,"科技模块费用错误");
  var restored=DemoSimulation.Restore(s.Capture(),r);restored.Advance(12.1);Check(restored.TechnologyModules["stealth"]==1,"科技制造队列存档恢复失败");
  s.Advance(12.1);Make(s,"attackModule","surface");Make(s,"technologyModule","virus");
  Check(s.Res[3]==arms,"打击模块不应消耗武装资源");
  s.SelectBody("p0");s.Carriers["dust"]=1;s.Carriers["asteroid"]=1;s.Carriers["satellite"]=1;
  var before=s.Capture();
  Check(!s.LaunchLoadout("surface","dust","stealth","virus"),"星尘科技槽未限制");
  Check(!s.LaunchLoadout("surface","asteroid","stealth","virus"),"小行星科技槽未独立限制");
  Check(!s.LaunchLoadout("stellar","asteroid"),"搭载能力越级");
  Check(s.Carriers["dust"]==1&&s.AttackModules["surface"]==1&&s.TechnologyModules["stealth"]==1,"失败投送扣了库存");
  e=s.Res[0];matter=s.Res[1];info=s.Res[2];
  Check(s.LaunchLoadout("surface","satellite","stealth","virus"),"卫星组合投送失败 "+s.Notice);
  Check(s.Missions.Count==1&&s.Carriers["satellite"]==0&&s.AttackModules["surface"]==0&&s.TechnologyModules["stealth"]==0&&s.TechnologyModules["virus"]==0,"组合投送库存或载体重复扣除");
  Check(s.Res[0]==e&&s.Res[1]==matter&&s.Res[2]==info,"发射重复收制造费");
  Check(s.Missions[0].concealed&&s.Missions[0].intercept==0&&s.Alert==0,"隐形降低察觉或免反制失败");
  double pop=s.Body.pop;s.Advance(38.1);Check(Math.Abs(pop-s.Body.pop-45)<.001&&s.Viruses.Count==1,"组合打击和病毒未同时生效");
  s.Advance(60.1);Check(Math.Abs(pop-s.Body.pop-117)<.001,"病毒周期伤害不符");
  var capacity=Fixture(3,r);capacity.Carriers["asteroid"]=1;capacity.AttackModules["stellar"]=1;Check(capacity.LoadoutError("stellar","asteroid",new string[0]).Contains("能力"),"搭载能力应独立于插槽");
  var t=Fixture(3,r);t.SelectBody("p2");t.Body.pop-=100;Make(t,"technologyModule","stealth");Make(t,"technologyModule","infoInvasion");
  t.Carriers["satellite"]=1;t.TargetNode=t.Body.nodes[1].id;double original=t.Body.pop;
  Check(t.LaunchLoadout("","satellite","stealth","infoInvasion"),"科技独立投送失败");
  var checkpoint=DemoSimulation.Restore(t.Capture(),r);checkpoint.Advance(36.1);Check(checkpoint.Missions.Count==0&&Math.Abs(checkpoint.InformationFlow(checkpoint.Body)-1.0/3)<.001,"入侵切链或在途存档失效");
  Check(checkpoint.Body.pop>=original,"信息入侵不应直接杀人口");
  checkpoint.Advance(45.1);Check(Math.Abs(checkpoint.InformationFlow(checkpoint.Body)-1)<.001,"信息入侵持续时间失效");
  Check(t.LoadoutError("","satellite",new[]{"virus"}).Contains("碳基"),"硅基病毒免疫校验失效");
  var u=Fixture(6,r);u.SelectBody("p2");u.Carriers["satellite"]=1;Check(u.LoadoutError("","satellite",new[]{"infoInvasion"}).Contains("硅基"),"碳基信息入侵校验失效");
  u.SetDebugFeatures(true);Check(!u.Submit("attackModule","galaxy")&&!u.Submit("technologyModule","infoVirus"),"调试全开恢复了已删模块");
  // Fourth-level first ship still exists and B permits intercept through the manufacturing path.
  var f=Fixture(4,r);f.Paused=false;f.Advance(181);Check(f.Ships.Count==1&&f.StoryPending,"强制首舰剧情失效");
  f.NextSolarDialogue();f.ChooseSolarEscape(false);f.Advance(15.1);Check(f.SolarStoryPhase==3,"15秒第二轮剧情失效");f.NextSolarDialogue();
  Check(f.Submit("apply","apply"),"剧情结束后申请未开放");f.Advance(25.1);
  Check(f.Submit("carrier","satellite"),"载体制造失败");Check(f.Submit("attackModule","blockade"),"拦截模块制造失败");f.Advance(15.1);f.SelectShip(f.Ships[0].id);
  Check(f.LaunchLoadout("blockade","satellite"),"首艘不可拦截 "+f.Notice);f.Advance(23.1);Check(f.Ships[0].done&&f.Escaped==0,"首艘拦截窗口不足");
  var mod=JsonUtility.FromJson<DemoRuleBook>(JsonUtility.ToJson(r));var invasion=mod.Find("technologyModule","infoInvasion");invasion.flowMultiplier=.2;invasion.duration=7;invasion.manufactureSeconds=3;invasion.cost.energy=2;
  mod.Validate();var v=Fixture(3,mod);v.SelectBody("p2");Make(v,"technologyModule","infoInvasion");v.Carriers["satellite"]=1;v.TargetNode=v.Body.nodes[1].id;
  Check(v.LaunchLoadout("","satellite","infoInvasion"),"可配置入侵提交失败");v.Advance(36.1);Check(Math.Abs(v.InformationFlow(v.Body)-1.4/3)<.001,"流速配置未生效");
  v.Advance(7.1);Check(Math.Abs(v.InformationFlow(v.Body)-1)<.001,"持续时间配置未生效");
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/ModuleLoadout.txt","PASS 模块分类、制造费用与队列、独立插槽和搭载能力、失败不扣库存、组合投送、隐形察觉与免反制、病毒周期、硅基限流及到期、配置变更、制造和在途存档、第四关B申请后首舰拦截。机制测试含资源与侦察夹具。");
  Debug.Log("MODULE_LOADOUT_PASS");
 }
 public static void Build(){
  Run();DemoBuilder.Prepare();
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName="Builds/WindowsDemo/裁星计划Demo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("构建失败");
  Debug.Log("MODULE_BUILD_PASS");
 }
}

