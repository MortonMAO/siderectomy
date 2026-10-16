using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Siderectomy {
 public sealed class DemoProfileUIQA:MonoBehaviour {
  static bool running;string directory;
  public static void TryStart(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--profile-ui-check");if(i<0||i+1>=args.Length||running)return;
   running=true;var qa=new GameObject("ProfileUIQA").AddComponent<DemoProfileUIQA>();DontDestroyOnLoad(qa.gameObject);
   qa.directory=args[i+1];Directory.CreateDirectory(qa.directory);DemoSaveStore.TestDirectory=Path.Combine(qa.directory,"TestSaves");
   Application.logMessageReceived+=qa.Log;qa.StartCoroutine(qa.Run());
  }
  void Log(string text,string stack,LogType kind){if(kind!=LogType.Exception&&kind!=LogType.Error)return;File.WriteAllText(Path.Combine(directory,"FAIL.txt"),text+"\n"+stack);Application.Quit(1);}
  void Check(bool v,string text){if(!v)throw new Exception("PROFILE_UI: "+text);}
  void Click(string name){var b=FindObjectsOfType<Button>().FirstOrDefault(x=>x.name==name&&x.gameObject.activeInHierarchy);Check(b!=null,"按钮缺失："+name);b.onClick.Invoke();}
  InputField Input(string name){var f=FindObjectsOfType<InputField>().FirstOrDefault(x=>x.name==name&&x.gameObject.activeInHierarchy);Check(f!=null,"输入框缺失："+name);return f;}
  string Text(string name){var t=FindObjectsOfType<Text>().FirstOrDefault(x=>x.name==name&&x.gameObject.activeInHierarchy);return t==null?"":t.text;}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+".png"),t.EncodeToPNG());Destroy(t);}
  IEnumerator Run(){
   yield return null;Check(FindObjectOfType<DemoFrontMenu>()!=null,"未从标题进入");
   Click("NewProfile");yield return null;
   var name=Input("ProfileName");Check(System.Text.RegularExpressions.Regex.IsMatch(name.text,@"^[A-Z][a-z]{2}\d{2}-\d{4}号计划$"),"默认名称不匹配");
   yield return Shot("01_存档命名");
   name.text="";Click("ConfirmNewProfile");yield return null;Check(Input("ProfileName")!=null&&DemoSaveStore.List().Count==0,"空存档名被接受");
   name.text="Oct15-1230号计划";Click("ConfirmNewProfile");yield return null;
   Check(Text("CharacterNameLabel")=="请输入您的个人AI调用ID："&&DemoSaveStore.List().Count==0,"第一步直接建档或缺少角色提示");
   yield return Shot("02_个人AI调用ID");
   Click("ConfirmCharacterName");yield return null;Check(SceneManager.GetActiveScene().name=="MainMenu"&&DemoSaveStore.List().Count==0,"空角色名建档");
   Input("CharacterName").text="冻橙";Click("BackToPlanName");yield return null;Check(Input("ProfileName").text=="Oct15-1230号计划","上一步丢失计划名称");
   Click("ConfirmNewProfile");yield return null;Check(Input("CharacterName").text=="冻橙","返回第二步丢失角色名");
   Click("CancelNewProfile");yield return null;Check(DemoSaveStore.List().Count==0,"取消后遗留半成品存档");
   Click("NewProfile");yield return null;Input("ProfileName").text="Oct15-1230号计划";Click("ConfirmNewProfile");yield return null;Input("CharacterName").text="冻橙";Click("ConfirmCharacterName");yield return null;yield return null;
   Check(SceneManager.GetActiveScene().name=="Office"&&DemoSaveStore.List().Count==1,"未唯一建档并进入办公室");
   Check(DemoSession.Profile.name=="Oct15-1230号计划"&&DemoSession.Profile.CharacterName=="冻橙","双名字混用");
   Check(DemoSession.Profile.story.cursor=="intro.welcome"&&Text("DialogueText")=="欢迎回来，冻橙","欢迎剧情缺失或名字错误");
   yield return Shot("03_欢迎回来");
   var loaded=DemoSaveStore.Load(DemoSession.Profile.id);Check(loaded.story.cursor=="intro.welcome"&&loaded.CharacterName=="冻橙","欢迎处读档丢失角色/进度");
   Click("DialogueNext");yield return null;Check(Text("Speaker")=="冻橙"&&Text("DialogueText")=="……休息日没睡好，早上有点晕。","截图台词或角色名不匹配");
   yield return Shot("04_接回原剧情");
   Click("DialogueNext");yield return null;Check(DemoSession.Profile.story.cursor=="intro.ai"&&Text("Speaker")=="飞白","后续剧情未接回");
   File.WriteAllText(Path.Combine(directory,"PASS.txt"),"PASS 真实两阶段UI、默认计划名、空输入校验、返回保留草稿、取消不建档、唯一建档、存档名/角色名分离、欢迎→截图原文→后续剧情、中途存档。");Application.Quit(0);
  }
 }
}
