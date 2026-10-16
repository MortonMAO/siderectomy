using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Siderectomy;
using UnityEngine;

public static class DemoProgressionChecks {
 [Serializable] sealed class Envelope {public int version=1;public string payload,sha256;}
 [Serializable] sealed class LegacyProfile {public int version=1,currentLevel=1,applyCount;public string id,name,createdUtc,updatedUtc;public bool secondEnding,hasCheckpoint;public DemoSnapshot checkpoint;}
 static int count;
 static void Check(bool value,string reason){if(!value)throw new Exception("PROGRESSION_CHECK: "+reason);count++;}
 static void Throws(Action action,string reason){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,reason);}
 // Deterministic goal completion for progress tests; gameplay and solar dialogue have separate regressions.
 static DemoSimulation Victory(int level){var s=new DemoSimulation(DemoLevel.Defaults()[level-1],DemoRuleBook.Load());foreach(var b in s.Bodies)b.pop=0;s.SolarReservedPopulation=0;if(level==4)s.SolarStoryPhase=4;s.Exposure=999999;s.Entropy=999999;s.CheckEnd();return s;}
 static void RawSave(string id,object value){string payload=JsonUtility.ToJson(value);string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");File.WriteAllText(Path.Combine(DemoSaveStore.DirectoryPath,"slot_"+id+".json"),JsonUtility.ToJson(new Envelope{payload=payload,sha256=hash}),Encoding.UTF8);}
 public static void Run(){var previous=DemoSaveStore.TestDirectory;var profile=DemoSession.Profile;DemoSaveStore.TestDirectory=Path.GetFullPath("Temp/ProgressionChecks_"+Guid.NewGuid().ToString("N"));count=0;
  try{
   Check(DemoProgression.LevelCount==6,"沿用六关战役");var p=DemoSaveStore.Create("六关逐关进度");DemoSession.Profile=p;
   Check(p.highestUnlockedLevel==1&&p.currentLevel==1&&p.completedLevels.Count==0,"新档初值");for(int n=1;n<=6;n++)Check(DemoProgression.CanEnter(p,n)==(n==1),"新档访问 "+n);
   Throws(()=>DemoSession.SaveProgress(null,6,0,false),"保存接口拒绝越级");Check(DemoSession.Profile.currentLevel==1&&DemoSaveStore.Load(p.id).highestUnlockedLevel==1,"拒绝不修改持久化进度");
   var failed=new DemoSimulation(DemoLevel.Defaults()[0],DemoRuleBook.Load());failed.Paused=false;failed.Advance(failed.Level.limit);
   Check(!failed.Won&&!DemoSession.RecordVictory(failed)&&DemoSession.Profile.highestUnlockedLevel==1,"失败不解锁");
   Check(!DemoSession.RecordVictory(new DemoSimulation(DemoLevel.Defaults()[0],DemoRuleBook.Load())),"运行中不能登记通关");
   DemoSession.SaveProgress(null,1,0,false);Check(DemoSession.Profile.highestUnlockedLevel==1,"放弃不解锁");
   var first=Victory(1);Check(first.Won&&first.KnowledgeScore<1&&first.EntropyScore<1,"低分目标完成夹具");Check(DemoSession.RecordVictory(first),"胜利可登记");
   Check(DemoSession.Profile.highestUnlockedLevel==2&&DemoSession.Profile.currentLevel==2&&DemoSession.Profile.completedLevels.SequenceEqual(new[]{1}),"第一关只解锁第二关");
   Check(DemoSaveStore.Load(p.id).highestUnlockedLevel==2,"解锁立即持久化");DemoSession.RecordVictory(first);Check(DemoSession.Profile.highestUnlockedLevel==2&&DemoSession.Profile.completedLevels.Count==1,"重复结果幂等");
   Throws(()=>DemoSession.RecordVictory(Victory(6)),"锁定关卡不能登记完成");
   for(int n=2;n<=6;n++){Check(DemoSession.RecordVictory(Victory(n)),"登记第"+n+"关");Check(DemoSession.Profile.highestUnlockedLevel==Math.Min(6,n+1)&&DemoSession.Profile.currentLevel==Math.Min(6,n+1),"逐关解锁边界 "+n);}
   Check(DemoSession.Profile.completedLevels.SequenceEqual(Enumerable.Range(1,6)),"末关封顶且六关完整记录");DemoSession.RecordVictory(first);
   Check(DemoSession.Profile.highestUnlockedLevel==6&&DemoSession.Profile.completedLevels.Count==6,"重玩不会回退或重复解锁");
   var independent=DemoSaveStore.Create("另一个档案");Check(independent.highestUnlockedLevel==1&&independent.completedLevels.Count==0,"档案进度隔离");
   foreach(int level in new[]{4,5,6}){
    var legacy=DemoSaveStore.Create("旧档位置");RawSave(legacy.id,new LegacyProfile{id=legacy.id,name="旧档位置",currentLevel=level});
    var migrated=DemoSaveStore.Load(legacy.id);Check(migrated.version==2&&migrated.currentLevel==level&&migrated.highestUnlockedLevel==level&&migrated.completedLevels.Count==0,"旧档保留位置但不伪造通关记录 "+level);
    Check(File.ReadAllText(Path.Combine(DemoSaveStore.DirectoryPath,"slot_"+legacy.id+".json")).Contains("\\\"version\\\":1"),"读取兼容不直接重写原文件 "+level);
    DemoSaveStore.Save(migrated);Check(DemoSaveStore.HasBackup(legacy.id),"旧档下次保存保留备份 "+level);
   }
   var six=new DemoSimulation(DemoLevel.Defaults()[5],DemoRuleBook.Load());for(int i=0;i<6;i++)six.Res[i]=10000;six.GlobalProbe=true;foreach(var b in six.Bodies)b.recon=true;
   six.Research["stealth"]=1;six.Carriers["satellite"]=1;six.TechnologyModules["stealth"]=1;six.AttackModules["surface"]=1;six.SelectBody("p2");
   Check(six.Submit("technologyModule","virus")&&six.LaunchLoadout("surface","satellite","stealth"),"旧v3制造和组合投送夹具");
   var checkpoint=DemoSaveStore.Create("旧第六关现场");RawSave(checkpoint.id,new LegacyProfile{id=checkpoint.id,name="旧第六关现场",currentLevel=5,hasCheckpoint=true,checkpoint=six.Capture()});
   var loaded=DemoSaveStore.Load(checkpoint.id);Check(loaded.currentLevel==6&&loaded.highestUnlockedLevel==6&&loaded.hasCheckpoint&&loaded.checkpoint.version==3,"旧档以第六关v3现场为准");
   var restored=DemoSimulation.Restore(loaded.checkpoint,DemoRuleBook.Load());Check(restored.Jobs.Any(j=>j.kind=="technologyModule"&&j.key=="virus")&&restored.Missions.Count==1&&restored.Missions[0].technology.Contains("stealth"),"存档兼容保留科技制造队列和搭载模块");
   var four=DemoSaveStore.Create("四关交付版档案");four.currentLevel=four.highestUnlockedLevel=4;four.completedLevels.AddRange(new[]{1,2,3});DemoSaveStore.Save(four);
   Check(DemoSaveStore.Load(four.id).highestUnlockedLevel==4&&!DemoProgression.CanEnter(four,5),"四关版v2档案不自动开放第五关");
   var invalid=DemoSaveStore.Create("禁止跨级现场");invalid.currentLevel=6;invalid.hasCheckpoint=true;invalid.checkpoint=six.Capture();RawSave(invalid.id,invalid);
   Throws(()=>DemoSaveStore.Load(invalid.id),"新格式拒绝未解锁现场");Throws(()=>DemoSaveStore.Save(invalid),"保存前校验关卡边界");Check(DemoSaveStore.List().Any(e=>e.id==invalid.id&&e.error!=null),"无效档案仍能管理");
   Directory.CreateDirectory("Docs/Validation/Progression");File.WriteAllText("Docs/Validation/Progression/关卡进度规则验收.txt","PASS "+count+" checks: six-level sequential progress, guarded entry/save, fail/abandon, low-score win, replay, persistence, independent profiles, v1 level4/5/6 migration, v3 module checkpoint preservation, four-level v2 profile compatibility, backups, invalid saves.\n");Debug.Log("PROGRESSION_CHECKS_OK "+count);
  }finally{DemoSaveStore.TestDirectory=previous;DemoSession.Profile=profile;}
 }
 public static void ValidateAndBuild(){Run();DemoSaveChecks.Run();DemoSixPaidPlaythrough.Run();var report=File.ReadAllText("Docs/Validation/SixPaidPlaythrough.txt");if(System.Text.RegularExpressions.Regex.Matches(report,@"L[1-6] PASS").Count!=6||report.Contains(" FAIL"))throw new Exception("六关付费指令通关回归失败");DemoBuilder.ValidateAndBuildWindows();}
}
