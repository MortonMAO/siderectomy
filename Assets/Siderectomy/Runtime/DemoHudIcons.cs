using System;
using UnityEngine;
namespace Siderectomy {
 [Serializable] public sealed class DemoHudIconSlot {public string key;public Sprite icon;}
 [CreateAssetMenu(menuName="裁星计划/HUD图标配置",fileName="HUD图标配置_代替换")]
 public sealed class DemoHudIcons : ScriptableObject {
  [Tooltip("载体、科技、武装、打击模块按key对应。拖入正式Sprite即可替换图标，不改玩法脚本。")]
  public DemoHudIconSlot[] slots=new DemoHudIconSlot[0];
  public Sprite Find(string key){foreach(var slot in slots)if(slot.key==key&&slot.icon!=null)return slot.icon;return null;}
 }
}
