using System;
using UnityEngine;
using UnityEngine.UI;
namespace Siderectomy {
 public sealed class DemoFrontMenu : MonoBehaviour {
  RectTransform canvas,panel;Text message;
  public static void ApplyPreferences(){AudioListener.volume=PlayerPrefs.GetFloat("cx_volume",.8f);if(PlayerPrefs.HasKey("cx_fullscreen")&&!Application.isBatchMode)Screen.fullScreen=PlayerPrefs.GetInt("cx_fullscreen")==1;}
  public void Initialize(){canvas=PrototypeUi.CanvasRoot();ShowHome();}
  void Page(string heading,string subtitle){foreach(Transform child in canvas){child.gameObject.SetActive(false);Destroy(child.gameObject);}PrototypeUi.Box(canvas,"MenuBackground",0,0,1600,900,new Color32(10,14,29,255));PrototypeUi.Box(canvas,"TopAccent",80,70,1440,4,new Color32(209,246,82,255));PrototypeUi.Label(canvas,"Brand","裁星计划",100,120,800,90,58);PrototypeUi.Label(canvas,"MenuHeading",heading,105,220,1150,46,26,PrototypeUi.Accent);PrototypeUi.Label(canvas,"MenuSubtitle",subtitle,105,275,1360,50,17,PrototypeUi.Muted);panel=PrototypeUi.Rect("MenuBody",canvas,100,345,1400,475);message=PrototypeUi.Label(canvas,"MenuMessage","",105,831,1360,45,17,new Color32(241,191,116,255));}
  void Run(Action action){try{action();}catch(Exception ex){if(message!=null)message.text="操作未完成："+ex.Message;}}
  Button B(Transform p,string name,string label,float x,float y,float width,Action action){var b=PrototypeUi.Button(p,name,label,x,y,width,55,()=>Run(action));b.GetComponentInChildren<Text>().fontSize=20;return b;}
  public void ShowHome(){if(TryShowArtworkHome())return;Page("模拟控制台","建立独立档案 → 接入办公室终端 → 开始或继续模拟");B(panel,"NewProfile","创建新存档",0,0,470,ShowNew);B(panel,"ContinueGame","继续游戏",0,75,470,ShowSlots);B(panel,"GameSettings","设置",0,150,470,ShowSettings);B(panel,"QuitGame","退出游戏",0,225,470,Quit);PrototypeUi.Label(panel,"MenuNote","PROJECT SIDERECTOMY\n\n六关模拟 / 逐关解锁 / 本地档案\n局内可保存生产、投送与文明状态\n\n选择“继续游戏”可读档、删档。",640,0,660,300,24,PrototypeUi.Muted);}
  bool TryShowArtworkHome(){var artwork=Resources.Load<Texture2D>("Title/Artwork_20261011");if(artwork==null)return false;foreach(Transform child in canvas){child.gameObject.SetActive(false);Destroy(child.gameObject);}var background=PrototypeUi.Box(canvas,"HomeBackground",0,0,1600,900,new Color32(10,14,29,255));PrototypeUi.Fill(background.rectTransform);var image=PrototypeUi.Rect("TitleArtwork",background.transform,0,0,1600,900).gameObject.AddComponent<RawImage>();image.texture=artwork;image.raycastTarget=false;PrototypeUi.Fill(image.rectTransform);var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=(float)artwork.width/artwork.height;
   PrototypeUi.Box(canvas,"HomeControlsFrame",1050,548,490,310,new Color32(10,16,31,207));panel=PrototypeUi.Rect("MenuBody",canvas,1090,565,410,280);B(panel,"NewProfile","创建新存档",0,0,410,ShowNew);B(panel,"ContinueGame","继续游戏",0,75,410,ShowSlots);B(panel,"GameSettings","设置",0,150,410,ShowSettings);B(panel,"QuitGame","退出游戏",0,225,410,Quit);var footer=PrototypeUi.Box(canvas,"HomeFooter",55,836,890,39,new Color32(10,16,31,193));PrototypeUi.Label(footer.transform,"MenuNote","六关 · 完成目标逐关解锁 · 独立存档 · 已解锁关卡可重玩",15,0,860,39,18,PrototypeUi.Ink);message=PrototypeUi.Label(canvas,"MenuMessage","",55,785,930,40,17,new Color32(241,191,116,255));return true;}
  string draftProfileName,draftCharacterName="";DemoProfile pendingCreatedProfile;
  InputField NameInput(string name,string value,string hint){
   var bg=PrototypeUi.Box(panel,name,0,55,760,62,new Color32(33,43,65,255));bg.raycastTarget=true;
   var input=bg.gameObject.AddComponent<InputField>();input.targetGraphic=bg;input.textComponent=PrototypeUi.Label(bg.transform,"Text","",16,4,728,54,23);
   var placeholder=PrototypeUi.Label(bg.transform,"Placeholder",hint,16,4,728,54,23,PrototypeUi.Muted);input.placeholder=placeholder;
   input.characterLimit=24;input.lineType=InputField.LineType.SingleLine;input.text=value??"";return input;
  }
  void ShowNew(){draftProfileName=DemoSaveStore.DefaultPlanName();draftCharacterName="";pendingCreatedProfile=null;ShowPlanName();}
  void ShowPlanName(){
   Page("创建新存档 · 1/2","先为计划命名；下一步设置个人AI调用ID。");
   PrototypeUi.Label(panel,"NameLabel","请为存档起个名字：",0,0,900,35,23);
   var input=NameInput("ProfileName",draftProfileName,"请输入存档名称");
   B(panel,"ConfirmNewProfile","创建完成",0,155,760,()=>{draftProfileName=DemoSaveStore.ValidateName(input.text,"存档名称");ShowCharacterName();});
   B(panel,"BackHome","取消并返回",0,245,300,ShowHome);
  }
  void ShowCharacterName(){
   Page("创建角色 · 2/2","计划："+draftProfileName+" · 调用ID将作为剧情中的角色称呼。");
   PrototypeUi.Label(panel,"CharacterNameLabel","请输入您的个人AI调用ID：",0,0,1000,35,25);
   var input=NameInput("CharacterName",draftCharacterName,"请输入角色名字");
   B(panel,"ConfirmCharacterName","确认并进入办公室",0,155,760,()=>{
    draftCharacterName=DemoSaveStore.ValidateName(input.text,"个人AI调用ID");
    if(pendingCreatedProfile==null)pendingCreatedProfile=DemoSaveStore.Create(draftProfileName,draftCharacterName);
    DemoSession.Profile=pendingCreatedProfile;DemoSession.ResumeRequested=false;UnityEngine.SceneManagement.SceneManager.LoadScene("Office");
   });
   B(panel,"BackToPlanName","上一步：修改存档名",0,245,380,()=>{draftCharacterName=input.text;ShowPlanName();});
   B(panel,"CancelNewProfile","取消创建",410,245,350,ShowHome);
  }
  public void ShowSlots(){Page("继续游戏 / 档案管理","载入有局内记录的档案会回到保存现场，并保持暂停。");var box=PrototypeUi.Box(panel,"SaveSlots",0,0,1390,345,PrototypeUi.Panel).rectTransform;box.GetComponent<Image>().raycastTarget=true;var scroll=box.gameObject.AddComponent<ScrollRect>();var view=PrototypeUi.Rect("Viewport",box,0,0,1390,345);view.gameObject.AddComponent<RectMask2D>();var entries=DemoSaveStore.List();var content=PrototypeUi.Rect("Content",view,0,0,1380,Mathf.Max(345,entries.Count*100));scroll.content=content;scroll.viewport=view;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;
   for(int i=0;i<entries.Count;i++){var e=entries[i];float y=i*100;string date=e.date;DateTime utc;if(DateTime.TryParse(e.date,out utc))date=utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");PrototypeUi.Label(content,"SaveDescription_"+e.id,e.name+"\n"+(e.error??("第"+e.level+"关 · 已解锁"+e.highestUnlockedLevel+"/"+DemoProgression.LevelCount+" · "+(e.hasCheckpoint?"局内现场已保存":"办公室")+" · "+date)),18,y+6,830,81,20);var load=B(content,"Load_"+e.id,"载入",875,y+20,165,()=>DemoSession.EnterProfile(DemoSaveStore.Load(e.id)));load.interactable=e.error==null;if(e.error!=null&&DemoSaveStore.HasBackup(e.id)){load.interactable=true;load.GetComponentInChildren<Text>().text="恢复备份";load.onClick.RemoveAllListeners();load.onClick.AddListener(()=>Run(()=>{var p=DemoSaveStore.LoadBackup(e.id);DemoSaveStore.Save(p);DemoSession.EnterProfile(p);}));}B(content,"Delete_"+e.id,"删除",1080,y+20,180,()=>ConfirmDelete(e));}
   if(entries.Count==0)PrototypeUi.Label(content,"NoSaves","尚无存档，请先创建新档案。",25,50,1000,70,23,PrototypeUi.Muted);B(panel,"BackHome","返回",0,380,260,ShowHome);
  }
  void ConfirmDelete(DemoSaveEntry e){Page("删除存档","即将删除“"+e.name+"”及其局内记录和备份，此操作无法撤销。");B(panel,"ConfirmDelete","确认删除",0,60,450,()=>{DemoSaveStore.Delete(e.id);ShowSlots();});B(panel,"CancelDelete","保留并返回",490,60,450,ShowSlots);}
  void ShowSettings(){Page("设置","调整音量和窗口显示方式；设置在下次启动时保留。");PrototypeUi.Label(panel,"VolumeLabel","主音量",0,0,350,40,23);var sliderRoot=PrototypeUi.Box(panel,"Volume",0,65,620,34,new Color32(20,29,47,255)).rectTransform;sliderRoot.GetComponent<Image>().raycastTarget=true;var slider=sliderRoot.gameObject.AddComponent<Slider>();var track=PrototypeUi.Box(sliderRoot,"Track",0,11,620,12,new Color32(45,59,82,255)).rectTransform;var fill=PrototypeUi.Box(track,"Fill",0,0,620,12,PrototypeUi.Accent).rectTransform;PrototypeUi.Fill(fill);var handleArea=PrototypeUi.Rect("HandleArea",sliderRoot,9,17,602,0);var handle=PrototypeUi.Box(handleArea,"Handle",0,0,18,34,Color.white).rectTransform;handle.pivot=new Vector2(.5f,.5f);handle.anchoredPosition=Vector2.zero;slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.value=AudioListener.volume;slider.onValueChanged.AddListener(v=>{AudioListener.volume=v;PlayerPrefs.SetFloat("cx_volume",v);});
   var full=B(panel,"Fullscreen",Screen.fullScreen?"显示：全屏（点击切换）":"显示：窗口（点击切换）",0,145,620,()=>{bool next=!Screen.fullScreen;Screen.fullScreen=next;PlayerPrefs.SetInt("cx_fullscreen",next?1:0);GameObject.Find("Fullscreen").GetComponentInChildren<Text>().text=next?"显示：全屏（点击切换）":"显示：窗口（点击切换）";});B(panel,"BackHome","保存并返回",0,245,350,()=>{PlayerPrefs.Save();ShowHome();});PrototypeUi.Label(panel,"SettingsNote","当前Demo尚未接入配乐，音量设置已预留。",720,30,600,90,20,PrototypeUi.Muted);
  }
  static void Quit(){PlayerPrefs.Save();Application.Quit();
   #if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
   #endif
  }
 }
}
