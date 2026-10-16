using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Siderectomy {
 public sealed class DemoIntegrationCheck : MonoBehaviour {
  static bool started;string directory;bool failed;
  public static void TryStart(PrototypeApp app){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--demo-check");if(started||i<0||i+1>=args.Length)return;started=true;Application.runInBackground=true;var go=new GameObject("DemoIntegrationCheck");DontDestroyOnLoad(go);var check=go.AddComponent<DemoIntegrationCheck>();check.directory=args[i+1];Directory.CreateDirectory(check.directory);Application.logMessageReceived+=check.OnLog;check.StartCoroutine(check.Run());}
  void OnLog(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){failed=true;File.AppendAllText(Path.Combine(directory,"运行错误.txt"),text+"\n"+stack+"\n");Application.Quit(1);}}
  static void Require(bool b,string s){if(!b)throw new Exception("DEMO_UI: "+s);}
  static void Click(string name){Canvas.ForceUpdateCanvases();var go=GameObject.Find(name);Require(go!=null,"缺少 "+name);DemoFlowCheck.Reveal(go);var button=go.GetComponent<Button>();Require(button!=null&&button.interactable,"不可点击 "+name);var rect=(RectTransform)go.transform;
   // Reveal controls in their scroll containers before performing the real raycast click.
   foreach(var scroll in go.GetComponentsInParent<ScrollRect>()){if(!scroll.vertical)continue;var point=scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));var bounds=scroll.viewport.rect;float offset=point.y<bounds.yMin+18?bounds.yMin+18-point.y:point.y>bounds.yMax-18?bounds.yMax-18-point.y:0;scroll.content.anchoredPosition+=new Vector2(0,offset);Canvas.ForceUpdateCanvases();}
   var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Require(hits.Count>0&&(hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"按钮被遮挡 "+name);ExecuteEvents.Execute(go,pointer,ExecuteEvents.pointerClickHandler);}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var camera=Camera.main;var canvas=FindObjectOfType<Canvas>();var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var target=new RenderTexture(1600,900,24);camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),pixels.EncodeToPNG());camera.targetTexture=oldTarget;RenderTexture.active=oldActive;canvas.renderMode=mode;canvas.worldCamera=oldCamera;Destroy(pixels);target.Release();Destroy(target);yield return null;}
  IEnumerator Run(){yield return null;yield return null;var view=FindObjectOfType<DemoMissionView>();Require(view!=null,"局内视图缺失");
   for(int n=1;n<=4;n++){Click("Level"+n);yield return null;yield return null;Require(view.Simulation.Level.number==n,"关卡切换");yield return Capture("第"+n+"关_初始");}
   Click("Tab_facility");yield return null;Click("Action_facility_energy");yield return null;Require(view.Simulation.Jobs.Count==1,"生产按钮未创建任务");Click("Pause");view.Simulation.Advance(12);view.Simulation.Paused=true;view.Refresh();yield return null;Require(view.Simulation.Facilities["energy"]==1,"建设没有完成");
   Click("Tab_carrier");yield return null;Click("Action_carrier_dust");view.Simulation.Paused=false;view.Simulation.Advance(8);view.Simulation.Paused=true;view.Refresh();yield return null;Require(view.Simulation.Carriers["dust"]==1,"载体制造失败");
   Click("Tab_operation");yield return null;yield return null;
   var dropdown=GameObject.Find("OperationSelect").GetComponent<Dropdown>();dropdown.Show();yield return null;Require(GameObject.Find("Dropdown List")!=null,"下拉菜单没有展开");yield return Capture("作业选择菜单");dropdown.Hide();yield return new WaitForSecondsRealtime(.3f);
   Click("Launch");Require(view.Simulation.Missions.Count==1,"探测投送失败");yield return Capture("第四关_投送界面");
   var flight=view.Simulation.Missions[0];Click("QTE_"+flight.id);yield return null;Require(view.Simulation.Paused,"校准没有暂停战场");yield return Capture("投送校准");Click("LockQTE");Require(flight.qte,"校准没有记录");yield return null;
   view.Simulation.Paused=false;view.Simulation.Advance(20);view.Simulation.Paused=true;view.Refresh();yield return null;Require(view.Simulation.GlobalProbe,"探测未完成");
   // Screenshot-only fixture reveals detail; runtime controls above use ordinary paid commands.
   view.Simulation.Find("p2").recon=true;view.Simulation.Revision++;view.Refresh();yield return null;yield return Capture("第四关_地球中心");
   var nodeScroll=GameObject.Find("NodeScroll").GetComponent<ScrollRect>();nodeScroll.verticalNormalizedPosition=0;yield return null;Click("Node_p2n24");Require(view.Simulation.TargetNode=="p2n24","滚动后无法选中最后一个中心");yield return null;yield return Capture("地球末尾中心选中");
   foreach(var graphic in FindObjectsOfType<Graphic>())Require(graphic.GetComponent<CanvasRenderer>()!=null,"UI缺少CanvasRenderer");
   var hudIcons=Resources.Load<DemoHudIcons>("HUD图标配置_代替换");Require(hudIcons!=null&&hudIcons.slots.Length==16,"HUD图标配置未导入");
   Click("HUD_carrier_dust");yield return null;Require(GameObject.Find("InventoryDescription")!=null,"库存详情没有打开");Click("GoInventoryProduction");yield return null;
   Click("Action_carrier_dust");view.Simulation.Paused=false;view.Simulation.Advance(8);view.Simulation.Paused=true;view.Refresh();yield return null;
   Require(view.Simulation.Carriers["dust"]==1,"第二次制造载体未入库");
   Click("Tab_attackModule");yield return null;double weaponBefore=view.Simulation.Res[3];Click("Action_attackModule_surface");yield return null;
   Require(view.Simulation.AttackModules["surface"]==1&&view.Simulation.Res[3]<weaponBefore,"预装配未扣资源或入库");
   Require(GameObject.Find("HUD_attackModule_surface").transform.Find("Quantity").GetComponent<Text>().text=="1","模块HUD没有同步库存");
   Require(GameObject.Find("HUD_carrier_dust").transform.Find("Quantity").GetComponent<Text>().text=="1","载体HUD没有同步库存");
   yield return Capture("物资储备HUD");Click("HUD_attackModule_surface");yield return null;yield return Capture("物资储备详情");Click("CloseInventory");yield return null;
   double weaponPrepared=view.Simulation.Res[3];view.QuickOperation("surface");yield return null;Click("Launch");yield return null;
   Require(view.Simulation.AttackModules["surface"]==0&&view.Simulation.Carriers["dust"]==0,"投送没有扣库存");Require(view.Simulation.Res[3]==weaponPrepared,"预装模块被重复收费");
   Require(GameObject.Find("HUD_attackModule_surface").transform.Find("Quantity").GetComponent<Text>().text=="0","发射后HUD没有扣库存");
   Click("Settings");yield return null;yield return Capture("第四关_调参");Click("CancelSettings");yield return null;Click("Help");yield return null;yield return Capture("操作说明");Click("CloseHelp");
   if(!failed)File.WriteAllText(Path.Combine(directory,"界面验收.txt"),"PASS 1–4关切换、生产建设、载体制造、作业下拉菜单、探测发射、投送校准、轨迹揭示、中心列表滚动与末尾中心选中、设置与说明弹窗、16项库存图标、库存详情跳转、预装配入库、发射扣载体与模块库存、无重复扣费、HUD同步刷新；按钮经过UI射线遮挡检查。\n详情截图使用侦察揭示夹具。\n");Application.Quit(failed?1:0);
  }
 }
}
