using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Siderectomy;
using UnityEditor;
using UnityEngine;
public static class DemoEditableConfigChecks {
 static void Check(bool value,string message){if(!value)throw new Exception("EDITABLE_CONFIG: "+message);}
 [MenuItem("裁星计划/导出当前关卡为显式配置")]
 public static void Export(){
  string file="Assets/Siderectomy/Resources/DemoEditableConfig.json";
  if(File.Exists(file)){Debug.Log("EDITABLE_CONFIG_EXISTS 原配置保留，不覆盖");return;}
  var campaign=Resources.Load<DemoCampaign>("DemoCampaign");var rules=DemoRuleBook.Load();var levels=campaign.levels;
  rules.Find("facility","energy").productionRate=levels[0].energyBonus;rules.Find("facility","matter").productionRate=levels[0].matterBonus;
  File.WriteAllText("Assets/Siderectomy/Resources/DemoRules.json",JsonUtility.ToJson(rules,true));
  var c=new DemoEditableConfig();var rows=new List<DemoConfiguredLevel>();
  foreach(var original in levels){
   var s=new DemoSimulation(original,rules);var l=original.Copy();l.bodies=null;
   if(l.energy==c.initialResources.energy)l.energy=-1;if(l.matter==c.initialResources.matter)l.matter=-1;if(l.info==c.initialResources.info)l.info=-1;if(l.a1==c.initialResources.a1)l.a1=-1;
   l.a2=l.a3=-1;if(l.energyRate==c.energyRate)l.energyRate=-1;if(l.matterRate==c.matterRate)l.matterRate=-1;
   if(l.energyBonus==rules.Find("facility","energy").productionRate)l.energyBonus=-1;if(l.matterBonus==rules.Find("facility","matter").productionRate)l.matterBonus=-1;
   rows.Add(new DemoConfiguredLevel{settings=l,bodies=s.Bodies.Select(b=>new DemoConfiguredBody{id=b.id,name=b.name,kind=b.kind,parent=b.parent,color=b.color,index=b.index,au=b.au,diameter=b.diameter,population=b.initialPop,species=b.initialPop>0?b.species:"none",regenerationRate=b.regenerationRate,scanExposureMultiplier=b.scanExposureMultiplier,nodes=b.nodes.Select(n=>new DemoNode{id=n.id,type=n.type,max=n.max,hp=n.max}).ToArray(),links=b.links.ToArray()}).ToArray()});
  }
  c.levels=rows.ToArray();c.Validate(rules);File.WriteAllText(file,JsonUtility.ToJson(c,true));AssetDatabase.Refresh();Debug.Log("EDITABLE_CONFIG_EXPORTED");
 }
 [MenuItem("裁星计划/验证配置导入与上报硬条件")]
 public static void Run(){
  var rules=DemoRuleBook.Load();var c=DemoEditableConfig.Load();Check(c!=null,"配置未导出");
  foreach(var row in c.levels){var s=new DemoSimulation(c.Resolve(row.settings.number,rules),rules);Check(s.Bodies.Count==row.bodies.Length,"天体数量错误");Check(s.InitialNodes==row.bodies.Sum(b=>b.nodes.Length),"节点数量错误");Check(Math.Abs(s.InitialPop-row.bodies.Sum(b=>b.population))<.000001,"人口错误");}
  var four=new DemoSimulation(c.Resolve(4,rules),rules);Check(four.Find("p2").nodes.Count==25&&four.Find("m0").initialPop==114,"太阳系基线漂移");
  var five=new DemoSimulation(c.Resolve(5,rules),rules);five.ReportedNoEscape=true;five.Paused=false;five.Ships.Add(new DemoShip{id=900,body="p2",pop=8,left=.01,total=1});five.Advance(.05);
  Check(five.Over&&!five.Won,"上报后外逃没有失败");
  var restored=DemoSimulation.Restore(five.Capture(),rules);Check(restored.ReportedNoEscape&&!restored.AllowsEscape,"读档丢失硬条件");
  var soft=new DemoSimulation(c.Resolve(5,rules),rules);soft.Paused=false;soft.Ships.Add(new DemoShip{id=901,body="p2",pop=8,left=.01,total=1});soft.Advance(.05);Check(!soft.Over&&soft.Escaped==1,"未上报时软条件被改变");
  var zero=new DemoSimulation(c.Resolve(6,rules),rules);zero.ReportedNoEscape=true;zero.Escaped=1;foreach(var b in zero.Bodies)b.pop=0;zero.CheckEnd();Check(zero.Over&&!zero.Won,"人口清零绕过外逃硬标准");
  four.ReportedNoEscape=true;four.SolarChoice="A";Check(four.CanIntercept,"上报后仍因历史A关闭拦截");
  // Disposable config: extra star, arbitrary IDs, 11+ planets, station population, individual HP, global resources.
  var copy=JsonUtility.FromJson<DemoEditableConfig>(JsonUtility.ToJson(c));var level=copy.levels[0];level.settings.solarOriginId="sun";
  var bodies=level.bodies.ToList();bodies.Add(new DemoConfiguredBody{id="other_star",name="测试第二恒星",kind="star",species="none",nodes=new DemoNode[0],links=new DemoInformationLink[0]});
  for(int i=0;i<12;i++)bodies.Add(new DemoConfiguredBody{id="custom_p"+i,name="自定义行星"+i,kind="planet",parent="other_star",species="carbon",population=1,index=i,nodes=new DemoNode[0],links=new DemoInformationLink[0]});
  bodies.Add(new DemoConfiguredBody{id="custom_station",name="有人口人工天体",kind="station",parent="custom_p0",species="silicon",population=37,nodes=new[]{new DemoNode{id="custom_hp",type="信息",max=321,hp=321}},links=new DemoInformationLink[0]});
  level.bodies=bodies.ToArray();copy.initialResources.energy=12345;copy.Validate(rules);var custom=new DemoSimulation(copy.Resolve(1,rules),rules);
  Check(custom.Bodies.Count(b=>b.kind=="star")==2&&custom.Level.planets==15&&custom.Find("custom_station").pop==37&&custom.Find("custom_station").nodes[0].hp==321&&custom.Res[0]==12345,"自定义配置未实际生效");
  custom.Facilities["energy"]=1;Check(Math.Abs(custom.Production("energy")-custom.Level.energyRate-custom.Level.energyBonus)<.000001,"采集点产出变化");
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/EditableConfig.txt","PASS 六关基线、全局资源、逐天体绝对人口、节点HP、多恒星/15行星/人工天体人口、自定义编号、上报硬条件、未上报软条件、零人口不得绕过、存档与拦截权限。");Debug.Log("EDITABLE_CONFIG_CHECK_PASS");
 }
 public static void Build(){Export();Run();DemoNarrativeChecks.Run();DemoOrbitChecks.Run();DemoBuilder.ValidateAndBuildWindows();}
}
