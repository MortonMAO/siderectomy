using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Siderectomy;
using UnityEditor;
using UnityEngine;
public static class DemoHudAssetBuilder {
 const string Art="Assets/Siderectomy/Art/HUD_代替换/",AssetPath="Assets/Siderectomy/Resources/HUD图标配置_代替换.asset";
 [InitializeOnLoadMethod]static void Register(){EditorApplication.delayCall+=AutoPrepare;}
 static void AutoPrepare(){if(Application.isBatchMode)return;if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=AutoPrepare;return;}if(!File.Exists(AssetPath))Prepare();}
 [MenuItem("裁星计划/创建或选中HUD图标配置")]
 public static void Select(){Prepare();Selection.activeObject=AssetDatabase.LoadAssetAtPath<DemoHudIcons>(AssetPath);}
 public static void Prepare(){
  if(AssetDatabase.LoadAssetAtPath<DemoHudIcons>(AssetPath)!=null)return;
  Configure("科技芯片_代替换.png",false);Configure("武装打击_代替换.png",false);Configure("载体图集_代替换.png",true);
  var carrierSprites=AssetDatabase.LoadAllAssetsAtPath(Art+"载体图集_代替换.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
  if(carrierSprites.Length!=6)throw new Exception("载体图集切片失败");
  var chip=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"科技芯片_代替换.png");var rocket=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"武装打击_代替换.png");
  var slots=new List<DemoHudIconSlot>();string[] carriers={"dust","satellite","asteroid","planet"};for(int i=0;i<4;i++)slots.Add(new DemoHudIconSlot{key="carrier:"+carriers[i],icon=carrierSprites[i]});
  foreach(string key in new[]{"stealth","dissolve","calibration"})slots.Add(new DemoHudIconSlot{key="research:"+key,icon=chip});
  foreach(string key in new[]{"a1","a2","a3"})slots.Add(new DemoHudIconSlot{key="armament:"+key,icon=rocket});
  foreach(string key in DemoSimulation.AttackModuleKeys)slots.Add(new DemoHudIconSlot{key="attackModule:"+key,icon=rocket});
  var config=ScriptableObject.CreateInstance<DemoHudIcons>();config.slots=slots.ToArray();AssetDatabase.CreateAsset(config,AssetPath);AssetDatabase.SaveAssets();
 }
 static void Configure(string file,bool sheet){var importer=AssetImporter.GetAtPath(Art+file) as TextureImporter;if(importer==null)throw new Exception("缺少HUD占位图片："+file);
  importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=sheet?SpriteImportMode.Multiple:SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
  if(sheet){int[] xs={65,355},ys={80,375,670};var slices=new SpriteMetaData[6];for(int i=0;i<6;i++)slices[i]=new SpriteMetaData{name="载体"+i+"_代替换",rect=new Rect(xs[i%2],991-ys[i/2]-250,250,250),alignment=0,pivot=new Vector2(.5f,.5f)};importer.spritesheet=slices;}
  importer.SaveAndReimport();
 }
 public static void ValidateAndBuild(){Prepare();DemoHudChecks.Run();DemoBuilder.ValidateAndBuildWindows();}
}
