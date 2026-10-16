using System.Linq;
using UnityEditor;
using UnityEngine;
using Siderectomy;
public static class DemoSixCampaignUpgrade {
 [MenuItem("裁星计划/更新六关战役配置")]
 public static void Run(){
  var campaign=AssetDatabase.LoadAssetAtPath<DemoCampaign>("Assets/Siderectomy/Resources/DemoCampaign.asset");
  if(campaign==null){campaign=ScriptableObject.CreateInstance<DemoCampaign>();AssetDatabase.CreateAsset(campaign,"Assets/Siderectomy/Resources/DemoCampaign.asset");}
  var defaults=DemoLevel.Defaults();
  campaign.levels=defaults.Select(d=>{
   var old=campaign.levels==null?null:campaign.levels.FirstOrDefault(l=>l.number==d.number);
   if(old==null)return d;
   var l=old.Copy();l.title=d.title;l.briefing=d.briefing;l.tutorial=d.tutorial;l.nodeGate=0;l.protection=0;l.info=System.Math.Max(l.info,d.info);if(l.number==3)l.limit=System.Math.Max(l.limit,d.limit);if(l.number==4)l.escapeFlightSeconds=System.Math.Max(l.escapeFlightSeconds,90);
   return l;
  }).ToArray();
  EditorUtility.SetDirty(campaign);AssetDatabase.SaveAssets();Debug.Log("SIX_CAMPAIGN_UPGRADED");
 }
}
