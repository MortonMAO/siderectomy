using UnityEngine;
namespace Siderectomy {
 // Coaxial orbit arcs joined by smooth radial transfers. +Y is up in map coordinates.
 public sealed class DemoOrbitRoute {
  public readonly Vector2 Origin,Arrival,Center,Radii;
  readonly Rect bounds;readonly float startAngle,sweep,startRadius,targetRadius,ringRadius,clearance;
  readonly float[] area=new float[129];
  public DemoOrbitRoute(Vector2 origin,Vector2 destination,Vector2 center,Vector2 radii,Rect mapBounds,float targetRing,float avoidRadius){
   Origin=origin;Center=center;Radii=new Vector2(Mathf.Max(1,radii.x),Mathf.Max(1,radii.y));bounds=mapBounds;clearance=Mathf.Max(0,avoidRadius);
   Vector2 start=Normalize(origin-center),end=Normalize(destination-center);startRadius=start.magnitude;startAngle=Mathf.Atan2(start.y,start.x);
   float endAngle=Vector2.Distance(destination,center)<clearance?startAngle+Mathf.PI*1.5f:Mathf.Atan2(end.y,end.x);
   sweep=Mathf.Repeat(endAngle-startAngle,Mathf.PI*2);if(sweep<Mathf.PI*.5f)sweep+=Mathf.PI*2;
   targetRadius=end.magnitude;if(Vector2.Distance(destination,center)<clearance)targetRadius=MinimumRadius(startAngle+sweep);
   ringRadius=targetRing>0?targetRing:Mathf.Max(targetRadius,MinimumRadius(startAngle+sweep));
   Arrival=center+Direction(startAngle+sweep)*targetRadius;
   float previous=startRadius*startRadius;
   for(int i=1;i<area.Length;i++){float radius=Normalize(GeometryPoint(i/128f)-Center).sqrMagnitude;area[i]=area[i-1]+(previous+radius)*.5f;previous=radius;}
  }
  Vector2 Normalize(Vector2 value){return new Vector2(value.x/Radii.x,value.y/Radii.y);}
  Vector2 Direction(float angle){return new Vector2(Radii.x*Mathf.Cos(angle),Radii.y*Mathf.Sin(angle));}
  float MinimumRadius(float angle){return clearance/Mathf.Max(.001f,Direction(angle).magnitude);}
  float MaximumRadius(Vector2 direction){
   float limit=float.PositiveInfinity;
   if(direction.x>.001f)limit=Mathf.Min(limit,(bounds.xMax-Center.x)/direction.x);else if(direction.x<-.001f)limit=Mathf.Min(limit,(bounds.xMin-Center.x)/direction.x);
   if(direction.y>.001f)limit=Mathf.Min(limit,(bounds.yMax-Center.y)/direction.y);else if(direction.y<-.001f)limit=Mathf.Min(limit,(bounds.yMin-Center.y)/direction.y);
   return Mathf.Max(0,limit);
  }
  public Vector2 GeometryPoint(float fraction){
   float t=Mathf.Clamp01(fraction);if(t<=0)return Origin;if(t>=1)return Arrival;
   float radius;
   if(t<.15f){float u=t/.15f;radius=Mathf.Lerp(startRadius,1,1-Mathf.Pow(1-u,3));}
   else if(t<.38f)radius=1;
   else if(t<.68f)radius=Mathf.Lerp(1,ringRadius,Smoother((t-.38f)/.3f));
   else if(t<.9f)radius=ringRadius;
   else radius=Mathf.Lerp(ringRadius,targetRadius,Smoother((t-.9f)/.1f));
   float angle=startAngle+sweep*t;Vector2 direction=Direction(angle);
   float lower=MinimumRadius(angle),upper=Mathf.Max(lower,MaximumRadius(direction));
   // Smooth the clipping shoulder instead of introducing a hard corner at the map border.
   float blend=.055f*Smoother(t/.04f)*Smoother((1-t)/.04f);
   if(blend>.000001f){float overlap=Mathf.Max(0,blend-Mathf.Abs(radius-upper));radius=Mathf.Min(radius,upper)-overlap*overlap/(4*blend);}
   radius=Mathf.Max(lower,radius);
   return Center+direction*radius;
  }
  static float Smoother(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
  public Vector2 FlightPoint(float progress){
   float t=Mathf.Clamp01(progress);if(t<=0)return Origin;if(t>=1)return Arrival;
   // Equal swept-area pacing: slower on the outer orbit, faster near the center.
   float wanted=t*area[128];int low=0,high=128;
   while(high-low>1){int middle=(low+high)/2;if(area[middle]<wanted)low=middle;else high=middle;}
   float span=area[high]-area[low];float f=span>.00001f?(wanted-area[low])/span:0;
   return GeometryPoint((low+f)/128f);
  }
 }
 // A visual transfer ellipse, not an N-body gravity solver. Coordinates use UI-local +Y up.
 public static class DemoTransferOrbit {
  public static float MinorRadius(Vector2 origin,Vector2 destination,Rect bounds){
   Vector2 axis=(origin-destination)*.5f;float major=axis.magnitude;
   if(major<.001f)return 0;
   Vector2 center=(origin+destination)*.5f,normal=new Vector2(-axis.y,axis.x)/major;
   float minor=Mathf.Min(major*.45f,140f);
   // Keep the CCW side inside the map without flipping the direction.
   for(int i=1;i<64;i++){
    float angle=Mathf.PI*i/64f;Vector2 baseline=center+axis*Mathf.Cos(angle),offset=normal*Mathf.Sin(angle);
    if(offset.x>.0001f)minor=Mathf.Min(minor,(bounds.xMax-baseline.x)/offset.x);
    else if(offset.x<-.0001f)minor=Mathf.Min(minor,(bounds.xMin-baseline.x)/offset.x);
    if(offset.y>.0001f)minor=Mathf.Min(minor,(bounds.yMax-baseline.y)/offset.y);
    else if(offset.y<-.0001f)minor=Mathf.Min(minor,(bounds.yMin-baseline.y)/offset.y);
   }
   return Mathf.Max(.001f,minor);
  }
  public static Vector2 GeometryPoint(Vector2 origin,Vector2 destination,float fraction,float minor){
   float t=Mathf.Clamp01(fraction);if(t<=0)return origin;if(t>=1)return destination;
   Vector2 axis=(origin-destination)*.5f;float major=axis.magnitude;if(major<.001f)return origin;
   Vector2 normal=new Vector2(-axis.y,axis.x)/major;
   float angle=t*Mathf.PI;
   return (origin+destination)*.5f+axis*Mathf.Cos(angle)+normal*minor*Mathf.Sin(angle);
  }
  public static Vector2 FlightPoint(Vector2 origin,Vector2 destination,float progress,float minor){
   float t=Mathf.Clamp01(progress);if(t<=0)return origin;if(t>=1)return destination;
   float major=Vector2.Distance(origin,destination)*.5f;if(major<.001f)return origin;
   float ratio=Mathf.Clamp01(minor/major),eccentricity=Mathf.Sqrt(Mathf.Max(0,1-ratio*ratio));
   float mean=t*Mathf.PI,low=0,high=Mathf.PI;
   // Solve E - e*sin(E) = M; constant mean anomaly approximates equal swept areas.
   for(int i=0;i<24;i++){float middle=(low+high)*.5f;if(middle-eccentricity*Mathf.Sin(middle)<mean)low=middle;else high=middle;}
   return GeometryPoint(origin,destination,(low+high)*.5f/Mathf.PI,minor);
  }
 }
}

