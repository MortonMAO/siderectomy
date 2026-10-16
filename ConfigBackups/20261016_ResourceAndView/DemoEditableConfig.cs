using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Siderectomy {
 [Serializable] public sealed class DemoConfiguredBody {
  public string id,name,kind,parent,color="#6faddc",species="carbon";
  public double population,au,diameter,regenerationRate,scanExposureMultiplier=1;
  public int index;public DemoNode[] nodes;public DemoInformationLink[] links;
 }
 [Serializable] public sealed class DemoConfiguredLevel {public DemoLevel settings;public DemoConfiguredBody[] bodies;}
 [Serializable] public sealed class DemoEditableConfig {
  public int version=1;
  public DemoCost initialResources=new DemoCost{energy=150,matter=150,info=40,a1=28};
  public double energyRate=1.2,matterRate=.8;
  public DemoConfiguredLevel[] levels;
  public static DemoEditableConfig Load(){var f=Resources.Load<TextAsset>("DemoEditableConfig");if(f==null)return null;var c=JsonUtility.FromJson<DemoEditableConfig>(f.text);c.Validate(DemoRuleBook.Load());return c;}
  public DemoLevel Resolve(int number,DemoRuleBook rules){
   var row=levels.First(x=>x.settings.number==number);var l=row.settings.Copy();l.bodies=JsonUtility.FromJson<DemoConfiguredLevel>(JsonUtility.ToJson(row)).bodies;
   l.energy=l.energy<0?initialResources.energy:l.energy;l.matter=l.matter<0?initialResources.matter:l.matter;l.info=l.info<0?initialResources.info:l.info;l.a1=l.a1<0?initialResources.a1:l.a1;
   l.a2=l.a2<0?initialResources.a2:l.a2;l.a3=l.a3<0?initialResources.a3:l.a3;
   l.energyRate=l.energyRate<0?energyRate:l.energyRate;l.matterRate=l.matterRate<0?matterRate:l.matterRate;
   l.energyBonus=l.energyBonus<0?rules.Find("facility","energy").productionRate:l.energyBonus;l.matterBonus=l.matterBonus<0?rules.Find("facility","matter").productionRate:l.matterBonus;
   l.planets=row.bodies.Count(x=>x.kind=="planet");l.moons=row.bodies.Count(x=>x.kind=="moon");l.stations=row.bodies.Count(x=>x.kind=="station");l.population=row.bodies.Sum(x=>x.population);
   return l;
  }
  public void Validate(DemoRuleBook rules){
   if(version!=1||levels==null||levels.Length!=6||levels.Select(x=>x.settings.number).OrderBy(x=>x).Where((n,i)=>n!=i+1).Any())throw new ArgumentException("配置必须包含1—6关且编号不重复");
   for(int i=0;i<6;i++)if(initialResources[i]<0||double.IsNaN(initialResources[i])||double.IsInfinity(initialResources[i]))throw new ArgumentException("全局初始资源无效");
   foreach(var row in levels){var l=Resolve(row.settings.number,rules);l.Validate();ValidateBodies(l.bodies);
    if(l.number==4&&rules.solarStory.enabled&&!l.bodies.Any(b=>b.id==l.solarOriginId))throw new ArgumentException("太阳系外逃出发天体不存在："+l.solarOriginId);
   }
  }
  public static void ValidateBodies(DemoConfiguredBody[] bodies){
   if(bodies==null||bodies.Length==0||bodies.Length>256||bodies.Select(b=>b.id).Distinct().Count()!=bodies.Length)throw new ArgumentException("每关须有1—256个唯一编号天体");
   var ids=new HashSet<string>(bodies.Select(b=>b.id));
   foreach(var b in bodies){
    if(string.IsNullOrWhiteSpace(b.id)||string.IsNullOrWhiteSpace(b.name)||!new[]{"star","planet","moon","station"}.Contains(b.kind)||!new[]{"carbon","silicon","none"}.Contains(b.species))throw new ArgumentException("天体编号/名称/种类/生物类型无效："+b.id);
    if(b.kind!="star"&&string.IsNullOrEmpty(b.parent))throw new ArgumentException("非恒星天体必须指定上级："+b.id);
    if(!string.IsNullOrEmpty(b.parent)&&(!ids.Contains(b.parent)||b.parent==b.id))throw new ArgumentException("上级天体不存在："+b.id);
    var path=new HashSet<string>();var current=b;while(current!=null){if(!path.Add(current.id))throw new ArgumentException("天体父级存在循环");current=string.IsNullOrEmpty(current.parent)?null:bodies.First(x=>x.id==current.parent);}
    foreach(double v in new[]{b.population,b.au,b.diameter,b.regenerationRate,b.scanExposureMultiplier})if(v<0||double.IsNaN(v)||double.IsInfinity(v))throw new ArgumentException("天体数值无效："+b.id);
    if(b.species=="none"&&b.population>0)throw new ArgumentException("无生物天体不能有人口："+b.id);
    var nodes=b.nodes??new DemoNode[0];if(nodes.Select(n=>n.id).Distinct().Count()!=nodes.Length)throw new ArgumentException("节点编号重复："+b.id);
    foreach(var n in nodes)if(string.IsNullOrEmpty(n.id)||!new[]{"政治","军事","能源","信息"}.Contains(n.type)||n.max<=0||double.IsNaN(n.max)||double.IsInfinity(n.max))throw new ArgumentException("节点属性无效："+b.id);
    foreach(var link in b.links??new DemoInformationLink[0])if(link.from==link.to||!nodes.Any(n=>n.id==link.from)||!nodes.Any(n=>n.id==link.to)||link.baseFlow<0||double.IsNaN(link.baseFlow)||double.IsInfinity(link.baseFlow))throw new ArgumentException("信息链端点或流速无效："+b.id);
   }
   var all=bodies.SelectMany(b=>b.nodes??new DemoNode[0]).ToArray();if(all.Select(n=>n.id).Distinct().Count()!=all.Length)throw new ArgumentException("同关不同天体的节点ID不能重复");
  }
 }
 public sealed partial class DemoSimulation {
  public bool ReportedNoEscape;
  public bool AllowsEscape {get{return Level.allowEscape&&!ReportedNoEscape;}}
  bool CreateConfiguredBodies(){
   if(Level.bodies==null||Level.bodies.Length==0)return false;
   DemoEditableConfig.ValidateBodies(Level.bodies);
   foreach(var c in Level.bodies)Bodies.Add(new DemoBody{id=c.id,name=c.name,kind=c.kind,parent=c.parent,color=c.color,index=c.index,au=c.au,diameter=c.diameter,pop=c.population,initialPop=c.population,species=c.species,regenerationRate=c.regenerationRate,scanExposureMultiplier=c.scanExposureMultiplier,recon=c.kind=="star",cities=(int)Math.Ceiling(c.population/Rules.mechanics.populationPerCity),nodes=(c.nodes??new DemoNode[0]).Select(n=>new DemoNode{id=n.id,type=n.type,max=n.max,hp=n.max}).ToList(),links=(c.links??new DemoInformationLink[0]).Select(l=>new DemoInformationLink{from=l.from,to=l.to,baseFlow=l.baseFlow}).ToList()});
   var info=Bodies.Where(b=>b.nodes.Any(n=>n.type=="信息")).ToArray();foreach(var b in info)b.backup=b.initialBackup=Level.backupStart/info.Length;
   return true;
  }
 }
}
