using System;
using System.IO;
using Siderectomy;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class DemoBuilder {
 const string Main="Assets/Siderectomy/Scenes/MainMenu.unity";
 const string Scene="Assets/Siderectomy/Scenes/StarSystemTest.unity";
 [InitializeOnLoadMethod]
 static void AutoOpen(){if(Application.isBatchMode)return;EditorApplication.delayCall+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode)return;var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!active.isDirty&&(string.IsNullOrEmpty(active.path)||active.path=="Assets/Scenes/SampleScene.unity"))Open();};}
 [MenuItem("裁星计划/打开开始界面")]
 public static void OpenMain(){Prepare();EditorSceneManager.OpenScene(Main);}
 [MenuItem("裁星计划/打开六关Demo")]
 public static void Open(){Prepare();EditorSceneManager.OpenScene(Scene);}
 [MenuItem("裁星计划/选中六关参数")]
 public static void Config(){Prepare();Selection.activeObject=AssetDatabase.LoadAssetAtPath<DemoCampaign>("Assets/Siderectomy/Resources/DemoCampaign.asset");}
 public static void Prepare(){
  DemoTitleAssets.Prepare();
  var c=AssetDatabase.LoadAssetAtPath<DemoCampaign>("Assets/Siderectomy/Resources/DemoCampaign.asset");
  if(c==null){c=ScriptableObject.CreateInstance<DemoCampaign>();AssetDatabase.CreateAsset(c,"Assets/Siderectomy/Resources/DemoCampaign.asset");}
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Main,true),new EditorBuildSettingsScene(Scene,true),new EditorBuildSettingsScene("Assets/Siderectomy/Scenes/Office.unity",true)};
  PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
  PlayerSettings.productName="裁星计划 1–4关 Demo";PlayerSettings.companyName="BY冻橙更新版";PlayerSettings.runInBackground=true;AssetDatabase.SaveAssets();
 }
 [MenuItem("裁星计划/验证并构建Windows Demo")]
 public static void ValidateAndBuildWindows(){
  Prepare();DemoModuleChecks.Run();Directory.CreateDirectory("Builds/WindowsDemo");
  var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Main,Scene,"Assets/Siderectomy/Scenes/Office.unity"},locationPathName="Builds/WindowsDemo/裁星计划Demo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
  if(r.summary.result!=BuildResult.Succeeded)throw new Exception("构建失败："+r.summary.result);
  EditorSceneManager.OpenScene(Scene);Debug.Log("DEMO_BUILD_OK");
 }
 [MenuItem("裁星计划/运行局内规则验收")]
 public static void Validate(){Prepare();DemoModuleChecks.Run();}
}
