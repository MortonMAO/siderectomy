using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace Siderectomy {
 public sealed partial class DemoMissionView {
  DemoLogFeed logFeed;Image communicator;GameObject conversationOptions;Text conversationText;bool conversationOpen;RectTransform storyOptions;int shownStoryPhase=-1;
  void OnSimulationMessage(string message){if(IsVisibleLog(message))logFeed.Push(message);}
  void BuildCommunications(){var area=PrototypeUi.Box(ui,"Communications",1250,650,350,250,Panel).rectTransform;PrototypeUi.Box(area,"Accent",0,0,350,3,new Color32(213,246,82,255));Label(area,"CommsTitle","COMMS / 通讯员",12,9,326,26,18,PrototypeUi.Accent);
   var portrait=Button(area,"Communicator","NPC\n形象占位",12,45,80,112,()=>SetConversation(!conversationOpen),19);communicator=portrait.GetComponent<Image>();conversationText=Label(area,"ConversationMessage","点击通讯员\n接入模拟对话",104,45,234,116,16);
   conversationOptions=PrototypeUi.Rect("ConversationOptions",area,12,170,326,42).gameObject;string[] options={"AAA","BBB","暂停键"};for(int i=0;i<3;i++){string option=options[i];Button(conversationOptions.transform,"Dialogue_"+option,option,i*110,0,103,40,()=>{if(option=="暂停键")OpenPauseMenu();else{conversationText.text=option=="AAA"?"通讯员：AAA 已收到。\n模拟链路运行正常。":"通讯员：BBB 已收到。\n等待你的下一步指令。";Simulation.Log("通讯员回应："+option);}},17);}
   storyOptions=PrototypeUi.Rect("SolarStoryOptions",area,12,170,326,44);storyOptions.gameObject.SetActive(false);
   Label(area,"CommsStatus","● 在线 · 点击形象可展开 / 收起对话",12,221,326,20,11,Muted);SetConversation(false);
  }
  void SetConversation(bool open){if(Simulation!=null&&Simulation.StoryPending){RefreshSolarDialogue();return;}conversationOpen=open;if(communicator==null)return;communicator.color=open?new Color32(106,87,159,255):new Color32(43,69,91,255);conversationOptions.SetActive(open);conversationText.text=open?"通讯员已接入。\n请选择一条对话指令。":"点击通讯员\n接入模拟对话";if(open&&Simulation!=null)Simulation.Log("通讯员已接入 · 对话选项已展开");}
  void RefreshSolarDialogue(){
   if(Simulation==null||storyOptions==null||shownStoryPhase==Simulation.SolarStoryPhase)return;
   shownStoryPhase=Simulation.SolarStoryPhase;Clear(storyOptions);
   storyOptions.gameObject.SetActive(Simulation.StoryPending);if(!Simulation.StoryPending){if(shownStoryPhase==5){conversationText.text="xxxxx\n15个游戏秒后再次接入通讯。";conversationOptions.SetActive(false);}if(shownStoryPhase==4){conversationText.text="xxxxx\n资源申请现已开放。";conversationOptions.SetActive(false);}return;}
   conversationOptions.SetActive(false);communicator.color=new Color32(106,87,159,255);
   conversationText.text=Simulation.SolarStoryPhase==3?rules.solarStory.second:rules.solarStory.first;
   if(Simulation.SolarStoryPhase==2){
    Button(storyOptions,"SolarChoiceA",rules.solarStory.optionA,0,0,160,42,()=>{Simulation.ChooseSolarEscape(true);RefreshSolarDialogue();Refresh();},14);
    Button(storyOptions,"SolarChoiceB",rules.solarStory.optionB,166,0,160,42,()=>{Simulation.ChooseSolarEscape(false);RefreshSolarDialogue();Refresh();},14);
   }else Button(storyOptions,"SolarDialogueNext",Simulation.SolarStoryPhase==1?"继续对话":"结束对话",0,0,326,42,()=>{Simulation.NextSolarDialogue();RefreshSolarDialogue();Refresh();},16);
  }
  void OpenDebugTools(){OpenModal();Label(dialog,"DebugTitle","功能权限调试",35,24,850,50,29,Gold);
   Label(dialog,"DebugNote","一键开启所有关卡功能（含研发前置和资源申请）。\n恢复本关权限：重新按关卡解锁、研发成果与剧情选择判定。\n不补资源、不重置库存，不撤销剧情或已有研发。",35,100,850,170,21,Muted);
   Button(dialog,"DebugAllFeatures","一键开启所有功能",35,305,400,60,()=>{Simulation.SetDebugFeatures(true);CloseModal();SetTab(tab);},21);
   Button(dialog,"DebugLevelFeatures","一键恢复本关应有功能",455,305,400,60,()=>{Simulation.SetDebugFeatures(false);CloseModal();SetTab(tab);},21);
   Button(dialog,"DebugClose","返回",35,420,260,55,CloseModal);
  }
  public void OpenPauseMenu(){OpenModal();Label(dialog,"PauseTitle","模拟进程已暂停",35,25,850,55,30,Gold);Label(dialog,"PauseDescription","当前关卡："+Simulation.Level.title+"\n保存会记录当前资源、队列、在途投送与文明状态。",35,95,850,75,20,Muted);
   Button(dialog,"ResetSimulation","重新模拟",35,203,850,66,()=>GuardPause(()=>{DemoSession.Ensure();DemoNarrativeSession.Record(Simulation);DemoSession.SaveProgress(null,Simulation.Level.number,Simulation.ApplyCount,Simulation.SecondEnding);InstallSimulation(new DemoSimulation(Simulation.Level,rules));}),23);
   Button(dialog,"AbandonSimulation","放弃模拟进程并退出",35,289,850,66,()=>GuardPause(()=>{DemoSession.Ensure();if(Simulation.Over&&Simulation.Won)DemoSession.RecordVictory(Simulation);else {DemoNarrativeSession.Record(Simulation);DemoSession.SaveProgress(null,Simulation.Level.number,Simulation.ApplyCount,Simulation.SecondEnding);}SceneManager.LoadScene("Office");}),23);
   Button(dialog,"SaveSimulation","保存模拟进程并退出",35,375,850,66,()=>GuardPause(()=>{if(Simulation.Over&&Simulation.Won)DemoSession.RecordVictory(Simulation);else {DemoNarrativeSession.Record(Simulation);DemoSession.SaveProgress(CaptureProgress(),Simulation.Level.number,Simulation.ApplyCount,Simulation.SecondEnding);}SceneManager.LoadScene("Office");}),23);
   Button(dialog,"ResumeSimulation","返回模拟",35,499,400,57,CloseModal,22);Label(dialog,"SaveError","",35,562,850,58,16,Gold);
  }
  void GuardPause(Action action){try{action();}catch(Exception ex){var error=dialog.Find("SaveError");if(error!=null)error.GetComponent<Text>().text="操作未完成，当前现场已保留："+ex.Message;}}
  public DemoSnapshot CaptureProgress(){var snapshot=Simulation.Capture();snapshot.Paused=modal.gameObject.activeSelf?beforeModal:Simulation.Paused;snapshot.localBodyId=map.LocalBodyId;snapshot.actionTab=tab;StoreLoadoutDraft();snapshot.operationKey=draftPrimary;snapshot.carrierKey=draftCarrier;snapshot.draftTechnologies=(string[])draftTechnology.Clone();snapshot.infoHide=Array.IndexOf(draftTechnology,"stealth")>=0;return snapshot;}
  public void RestoreFromSave(DemoSnapshot snapshot){DemoSession.Ensure();if(snapshot==null||snapshot.level==null||!DemoProgression.CanEnter(DemoSession.Profile,snapshot.level.number))throw new InvalidOperationException("存档中的关卡尚未解锁");InstallSimulation(DemoSimulation.Restore(snapshot,rules));Simulation.Paused=true;if(!string.IsNullOrEmpty(snapshot.localBodyId)&&Simulation.Find(snapshot.localBodyId)!=null)map.LocalBodyId=snapshot.localBodyId;draftPrimary=snapshot.operationKey=="infoVirus"?"infoInvasion":snapshot.operationKey??"";draftCarrier=snapshot.carrierKey??"dust";draftTechnology=snapshot.draftTechnologies??(snapshot.infoHide||snapshot.physicalHide?new[]{"stealth"}:new string[0]);SetTab(string.IsNullOrEmpty(snapshot.actionTab)?"facility":snapshot.actionTab);if(opDrop!=null){int carrier=Array.FindIndex(rules.carriers,r=>r.key==snapshot.carrierKey);carrierDrop.SetValueWithoutNotify(Math.Max(0,carrier));RebuildAttackOptions("");if(!string.IsNullOrEmpty(snapshot.operationKey))QuickOperation(snapshot.operationKey=="infoVirus"?"infoInvasion":snapshot.operationKey);var draft=snapshot.draftTechnologies??new string[0];infoHide.SetIsOnWithoutNotify(snapshot.infoHide||Array.IndexOf(draft,"stealth")>=0);invasionToggle.SetIsOnWithoutNotify(Array.IndexOf(draft,"infoInvasion")>=0);virusToggle.SetIsOnWithoutNotify(Array.IndexOf(draft,"virus")>=0);}Refresh();logFeed.Clear();logFeed.Push("局内存档已恢复 · 当前暂停，点击继续推进模拟");}
 }
}
