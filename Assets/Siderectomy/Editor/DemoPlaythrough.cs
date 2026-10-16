using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Siderectomy;
using UnityEngine;
// Paid-command reference strategy. No starting resource grants, no injected events, no altered level settings.
public static class DemoPlaythrough {
 static bool Order(DemoSimulation s,string kind,string key){var r=s.Rules.Find(kind,key);return s.Unlocked(r)&&s.CanPay(r.cost)&&s.Submit(kind,key);}
 static int Pending(DemoSimulation s,string kind,string key){return s.Jobs.Count(j=>j.kind==kind&&j.key==key);}
 static void Facility(DemoSimulation s,string key,int target){if(s.Facilities[key]+Pending(s,"facility",key)<target)Order(s,"facility",key);}
 static void Batch(DemoSimulation s,string key,int index,double target){if(s.Res[index]<target)Order(s,"batch",key);}
 static void Carrier(DemoSimulation s,string key,int target){if(s.Carriers[key]+Pending(s,"carrier",key)<target)Order(s,"carrier",key);}
 static bool Fire(DemoSimulation s,string op,string carrier,string body,string node="auto",bool stealth=false){s.Selected=body;s.TargetNode=node;bool modules=stealth&&s.Research.ContainsKey("stealth");if(s.LaunchError(op,carrier,modules,modules)!=null||!s.CanPay(s.LaunchCost(op,modules,modules)))return false;bool ok=s.Launch(op,carrier,modules,modules);if(ok&&op!="blockade")s.Calibrate(s.Missions.Last(),true);return ok;}
 [UnityEditor.MenuItem("裁星计划/运行四关付费指令通关验证")]
 public static void Run(){var report=new List<string>();foreach(int n in new[]{1,2,3,4}){
   var s=new DemoSimulation(DemoLevel.Defaults()[n-1],DemoRuleBook.Load());s.Paused=false;
   while(!s.Over){
    // Intercept every new ship before optional spending; retain basic armaments for blockades.
    if(s.Ships.Count(x=>!x.done)>s.Missions.Count(m=>m.op=="blockade"))Fire(s,"blockade","satellite",s.Ships.First(x=>!x.done).body);
    Facility(s,"energy",1);Facility(s,"matter",1);Facility(s,"military",1);
    if(!s.Research.ContainsKey("stealth")&&Pending(s,"research","stealth")==0)Order(s,"research","stealth");
    if(s.Research.ContainsKey("stealth"))Facility(s,"signal",1);
    Facility(s,"energy",3);Facility(s,"matter",3);Facility(s,"military",2);
    if(n>=2)Carrier(s,"satellite",2);
    Batch(s,"a1",3,20);if(n>=2)Batch(s,"a2",4,n==2?8:14);if(n>=3)Batch(s,"a3",5,4);
    Carrier(s,"dust",2);
    if(!s.GlobalProbe&&!s.Missions.Any(m=>m.op=="probe"))Fire(s,"probe","dust",s.Selected);
    if(s.GlobalProbe){
     foreach(var b in s.Bodies.Where(b=>b.initialPop>0||b.nodes.Count>0))if(!b.recon&&!s.Missions.Any(m=>m.op=="recon"&&m.body==b.id))Fire(s,"recon","dust",b.id);
     if(n>=3){Carrier(s,"planet",1);if(!s.Missions.Any(m=>m.op=="stellar"))Fire(s,"stellar","planet","sun","auto",true);}
     else {foreach(var b in s.Bodies.Where(b=>b.recon))foreach(var node in b.nodes.OrderBy(x=>x.type=="能源"?0:x.type=="军事"?1:2))if(node.hp>0&&!s.Missions.Any(m=>m.body==b.id&&m.node==node.id))Fire(s,"surface","dust",b.id,node.id);}
     if(s.GateOpen){if(n==2)Carrier(s,"asteroid",1);foreach(var b in s.Bodies.Where(b=>b.recon&&b.pop>0))if(!s.Missions.Any(m=>m.body==b.id&&m.node=="auto"&&m.op!="recon"))Fire(s,n==2?"planetary":"surface",n==2?"asteroid":"dust",b.id);}
    }
    s.Advance(1);
   }
   report.Add("L"+n+" "+(s.Won?"PASS":"FAIL")+" · "+DemoSimulation.Clock(s.Time)+" · 外逃 "+s.Escaped+" · 剩余 "+s.Remaining.ToString("0.0")+"P · 中心 "+s.Cleared+"/"+s.InitialNodes+"\n"+s.ResultReason);
   if(!s.Won)report.Add("库存 "+string.Join(",",s.Res.Select(x=>x.ToString("0")))+" · 载体 "+string.Join(",",s.Carriers.Select(x=>x.Key+":"+x.Value))+"\n"+string.Join("\n",s.Logs.Take(12)));
  }
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/付费指令通关验证.txt",string.Join("\n\n",report)+"\n使用默认关卡参数、普通资源与付费指令；自动执行完美投送校准；不代表真人操作节奏。\n");Debug.Log("DEMO_PLAYTHROUGH_DONE\n"+string.Join("\n",report));
 }
}
