using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace Siderectomy {
 // All action cards expose the same replaceable Image slot, independently of their action text.
 public sealed class DemoOptionCard : MonoBehaviour {
  public Image Icon;public Text Content;public Button Button;
  public static DemoOptionCard Create(Transform parent,string name,float x,float y,float width,float height,UnityAction action){
   var b=PrototypeUi.Button(parent,name,"",x,y,width,height,action);var card=b.gameObject.AddComponent<DemoOptionCard>();card.Button=b;card.Content=b.GetComponentInChildren<Text>();card.Content.alignment=TextAnchor.UpperLeft;card.Content.fontSize=15;card.Content.rectTransform.anchoredPosition=new Vector2(76,-9);card.Content.rectTransform.sizeDelta=new Vector2(width-86,height-18);
   card.Icon=PrototypeUi.Box(b.transform,"UIIcon_代替换",10,12,54,54,new Color32(61,72,103,255));card.Icon.preserveAspect=true;var hint=PrototypeUi.Label(card.Icon.transform,"Placeholder","ICON",0,0,54,54,11,PrototypeUi.Muted);hint.alignment=TextAnchor.MiddleCenter;return card;
  }
  public void SetIcon(Sprite sprite){Icon.sprite=sprite;Icon.color=sprite==null?new Color32(61,72,103,255):Color.white;Icon.transform.GetChild(0).gameObject.SetActive(sprite==null);}
 }
}
