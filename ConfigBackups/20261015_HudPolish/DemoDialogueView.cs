using System;
using UnityEngine;
using UnityEngine.UI;
namespace Siderectomy {
 // One renderer and runner for office scenes, mission story and tutorial cues.
 public sealed class DemoDialogueView:MonoBehaviour {
  RectTransform root,options;Text speaker,body,caption,error;Button next;string shown;float elapsed;
  Action<DemoStoryState> commit;Func<bool> pump;
  public bool Active {get{return root!=null&&root.gameObject.activeSelf;}}
  public void Initialize(Transform parent,Action<DemoStoryState> save,Func<bool> nextTrigger,bool compact=false){
   commit=save;pump=nextTrigger;
   root=PrototypeUi.Box(parent,"StoryDialogue",0,0,1600,900,new Color32(4,10,19,compact?(byte)100:(byte)215)).rectTransform;root.GetComponent<Image>().raycastTarget=true;root.SetAsLastSibling();
   var panel=PrototypeUi.Box(root,"DialoguePanel",compact?710:150,compact?470:320,compact?870:1300,compact?410:540,PrototypeUi.Panel).rectTransform;
   float w=compact?814:1244;
   caption=PrototypeUi.Label(panel,"StoryDate","",28,12,w,30,17,PrototypeUi.Muted);
   speaker=PrototypeUi.Label(panel,"Speaker","",28,48,w,35,25,PrototypeUi.Accent);
   var viewport=PrototypeUi.Rect("TextViewport",panel,28,94,w,compact?105:200);viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
   var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.scrollSensitivity=35;
   var content=PrototypeUi.Rect("Content",viewport,0,0,w-18,200);body=PrototypeUi.Label(content,"DialogueText","",0,0,w-18,200,22);body.alignment=TextAnchor.UpperLeft;scroll.viewport=viewport;scroll.content=content;
   options=PrototypeUi.Rect("Choices",panel,28,compact?210:305,w,160);
   next=PrototypeUi.Button(panel,"DialogueNext","继续",compact?632:1062,compact?354:479,210,40,()=>Advance(null,false));
   error=PrototypeUi.Label(panel,"DialogueError","",28,compact?354:479,compact?590:1020,40,16,new Color32(236,194,123,255));
   Refresh();
  }
  public void Refresh(){
   var p=DemoSession.Profile;if(p==null)return;bool active=DemoNarrative.Busy(p.story);root.gameObject.SetActive(active);if(!active){shown=null;return;}
   var n=DemoStoryBook.Load().Find(p.story.cursor);if(n==null)return;if(shown==n.id)return;shown=n.id;elapsed=0;
   caption.text="第"+n.day+"天 · "+(p.story.activeTrigger.StartsWith("office.")?"办公室通讯":p.story.activeTrigger.StartsWith("tutorial.")?"操作引导":"执行终端通讯");
   speaker.text=p.DialogueText(n.speaker);body.text=p.DialogueText(n.text);
   float h=Mathf.Max(200,body.preferredHeight+12);body.rectTransform.sizeDelta=new Vector2(body.rectTransform.sizeDelta.x,h);((RectTransform)body.transform.parent).sizeDelta=new Vector2(body.rectTransform.sizeDelta.x,h);((RectTransform)body.transform.parent).anchoredPosition=Vector2.zero;
   for(int i=options.childCount-1;i>=0;i--){var old=options.GetChild(i).gameObject;old.SetActive(false);Destroy(old);}
   bool choice=n.choices!=null&&n.choices.Length>0;next.gameObject.SetActive(!choice);error.text="";
   if(choice)for(int i=0;i<n.choices.Length;i++){var c=n.choices[i];var button=PrototypeUi.Button(options,"Choice_"+c.id,c.text,0,i*49,options.rect.width,44,()=>Advance(c.id,false));button.GetComponentInChildren<Text>().fontSize=18;}
  }
  void Advance(string choice,bool timed){
   try{var state=JsonUtility.FromJson<DemoStoryState>(JsonUtility.ToJson(DemoSession.Profile.story));DemoNarrative.Step(state,DemoStoryBook.Load(),choice,timed);DemoNarrative.RefreshEnding(state);commit(state);shown=null;if(!DemoNarrative.Busy(state)&&pump!=null)pump();Refresh();}
   catch(Exception ex){error.text="保存失败，未推进："+ex.Message;elapsed=0;}
  }
  void Update(){
   Refresh();if(!Active)return;var n=DemoStoryBook.Load().Find(DemoSession.Profile.story.cursor);
   if(n.timeout>0){elapsed+=Time.unscaledDeltaTime;error.text="等待 "+Mathf.CeilToInt(Mathf.Max(0,n.timeout-elapsed))+" 秒";if(elapsed>=n.timeout)Advance(null,true);}
   if((n.choices==null||n.choices.Length==0)&&(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)))Advance(null,false);
  }
 }
}
