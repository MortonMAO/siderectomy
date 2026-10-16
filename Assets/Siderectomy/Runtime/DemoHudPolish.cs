using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Siderectomy {
 // Drag only the clock face / grip, leaving pause and speed buttons clickable.
 public sealed class DemoFloatingDrag : MonoBehaviour,IBeginDragHandler,IDragHandler {
  public RectTransform Target;Vector2 origin,pointer;
  public void OnBeginDrag(PointerEventData e){origin=Target.anchoredPosition;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Target.parent,e.position,e.pressEventCamera,out pointer);}
  public void OnDrag(PointerEventData e){Vector2 p;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Target.parent,e.position,e.pressEventCamera,out p);MoveTo(origin+p-pointer);}
  public void MoveTo(Vector2 p){var parent=(RectTransform)Target.parent;Target.anchoredPosition=new Vector2(Mathf.Clamp(p.x,0,Mathf.Max(0,parent.rect.width-Target.sizeDelta.x)),Mathf.Clamp(p.y,-parent.rect.height+Target.sizeDelta.y,-65));}
 }
 public static class DemoHudSurface {
  static Sprite leftTab;
  public static Sprite LeftTab {get{if(leftTab==null){var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.name="LeftRoundedTab";for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=Mathf.Max(0,10-x),dy=Mathf.Max(0,Mathf.Abs(y-15.5f)-5.5f);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(10.5f-Mathf.Sqrt(dx*dx+dy*dy))));}texture.Apply();leftTab=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(11,11,1,11));}return leftTab;}}
 }
 public sealed partial class DemoMissionView {
  RectTransform sidebarRoot,communicationsRoot;Button informationTab,npcTab;bool informationOpen=true;
  public bool InformationOpen {get{return informationOpen;}}
  public bool CommunicationsOpen {get{return conversationOpen;}}
  static void RightEdge(RectTransform r,float top){r.anchorMin=r.anchorMax=new Vector2(1,1);r.anchoredPosition=new Vector2(-r.sizeDelta.x,-top);}
  void BuildInformationFold(){sidebarRoot=(RectTransform)side.parent.parent;RightEdge(sidebarRoot,65);Button(sidebarRoot,"CollapseInformation","›\n[I]",-29,8,29,55,()=>SetInformationOpen(false),12);
   informationTab=Button(ui,"InformationTab","‹\n信\n息\n[I]",0,160,42,124,()=>SetInformationOpen(true),15);RightEdge((RectTransform)informationTab.transform,160);var image=informationTab.GetComponent<Image>();image.sprite=DemoHudSurface.LeftTab;image.type=Image.Type.Sliced;SetInformationOpen(true);
  }
  public void SetInformationOpen(bool open){informationOpen=open;sidebarRoot.gameObject.SetActive(open);informationTab.gameObject.SetActive(!open);if(!open&&Simulation!=null){Simulation.ClearSelection();RefreshSide();RefreshManifest();}UpdateMapLayout();}
  public bool HandlePanelShortcut(KeyCode key){if(Simulation==null||modal.gameObject.activeSelf||TextEntryFocused()||DropdownOpen()||Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt))return false;if(key==KeyCode.I){SetInformationOpen(!informationOpen);return true;}if(key==KeyCode.O){SetConversation(!CommunicationsOpen);return true;}return false;}
  void PollPanelShortcuts(){if(Input.GetKeyDown(KeyCode.I))HandlePanelShortcut(KeyCode.I);if(Input.GetKeyDown(KeyCode.O))HandlePanelShortcut(KeyCode.O);}
  void ShowCommunications(bool open){bool story=narrativeView!=null&&DemoSession.Profile!=null&&DemoNarrative.Busy(DemoSession.Profile.story);communicationsRoot.gameObject.SetActive(open&&!story);npcTab.gameObject.SetActive(!open);conversationOpen=open;if(narrativeView!=null)narrativeView.Folded=!open;UpdateMapLayout();}
  RectTransform mapPanel;Vector2 mapLayoutSize;
  // Refresh rendering even while dialogue deliberately stops simulation time.
  void SyncMapVisuals(){map.Refresh();if(preview!=null)preview.Refresh();Canvas.ForceUpdateCanvases();PositionLabels();map.Refresh();}
  void UpdateMapLayout(){if(map==null||ui==null)return;bool full=!InformationOpen&&!CommunicationsOpen;var size=new Vector2(Mathf.Max(900,ui.rect.width-(full?0:350)),Mathf.Max(600,ui.rect.height-(full?65:135)));if(size==mapLayoutSize)return;mapLayoutSize=size;mapPanel.sizeDelta=new Vector2(ui.rect.width,ui.rect.height-65);map.rectTransform.sizeDelta=size;if(starBackdrop!=null)((RectTransform)starBackdrop.transform).sizeDelta=mapPanel.sizeDelta;if(overviewButton!=null){var r=(RectTransform)overviewButton.transform;r.anchoredPosition=new Vector2(size.x-170,-5);}if(unknownPrompt!=null)unknownPrompt.rectTransform.anchoredPosition=new Vector2(size.x*.5f-380,-260);map.Refresh();if(Simulation!=null&&map.Simulation==Simulation&&Simulation.Bodies.TrueForAll(b=>bodyLabels.ContainsKey(b.id))){Canvas.ForceUpdateCanvases();PositionLabels();map.Refresh();}}
  void RefreshUnselectedSide(){Clear(side);preview=null;nodesContent=null;nodeLabels.Clear();jobLabels.Clear();missionLabels.Clear();sideBody=Simulation.Selected;
   currentViewing=Label(side,"CurrentViewing","当前查看：星系总览",18,16,315,45,17,PrototypeUi.Accent);details=Label(side,"BodyMetrics","尚未选择天体\n点击星图或下方目录选择目标。",18,72,315,58,16);status=null;
   Label(side,"BodyDirectoryHeading","天体目录",18,145,315,25,16,Muted);var directory=Scroll(side,"BodyDirectory",18,178,310,360,Simulation.Bodies.Count*30);for(int i=0;i<Simulation.Bodies.Count;i++){var item=Simulation.Bodies[i];Button(directory,"BodyEntry_"+item.id,item.name+" / "+(Simulation.Find(item.parent)?.name??"星系"),0,i*30,298,28,()=>SelectBody(item.id),13);}side.sizeDelta=new Vector2(side.sizeDelta.x,605);side.anchoredPosition=Vector2.zero;CompactSideText();lastRevision=Simulation.Revision;
  }
  void CompactSideText(){foreach(var t in side.GetComponentsInChildren<Text>(true)){if(t.GetComponentInParent<Button>(true)!=null)continue;t.alignment=TextAnchor.UpperLeft;t.lineSpacing=.95f;}}
 }
}
