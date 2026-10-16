using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Siderectomy {
 public sealed class DemoSixUIQA:MonoBehaviour {
  string directory;bool moduleCheck;
  public static void TryStart(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--module-ui-check");bool modules=i>=0;if(i<0)i=Array.IndexOf(args,"--six-ui-check");if(i<0||i+1>=args.Length)return;var qa=new GameObject("SixUIQA").AddComponent<DemoSixUIQA>();qa.moduleCheck=modules;qa.directory=args[i+1];Directory.CreateDirectory(qa.directory);Application.logMessageReceived+=qa.OnLog;qa.StartCoroutine(qa.Run());}
  void OnLog(string m,string trace,LogType t){if(t!=LogType.Exception&&t!=LogType.Error)return;File.AppendAllText(Path.Combine(directory,"Errors.txt"),m+"\n"+trace);Application.Quit(1);}
  void Check(bool v,string m){if(!v)throw new Exception("SIX_UI: "+m);}
  void Click(string name){Canvas.ForceUpdateCanvases();var go=GameObject.Find(name);Check(go!=null,"缺少"+name);DemoFlowCheck.Reveal(go);var b=go.GetComponent<Button>();Check(b!=null&&b.interactable,"按钮不可用"+name);var rect=(RectTransform)go.transform;var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&(hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"按钮被遮挡"+name);ExecuteEvents.Execute(go,data,ExecuteEvents.pointerClickHandler);}
  IEnumerator Screenshot(string name){
   yield return new WaitForEndOfFrame();var camera=Camera.main;var canvas=FindObjectOfType<Canvas>();
   var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldTarget=camera.targetTexture;var active=RenderTexture.active;
   var target=new RenderTexture(1600,900,24);camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
   pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),pixels.EncodeToPNG());
   camera.targetTexture=oldTarget;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=oldCamera;Destroy(pixels);target.Release();Destroy(target);yield return null;
  }
  IEnumerator Run(){
   yield return null;yield return null;var view=FindObjectOfType<DemoMissionView>();Check(view!=null,"缺少关卡视图");DemoSession.Profile.highestUnlockedLevel=DemoProgression.LevelCount;DemoSaveStore.Save(DemoSession.Profile);view.Refresh();
   if(moduleCheck){yield return RunModules(view);yield break;}
   for(int i=1;i<=6;i++){Click("Level"+i);yield return null;Check(view.Simulation.Level.number==i,"关卡切换失败");}
   Click("Level3");yield return null;var s=view.Simulation;s.GlobalProbe=true;s.Find("p2").recon=true;s.Revision++;view.Refresh();view.EnterLocalView();yield return null;yield return null;
   Check(view.ReconDetails().Contains("硅基")&&view.ReconDetails().Contains("3段"),"详情未显示硅基信息链");yield return Screenshot("03_硅基信息链");
   Click("Level4");yield return null;s=view.Simulation;s.GlobalProbe=true;s.Revision++;s.Paused=false;s.Advance(181);s.Paused=true;view.Refresh();yield return null;
   Check(s.Ships.Count==1,"首艘未离港");yield return Screenshot("04_第一轮剧情");Click("SolarDialogueNext");yield return null;Click("SolarChoiceB");yield return null;
   s.Paused=false;s.Advance(15.1);s.Paused=true;view.Refresh();yield return null;yield return Screenshot("04_第二轮剧情");Click("SolarDialogueNext");yield return null;Check(s.CanApply,"申请未开放");
   Click("Tab_apply");yield return null;Check(GameObject.Find("Apply").GetComponent<Button>().interactable,"申请按钮未刷新");yield return Screenshot("04_申请资源开放");
   view.CloseSecondaryMenu();Click("DebugTools");yield return null;Click("DebugAllFeatures");yield return null;Check(s.DebugAllFeatures,"全开未生效");view.CloseSecondaryMenu();Click("DebugTools");yield return null;Click("DebugLevelFeatures");yield return null;Check(!s.DebugAllFeatures,"恢复权限未生效");
   Click("Level6");yield return null;s=view.Simulation;s.GlobalProbe=true;foreach(var b in s.Bodies)b.recon=true;s.Revision++;view.SelectBody("p1");view.EnterLocalView();view.Refresh();yield return null;yield return Screenshot("06_复合文明");
   File.WriteAllText(Path.Combine(directory,"PASS.txt"),"PASS 六关实际按钮切换、硅基详情与信息链、通讯栏两轮剧情、B选择、15秒等待、申请按钮开放、调试全开与恢复。截图使用侦察揭示及快进夹具。");Application.Quit(0);
  }
  void Press(GameObject go,RectTransform hit=null){
   Canvas.ForceUpdateCanvases();Check(go!=null,"控件缺失");var rect=hit??(RectTransform)go.transform;
   var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
   var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&(hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"控件被遮挡 "+go.name);
   ExecuteEvents.Execute(go,data,ExecuteEvents.pointerClickHandler);
  }
  IEnumerator Choose(string id,int index){
   var drop=GameObject.Find(id).GetComponent<Dropdown>();Press(drop.gameObject);yield return null;yield return null;
   var list=GameObject.Find("Dropdown List");Check(list!=null,"下拉未展开");var options=list.GetComponentsInChildren<Toggle>().Where(t=>t.gameObject.activeInHierarchy).ToArray();
   Check(index<options.Length,"选项缺失 "+id);Press(options[index].gameObject);yield return new WaitForSecondsRealtime(.2f);Check(drop.value==index,"下拉选择失败 "+id);
  }
  void ToggleModule(string id){var go=GameObject.Find(id);Check(go.GetComponent<Toggle>().interactable,"科技搭载不可交互");Press(go,(RectTransform)go.transform.Find("Box"));}
  IEnumerator RunModules(DemoMissionView view){
   Click("Level2");yield return null;var s=view.Simulation;s.GlobalProbe=true;foreach(var b in s.Bodies)b.recon=true;s.Revision++;view.SelectBody("p0");view.Refresh();
   Click("Tab_research");yield return null;Click("Action_research_stealth");s.Paused=false;s.Advance(35.1);s.Paused=true;view.Refresh();yield return null;
   Click("Tab_attackModule");yield return null;Click("Action_attackModule_surface");yield return null;Click("ModuleGroupTechnology");yield return null;
   Click("Action_attackModule_stealth");Click("Action_attackModule_virus");s.Paused=false;s.Advance(12.1);s.Paused=true;view.Refresh();yield return null;
   Check(s.TechnologyModules["stealth"]==1&&s.TechnologyModules["virus"]==1&&s.AttackModules["surface"]==1,"实际制造按钮未入库");
   yield return Screenshot("01_科技模块制造");
   Click("Tab_carrier");yield return null;Click("Action_carrier_satellite");s.Paused=false;s.Advance(15.1);s.Paused=true;view.Refresh();yield return null;
   Click("Tab_operation");yield return null;yield return Choose("CarrierSelect",1);
   yield return Choose("AttackSelect",1);ToggleModule("StealthModule");ToggleModule("VirusModule");view.Refresh();yield return null;
   Check(GameObject.Find("Launch").GetComponent<Button>().interactable,"组合发射未开放");yield return Screenshot("02_卫星组合搭载");
   Click("Tab_attackModule");yield return null;Click("Tab_operation");yield return null;Check(GameObject.Find("CarrierSelect").GetComponent<Dropdown>().value==1&&GameObject.Find("StealthModule").GetComponent<Toggle>().isOn&&GameObject.Find("VirusModule").GetComponent<Toggle>().isOn,"切换制造窗口丢失搭载");
   var draft=view.CaptureProgress();Check(draft.draftTechnologies.Length==2,"搭载草稿未保存");view.RestoreFromSave(draft);yield return null;
   Check(GameObject.Find("CarrierSelect").GetComponent<Dropdown>().value==1&&GameObject.Find("StealthModule").GetComponent<Toggle>().isOn&&GameObject.Find("VirusModule").GetComponent<Toggle>().isOn,"搭载草稿恢复失败");
   Click("Launch");yield return null;Check(view.Simulation.Missions.Count==1&&view.Simulation.Carriers["satellite"]==0,"实际发射按钮失效");
   s=view.Simulation;s.Paused=false;s.Advance(38.1);s.Paused=true;view.Refresh();yield return null;
   Check(s.Find("p0").pop==135&&s.Viruses.Count==1,"界面投送效果失效");
   Click("Level3");yield return null;s=view.Simulation;s.GlobalProbe=true;foreach(var b in s.Bodies)b.recon=true;s.Research["stealth"]=1;s.Revision++;view.SelectBody("p2");view.SelectNode(s.Body.nodes[1].id);
   view.CloseSecondaryMenu();Click("Tab_attackModule");yield return null;Click("ModuleGroupTechnology");yield return null;Click("Action_attackModule_infoInvasion");
   s.Paused=false;s.Advance(10.1);s.Paused=true;view.Refresh();s.Carriers["satellite"]=1;
   Click("Tab_operation");yield return null;yield return Choose("CarrierSelect",1);yield return Choose("AttackSelect",0);ToggleModule("InformationInvasionModule");view.Refresh();yield return null;Click("Launch");
   s.Paused=false;s.Advance(36.1);s.Paused=true;view.EnterLocalView();view.CloseSecondaryMenu();view.Refresh();yield return null;
   Check(Math.Abs(s.InformationFlow(s.Body)-1.0/3)<.001,"实际入侵未阻断链");yield return Screenshot("03_硅基链阻断");
   view.SetTab("operation");yield return null;yield return Choose("CarrierSelect",2);
   Check(GameObject.Find("CarrierCapacity").GetComponent<Text>().text.Contains("1科技槽"),"小行星科技槽显示错误");
   File.WriteAllText(Path.Combine(directory,"PASS.txt"),"PASS 实际研发与模块制造按钮、科技入库、载体先选与下拉、组合搭载、草稿存档恢复、发射、病毒持续效果、信息入侵红色链条、小行星独立插槽。扫描与侦察使用揭示夹具；硅基演示注入1个载体。");Application.Quit(0);
  }

 }
}
