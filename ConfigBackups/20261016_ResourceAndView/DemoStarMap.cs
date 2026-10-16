using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Siderectomy {
 [RequireComponent(typeof(CanvasRenderer))]
 public sealed class DemoStarMap : MaskableGraphic,IPointerClickHandler {
  public DemoSimulation Simulation;public PrototypeAssets Art;public System.Action OnSelect;public string LocalBodyId;public bool Compact;
  public readonly Dictionary<string,Vector2> Positions=new Dictionary<string,Vector2>();public readonly Dictionary<string,Vector2> LabelPositions=new Dictionary<string,Vector2>();
  public readonly Dictionary<string,Vector2> NodePositions=new Dictionary<string,Vector2>();public readonly Dictionary<int,Vector2> ShipPositions=new Dictionary<int,Vector2>();
  public readonly Dictionary<int,Vector2> MissionPositions=new Dictionary<int,Vector2>();
  public readonly Dictionary<int,DemoOrbitRoute> MissionRoutes=new Dictionary<int,DemoOrbitRoute>();
  public Vector2 LaunchBasePosition {get{return new Vector2(rectTransform.rect.width-140,-rectTransform.rect.height+105);}}
  public void EnterLocal(){if(!DemoSystemDiscovery.Revealed(Simulation))return;var b=Simulation.Body;LocalBodyId=b.kind=="moon"||b.kind=="station"?b.parent:b.id;Refresh();}
  public void ExitLocal(){LocalBodyId=null;Refresh();}
  public void Refresh(){SetVerticesDirty();} public Vector2 LocalPosition(string id){Vector2 p;return Positions.TryGetValue(id,out p)?p:Vector2.zero;}
  protected override void OnPopulateMesh(VertexHelper v){v.Clear();MissionPositions.Clear();MissionRoutes.Clear();if(Simulation==null)return;float w=rectTransform.rect.width,h=rectTransform.rect.height;Vector2 c=new Vector2(w*.53f,-h*.49f);float rx=w*.39f,ry=h*.35f;Positions.Clear();
   if(!DemoSystemDiscovery.Revealed(Simulation)){NodePositions.Clear();ShipPositions.Clear();LabelPositions.Clear();if(!Compact){Ring(v,LaunchBasePosition,9,2,PrototypeUi.Accent,4);foreach(var m in Simulation.Missions)if(m.op=="probe"){float t=(float)(1-m.left/m.total);Ring(v,c,25+t*100,1.5f,new Color(.3f,.85f,.86f,.25f));}}return;}
   for(int i=0;i<120;i++)Disc(v,new Vector2((i*97+31)%w,-((i*61+19)%h)),i%5==0?1.2f:.7f,new Color32(155,180,208,(byte)(i%3==0?105:50)),6);if(Simulation.Level.bodies==null)Positions["sun"]=c;
   if(Simulation.Level.bodies!=null)ConfiguredPositions(v,c,rx,ry);else foreach(var b in Simulation.Bodies)if(b.kind=="planet"){float f=(float)(b.index+1)/Simulation.Level.planets;Vector2 prev=c+new Vector2(rx*f,0);for(int j=1;j<=96;j++){float a=j*Mathf.PI*2/96;Vector2 next=c+new Vector2(Mathf.Cos(a)*rx*f,Mathf.Sin(a)*ry*f);Line(v,prev,next,.75f,new Color32(75,101,139,52));prev=next;}float angle=b.index*.91f+(float)Simulation.Time*.003f/(b.index+1);Positions[b.id]=c+new Vector2(Mathf.Cos(angle)*rx*f,Mathf.Sin(angle)*ry*f);}
   foreach(var b in Simulation.Bodies){if(Positions.ContainsKey(b.id))continue;Vector2 parent;if(string.IsNullOrEmpty(b.parent)||!Positions.TryGetValue(b.parent,out parent))continue;float a=b.index*2.3f+(float)Simulation.Time*.013f;Positions[b.id]=parent+new Vector2(Mathf.Cos(a)*(b.kind=="station"?34:22),Mathf.Sin(a)*(b.kind=="station"?28:20));}
   if(!string.IsNullOrEmpty(LocalBodyId)){v.Clear();Positions.Clear();var root=Simulation.Find(LocalBodyId);if(root!=null){Vector2 center=new Vector2(w*(Compact?.5f:.28f),-h*.5f);Positions[root.id]=center;var children=Simulation.Bodies.FindAll(b=>b.parent==root.id);for(int i=0;i<children.Count;i++){var b=children[i];float r=(Compact?22:52)+i*(Compact?6:17);float a=b.kind=="planet"?b.index*.91f+(float)Simulation.Time*.003f/(b.index+1):b.index*2.3f+(float)Simulation.Time*.013f;Ellipse(v,center,r,r*.65f,.7f,new Color32(75,101,139,65));Positions[b.id]=center+new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.65f);}}}
   foreach(var b in Simulation.Bodies){Vector2 p;if(!Positions.TryGetValue(b.id,out p))continue;float radius=b.kind=="star"?21:b.kind=="planet"?(Compact?20:24):b.kind=="station"?12:11;Color tint;ColorUtility.TryParseHtmlString(b.color,out tint);var sprite=Art==null?null:b.kind=="planet"?Art.Earth:b.kind=="moon"?Art.Backup:b.kind=="station"?Art.Satellite:null;if(sprite==null)Disc(v,p,radius,tint);if(!Simulation.SelectionCleared&&b.id==Simulation.Selected&&Simulation.SelectedShip<0)Ring(v,p,radius+5,1.2f,new Color(.31f,.85f,.86f,.7f));}
   NodePositions.Clear();if(!Simulation.SelectionCleared&&!Compact&&!string.IsNullOrEmpty(LocalBodyId)&&Simulation.SelectedShip<0&&Simulation.Body.recon&&Positions.ContainsKey(Simulation.Selected)){var b=Simulation.Body;for(int i=0;i<b.nodes.Count;i++){var n=b.nodes[i];Vector2 p=new Vector2(w*.57f+(i%5)*92,-65-(i/5)*52);NodePositions[n.id]=p;Disc(v,p,10,n.hp<=0?new Color32(70,78,90,255):PrototypeUi.Accent);if(Simulation.TargetNode==n.id)Ring(v,p,15,2,Color.white);}}
   if(!Simulation.SelectionCleared&&!Compact&&Simulation.Body.recon){var body=Simulation.Body;foreach(var link in body.links){Vector2 a,b;if(NodePositions.TryGetValue(link.from,out a)&&NodePositions.TryGetValue(link.to,out b)){float flow=(float)Simulation.LinkFlow(body,link);Line(v,a,b,2,flow>0?new Color(0.35f,0.8f,0.9f,Mathf.Clamp01(flow)):new Color32(241,129,137,155));if(flow>0)Disc(v,Vector2.Lerp(a,b,(float)(Simulation.Time*.4%1)),3,PrototypeUi.Accent);}}}
   ShipPositions.Clear();if(Compact)return;
   foreach(var s in Simulation.Ships)if(!s.done){Vector2 origin;if(!Positions.TryGetValue(s.body,out origin))continue;Vector2 dest=new Vector2(w-240,-h+75),prev=EscapePoint(origin,dest,0,s.id);for(int j=1;j<=48;j++){var next=EscapePoint(origin,dest,j/48f,s.id);if(j%4==1)Line(v,prev,next,.7f,new Color32(241,129,137,35));prev=next;}Vector2 p=EscapePoint(origin,dest,(float)(1-s.left/s.total),s.id);ShipPositions[s.id]=p;Disc(v,p,8,new Color32(241,129,137,255),4);Ring(v,p,12,1.5f,Simulation.SelectedShip==s.id?Color.white:new Color32(241,129,137,140));}
   Vector2 start=LaunchBasePosition;Ring(v,start,9,2,PrototypeUi.Accent,4);
   Vector2 orbitCenter=c,orbitRadii=new Vector2(rx,ry);float avoid=28;
   if(!string.IsNullOrEmpty(LocalBodyId)&&Positions.ContainsKey(LocalBodyId)){orbitCenter=Positions[LocalBodyId];int children=Simulation.Bodies.FindAll(b=>b.parent==LocalBodyId).Count;float radius=52+Mathf.Max(0,children-1)*17;orbitRadii=new Vector2(radius,radius*.65f);avoid=Simulation.Find(LocalBodyId).kind=="star"?28:22;}
   foreach(var m in Simulation.Missions){Vector2 dest;if(m.ship>=0){if(!ShipPositions.TryGetValue(m.ship,out dest))continue;}else if(!Positions.TryGetValue(m.body,out dest))continue;
    float targetRing=0;var target=Simulation.Find(m.body);Vector2 parent;
    if(m.ship<0&&target!=null&&target.parent!=null&&Positions.TryGetValue(target.parent,out parent)&&Vector2.Distance(parent,orbitCenter)>.1f)targetRing=new Vector2((parent.x-orbitCenter.x)/orbitRadii.x,(parent.y-orbitCenter.y)/orbitRadii.y).magnitude;
    if(Vector2.Distance(dest,orbitCenter)<avoid)targetRing=string.IsNullOrEmpty(LocalBodyId)?1f/Simulation.Level.planets:52f/orbitRadii.x;
    var route=new DemoOrbitRoute(start,dest,orbitCenter,orbitRadii,new Rect(12,-h+12,w-24,h-24),targetRing,avoid);MissionRoutes[m.id]=route;Vector2 previous=start;
    DashedRoute(v,route);
    float progress=m.total>0?Mathf.Clamp01((float)(1-m.left/m.total)):1;Vector2 point=route.FlightPoint(progress);MissionPositions[m.id]=point;Disc(v,point,3.5f,new Color32(236,194,123,255));
   }
   foreach(var e in Simulation.Effects){Vector2 p;if(!Positions.TryGetValue(e.body,out p))continue;float t=(float)(Simulation.Time-e.time);Ring(v,p,12+t*28,2,new Color(1,.5f,.54f,Mathf.Clamp01(1-t/1.8f)));}
   foreach(var pair in LabelPositions){Vector2 origin;if(Positions.TryGetValue(pair.Key,out origin)){Vector2 dest=pair.Value+new Vector2(0,-12);if(Vector2.Distance(origin,dest)>30)Line(v,origin,dest,.8f,new Color32(105,135,165,75));}}
  }
  void ConfiguredPositions(VertexHelper v,Vector2 center,float rx,float ry){
   var roots=Simulation.Bodies.FindAll(b=>string.IsNullOrEmpty(b.parent));
   for(int i=0;i<roots.Count;i++)Positions[roots[i].id]=roots.Count==1?center:center+new Vector2(Mathf.Cos(i*Mathf.PI*2/roots.Count)*rx*.5f,Mathf.Sin(i*Mathf.PI*2/roots.Count)*ry*.5f);
   for(int depth=0;depth<Simulation.Bodies.Count;depth++){
    bool changed=false;
    foreach(var b in Simulation.Bodies){Vector2 parent;if(Positions.ContainsKey(b.id)||string.IsNullOrEmpty(b.parent)||!Positions.TryGetValue(b.parent,out parent))continue;
     var siblings=Simulation.Bodies.FindAll(x=>x.parent==b.parent&&x.kind==b.kind);siblings.Sort((a,z)=>a.index.CompareTo(z.index));int rank=siblings.IndexOf(b);
     float angle=b.kind=="planet"?rank*.91f+(float)Simulation.Time*.003f/(rank+1):b.index*2.3f+(float)Simulation.Time*.013f;
     float f=(rank+1f)/Mathf.Max(1,siblings.Count),x=b.kind=="planet"||b.kind=="star"?rx*f/(roots.Count>1?3:1):b.kind=="station"?34:22,y=b.kind=="planet"||b.kind=="star"?ry*f/(roots.Count>1?3:1):b.kind=="station"?28:20;
     if(b.kind=="planet"||b.kind=="star")Ellipse(v,parent,x,y,.75f,new Color32(75,101,139,52));
     Positions[b.id]=parent+new Vector2(Mathf.Cos(angle)*x,Mathf.Sin(angle)*y);changed=true;
    }if(!changed)break;
   }
  }
  readonly Dictionary<string,Image> previewSprites=new Dictionary<string,Image>();
  void LateUpdate(){if(!Compact||Simulation==null)return;foreach(var image in previewSprites.Values)image.gameObject.SetActive(false);if(!DemoSystemDiscovery.Revealed(Simulation))return;foreach(var b in Simulation.Bodies){Vector2 p;if(!Positions.TryGetValue(b.id,out p))continue;var sprite=Art==null?null:b.kind=="planet"?Art.Earth:b.kind=="moon"?Art.Backup:b.kind=="station"?Art.Satellite:null;if(sprite!=null){SpriteBody(b.id,p,b.kind=="planet"?40:22,sprite);previewSprites[b.id].gameObject.SetActive(true);}}}
  void SpriteBody(string id,Vector2 p,float size,Sprite sprite){Image image;if(!previewSprites.TryGetValue(id,out image)){image=PrototypeUi.Box(transform,"PreviewArt_"+id,0,0,size,size,Color.white);image.sprite=sprite;image.preserveAspect=true;previewSprites[id]=image;}image.rectTransform.anchoredPosition=p+new Vector2(-size*.5f,size*.5f);}
  static void DashedRoute(VertexHelper v,DemoOrbitRoute route){Vector2 previous=route.Origin;float distance=0;const float dash=9,gap=25;for(int i=1;i<=512;i++){Vector2 next=route.GeometryPoint(i/512f);float length=Vector2.Distance(previous,next),offset=0;while(offset<length-.0001f){float phase=Mathf.Repeat(distance,dash+gap),step=Mathf.Min(length-offset,(phase<dash?dash:dash+gap)-phase);if(step<.0001f)step=Mathf.Min(length-offset,.001f);if(phase<dash)Line(v,Vector2.Lerp(previous,next,offset/length),Vector2.Lerp(previous,next,(offset+step)/length),.75f,new Color32(103,213,220,37));distance+=step;offset+=step;}previous=next;}}
  static void Disc(VertexHelper v,Vector2 p,float r,Color c,int n=24){int ix=v.currentVertCount;v.AddVert(p,c,Vector2.zero);for(int i=0;i<=n;i++){float a=i*Mathf.PI*2/n;v.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,c,Vector2.zero);if(i>0)v.AddTriangle(ix,ix+i,ix+i+1);}}
  static void Ring(VertexHelper v,Vector2 p,float r,float width,Color c,int n=48){Vector2 prev=p+Vector2.right*r;for(int i=1;i<=n;i++){float a=i*Mathf.PI*2/n;Vector2 next=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;Line(v,prev,next,width,c);prev=next;}}
  static void Ellipse(VertexHelper v,Vector2 p,float rx,float ry,float width,Color c){Vector2 prev=p+Vector2.right*rx;for(int i=1;i<=48;i++){float a=i*Mathf.PI*2/48;Vector2 next=p+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry);Line(v,prev,next,width,c);prev=next;}}
  static void Line(VertexHelper v,Vector2 a,Vector2 b,float width,Color c){Vector2 d=(b-a).normalized,normal=new Vector2(-d.y,d.x)*width*.5f;int i=v.currentVertCount;v.AddVert(a-normal,c,Vector2.zero);v.AddVert(a+normal,c,Vector2.zero);v.AddVert(b+normal,c,Vector2.zero);v.AddVert(b-normal,c,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
  public static Vector2 EscapePoint(Vector2 origin,Vector2 dest,float progress,int id){float a=Mathf.Clamp01(progress)*Mathf.PI;Vector2 center=(origin+dest)*.5f;return center+(origin-dest)*.5f*Mathf.Cos(a)+Vector2.up*(45+id%3*16)*Mathf.Sin(a);}
  public void OnPointerClick(PointerEventData e){if(Compact||!DemoSystemDiscovery.Revealed(Simulation))return;Vector2 p;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out p))return;
   int ship=-1;float nearest=22;foreach(var pair in ShipPositions){float d=Vector2.Distance(pair.Value,p);if(d<nearest){ship=pair.Key;nearest=d;}}if(ship>=0&&Simulation.SelectShip(ship)){OnSelect?.Invoke();return;}
   foreach(var pair in NodePositions)if(Vector2.Distance(pair.Value,p)<20&&Simulation.SelectNode(pair.Key)){OnSelect?.Invoke();return;}
   string id=null;nearest=29;foreach(var pair in Positions){float d=Vector2.Distance(pair.Value,p);if(d<nearest){id=pair.Key;nearest=d;}}if(id!=null){Simulation.SelectBody(id);OnSelect?.Invoke();}}
 }
}
