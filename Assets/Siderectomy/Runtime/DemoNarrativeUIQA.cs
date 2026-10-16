using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace Siderectomy {
 public sealed class DemoNarrativeUIQA:MonoBehaviour {
  static bool running;string directory;
  public static void TryStart(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--narrative-ui-check");if(i<0||i+1>=args.Length||running)return;
   running=true;var qa=new GameObject("NarrativeUIQA").AddComponent<DemoNarrativeUIQA>();DontDestroyOnLoad(qa.gameObject);qa.directory=args[i+1];Directory.CreateDirectory(qa.directory);
   DemoSaveStore.TestDirectory=Path.Combine(qa.directory,"TestSaves");Application.logMessageReceived+=qa.Log;qa.StartCoroutine(qa.Run());
  }
  void Log(string text,string trace,LogType kind){if(kind!=LogType.Error&&kind!=LogType.Exception)return;File.WriteAllText(Path.Combine(directory,"FAIL.txt"),text+"\n"+trace);Application.Quit(1);}
  void Check(bool v,string text){if(!v)throw new Exception("NARRATIVE_UI: "+text);}
  IEnumerator Click(string name){yield return null;Canvas.ForceUpdateCanvases();var b=FindObjectsOfType<Button>().FirstOrDefault(x=>x.name==name&&x.gameObject.activeInHierarchy);Check(b!=null,"按钮缺失："+name);var e=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,b.transform.TransformPoint(((RectTransform)b.transform).rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);Check(hits.Count>0&&(hits[0].gameObject==b.gameObject||hits[0].gameObject.transform.IsChildOf(b.transform)),"按钮被遮挡："+name);ExecuteEvents.Execute(b.gameObject,e,ExecuteEvents.pointerClickHandler);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+".png"),tex.EncodeToPNG());Destroy(tex);}
  IEnumerator Until(string cursor,int max=250){
   for(int i=0;i<max&&DemoSession.Profile.story.cursor!=cursor&&DemoNarrative.Busy(DemoSession.Profile.story);i++){
    var n=DemoStoryBook.Load().Find(DemoSession.Profile.story.cursor);if(n.choices!=null&&n.choices.Length>0)yield return Click("Choice_"+n.choices[0].id);else yield return Click("DialogueNext");yield return null;
   }
   if(!string.IsNullOrEmpty(cursor))Check(DemoSession.Profile.story.cursor==cursor,"未抵达："+cursor);
  }
  IEnumerator Run(){
   yield return null;
   DemoSession.Profile=DemoSaveStore.Create("剧情验收");SceneManager.LoadScene("Office");yield return null;yield return null;
   Check(DemoSession.Profile.story.cursor=="intro.welcome","办公室欢迎剧情未播");
   yield return Shot("01_办公室开场");yield return Until("intro.mood");yield return Shot("02_办公室选项");
   Check(!FindObjectsOfType<Text>().Any(x=>x.name=="Speaker"&&x.text.Contains("{player}")),"玩家名字占位符未替换");
   bool blocked=false;try{DemoSession.EnterSimulation();}catch(InvalidOperationException){blocked=true;}Check(blocked,"剧情可被终端绕过");
   yield return Click("Choice_accept");yield return Until("");
   DemoSession.Profile.highestUnlockedLevel=6;DemoSession.Profile.currentLevel=6;DemoSaveStore.Save(DemoSession.Profile);DemoSession.EnterSimulation();yield return null;yield return null;
   yield return Until("safety.ask");yield return Shot("03_第六关双重确认");yield return Click("Choice_continue");yield return null;yield return Click("DialogueNext");yield return null;yield return Click("Choice_cancel");yield return null;yield return Click("DialogueNext");yield return null;
   Check(!DemoNarrative.Has(DemoSession.Profile.story,"unsafe_confirmed"),"取消被记录为危险执行");
   var p=DemoSaveStore.Create("太阳系剧情验收");p.highestUnlockedLevel=6;p.currentLevel=4;p.story.completed.Add("office.intro");DemoSaveStore.Save(p);DemoSession.Profile=p;DemoSession.EnterSimulation();yield return null;yield return null;
   var view=FindObjectOfType<DemoMissionView>();view.Simulation.BeginSolarStory();yield return null;yield return null;
   yield return Until("solar.ask");Check(view.CommunicationsOpen,"剧情未自动展开通讯");view.HandlePanelShortcut(KeyCode.O);Check(!view.CommunicationsOpen&&DemoSession.Profile.story.cursor=="solar.ask","O收起推进或丢失了剧情");yield return Click("NPCTab");Check(view.CommunicationsOpen,"NPC未恢复待处理剧情");yield return Click("FoldStory");Check(!view.CommunicationsOpen,"剧情收起按钮无效");yield return Click("NPCTab");yield return Shot("04_太阳系选择");yield return Click("Choice_confirm");yield return null;yield return Click("DialogueNext");yield return null;yield return null;Check(!view.CommunicationsOpen,"第一轮剧情结束未自动收起");
   Check(view.Simulation.SolarChoice=="B"&&view.Simulation.CanIntercept,"太阳系B未开放拦截");
   view.Simulation.Paused=false;view.Simulation.Advance(15.1);yield return null;yield return null;
   Check(DemoSession.Profile.story.activeTrigger=="mission.resources","15秒后未接第二轮");
   yield return Shot("05_资源申请剧情");yield return Click("DialogueNext");yield return null;yield return null;Check(view.Simulation.CanApply,"第二轮后资源申请未开放");Check(!view.CommunicationsOpen,"第二轮剧情结束未自动收起");
   view.Simulation.Over=true;view.Simulation.Won=true;DemoSession.RecordVictory(view.Simulation);SceneManager.LoadScene("Office");yield return null;yield return null;
   yield return Until("post4.report");yield return Click("Choice_yes");yield return null;yield return Click("DialogueNext");yield return null;
   Check(DemoSession.Profile.story.cursor=="post4.reportchoice","上报关键选项缺失");
   var loaded=DemoSaveStore.Load(DemoSession.Profile.id);Check(loaded.story.cursor=="post4.reportchoice","未保存中途选择");
   yield return Shot("06_异常上报与存档");yield return Click("Choice_report");yield return Until("");
   Check(DemoNarrative.Has(DemoSession.Profile.story,"reported")&&DemoSession.Profile.story.completed.Contains("office.clear.4"),"上报/办公室结束记录缺失");
   var first=DemoSaveStore.Create("首关扫描显示验收");first.story.completed.Add("office.intro");foreach(var id in new[]{"tutorial.1.0","tutorial.1.1","tutorial.1.2"})first.story.completed.Add(id);DemoSession.Profile=first;DemoSaveStore.Save(first);DemoSession.EnterSimulation();yield return null;yield return null;
   view=FindObjectOfType<DemoMissionView>();var scan=view.Simulation;Check(scan.Res[2]==50,"第一关初始信息不是50");scan.TutorialStep=2;scan.Carriers["dust"]=1;Check(scan.LaunchLoadout("probe","dust"),"无法执行真实扫描");scan.Paused=false;scan.Advance(scan.Missions.Last().total+.1);yield return null;yield return null;yield return null;
   Check(scan.GlobalProbe&&DemoNarrative.Busy(DemoSession.Profile.story)&&DemoSession.Profile.story.activeTrigger=="tutorial.1.3","未在扫描后触发卡农引导");foreach(var id in new[]{"p0","p1","p2","m0"})Check(GameObject.Find("Art_"+id)!=null&&GameObject.Find("Name_"+id)!=null,"剧情中天体图像/名称不可见："+id);double stopped=scan.Time;yield return null;yield return null;Check(scan.Time==stopped,"引导期间模拟未暂停");yield return Shot("07_扫描完成_卡农引导星球可见");yield return Until("");
   view.SelectBody("m0");view.EnterLocalView();yield return null;yield return null;yield return null;var starMap=FindObjectsOfType<DemoStarMap>().First(x=>!x.Compact);var size=starMap.rectTransform.rect.size;Check(starMap.LocalBodyId=="m0"&&Vector2.Distance(starMap.Positions["m0"],new Vector2(size.x*.5f,-size.y*.5f))<1,"局部卫星未居中");Check(GameObject.Find("Art_m0").GetComponent<Image>().rectTransform.sizeDelta.x>=128,"局部卫星未放大");yield return Shot("08_局部卫星_居中放大");view.ExitLocalView();yield return null;yield return null;yield return null;Check(GameObject.Find("Art_m0").GetComponent<Image>().rectTransform.sizeDelta.x<30,"退出局部未恢复尺寸");
   File.WriteAllText(Path.Combine(directory,"PASS.txt"),"PASS 剧情收展、选项与分支；第一关真实扫描完成后卡农引导期间星球图片和名称持续可见、模拟暂停；局部卫星居中放大与返回总览尺寸恢复。独立测试存档，战斗完成使用夹具。");Application.Quit(0);
  }
 }
}
