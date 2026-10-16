using System;
using System.IO;
using System.Globalization;
using System.Linq;
using Siderectomy;
using UnityEditor;
using UnityEngine;
public static class DemoProfileCreationChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception("PROFILE_CREATION: "+message);}
 [MenuItem("裁星计划/验证双阶段建档与角色名")]
 public static void Run(){
  Check(DemoSaveStore.DefaultPlanName(new DateTimeOffset(2026,10,15,12,30,0,TimeSpan.FromHours(8)))=="Oct15-1230号计划","默认名称格式错误");
  Check(DemoSaveStore.DefaultPlanName(new DateTimeOffset(2026,10,14,20,30,0,TimeSpan.Zero))=="Oct15-0430号计划","北京时间跨日错误");
  var culture=CultureInfo.CurrentCulture;
  try{CultureInfo.CurrentCulture=new CultureInfo("zh-CN");Check(DemoSaveStore.DefaultPlanName(new DateTimeOffset(2026,1,3,1,2,0,TimeSpan.FromHours(8)))=="Jan03-0102号计划","月份缩写受系统语言影响");}finally{CultureInfo.CurrentCulture=culture;}
  string previous=DemoSaveStore.TestDirectory;
  try{
   DemoSaveStore.TestDirectory=Path.GetFullPath("Temp/ProfileCreationChecks_"+Guid.NewGuid().ToString("N"));
   bool invalid=false;try{DemoSaveStore.Create("测试计划","  ");}catch(ArgumentException){invalid=true;}
   Check(invalid&&!Directory.Exists(DemoSaveStore.DirectoryPath),"空角色名生成了半成品档案");
   var p=DemoSaveStore.Create("  Oct15-1230号计划  ","  冻橙  ");var loaded=DemoSaveStore.Load(p.id);
   Check(loaded.name=="Oct15-1230号计划"&&loaded.characterName=="冻橙","存档名与角色名未分离/持久化");
   Check(DemoSaveStore.List().Single().name==loaded.name,"列表错误地显示角色名");
   Check(loaded.DialogueText("欢迎回来，{player}")=="欢迎回来，冻橙"&&loaded.DialogueText("xxx / （玩家ID）")=="冻橙 / 冻橙","剧情使用了存档名");
   p.characterName="xxx{player}";Check(p.DialogueText("欢迎回来，{player}")=="欢迎回来，xxx{player}","角色名中的占位符被再次替换");
   var legacy=new DemoProfile{name="旧档称呼"};DemoProgression.Normalize(legacy);Check(legacy.CharacterName=="旧档称呼","旧档称呼被覆盖");
   var book=DemoStoryBook.Load();var state=new DemoStoryState();DemoNarrative.Start(state,book,"office.intro");Check(state.cursor=="intro.welcome","开场缺少欢迎");
   DemoNarrative.Step(state,book);Check(state.cursor=="intro"&&book.Find(state.cursor).text=="……休息日没睡好，早上有点晕。","没有接回截图中的原台词");
   DemoNarrative.Step(state,book);Check(state.cursor=="intro.ai","后续剧情被改动");
  }finally{DemoSaveStore.TestDirectory=previous;}
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/ProfileCreation.txt","PASS 北京时间英文月份命名、跨日、语言无关、双名字隔离与持久化、空角色不建档、列表名称、旧档称呼、占位符不递归、欢迎→原开场→后续剧情。");
  Debug.Log("PROFILE_CREATION_CHECK_PASS");
 }
 public static void Build(){Run();DemoNarrativeChecks.Run();DemoProgressionChecks.Run();DemoBuilder.ValidateAndBuildWindows();}
}
