using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Siderectomy {
 public sealed class DemoLogFeed : MonoBehaviour {
  sealed class Entry {public RectTransform rect;public CanvasGroup group;public float born;}
  readonly List<Entry> entries=new List<Entry>();
  public const float FadeIn=.25f,Hold=3f,FadeOut=.6f;
  public int Count {get{return entries.Count;}}
  public void Clear(){foreach(var e in entries)Destroy(e.rect.gameObject);entries.Clear();}
  public void Push(string text){if(string.IsNullOrEmpty(text))return;if(entries.Count==4){Destroy(entries[0].rect.gameObject);entries.RemoveAt(0);}var row=PrototypeUi.Box(transform,"LogToast",0,150,530,38,new Color32(19,29,49,255)).rectTransform;var group=row.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;PrototypeUi.Box(row,"Accent",0,0,3,38,PrototypeUi.Accent);var label=PrototypeUi.Label(row,"Message",text,12,2,508,34,14);label.resizeTextForBestFit=true;label.resizeTextMinSize=11;label.resizeTextMaxSize=14;entries.Add(new Entry{rect=row,group=group,born=Time.unscaledTime});}
  void Update(){for(int i=entries.Count-1;i>=0;i--){var e=entries[i];float age=Time.unscaledTime-e.born;if(age>=FadeIn+Hold+FadeOut){Destroy(e.rect.gameObject);entries.RemoveAt(i);continue;}e.group.alpha=age<FadeIn?age/FadeIn:age<FadeIn+Hold?1:1-(age-FadeIn-Hold)/FadeOut;}for(int i=0;i<entries.Count;i++){var e=entries[i];var target=new Vector2(0,-(4-entries.Count+i)*41);e.rect.anchoredPosition=Vector2.Lerp(e.rect.anchoredPosition,target,1-Mathf.Exp(-14*Time.unscaledDeltaTime));}}
 }
}
