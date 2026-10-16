using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Siderectomy;
using UnityEngine;
public static class DemoSixPaidPlaythrough {
 static int Pending(DemoSimulation s,string kind,string key){return s.Jobs.Count(j=>j.kind==kind&&j.key==key);}
 static bool Order(DemoSimulation s,string kind,string key){var d=s.Rules.Find(kind,key);return s.Unlocked(d)&&s.CanPay(d.cost)&&s.Submit(kind,key);}
 static void Fac(DemoSimulation s,string key,int target){if(s.Facilities[key]+Pending(s,"facility",key)<target)Order(s,"facility",key);}
 static void Carrier(DemoSimulation s,string key,int target){if(s.Carriers[key]+Pending(s,"carrier",key)<target)Order(s,"carrier",key);}
 static void ReserveModule(DemoSimulation s,string kind,string key){
  var r=s.Rules.Find(kind,key);if(r==null||!s.ModuleAvailable(r))return;
  var inventory=kind=="attackModule"?s.AttackModules:s.TechnologyModules;
  if(inventory[key]+Pending(s,kind,key)<1&&s.CanPay(r.cost))s.Submit(kind,key);
 }
 static bool Fire(DemoSimulation s,string op,string c,string body,string node="auto",bool hide=false){
  s.Selected=body;s.TargetNode=node;s.SelectedShip=-1;bool ih=hide&&s.Research.ContainsKey("stealth");
  if(DemoSimulation.AttackModuleKeys.Contains(op))ReserveModule(s,"attackModule",op);
  if(DemoSimulation.TechnologyModuleKeys.Contains(op))ReserveModule(s,"technologyModule",op);
  if(ih)ReserveModule(s,"technologyModule","stealth");
  if(s.LaunchError(op,c,ih,false)!=null||!s.CanPay(s.LaunchCost(op,ih,false)))return false;
  bool result=s.Launch(op,c,ih,false);if(result&&op!="blockade")s.Calibrate(s.Missions.Last(),true);return result;
 }
 public static void Run(){
  var report=new List<string>();
  foreach(var level in Resources.Load<DemoCampaign>("DemoCampaign").levels){
   var s=new DemoSimulation(level,DemoRuleBook.Load());s.Paused=false;int iterations=0;
   while(!s.Over&&iterations++<5000){
    if(s.StoryPending){if(s.SolarStoryPhase==2)s.ChooseSolarEscape(false);else s.NextSolarDialogue();continue;}
    if(s.CanApply&&s.ApplyCount==0&&!s.Jobs.Any(j=>j.kind=="apply"))s.Submit("apply","apply");
    foreach(var ship in s.Ships.Where(x=>!x.done&&!s.Missions.Any(m=>m.ship==x.id)).ToArray()){
     s.Selected=ship.body;s.SelectedShip=ship.id;s.TargetNode="auto";ReserveModule(s,"attackModule","blockade");
     if(s.LaunchError("blockade","satellite",false,false)==null&&s.CanPay(s.LaunchCost("blockade",false,false)))s.Launch("blockade","satellite");
    }
    Fac(s,"energy",3);Fac(s,"matter",3);
    if(level.number>=2&&!s.Research.ContainsKey("stealth")&&Pending(s,"research","stealth")==0)Order(s,"research","stealth");
    if(s.Research.ContainsKey("stealth"))Fac(s,"signal",2);
    Carrier(s,"dust",2);if(level.number>=2)Carrier(s,"satellite",3);
    if(!s.GlobalProbe&&!s.Missions.Any(m=>m.op=="probe"))Fire(s,"probe","dust",s.Selected);
    if(s.GlobalProbe){
     foreach(var b in s.Bodies.Where(b=>b.initialPop>0))if(!b.recon&&!s.Missions.Any(m=>m.op=="recon"&&m.body==b.id))Fire(s,"recon",level.number==1?"dust":"satellite",b.id,"auto",level.number>1);
     foreach(var b in s.Bodies.Where(b=>b.recon&&b.species=="silicon"&&b.pop>0)){
      if(b.virusCount+s.Missions.Count(m=>(m.technology??new string[0]).Contains("infoInvasion")&&m.body==b.id)<(level.number==3?2:3))
       for(int i=1;i<b.nodes.Count;i+=2)if(b.nodes[i].hp>0&&b.links.Any(l=>(l.from==b.nodes[i].id||l.to==b.nodes[i].id)&&s.LinkFlow(b,l)>0)&&!s.Missions.Any(m=>(m.technology??new string[0]).Contains("infoInvasion")&&m.body==b.id&&m.node==b.nodes[i].id))Fire(s,"infoInvasion","satellite",b.id,b.nodes[i].id,true);
     }
     if(level.number>=3){Carrier(s,"planet",1);if(!s.Missions.Any(m=>m.op=="stellar"))Fire(s,"stellar","planet","sun","auto",true);}
     if(level.number==2)Carrier(s,"asteroid",1);
     foreach(var b in s.Bodies.Where(b=>b.recon&&b.pop>0))if(!s.Missions.Any(m=>m.body==b.id&&m.op==(level.number==2?"planetary":"surface")))Fire(s,level.number==2?"planetary":"surface",level.number==2?"asteroid":level.number>1&&s.Research.ContainsKey("stealth")?"satellite":"dust",b.id,"auto",level.number>1&&s.Research.ContainsKey("stealth"));
    }
    s.Advance(1);
   }
   report.Add("L"+level.number+" "+(s.Won?"PASS":"FAIL")+" "+DemoSimulation.Clock(s.Time)+" 外逃"+s.Escaped+" 剩余"+s.Remaining.ToString("0.0")+"P\n"+s.ResultReason);
   if(!s.Won)report.Add(string.Join("\n",s.Logs.Take(8)));
  }
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/SixPaidPlaythrough.txt",string.Join("\n\n",report)+"\n普通付费指令，无注入资源；第四关起使用正常资源申请。自动完美校准，不代表真人平衡。");Debug.Log("SIX_PAID_PLAYTHROUGH\n"+string.Join("\n",report));
 }
 public static void Build(){Run();DemoModuleChecks.Build();}
}
