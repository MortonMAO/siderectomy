using System;
using Siderectomy;
using UnityEngine;
using UnityEditor;
public static class DemoOrbitChecks {
 static void Check(bool value,string message){if(!value)throw new Exception("ORBIT_CHECK: "+message);}
 static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
 [MenuItem("裁星计划/验证椭圆投送轨迹")]
 public static void Run(){
  var bounds=new Rect(12,-753,1226,741);var origin=new Vector2(300,-720);
  var targets=new[]{new Vector2(650,-380),new Vector2(1150,-690),new Vector2(120,-200),new Vector2(300,-60),new Vector2(900,-720),new Vector2(305,-720)};
  foreach(var target in targets){
   float minor=DemoTransferOrbit.MinorRadius(origin,target,bounds),major=Vector2.Distance(origin,target)*.5f;
   var center=(origin+target)*.5f;var u=(origin-target).normalized;var n=new Vector2(-u.y,u.x);
   Check(DemoTransferOrbit.FlightPoint(origin,target,0,minor)==origin,"起点错误");
   Check(DemoTransferOrbit.FlightPoint(origin,target,1,minor)==target,"终点错误");
   Vector2 previous=origin-center;
   for(int i=1;i<=1024;i++){
    var point=DemoTransferOrbit.GeometryPoint(origin,target,i/1024f,minor);var radial=point-center;
    Check(Cross(previous,radial)>=-.001f,"轨迹方向不是逆时针");
    Check(point.x>=bounds.xMin-1&&point.x<=bounds.xMax+1&&point.y>=bounds.yMin-1&&point.y<=bounds.yMax+1,"轨迹越出星图");
    float x=Vector2.Dot(radial,u)/major,y=Vector2.Dot(radial,n)/minor;
    Check(Mathf.Abs(x*x+y*y-1)<.002f,"轨迹不满足椭圆方程");previous=radial;
   }
   float eccentricity=Mathf.Sqrt(1-minor*minor/(major*major)),previousAngle=-1;
   for(int i=1;i<100;i++){
    float progress=i/100f;var point=DemoTransferOrbit.FlightPoint(origin,target,progress,minor)-center;
    float angle=Mathf.Atan2(Vector2.Dot(point,n)/minor,Vector2.Dot(point,u)/major);
    Check(angle>previousAngle,"运动发生倒退");
    Check(Mathf.Abs(angle-eccentricity*Mathf.Sin(angle)-progress*Mathf.PI)<.0001f,"开普勒时间映射错误");previousAngle=angle;
   }
  }
  var p=new Vector2(10,-10);Check(DemoTransferOrbit.FlightPoint(p,p,.5f,0)==p,"重合目标产生异常");
  float b=DemoTransferOrbit.MinorRadius(origin,targets[0],bounds);
  Check(Vector2.Distance(origin,DemoTransferOrbit.FlightPoint(origin,targets[0],.1f,b))>Vector2.Distance(targets[0],DemoTransferOrbit.FlightPoint(origin,targets[0],.9f,b)),"近点快远点慢未生效");
  RunCenterRoutes();Debug.Log("ORBIT_GEOMETRY_PASS");
 }
 static void RunCenterRoutes(){
  var origin=new Vector2(1190,-720);var center=new Vector2(637.5f,-382.5f);var radii=new Vector2(537.5f,290.7f);var bounds=new Rect(12,-753,1226,741);
  for(int ring=1;ring<=8;ring++)for(int direction=0;direction<16;direction++){
   float angle=direction*Mathf.PI/8,f=ring/8f;var target=center+new Vector2(Mathf.Cos(angle)*radii.x*f,Mathf.Sin(angle)*radii.y*f);
   var route=new DemoOrbitRoute(origin,target,center,radii,bounds,f,28);Check(route.GeometryPoint(0)==origin,"基地起点错误");Check(Vector2.Distance(route.Arrival,target)<.001f,"目标位置错误");
   Vector2 previous=new Vector2((origin.x-center.x)/radii.x,(origin.y-center.y)/radii.y);
   for(int i=1;i<=1024;i++){
    var point=route.GeometryPoint(i/1024f);var radial=new Vector2((point.x-center.x)/radii.x,(point.y-center.y)/radii.y);
    Check(Cross(previous,radial)>=-.00001f,"绕星系中心的方向发生倒退");Check(Vector2.Distance(point,center)>=27.99f,"航线穿越恒星禁入区");
    Check(point.x>=bounds.xMin-.1f&&point.x<=bounds.xMax+.1f&&point.y>=bounds.yMin-.1f&&point.y<=bounds.yMax+.1f,"绕行超出地图");previous=radial;
   }
   var outer=route.GeometryPoint(.25f)-center;Check(Mathf.Abs(new Vector2(outer.x/radii.x,outer.y/radii.y).magnitude-1)<.0001f,"外侧巡航不贴合绘制轨道");
   var inner=route.GeometryPoint(.8f)-center;Check(Mathf.Abs(new Vector2(inner.x/radii.x,inner.y/radii.y).magnitude-f)<.0001f,"目标巡航不贴合绘制轨道");
   foreach(float join in new[]{.15f,.38f,.68f,.9f}){const float delta=.0001f;Vector2 a=route.GeometryPoint(join)-route.GeometryPoint(join-delta),b=route.GeometryPoint(join+delta)-route.GeometryPoint(join);Check(Vector2.Angle(a,b)<2,"轨道连接切线不连续");}
   previous=new Vector2((origin.x-center.x)/radii.x,(origin.y-center.y)/radii.y);
   for(int i=1;i<=128;i++){var point=route.FlightPoint(i/128f);var radial=new Vector2((point.x-center.x)/radii.x,(point.y-center.y)/radii.y);Check(Cross(previous,radial)>=-.00001f&&Vector2.Distance(point,center)>=27.99f,"时间映射违反方向或避让");previous=radial;}
  }
  var stellar=new DemoOrbitRoute(origin,center,center,radii,bounds,.125f,28);Check(Mathf.Abs(Vector2.Distance(stellar.Arrival,center)-28)<.001f,"恒星目标未停在外缘");
  Debug.Log("CENTER_ORBIT_ROUTE_PASS 128个目标方位，基地右下角、外侧与目标椭圆巡航、逆时针及恒星避让");
 }
 public static void Build(){Run();DemoBuilder.ValidateAndBuildWindows();}
}

