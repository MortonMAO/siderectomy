using System;
using System.IO;
using System.Linq;
using Siderectomy;
using UnityEngine;
using UnityEditor;
public static class DemoNarrativeChecks {
 static void Check(bool v,string text){if(!v)throw new Exception("NARRATIVE_CHECK: "+text);}
 static DemoProfile Profile(){var p=new DemoProfile{highestUnlockedLevel=6};DemoProgression.Normalize(p);return p;}
 static void Pick(DemoStoryState s,DemoStoryBook b,string id){DemoNarrative.Step(s,b,id);}
 [MenuItem("裁星计划/验证剧情与结局记录")]
 public static void Run(){
  var b=DemoStoryBook.Load();b.Validate();Check(b.nodes.Length>700,"缺少文案");
  var p=Profile();var s=p.story;
  Check(DemoNarrative.Start(s,b,"mission.six.safety"),"安全剧情未启动");
  DemoNarrative.Step(s,b);Pick(s,b,"continue");DemoNarrative.Step(s,b);Pick(s,b,"cancel");
  Check(!DemoNarrative.Has(s,"unsafe_confirmed"),"取消误入第三结局");
  s=new DemoStoryState();DemoNarrative.Start(s,b,"mission.six.safety");DemoNarrative.Step(s,b);Pick(s,b,"continue");DemoNarrative.Step(s,b);Pick(s,b,"confirm");
  Check(DemoNarrative.Has(s,"unsafe_confirmed"),"双确认未记录");
  var rules=DemoRuleBook.Load();
  var four=new DemoSimulation(DemoLevel.Defaults()[3],rules);four.Escaped=3;four.EscapedPop=24;four.Over=true;four.Won=true;
  DemoNarrative.Record(p,four);DemoNarrative.Set(p.story,"rosetta_available","rosetta_overwrite");
  Check(!DemoNarrative.Has(p.story,"escape_three_levels"),"同关三艘被算成三关");
  var five=new DemoSimulation(DemoLevel.Defaults()[4],rules);five.Escaped=1;five.Over=true;five.Won=false;DemoNarrative.Record(p,five);
  Check(!DemoNarrative.Has(p.story,"escape_clear_5"),"失败局混入通关统计");
  five.Won=true;DemoNarrative.Record(p,five);
  var six=new DemoSimulation(DemoLevel.Defaults()[5],rules);six.Over=true;six.Won=true;six.Ships.Add(new DemoShip{id=1,pop=8});DemoNarrative.Record(p,six);
  Check(p.story.ending!="zero","离港算作成功外逃");
  six.Escaped=1;DemoNarrative.Record(p,six);Check(p.story.ending=="zero","三关外逃未解锁暗线");
  DemoNarrative.Set(p.story,"reported");DemoNarrative.RefreshEnding(p.story);Check(p.story.ending=="normal","上报未排除暗线");
  DemoNarrative.Set(p.story,"unsafe_confirmed");DemoNarrative.RefreshEnding(p.story);Check(p.story.ending=="death","第三结局优先级错误");
  var replay=new DemoSimulation(DemoLevel.Defaults()[4],rules);replay.Over=true;replay.Won=true;DemoNarrative.Record(p,replay);
  Check(p.story.attempts.Count(x=>x.level==5&&x.canonical)==1,"重玩覆盖首通");
  var snap=four.Capture();Check(DemoSimulation.Restore(snap,rules).RunId==four.RunId,"读档生成新局重复计数");
  var old=new DemoProfile{version=1,currentLevel=4};DemoProgression.Normalize(old);Check(old.story!=null&&old.story.decisions.Count==0,"旧档迁移编造选择");
  string previous=DemoSaveStore.TestDirectory;
  try{
   DemoSaveStore.TestDirectory=Path.Combine(Path.GetTempPath(),"SiderectomyNarrativeChecks",Guid.NewGuid().ToString("N"));
   var saved=DemoSaveStore.Create("剧情测试");saved.highestUnlockedLevel=6;
   DemoNarrative.Start(saved.story,b,"mission.solar");DemoNarrative.Step(saved.story,b);
   DemoSaveStore.Save(saved);var loaded=DemoSaveStore.Load(saved.id);Check(loaded.story.cursor=="solar.ask","中途读档跳过选择");
   DemoNarrative.Step(loaded.story,b,null,true);Check(loaded.story.cursor=="solar.hesitate","10秒犹豫分支错误");
   loaded.checkpoint=four.Capture();loaded.hasCheckpoint=true;loaded.currentLevel=4;
   DemoNarrative.QueueVictory(loaded,four,true);DemoNarrative.QueueVictory(loaded,four,false);
   Check(loaded.story.pending.Count(x=>x=="office.clear.4")==1,"重复结算重复排队");
   DemoSaveStore.Save(loaded);loaded=DemoSaveStore.Load(loaded.id);Check(loaded.story.pending.Contains("office.clear.4"),"办公室队列未落盘");
  }finally{DemoSaveStore.TestDirectory=previous;}
  Directory.CreateDirectory("Docs/Validation");
  File.WriteAllText("Docs/Validation/Narrative.txt","PASS 配置闭合、安全双确认/取消、三关各外逃、同关多艘不累计、失败局排除、离港与逃出分离、首通与重玩分离、优先级、旧档迁移、中途读档、犹豫超时、办公室队列幂等。");
  Debug.Log("NARRATIVE_CHECK_PASS nodes="+b.nodes.Length);
 }
 public static void Build(){Run();DemoOrbitChecks.Run();DemoBuilder.ValidateAndBuildWindows();}
}
