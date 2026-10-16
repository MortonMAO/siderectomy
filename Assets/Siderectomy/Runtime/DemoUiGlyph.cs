using UnityEngine;
using UnityEngine.UI;
namespace Siderectomy {
 // Small code-drawn UI symbols; no existing art asset is modified.
 [RequireComponent(typeof(CanvasRenderer))]
 public sealed class DemoUiGlyph : MaskableGraphic {
  public string symbol;
  public static DemoUiGlyph Create(Transform root,string name,string symbol,float x,float y,float size,Color tint){var rect=PrototypeUi.Rect(name,root,x,y,size,size);rect.gameObject.AddComponent<CanvasRenderer>();var g=rect.gameObject.AddComponent<DemoUiGlyph>();g.symbol=symbol;g.color=tint;g.raycastTarget=false;return g;}
  VertexHelper mesh;
  Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x*r.width/32,r.yMax-y*r.height/32);}
  void Polygon(params Vector2[] points){int n=mesh.currentVertCount;foreach(var p in points)mesh.AddVert(P(p.x,p.y),color,Vector2.zero);for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(n,n+i,n+i+1);}
  void Box(float x,float y,float w,float h){Polygon(new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));}
  void Line(float x,float y,float a,float b,float width=2){var start=new Vector2(x,y);var end=new Vector2(a,b);var direction=(end-start).normalized;var normal=new Vector2(-direction.y,direction.x)*width*.5f;Polygon(start-normal,start+normal,end+normal,end-normal);}
  void Ring(float x,float y,float radius){for(int i=0;i<20;i++){float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,2);}}
  protected override void OnPopulateMesh(VertexHelper helper){helper.Clear();mesh=helper;switch(symbol){
   case "facility":Box(3,16,26,12);Box(5,5,5,11);Box(18,9,4,7);Line(10,16,17,11,3);Box(7,28,3,3);Box(22,28,3,3);break;
   case "batch":case "matter":Line(3,9,15,3);Line(15,3,28,9);Line(28,9,15,16);Line(15,16,3,9);Line(3,9,3,23);Line(3,23,15,30);Line(15,30,28,23);Line(28,23,28,9);Line(15,16,15,30);break;
   case "carrier":Polygon(new Vector2(16,2),new Vector2(22,10),new Vector2(22,23),new Vector2(10,23),new Vector2(10,10));Polygon(new Vector2(9,16),new Vector2(3,27),new Vector2(9,24));Polygon(new Vector2(23,16),new Vector2(29,27),new Vector2(23,24));Box(12,26,2,5);Box(18,26,2,5);break;
   case "research":Box(11,3,10,3);Line(13,6,13,14);Line(19,6,19,14);Line(13,14,5,27);Line(19,14,27,27);Line(5,27,27,27);Box(11,20,10,5);break;
   case "attackModule":Ring(16,16,10);Box(15,1,2,10);Box(15,21,2,10);Box(1,15,10,2);Box(21,15,10,2);Box(14,14,4,4);break;
   case "operation":Ring(6,26,3);Line(8,24,25,7,3);Line(15,6,26,6,3);Line(26,6,26,17,3);break;
   case "apply":Line(3,7,29,7);Line(3,7,3,26);Line(29,7,29,26);Line(3,26,29,26);Line(3,8,16,18);Line(16,18,29,8);break;
   case "energy":Polygon(new Vector2(17,1),new Vector2(6,18),new Vector2(14,18),new Vector2(11,31),new Vector2(27,12),new Vector2(18,12));break;
   default:Box(8,8,16,16);for(int i=6;i<28;i+=7){Box(i,2,2,5);Box(i,25,2,5);Box(2,i,5,2);Box(25,i,5,2);}break;
  }}
 }
}
