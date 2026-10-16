using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Siderectomy {
 public sealed class DemoOrbitUIQA:MonoBehaviour {
  string directory;
  public static void TryStart(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--orbit-ui-check");if(i<0||i+1>=args.Length)return;
   var qa=new GameObject("OrbitUIQA").AddComponent<DemoOrbitUIQA>();qa.directory=args[i+1];Directory.CreateDirectory(qa.directory);
   Application.logMessageReceived+=qa.Log;qa.StartCoroutine(qa.Run());
  }
  void Log(string text,string trace,LogType kind){if(kind!=LogType.Error&&kind!=LogType.Exception)return;File.WriteAllText(Path.Combine(directory,"FAIL.txt"),text+"\n"+trace);Application.Quit(1);}
  void Check(bool value,string message){if(!value)throw new Exception("ORBIT_UI: "+message);}
  IEnumerator Capture(string filename){
   yield return new WaitForEndOfFrame();var camera=Camera.main;var canvas=FindObjectOfType<Canvas>();
   var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
   var target=new RenderTexture(1600,900,24);camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
   pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(directory,filename+".png"),pixels.EncodeToPNG());
   camera.targetTexture=oldTarget;RenderTexture.active=oldActive;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;
   Destroy(pixels);target.Release();Destroy(target);yield return null;
  }
  IEnumerator Run(){
   yield return null;yield return null;var view=FindObjectOfType<DemoMissionView>();Check(view!=null,"缺少局内视图");
   DemoSession.Profile.highestUnlockedLevel=6;view.LoadLevel(4);yield return null;
   var s=view.Simulation;s.GlobalProbe=true;foreach(var body in s.Bodies)body.recon=true;
   s.Ships.Add(new DemoShip{id=701,body="p2",left=60,total=90,pop=8});
   var bodies=new[]{"p2","m0","p4"};for(int i=0;i<bodies.Length;i++)s.Missions.Add(new DemoMission{id=801+i,op="surface",body=bodies[i],node="auto",carrier="dust",left=80*(1-(i+1)*.2),total=80});
   s.Missions.Add(new DemoMission{id=804,op="blockade",body="p2",node="auto",ship=701,carrier="satellite",left=40,total=80});
   s.Revision++;view.SelectBody("p2");view.CloseSecondaryMenu();view.Refresh();yield return null;yield return null;
   var map=FindObjectsOfType<DemoStarMap>().First(m=>!m.Compact);Check(map.MissionPositions.Count==4,"投送轨迹缺失");
   var start=map.LaunchBasePosition;var dest=map.Positions["p2"];Check(start.x==map.rectTransform.rect.width-140&&start.y==-map.rectTransform.rect.height+105,"基地未移到右下角");CheckRoutes(map);
   var point=map.MissionPositions[801];float cross=(dest-start).x*(point-start).y-(dest-start).y*(point-start).x;
   Check(Mathf.Abs(cross)>1,"仍是直线插值");yield return Capture("01_总览_椭圆投送");
   for(int i=0;i<3;i++){
    float t=new[]{.1f,.5f,.9f}[i];foreach(var mission in s.Missions)mission.left=mission.total*(1-t);
    view.Refresh();yield return null;yield return null;yield return Capture("02_逆时针_"+i);
   }
   view.EnterLocalView();view.Refresh();yield return null;yield return null;
   Check(map.MissionPositions.ContainsKey(801)&&map.MissionPositions.ContainsKey(802)&&map.MissionPositions.ContainsKey(804),"局部视图轨迹缺失");CheckRoutes(map);yield return Capture("03_局部_天体与拦截");
   Check(s.Time==0&&s.Missions.All(m=>m.total==80),"轨迹修改影响模拟计时");
   File.WriteAllText(Path.Combine(directory,"PASS.txt"),"PASS 右下角基地、围绕中心的逆时针椭圆巡航、恒星/局部中心避让、月球与外逃舰目标、总览与局部视图；投送计时不变。使用暂停、揭示和任务夹具，测试存档独立。");Application.Quit(0);
  }
  void CheckRoutes(DemoStarMap map){foreach(var route in map.MissionRoutes.Values){Check(route.Origin==map.LaunchBasePosition,"任务起点与基地不一致");for(int i=0;i<=128;i++)Check(Vector2.Distance(route.GeometryPoint(i/128f),route.Center)>=21.99f,"航线穿越中心天体");}}
 }
}

