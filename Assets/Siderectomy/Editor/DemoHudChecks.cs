using System;
using System.IO;
using Siderectomy;
using UnityEngine;
public static class DemoHudChecks {
 static void Require(bool ok,string why){if(!ok)throw new Exception("HUD_CHECK_FAILED: "+why);}
 public static void Run(){var s=new DemoSimulation(DemoLevel.Defaults()[3],DemoRuleBook.Load());s.GlobalProbe=true;s.Body.recon=true;s.Carriers["dust"]=2;
  Require(s.PrepareAttackModule("surface"),"模块预装配失败");Require(s.AttackModules["surface"]==1&&s.Res[3]==22&&s.Res[0]==138&&s.Res[1]==142,"入库或费用不正确");
  double e=s.Res[0],m=s.Res[1],a=s.Res[3];Require(s.Launch("surface","dust"),"库存投送失败");Require(s.AttackModules["surface"]==0&&s.Res[0]==e&&s.Res[1]==m&&s.Res[3]==a&&s.Carriers["dust"]==1,"发射重复扣费或未扣库存");
  Require(s.Launch("surface","dust")&&s.Res[3]==a-6,"零库存自动装配未保留原规则");
  Require(!s.PrepareAttackModule("galaxy")&&s.AttackModules["galaxy"]==0,"星系模块绕过解锁");
  Require(s.PrepareAttackModule("surface"),"第二模块入库失败");a=s.Res[3];Require(!s.Launch("surface","dust")&&s.AttackModules["surface"]==1&&s.Res[3]==a,"失败投送扣库存");
  s.Res[0]=0;Require(!s.PrepareAttackModule("stellar")&&s.AttackModules["stellar"]==0,"资源不足仍入库");
  var icons=Resources.Load<DemoHudIcons>("HUD图标配置_代替换");Require(icons!=null&&icons.slots.Length==16,"图标配置不完整");foreach(var slot in icons.slots)Require(slot.icon!=null,"图标缺失 "+slot.key);
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/HUD库存规则验收.txt","PASS 模块扣费与入库、库存发射不重复扣费、零库存自动装配、失败投送不扣库存、解锁与资源限制、16个可替换图标槽。\n");Debug.Log("HUD_CHECKS_OK");
 }
}
