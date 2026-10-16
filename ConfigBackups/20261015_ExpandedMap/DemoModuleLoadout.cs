using System;
using System.Linq;
using System.Collections.Generic;
namespace Siderectomy {
 public sealed partial class DemoSimulation {
  public DemoRule TechnologyRule(string key){return Rules.Find("technologyModule",key);}
  public bool ModuleAvailable(DemoRule rule){return Unlocked(rule)&&(rule.key!="stealth"||DebugAllFeatures||Research.ContainsKey("stealth"));}
  public bool ManufactureModule(string kind,string key){
   if(Over)return Reject("任务已结束");
   var rule=Rules.Find(kind,key);
   if(rule==null||!ModuleAvailable(rule))return Reject(key=="stealth"?"先完成隐形技术研发，再制造模块":"本关尚未解锁该模块");
   if(!Pay(rule.cost))return false;
   Jobs.Add(new DemoJob{id=++serial,kind=kind,key=key,left=rule.manufactureSeconds,total=rule.manufactureSeconds});
   Log("制造"+rule.name+" · "+rule.manufactureSeconds+"秒后入库");return true;
  }
  public bool PrepareAttackModule(string key){return ManufactureModule("attackModule",key);}
  // Legacy public entry points use the same inventory/slot validation as the new composer.
  public string LaunchError(string op,string carrier,bool infoHide=false,bool physicalHide=false){
   string[] tech=(infoHide||physicalHide)?new[]{"stealth"}:new string[0];
   if(TechnologyModuleKeys.Contains(op)){tech=tech.Concat(new[]{op}).Distinct().ToArray();op="";}
   return LoadoutError(op,carrier,tech);
  }
  public DemoCost LaunchCost(string op,bool infoHide=false,bool physicalHide=false){
   return op=="probe"||op=="recon"?Rules.Find("operation",op).cost:new DemoCost();
  }
  public bool Launch(string op,string carrier,bool infoHide=false,bool physicalHide=false){
   string[] tech=(infoHide||physicalHide)?new[]{"stealth"}:new string[0];
   if(TechnologyModuleKeys.Contains(op)){tech=tech.Concat(new[]{op}).Distinct().ToArray();op="";}
   return LaunchLoadout(op,carrier,tech);
  }
  public string LoadoutError(string primary,string carrier,IEnumerable<string> technologies){
   var tech=(technologies??new string[0]).ToArray();var c=Rules.Find("carrier",carrier);var b=Body;
   if(Over)return "任务已结束";
   if(!Unlocked(c))return "本关未解锁该载体";
   if(b==null)return "请先选择天体";
   if(tech.Distinct().Count()!=tech.Length)return "同种科技模块只能搭载一个";
   bool utility=primary=="probe"||primary=="recon";
   var d=string.IsNullOrEmpty(primary)?null:Rules.Find("operation",primary);
   if(!string.IsNullOrEmpty(primary)&&(d==null||(!utility&&d.moduleType!="attack")))return "该作业已移除，请重新搭载";
   if(d!=null&&!Unlocked(d))return "本关尚未解锁该打击或侦察操作";
   if(!utility&&d==null&&!tech.Any(k=>k!="stealth"))return "请搭载打击模块或入侵模块";
   if(utility&&tech.Any(k=>k!="stealth"))return "侦察仅支持附加隐形模块";
   if((!utility&&d!=null?1:0)>c.attackSlots)return "打击插槽不足";
   if(tech.Length>c.technologySlots)return "科技插槽不足：该载体只有"+c.technologySlots+"个";
   if(d!=null&&c.capacityRank<Rules.Find("carrier",d.carrier).capacityRank)return "载体搭载能力不足，需要"+Rules.Find("carrier",d.carrier).name+"或更高级载体";
   foreach(var k in tech){
    var rule=TechnologyRule(k);
    if(rule==null||!ModuleAvailable(rule))return k=="stealth"?"先完成隐形技术研发":"本关未解锁该科技模块";
    if(!string.IsNullOrEmpty(rule.carrier)&&c.capacityRank<Rules.Find("carrier",rule.carrier).capacityRank)return rule.name+"需要"+Rules.Find("carrier",rule.carrier).name+"或更高级载体";
   }
   if(primary!="probe"&&!GlobalProbe)return "先完成星系探测，确认天体轨迹";
   if(SelectedShip>=0){
    if(Ship==null||Ship.done)return "所选外逃舰已离场或已被拦截";
    if(primary!="blockade")return "外逃舰目标请搭载拦截模块";
   }
   if(primary=="blockade"){
    if(!CanIntercept)return SolarChoice=="A"?"本局已选择放行，拦截关闭":"第四关剧情选择B后开放拦截";
    if(!Ships.Any(s=>!s.done))return "当前没有在途外逃舰";
    if(tech.Any(k=>k!="stealth"))return "拦截外逃舰仅支持附加隐形模块";
   }else{
    if(primary=="satellite"&&b.kind!="moon"&&b.kind!="station")return "卫星打击只接受卫星或人工天体";
    if(!utility&&primary!="stellar"&&!b.recon)return "先完成该天体深度侦察";
    if(tech.Any(k=>k!="stealth")&&!b.recon)return "入侵前请完成该天体深度侦察";
    if((primary=="surface"||primary=="planetary"||tech.Any(k=>k!="stealth"))&&b.kind=="star")return "恒星没有城市或功能节点";
    if(tech.Contains("virus")&&(b.species!="carbon"||b.initialPop<=0))return "病毒入侵仅对碳基常住人口有效";
    if(tech.Contains("infoInvasion")&&(b.species!="silicon"||b.links.Count==0))return "信息入侵仅对硅基信息链有效";
    if(TargetNode!="auto"&&!b.nodes.Any(n=>n.id==TargetNode&&n.hp>0)&&!utility&&primary!="stellar")return "所选节点已摧毁，请重新选择目标";
    if(tech.Contains("infoInvasion")){
     var r=TechnologyRule("infoInvasion");if(r.useLimit>0&&b.virusCount+Missions.Count(m=>m.body==b.id&&(m.technology??new string[0]).Contains("infoInvasion"))>=r.useLimit)return "该目标入侵次数已达上限";
    }
   }
   if(Carriers[carrier]<=0)return "没有"+c.name+"载体库存";
   if(!utility&&d!=null&&AttackModules[primary]<=0)return "请先制造"+d.name+"模块";
   foreach(var k in tech)if(TechnologyModules[k]<=0)return "请先制造"+TechnologyRule(k).name+"模块";
   if(utility&&!CanPay(d.cost))return "侦察资源不足："+d.cost;
   return null;
  }
  public double LoadoutSeconds(string primary,string carrier,IEnumerable<string> tech){
   var d=Rules.Find("operation",primary??"");double work=d==null?0:d.seconds;
   foreach(var key in tech??new string[0])work=Math.Max(work,TechnologyRule(key).seconds);
   return Rules.Find("carrier",carrier).flight+work;
  }
  public bool LaunchLoadout(string primary,string carrier,params string[] technologies){
   var tech=technologies??new string[0];string error=LoadoutError(primary,carrier,tech);if(error!=null)return Reject(error);
   var d=Rules.Find("operation",primary??"");var c=Rules.Find("carrier",carrier);bool utility=primary=="probe"||primary=="recon";
   if(utility&&!Pay(d.cost))return false;
   if(!utility&&d!=null)AttackModules[primary]--;
   foreach(var k in tech)TechnologyModules[k]--;
   Carriers[carrier]--;if(tech.Length>0)TutorialModuleUsed=true;
   bool concealed=tech.Any(k=>TechnologyRule(k).immuneCounter);
   double rate=Level.awareness?Math.Min(Rules.mechanics.interceptCap,Counter*Rules.mechanics.interceptPerCounter+Alive("军事")*Rules.mechanics.interceptPerMilitary):0;
   double exposure=(d==null?0:d.alert)+c.exposure;
   if(utility)exposure*=Body.scanExposureMultiplier;
   foreach(var k in tech)exposure+=TechnologyRule(k).alert;
   var targetShip=primary=="blockade"?(SelectedShip>=0?Ship:Ships.First(s=>!s.done)):null;
   double duration=LoadoutSeconds(primary,carrier,tech);
   var mission=new DemoMission{id=++serial,ship=targetShip==null?-1:targetShip.id,op=d==null?"technology":primary,body=targetShip==null?Selected:targetShip.body,node=TargetNode,carrier=carrier,left=duration,total=duration,intercept=concealed?0:Math.Max(0,rate),concealed=concealed,technology=(string[])tech.Clone()};
   Missions.Add(mission);Expose(exposure);
   Log("发射 "+MissionName(mission)+" → "+(primary=="probe"?"星系扫描区域":targetShip==null?Body.name:"外逃舰 #"+targetShip.id)+" · "+duration.ToString("0")+"秒；载体与模块已扣库存");return true;
  }
  public string MissionName(DemoMission m){
   var d=Rules.Find("operation",m.op);var names=new List<string>();if(d!=null)names.Add(d.name);
   foreach(var k in m.technology??new string[0]){var r=TechnologyRule(k);if(r!=null)names.Add(r.name);}
   return names.Count==0?"旧作业":string.Join(" + ",names);
  }
  void CompleteMission(DemoMission m){
   var d=Rules.Find("operation",m.op);var b=Find(m.body);var tech=m.technology??new string[0];if(b==null)return;
   if(d!=null)Entropy+=d.entropy;
   double strength=m.boost*(Research.ContainsKey("calibration")?Research["calibration"]:1)*Level.damageScale;
   bool offensive=(d!=null&&d.moduleType=="attack")||tech.Any(k=>k!="stealth");
   if(offensive&&!m.concealed&&Random()<m.intercept){strength*=Rules.mechanics.interceptedStrength;Expose(Rules.mechanics.interceptedAlert);Log("投送遭到反制：效果×"+Rules.mechanics.interceptedStrength);}
   switch(m.op){
    case "probe":GlobalProbe=true;Log("星系探测完成：所有自然天体与轨道站轨迹已确认");break;
    case "recon":b.recon=true;Log(b.name+"深度侦察完成");break;
    case "blockade":
     var ship=Ships.FirstOrDefault(s=>s.id==m.ship&&!s.done);
     if(ship!=null){ship.done=true;Find(ship.body).pop+=ship.pop;Log("外逃舰 #"+ship.id+"已阻截，人口返回原天体，仍需清理");}
     else Log("阻截目标已离场，本次投送不转移至其他舰");break;
    case "stellar":
     foreach(var t in Bodies)foreach(var n in t.nodes.Where(n=>n.hp>0).ToArray())DamageNodes(t,d.hp*strength,n.id,d.nodeDamage*strength);
     Kill(Bodies.OrderByDescending(t=>t.pop),d.pop*strength);Log(d.name+"落地，伤害全域人口和节点");break;
    case "surface":case "satellite":case "planetary":
     if(m.node!="auto")DamageNodes(b,d.hp*strength,m.node,d.nodeDamage*strength);else Kill(new[]{b},d.pop*strength);
     Log(d.name+"落地 · 打击顺序自由");break;
   }
   foreach(var k in tech){
    var rule=TechnologyRule(k);if(rule==null)continue;
    if(k=="virus"&&b.species=="carbon"){
     TutorialSoftOperation=true;Viruses.Add(new DemoVirus{body=b.id,next=Time+rule.interval,strength=strength,count=rule.ticks});
     Log("病毒入侵："+b.name+"每"+rule.interval+"秒损失"+rule.effect+"P，共"+rule.ticks+"次");
    }
    if(k=="infoInvasion"&&b.species=="silicon"){
     b.virusCount++;var links=m.node=="auto"?b.links.Where(l=>LinkFlow(b,l)>0).Take(1):b.links.Where(l=>l.from==m.node||l.to==m.node);
     foreach(var link in links){link.multiplier=Time<link.suppressedUntil?Math.Min(link.multiplier,rule.flowMultiplier):rule.flowMultiplier;link.suppressedUntil=Math.Max(link.suppressedUntil,Time+rule.duration);}
     Log("信息入侵："+b.name+"目标链流速×"+rule.flowMultiplier+"，持续"+rule.duration+"秒");
    }
   }
   Effects.Add(new DemoEffect{body=b.id,time=Time});Revision++;
  }
 }
}

