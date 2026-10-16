using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Siderectomy;
using UnityEngine;
public static class DemoChecks {
 static List<string> passed;static void Check(bool ok,string name){if(!ok)throw new Exception("DEMO_CHECK_FAILED: "+name);passed.Add(name);}
 static DemoSimulation New(int n){return new DemoSimulation(DemoLevel.Defaults()[n-1],DemoRuleBook.Load());}
 static void Fund(DemoSimulation s){for(int i=0;i<6;i++)s.Res[i]=10000;s.GlobalProbe=true;s.Research["stealth"]=1;foreach(var b in s.Bodies)b.recon=true;foreach(var k in s.Carriers.Keys.ToArray())s.Carriers[k]=20;}
 public static void Run(){passed=new List<string>();
  for(int n=1;n<=4;n++){var s=New(n);Check(s.Bodies.Count(b=>b.kind=="star")==1,"L"+n+"恒星数量");Check(s.Bodies.Count(b=>b.kind=="planet")==s.Level.planets&&s.Bodies.Count(b=>b.kind=="moon")==s.Level.moons&&s.Bodies.Count(b=>b.kind=="station")==s.Level.stations,"L"+n+"天体数量");Check(Math.Abs(s.InitialPop-s.Level.population)<.001,"L"+n+"人口配置");}
  var a=New(4);Check(a.InitialNodes==25&&a.GateCount==18&&a.Find("p2").pop==1536&&a.Find("m0").pop==114,"太阳系1650P / 25中心 / 18门槛");Check(a.Bodies.Where(b=>b.id!="p2").All(b=>b.nodes.Count==0),"全部中心位于地球");
  double t=a.Time,e=a.Res[0];a.Advance(10);Check(a.Time==t&&a.Res[0]==e,"暂停停止全部时间与生产");
  Check(a.Submit("facility","military"),"军工可提交");a.Paused=false;a.Advance(15);Check(a.Facilities["military"]==1,"建设计时与完成");Check(a.Submit("batch","a1"),"制造批次提交");Check(!a.Submit("batch","a2"),"共享军工队列上限");a.Advance(7);Check(a.Res[3]==38,"A1制造+10");
  var b=New(4);Fund(b);b.Paused=false;Check(b.Launch("surface","dust"),"地表打击发射");Check(b.Carriers["dust"]==19,"一次性消耗载体");Check(!b.Launch("galaxy","planet"),"星系打击L4禁用");
  b.Kill(b.Bodies,10000);Check(Math.Abs(b.Killed-495)<.001&&Math.Abs(b.Remaining-1155)<.001,"保护网络最多清除30%");foreach(var node in b.Find("p2").nodes.Take(18))node.hp=0;Check(b.GateOpen,"18个中心解锁人口清除");b.Kill(b.Bodies,10000);b.CheckEnd();Check(b.Won,"保护解除后人口清零胜利");
  var c=New(4);Fund(c);c.Paused=false;c.Selected="sun";Check(c.Launch("stellar","planet"),"恒星打击允许太阳目标");c.Missions[0].intercept=0;c.Advance(100);Check(c.Find("p2").nodes.All(x=>Math.Abs(x.hp-x.max*.3)<.001),"恒星打击全部中心70%伤害");Check(Math.Abs(c.Killed-495)<.001,"范围人口伤害仍遵循门槛");
  var d=New(4);Fund(d);d.Paused=false;Check(d.Launch("surface","dust"),"QTE载荷创建");var m=d.Missions[0];double left=m.left;d.Calibrate(m,true);Check(m.boost==1.2&&m.left==left-5,"QTE成功效果及缩时");d.Calibrate(m,false);Check(m.boost==1.2,"禁止重复接管");
  foreach(int n in new[]{2,3,4}){var s=New(n);s.Paused=false;s.Find("p2").pop-=8;s.Ships.Add(new DemoShip{body="p2",pop=8,left=.05});s.Advance(.1);Check(n==4?s.SecondEnding&&!s.Over:s.Over&&!s.Won,"L"+n+"外逃规则");Check(Math.Abs(s.Remaining+s.EscapedPop+s.Killed-s.InitialPop)<.001,"L"+n+"外逃人口守恒");}
  var f=New(4);Fund(f);f.Paused=false;f.Find("p2").pop-=8;f.Ships.Add(new DemoShip{body="p2",pop=8,left=30});Check(f.Launch("blockade","satellite"),"拦截提交");f.Advance(23);Check(f.Ships[0].done&&f.Escaped==0&&Math.Abs(f.Remaining-f.InitialPop)<.001,"拦截返回人口");
  var timeout=New(1);timeout.Paused=false;timeout.Advance(481);Check(timeout.Over&&!timeout.Won&&Math.Abs(timeout.Time-480)<.001,"超时失败");var noAlert=New(1);noAlert.Expose(100);Check(noAlert.Alert==0&&!noAlert.EscapeActive,"第一关无察觉");
  var gate=New(1);gate.GlobalProbe=false;gate.Carriers["dust"]=1;Check(!gate.Launch("recon","dust"),"侦察依赖轨迹探测");
  for(int n=1;n<=4;n++)Check(!New(n).Unlocked(New(n).Rules.Find("operation","galaxy")),"L"+n+"禁用星系武器");
  var escapedWin=New(4);escapedWin.Paused=false;escapedWin.Find("p2").pop-=8;escapedWin.Ships.Add(new DemoShip{body="p2",pop=8,left=.05});escapedWin.Advance(.1);foreach(var node in escapedWin.Bodies.SelectMany(x=>x.nodes))node.hp=0;escapedWin.Kill(escapedWin.Bodies,10000);escapedWin.CheckEnd();Check(escapedWin.Won&&escapedWin.SecondEnding,"第四关带外逃通关");
  var target=New(4);Fund(target);target.Paused=false;target.Level.aiScale=0;target.Find("p2").pop-=16;target.Ships.Add(new DemoShip{id=501,body="p2",pop=8,left=60,total=60});target.Ships.Add(new DemoShip{id=502,body="p2",pop=8,left=60,total=60});Check(target.SelectShip(502),"可选择第二艘外逃舰");var resources=(double[])target.Res.Clone();int stock=target.Carriers["satellite"];var cost=target.LaunchCost("blockade",false,false);Check(target.Launch("blockade","satellite"),"指定舰船阻截发射");var locked=target.Missions.Last();Check(locked.ship==502,"阻截绑定所选舰船ID");for(int i=0;i<6;i++)Check(Math.Abs(target.Res[i]-(resources[i]-cost[i]))<.001,"阻截费用保持资源"+i);Check(target.Carriers["satellite"]==stock-1,"指定阻截仅消耗一个载体");target.SelectShip(501);target.Advance(23.1);Check(!target.Ships[0].done&&target.Ships[1].done,"换选不转移已发射阻截目标");Check(Math.Abs(target.Remaining-target.InitialPop)<.001,"指定阻截人口守恒");
  var lost=New(4);Fund(lost);lost.Paused=false;lost.Level.aiScale=0;lost.Find("p2").pop-=16;lost.Ships.Add(new DemoShip{id=601,body="p2",pop=8,left=1,total=60});lost.Ships.Add(new DemoShip{id=602,body="p2",pop=8,left=60,total=60});lost.SelectShip(601);Check(lost.Launch("blockade","satellite"),"即将离场目标可发射");lost.Advance(23.1);Check(lost.Escaped==1&&!lost.Ships[1].done,"目标离场后不误拦其他舰船");Check(!lost.Launch("blockade","satellite"),"过期目标拒绝发射");
  var hidden=New(4);string hiddenNode=hidden.Find("p2").nodes[0].id;Check(!hidden.SelectNode(hiddenNode),"未侦察不能选择中心ID");string selected=hidden.Selected;hidden.SelectBody("p7");Check(hidden.Selected==selected&&!DemoSystemDiscovery.Revealed(hidden),"探测前禁止选择未知星球");hidden.Find("p2").recon=true;Check(!hidden.SelectNode(hiddenNode),"星系未揭示时不能绕过选择门槛");hidden.GlobalProbe=true;Check(hidden.SelectNode(hiddenNode)&&hidden.TargetNode==hiddenNode,"侦察后中心选择共享ID");hidden.Find("p2").nodes[0].hp=0;Check(!hidden.SelectNode(hiddenNode),"摧毁中心不可重复选中");
  var slow=New(4);slow.Paused=false;slow.Level.aiScale=0;slow.EscapeActive=true;slow.Escape=100;slow.Advance(.05);Check(slow.Ships.Count==1&&slow.Ships[0].total==60,"自然外逃窗口60游戏秒");slow.Advance(59);Check(!slow.Ships[0].done,"延长窗口内仍可拦截");slow.Advance(1);Check(slow.SecondEnding,"窗口结束保持第四关分支规则");
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/规则验收.txt",string.Join("\n",passed.Select(x=>"PASS "+x))+"\n测试使用独立状态和部分边界测试资源注入；不代表完整玩家节奏平衡。\n");Debug.Log("DEMO_CHECKS_OK: "+passed.Count);
 }
}
