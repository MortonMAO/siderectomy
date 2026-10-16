using System;
using System.IO;
using System.Linq;
using Siderectomy;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class DemoImportedConfigChecks {
 static void Check(bool v,string text){if(!v)throw new Exception("IMPORTED_CONFIG: "+text);}
 static DemoSimulation Fixture(DemoEditableConfig config,DemoRuleBook rules){
  var s=new DemoSimulation(config.Resolve(6,rules),rules);for(int i=0;i<6;i++)s.Res[i]=100000;s.Research["stealth"]=1;s.Paused=false;s.Level.awareness=false;s.GlobalProbe=true;foreach(var b in s.Bodies)b.recon=true;return s;
 }
 [MenuItem("裁星计划/验证当前导入数值")]
 public static void Run(){
  var r=DemoRuleBook.Load();var config=DemoEditableConfig.Load();Check(config!=null,"未读取显式关卡配置");
  foreach(var row in config.levels){
   var s=new DemoSimulation(config.Resolve(row.settings.number,r),r);
   Check(s.Bodies.Count==row.bodies.Length&&s.InitialNodes==row.bodies.Sum(b=>b.nodes.Length),"天体或节点数量漂移");
   Check(Math.Abs(s.InitialPop-row.bodies.Sum(b=>b.population))<.000001,"人口漂移");
   Check(Math.Abs(s.Production("energy")-s.Level.energyRate)<.000001,"基础产速错误");
   Check(Math.Abs(s.Production("info")-config.infoRate)<.000001,"基础信息产速未继承全局配置");s.Facilities["signal"]=2;Check(Math.Abs(s.Production("info")-config.infoRate-2*r.Find("facility","signal").productionRate)<.000001,"信息收集点未叠加基础产速");
   s.Facilities["energy"]=3;
   Check(Math.Abs(s.Production("energy")-s.Level.energyRate-s.Level.energyBonus*(1+r.mechanics.secondProducerMultiplier+r.mechanics.laterProducerMultiplier))<.000001,"采集点产出/递减未使用新配置");
  }
  foreach(string kind in new[]{"attackModule","technologyModule"})foreach(var rule in r.Table(kind)){
   var s=Fixture(config,r);var before=(double[])s.Res.Clone();
   if(kind=="attackModule"){int arm=Enumerable.Range(3,3).FirstOrDefault(i=>rule.cost[i]>0);if(arm>=3){var poor=Fixture(config,r);poor.Res[arm]=rule.cost[arm]-.5;var stock=(double[])poor.Res.Clone();Check(!poor.Submit(kind,rule.key)&&poor.Res.SequenceEqual(stock)&&poor.Jobs.Count==0,"武装不足时未原子拒绝制造："+rule.key);}}
   Check(s.Submit(kind,rule.key),"模块制造失败："+rule.key);
   for(int i=0;i<6;i++)Check(Math.Abs(before[i]-s.Res[i]-rule.cost[i])<.000001,"扣费与配置不符："+rule.key);
   s.Advance(rule.manufactureSeconds+.1);Check((kind=="attackModule"?s.AttackModules[rule.key]:s.TechnologyModules[rule.key])==1,"制造时长/入库与配置不符："+rule.key);
  }
  foreach(var carrier in r.carriers){
   var s=Fixture(config,r);var before=(double[])s.Res.Clone();Check(s.Submit("carrier",carrier.key),"载体制造失败");
   for(int i=0;i<6;i++)Check(Math.Abs(before[i]-s.Res[i]-carrier.cost[i])<.000001,"载体扣费不符");
   s.Advance(carrier.seconds+.1);Check(s.Carriers[carrier.key]==1,"载体制造时间不符");
  }
  var first=new DemoSimulation(config.Resolve(1,r),r);Check(first.ModuleAvailable(r.Find("attackModule","surface")),"第一关打击制造被解锁阻断");first.Paused=false;if(first.Res[3]<r.Find("attackModule","surface").cost.a1){Check(first.Submit("facility","military"),"第一关军工点无法建造");first.Advance(r.Find("facility","military").seconds+.1);Check(first.Submit("batch","a1"),"第一关基础武装无法补产");first.Advance(r.Find("batch","a1").seconds+.1);}Check(first.CanPay(r.Find("attackModule","surface").cost),"第一关补产武装后仍不能制造");
  var idle=new DemoSimulation(config.Resolve(1,r),r);double initialInfo=idle.Res[2];idle.Advance(10);Check(idle.Res[2]==initialInfo,"暂停仍生产信息");idle.Paused=false;idle.Advance(10);Check(Math.Abs(idle.Res[2]-initialInfo-10*config.infoRate)<.000001,"无设施基础信息未实际到账");
  first.Paused=false;Check(first.Submit("attackModule","surface"),"第一关地表打击提交失败");first.Advance(r.Find("attackModule","surface").manufactureSeconds+.1);Check(first.AttackModules["surface"]==1,"第一关打击未制造完成");
  var shot=Fixture(config,r);shot.SelectBody(shot.Bodies.First(b=>b.nodes.Count>0&&b.pop>0).id);var target=shot.Body.nodes[0];double hp=target.hp;
  shot.TargetNode=target.id;shot.AttackModules["surface"]=1;shot.Carriers["dust"]=1;var launchResources=(double[])shot.Res.Clone();Check(shot.LaunchLoadout("surface","dust"),"新打击配置发射失败");Check(shot.Res.SequenceEqual(launchResources),"发射重复扣除模块制造费用");
  var mission=shot.Missions.Last();mission.concealed=true;var damage=r.Find("operation","surface");double expected=Math.Max(0,hp-(target.max*damage.hp+damage.nodeDamage)*shot.Level.damageScale);
  shot.Advance(mission.total+.1);Check(Math.Abs(target.hp-expected)<.000001,"节点打击伤害不符");
  var slots=Fixture(config,r);slots.SelectBody(slots.Bodies.First(b=>b.species=="carbon"&&b.pop>0).id);
  slots.Carriers["asteroid"]=1;slots.AttackModules["surface"]=1;slots.TechnologyModules["stealth"]=1;slots.TechnologyModules["virus"]=1;
  if(r.Find("carrier","asteroid").technologySlots>=2)Check(slots.LaunchLoadout("surface","asteroid","stealth","virus"),"配置允许的两科技搭载被拒绝");
  var hard=Fixture(config,r);hard.Level.awareness=true;hard.ReportedNoEscape=true;hard.Ships.Add(new DemoShip{id=900,body=hard.Body.id,pop=1,left=.01,total=1});hard.Advance(.05);Check(hard.Over&&!hard.Won,"上报后的禁止外逃硬标准失效");
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/ImportedConfig.txt","PASS 六关显式配置、人口/节点、采集与递减、8种模块实际扣费/制造入库、4载体扣费/时间、首关制造可用、新节点伤害、配置化双科技槽、上报后禁止外逃。战斗采用隔离夹具，不是完整平衡性通关。");
  Debug.Log("IMPORTED_CONFIG_CHECK_PASS");
 }
 public static void Build(){
  Run();DemoProfileCreationChecks.Run();DemoNarrativeChecks.Run();DemoOrbitChecks.Run();DemoBuilder.Prepare();
  var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName="Builds/WindowsDemo/裁星计划Demo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
  if(result.summary.result!=BuildResult.Succeeded)throw new Exception("构建失败："+result.summary.result);
  EditorSceneManager.OpenScene("Assets/Siderectomy/Scenes/StarSystemTest.unity");Debug.Log("IMPORTED_CONFIG_BUILD_OK");
 }
}
