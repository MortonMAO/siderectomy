using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Siderectomy;
public static class DemoSixCampaignChecks {
 static void Check(bool v,string msg){if(!v)throw new Exception(msg);}
 static void Supply(DemoSimulation s){for(int i=0;i<6;i++)s.Res[i]=10000;s.GlobalProbe=true;s.Research["stealth"]=1;foreach(var b in s.Bodies)b.recon=true;}
 static DemoSimulation Solar(){var s=new DemoSimulation(DemoLevel.Defaults()[3],DemoRuleBook.Load());s.Paused=false;foreach(var b in s.Bodies){b.pop=0;foreach(var n in b.nodes)n.hp=0;}s.Advance(181);return s;}
 public static void Run(){
  DemoSixCampaignUpgrade.Run();var rules=DemoRuleBook.Load();var campaign=Resources.Load<DemoCampaign>("DemoCampaign");
  Check(campaign.levels.Length==6,"未更新六关配置");
  foreach(var l in campaign.levels){var s=new DemoSimulation(l,rules);Check(Math.Abs(s.Remaining-l.population)<.001,"人口汇总错误："+l.number);Check(s.GateOpen,"仍有节点门槛");}
  foreach(int n in new[]{3,5,6}){
   var level=DemoLevel.Defaults()[n-1];level.awareness=false;var s=new DemoSimulation(level,rules);Supply(s);s.Paused=false;
   var b=s.Bodies.First(x=>x.species=="silicon"&&x.initialPop>0);Check(b.links.Count==(n==3?3:5),"信息链长度错误");
   s.Selected=b.id;s.TargetNode="auto";b.pop=b.initialPop*.5;
   double before=b.pop;s.Advance(1);Check(b.pop>before,"硅基人口不恢复");
   Check(s.LaunchError("virus","satellite",false,false)!=null,"病菌对硅基错误生效");
   double baseline=s.RegenerationPerSecond(b);s.TargetNode=b.nodes[1].id;s.Carriers["satellite"]=1;
   Check(s.Launch("infoVirus","satellite"),"信息入侵失败");s.Advance(37);
   Check(s.InformationFlow(b)<1&&s.RegenerationPerSecond(b)<baseline,"信息入侵未减缓恢复");
   var snapshot=s.Capture();var restored=DemoSimulation.Restore(snapshot,rules);Check(restored.Bodies.First(x=>x.id==b.id).links.Count==b.links.Count,"链条存档丢失");
   s.Advance(46);Check(s.InformationFlow(b)==1,"入侵到期未恢复流速");
   foreach(var node in b.nodes)node.hp=0;Check(s.RegenerationPerSecond(b)==0,"全部节点毁坏仍恢复");
  }
  var clean=new DemoSimulation(DemoLevel.Defaults()[0],rules);clean.Kill(clean.Bodies,clean.InitialPop);clean.CheckEnd();Check(clean.Won&&clean.Cleared==0,"仍需70%节点才能清零");
  var a=Solar();Check(a.SolarStoryPhase==1&&a.Ships.Count==1,"提前清场跳过首次离港");Check(!a.Over,"剧情未结束就结算");
  a.NextSolarDialogue();a.ChooseSolarEscape(true);double firstTime=a.Time;
  a.Advance(14.9);Check(a.SolarStoryPhase==5&&!a.CanApply,"第二轮提前触发");a.Advance(.2);Check(a.SolarStoryPhase==3&&a.Time>=firstTime+15,"第二轮15秒计时错误");
  a.NextSolarDialogue();Check(a.CanApply&&!a.CanIntercept,"A分支权限错误");a.Advance(100);Check(a.Won&&a.Escaped==1,"放行路径无法通关");
  var bSolar=Solar();bSolar.NextSolarDialogue();bSolar.ChooseSolarEscape(false);bSolar.Advance(15.1);bSolar.NextSolarDialogue();Supply(bSolar);
  bSolar.Submit("apply","apply");bSolar.Advance(25.1);Check(bSolar.Res[0]>100000,"剧情申请未拨付");bSolar.Carriers["satellite"]=1;bSolar.SelectShip(bSolar.Ships[0].id);
  Check(bSolar.Launch("blockade","satellite"),"B不能拦截首艘");bSolar.Advance(23.1);Check(bSolar.Ships[0].done&&bSolar.Escaped==0,"首艘无法及时阻截");
  var last=new DemoSimulation(DemoLevel.Defaults()[5],rules);last.Paused=false;
  var carbon=last.Bodies.First(x=>x.initialPop>0&&x.species=="carbon");carbon.pop=0;last.CheckEnd();Check(!last.Over,"第六关只清碳基就胜利");
  foreach(var b in last.Bodies)b.pop=0;last.CheckEnd();Check(last.Won,"第六关两类清零不胜利");
  last.SetDebugFeatures(true);Check(last.Unlocked(rules.Find("operation","galaxy")),"全开失败");last.SetDebugFeatures(false);
  DemoStoryChecks.Run();DemoConfigChecks.Run();
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/SixCampaign.txt","PASS 六关配置、3/5/5信息链、人口恢复、入侵压流与到期、存档、取消节点保护、强制首艘离港、15秒剧情、A放行、B申请后首艘阻截、第六关双文明清零、调试恢复。使用独立充足资源夹具，非完整真人平衡验证。");
  Debug.Log("SIX_CAMPAIGN_CHECK_PASS");
 }
 public static void Build(){Run();DemoConfigChecks.Build();}
}
