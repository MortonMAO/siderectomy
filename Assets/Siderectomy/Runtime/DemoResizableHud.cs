using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Siderectomy {
 public sealed class DemoHudDragHandle : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler {
  public DemoResizableHud owner;public int x,y;public bool move;
  public void OnBeginDrag(PointerEventData e){owner.Begin(e);}
  public void OnDrag(PointerEventData e){owner.Drag(e,x,y,move);}
  public void OnEndDrag(PointerEventData e){}
 }
 public sealed class DemoResizableHud : MonoBehaviour {
  RectTransform root,parent;Action<Vector2> resize;Vector2 startPointer,startPosition,startSize;
  public void Initialize(RectTransform target,Action<Vector2> callback){root=target;parent=(RectTransform)root.parent;resize=callback;
   Handle("DragHeader",new Vector2(0,1),new Vector2(1,1),new Vector2(8,-29),new Vector2(-8,-3),0,0,true);
   Handle("ResizeLeft",new Vector2(0,0),new Vector2(0,1),Vector2.zero,new Vector2(6,0),-1,0);
   Handle("ResizeRight",new Vector2(1,0),new Vector2(1,1),new Vector2(-6,0),Vector2.zero,1,0);
   Handle("ResizeTop",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-6),Vector2.zero,0,1);
   Handle("ResizeBottom",new Vector2(0,0),new Vector2(1,0),Vector2.zero,new Vector2(0,6),0,-1);
   foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1}){var anchor=new Vector2(x<0?0:1,y<0?0:1);Handle("ResizeCorner"+x+"_"+y,anchor,anchor,new Vector2(x<0?0:-12,y<0?0:-12),new Vector2(x<0?12:0,y<0?12:0),x,y);}
   PrototypeUi.Label(root,"DragHint","拖动 / 边缘缩放",128,3,120,22,10,PrototypeUi.Muted);
   Apply(root.anchoredPosition,root.sizeDelta);
  }
  void Handle(string name,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax,int x,int y,bool move=false){var image=PrototypeUi.Box(root,name,0,0,1,1,new Color(0.25f,.65f,.7f,move?0:.13f));image.raycastTarget=true;var r=image.rectTransform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;var h=image.gameObject.AddComponent<DemoHudDragHandle>();h.owner=this;h.x=x;h.y=y;h.move=move;}
  public void Begin(PointerEventData e){RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,e.position,e.pressEventCamera,out startPointer);startPosition=root.anchoredPosition;startSize=root.sizeDelta;}
  public void Drag(PointerEventData e,int x,int y,bool move){Vector2 point;RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,e.position,e.pressEventCamera,out point);Vector2 d=point-startPointer,pos=startPosition,size=startSize;if(move)pos+=d;else{if(x!=0){float width=Mathf.Clamp(startSize.x+d.x*x,258,540);size.x=width;if(x<0)pos.x+=startSize.x-width;}if(y!=0){float height=Mathf.Clamp(startSize.y+d.y*y,220,650);size.y=height;if(y>0)pos.y+=height-startSize.y;}}Apply(pos,size);}
  public void Apply(Vector2 position,Vector2 size){size=new Vector2(Mathf.Clamp(size.x,258,Mathf.Min(540,parent.rect.width)),Mathf.Clamp(size.y,220,Mathf.Min(650,parent.rect.height-140)));position.x=Mathf.Clamp(position.x,0,parent.rect.width-size.x);position.y=Mathf.Clamp(position.y,-parent.rect.height+68+size.y,-65);root.anchoredPosition=position;root.sizeDelta=size;resize?.Invoke(size);}
 }
}
