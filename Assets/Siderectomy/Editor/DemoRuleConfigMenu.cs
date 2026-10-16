using UnityEditor;
using UnityEngine;
public static class DemoRuleConfigMenu {
 [MenuItem("裁星计划/配置表/打开全局数值规则")]
 public static void OpenRules(){var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Siderectomy/Resources/DemoRules.json");Selection.activeObject=asset;AssetDatabase.OpenAsset(asset);}
 [MenuItem("裁星计划/配置表/选中关卡参数")]
 public static void OpenLevels(){DemoBuilder.Config();}
}
