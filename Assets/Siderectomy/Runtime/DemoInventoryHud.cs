using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Siderectomy {
 public sealed class DemoInventoryHud : MonoBehaviour {
  sealed class Cell {public string kind,key;public Text count,state;public Image icon;public Button button;}
  readonly List<Cell> cells=new List<Cell>();DemoMissionView view;DemoHudIcons icons;Text researchTotal;
  public void Initialize(RectTransform root,DemoMissionView owner){view=owner;icons=Resources.Load<DemoHudIcons>("HUD图标配置_代替换");
   PrototypeUi.Box(root,"ResourceListBackground",0,0,258,370,new Color32(19,27,40,250)).raycastTarget=true;
   PrototypeUi.Label(root,"ResourceHeading","物资储备",12,0,235,29,16,PrototypeUi.Accent);
   string[] basics={"energy","matter","info"},labels={"能源","物质","信息"};for(int i=0;i<3;i++)Add(root,"resource",basics[i],labels[i],31+i*26);
   var outer=PrototypeUi.Box(root,"InventoryScroll",4,112,249,250,new Color32(19,27,40,255)).rectTransform;outer.GetComponent<Image>().raycastTarget=true;var scroll=outer.gameObject.AddComponent<ScrollRect>();var viewport=PrototypeUi.Rect("Viewport",outer,0,0,240,250);viewport.gameObject.AddComponent<RectMask2D>();var content=PrototypeUi.Rect("Content",viewport,0,0,240,560);scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=33;scroll.movementType=ScrollRect.MovementType.Clamped;
   var bar=PrototypeUi.Box(outer,"Scrollbar",242,0,5,250,new Color32(30,43,61,255));bar.raycastTarget=true;var scrollbar=bar.gameObject.AddComponent<Scrollbar>();var handle=PrototypeUi.Box(bar.transform,"Handle",0,0,5,40,PrototypeUi.Muted);PrototypeUi.Fill(handle.rectTransform);scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=scrollbar;
   float y=0;Header(content,"载体",y);y+=24;string[] carriers={"dust","satellite","asteroid","planet"},cn={"星尘级","卫星级","小行星级","行星级"};for(int i=0;i<4;i++){Add(content,"carrier",carriers[i],cn[i],y);y+=28;}
   researchTotal=Header(content,"科技",y);y+=24;string[] tech={"stealth","calibration"},tn={"隐形技术","武装校准"};for(int i=0;i<2;i++){Add(content,"research",tech[i],tn[i],y);y+=28;}
   Header(content,"武装",y);y+=24;for(int i=0;i<3;i++){Add(content,"armament","a"+(i+1),(i+1)+"级武装",y);y+=28;}
   Header(content,"打击模块",y);y+=24;string[] names={"地表","卫星","行星","恒星","拦截"};for(int i=0;i<5;i++){Add(content,"attackModule",DemoSimulation.AttackModuleKeys[i],names[i],y);y+=28;}Header(content,"科技模块",y);y+=24;string[] mn={"隐形","信息入侵","病毒入侵"};for(int i=0;i<3;i++){Add(content,"technologyModule",DemoSimulation.TechnologyModuleKeys[i],mn[i],y);y+=28;}content.sizeDelta=new Vector2(240,y+4);root.gameObject.AddComponent<DemoResizableHud>().Initialize(root,Resize);
  }
  void Resize(Vector2 size){var root=(RectTransform)transform;var bg=(RectTransform)root.Find("ResourceListBackground");bg.sizeDelta=size;var outer=(RectTransform)root.Find("InventoryScroll");outer.sizeDelta=new Vector2(size.x-9,size.y-120);var viewport=(RectTransform)outer.Find("Viewport");viewport.sizeDelta=new Vector2(size.x-18,size.y-120);var content=(RectTransform)viewport.Find("Content");content.sizeDelta=new Vector2(size.x-18,content.sizeDelta.y);var bar=(RectTransform)outer.Find("Scrollbar");bar.anchoredPosition=new Vector2(size.x-16,0);bar.sizeDelta=new Vector2(5,size.y-120);foreach(var c in cells){var row=(RectTransform)c.button.transform;row.sizeDelta=new Vector2(size.x-22,26);c.count.rectTransform.anchoredPosition=new Vector2(size.x-138,0);c.state.rectTransform.anchoredPosition=new Vector2(size.x-70,0);} }
  Text Header(Transform p,string text,float y){var t=PrototypeUi.Label(p,"HUDGroup_"+text,text,8,y,229,24,12,PrototypeUi.Muted);return t;}
  void Add(Transform root,string kind,string key,string label,float y){
   var box=PrototypeUi.Box(root,"HUD_"+kind+"_"+key,4,y,236,26,new Color32(24,34,49,255));box.raycastTarget=true;var button=box.gameObject.AddComponent<Button>();button.targetGraphic=box;
   var icon=PrototypeUi.Box(box.transform,"Icon_代替换",5,4,18,18,Color.white);icon.preserveAspect=true;icon.sprite=icons==null?null:icons.Find(kind+":"+key);icon.enabled=icon.sprite!=null;if(kind=="resource")DemoUiGlyph.Create(box.transform,"ResourceGlyph_代替换",key,5,4,18,PrototypeUi.Accent);
   PrototypeUi.Label(box.transform,"Name",label,29,0,91,26,13);var count=PrototypeUi.Label(box.transform,"Quantity","0",120,0,63,26,14,PrototypeUi.Accent);count.alignment=TextAnchor.MiddleRight;var state=PrototypeUi.Label(box.transform,"State","",188,0,45,26,10,PrototypeUi.Muted);state.alignment=TextAnchor.MiddleRight;
   cells.Add(new Cell{kind=kind,key=key,count=count,state=state,icon=icon,button=button});button.onClick.AddListener(()=>{if(kind=="resource")view.ShowBasicResource(key);else view.ShowInventoryItem(kind,key);});
  }
  public void Refresh(DemoSimulation s){if(s==null)return;if(icons==null)icons=Resources.Load<DemoHudIcons>("HUD图标配置_代替换");researchTotal.text="科技 · 已研发 "+s.Research.Count+"/"+s.Rules.research.Count(r=>s.Unlocked(r));
   foreach(var c in cells){bool basic=c.kind=="resource";var rule=basic?null:s.Rules.Find(c.kind=="armament"?"batch":c.kind,c.key);bool unlocked=basic||s.Unlocked(rule);int pending=s.Jobs.Count(j=>j.kind==(c.kind=="armament"?"batch":c.kind)&&j.key==c.key);
    double stock=basic?s.Res[c.key=="energy"?0:c.key=="matter"?1:2]:c.kind=="carrier"?s.Carriers[c.key]:c.kind=="research"?(s.Research.ContainsKey(c.key)?1:0):c.kind=="armament"?s.Res[3+int.Parse(c.key.Substring(1))-1]:c.kind=="technologyModule"?s.TechnologyModules[c.key]:s.AttackModules[c.key];
    c.count.text=unlocked?(c.kind=="research"?stock+"/1":System.Math.Floor(stock).ToString()):"未解锁";c.count.fontSize=unlocked?14:11;c.count.color=unlocked?PrototypeUi.Accent:PrototypeUi.Muted;c.state.text=basic?(s.Production(c.key).ToString("+0.0")+"/s"):pending>0?(c.kind=="research"?"在研":"在造"+pending):"";
    if(icons!=null)c.icon.sprite=icons.Find(c.kind+":"+c.key);c.icon.enabled=c.icon.sprite!=null;c.icon.color=unlocked?Color.white:new Color(.4f,.4f,.4f,.7f);
   }
  }
 }
}
