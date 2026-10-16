using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Siderectomy {
 public sealed class DemoDockHint : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
  public Action<bool> Hint;
  public void OnPointerEnter(PointerEventData e){Hint?.Invoke(true);}public void OnPointerExit(PointerEventData e){Hint?.Invoke(false);}
  public void OnSelect(BaseEventData e){Hint?.Invoke(true);}public void OnDeselect(BaseEventData e){Hint?.Invoke(false);}
 }
 public sealed partial class DemoMissionView {
  public static readonly string[] FunctionTabs={"facility","batch","carrier","research","attackModule","operation","apply"};
  public static readonly string[] FunctionNames={"生产设施","二级资源","运输载体","研发","模块制造","作业投送","资源申请"};
  public static readonly KeyCode[] FunctionKeys={KeyCode.Q,KeyCode.W,KeyCode.E,KeyCode.R,KeyCode.T,KeyCode.Y,KeyCode.U};
  RectTransform secondaryPanel;Text dockHint,secondaryTitle;readonly Dictionary<string,Button> dockButtons=new Dictionary<string,Button>();
  public bool SecondaryMenuOpen {get{return secondaryPanel!=null&&secondaryPanel.gameObject.activeSelf;}}
  public string ActiveFunction {get{return tab;}}
  void BuildFunctionDock(){var dock=PrototypeUi.Box(ui,"FunctionDock",0,832,1080,68,Panel).rectTransform;PrototypeUi.Box(dock,"TopLine",0,0,1080,2,PrototypeUi.Accent);
   for(int i=0;i<FunctionTabs.Length;i++){int index=i;string key=FunctionTabs[i];var b=Button(dock,"Tab_"+key,"",12+i*68,9,58,50,()=>ToggleFunction(key));DemoUiGlyph.Create(b.transform,"FunctionIcon_代替换",key,9,5,30,PrototypeUi.Ink);var imageSlot=PrototypeUi.Box(b.transform,"ReplacementIcon",9,5,30,30,Color.white);imageSlot.enabled=false;imageSlot.preserveAspect=true;Label(b.transform,"Key",FunctionKeys[i].ToString(),40,31,16,18,12,Gold);var hint=b.gameObject.AddComponent<DemoDockHint>();hint.Hint=show=>{if(show)dockHint.text=FunctionKeys[index]+" · "+FunctionNames[index]+"  /  再次点击或按键收起";else RefreshDock();};dockButtons[key]=b;}
   dockHint=Label(dock,"FunctionHint","Q–U 打开功能 · 空格启停 · Esc 关闭 / 暂停菜单",505,12,565,45,15,Muted);
   secondaryPanel=PrototypeUi.Box(ui,"Commands",284,474,790,350,Panel).rectTransform;secondaryPanel.GetComponent<Image>().raycastTarget=true;PrototypeUi.Box(secondaryPanel,"TopLine",0,0,790,2,PrototypeUi.Accent);secondaryTitle=Label(secondaryPanel,"SecondaryTitle","",16,7,650,37,20);Button(secondaryPanel,"CloseSecondary","×",744,8,35,32,CloseSecondaryMenu,22);
   commands=Scroll(secondaryPanel,"ActionScroll",12,52,766,286,370);
  }
  void RefreshDock(){int index=Array.IndexOf(FunctionTabs,tab);foreach(var pair in dockButtons)pair.Value.GetComponent<Image>().color=SecondaryMenuOpen&&pair.Key==tab?new Color32(66,90,129,255):new Color32(27,39,58,255);if(dockHint!=null)dockHint.text=SecondaryMenuOpen&&index>=0?FunctionKeys[index]+" · "+FunctionNames[index]+"  /  同键收起 · Esc 返回星图":"Q–U 打开功能 · 空格启停 · Esc 暂停菜单";if(secondaryTitle!=null&&index>=0)secondaryTitle.text=FunctionNames[index]+"  ["+FunctionKeys[index]+"]";}
  public void CloseSecondaryMenu(){if(secondaryPanel!=null)secondaryPanel.gameObject.SetActive(false);RefreshDock();}
  public void ToggleFunction(string function){if(SecondaryMenuOpen&&tab==function)CloseSecondaryMenu();else if(tab==function){secondaryPanel.gameObject.SetActive(true);RefreshDock();Refresh();}else SetTab(function);}
  bool TextEntryFocused(){if(EventSystem.current==null)return false;var focused=EventSystem.current.currentSelectedGameObject;return focused!=null&&focused.GetComponentInParent<InputField>()!=null;}
  bool DropdownOpen(){return GameObject.Find("Dropdown List")!=null;}
  public bool HandleFunctionShortcut(KeyCode key){if(Simulation==null||modal.gameObject.activeSelf||TextEntryFocused()||DropdownOpen()||Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt)||Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.RightCommand))return false;int index=Array.IndexOf(FunctionKeys,key);if(index<0)return false;ToggleFunction(FunctionTabs[index]);return true;}
  void PollFunctionShortcuts(){foreach(var key in FunctionKeys)if(Input.GetKeyDown(key)){HandleFunctionShortcut(key);break;}}
  void HandleEscape(){if(TextEntryFocused())return;if(DropdownOpen()){foreach(var drop in ui.GetComponentsInChildren<Dropdown>())drop.Hide();return;}if(modal.gameObject.activeSelf)CloseModal();else if(SecondaryMenuOpen)CloseSecondaryMenu();else OpenPauseMenu();}
  public void ShowBasicResource(string key){OpenModal();int index=key=="energy"?0:key=="matter"?1:2;Label(dialog,"ResourceTitle",DemoSimulation.ResourceNames[index]+" · 资源储备",35,25,850,55,29,Gold);string text="当前数量："+Math.Floor(Simulation.Res[index])+"\n\n"+(index<2?"当前产出："+Simulation.Production(key).ToString("0.00")+" / 秒\n建设对应生产设施可提高产出。":"研发隐形探测 → 建造信息收集点 → 随时间自动产出。\n当前产出："+Simulation.Production("info").ToString("0.00")+" / 秒")+"\n\n左下列表持续显示实际可用数量。";Label(dialog,"ResourceDescription",text,35,105,850,320,22);Button(dialog,"CloseInventory","返回模拟",35,535,250,55,CloseModal);Button(dialog,"GoInventoryProduction","前往生产",310,535,250,55,()=>{CloseModal();SetTab(index<2||Simulation.Research.ContainsKey("stealth")?"facility":"research");});}
 }
}
