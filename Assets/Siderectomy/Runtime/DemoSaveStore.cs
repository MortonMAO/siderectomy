using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;

namespace Siderectomy {
 [Serializable] public sealed class DemoProfile {
  public int version=2,currentLevel=1,applyCount,highestUnlockedLevel=1;
  public List<int> completedLevels=new List<int>();
  public string id,name,characterName,createdUtc,updatedUtc;
  public string CharacterName {get{return string.IsNullOrWhiteSpace(characterName)?name:characterName;}}
  public string DialogueText(string value){return System.Text.RegularExpressions.Regex.Replace(value??"",@"\{player\}|xxx|（玩家ID）",m=>CharacterName??"");}
  public bool secondEnding,hasCheckpoint;
  public DemoSnapshot checkpoint;
  public DemoStoryState story;
 }
 [Serializable] sealed class DemoSaveEnvelope {public int version=1;public string payload,sha256;}
 public sealed class DemoSaveEntry {public string id,name,date,error;public int level,highestUnlockedLevel;public bool hasCheckpoint;}
 public static class DemoSaveStore {
  // Tests use a separate directory; real saves never live inside Assets or the build folder.
  public static string TestDirectory;
  public static string DirectoryPath {get{return TestDirectory??Path.Combine(Application.persistentDataPath,"Saves");}}
  static string FileFor(string id){Guid value;if(!Guid.TryParseExact(id,"N",out value))throw new IOException("存档编号无效");return Path.Combine(DirectoryPath,"slot_"+id+".json");}
  static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");}
  public static string DefaultPlanName(DateTimeOffset time){return time.ToOffset(TimeSpan.FromHours(8)).ToString("MMMdd-HHmm",System.Globalization.CultureInfo.InvariantCulture)+"号计划";}
  public static string DefaultPlanName(){return DefaultPlanName(DateTimeOffset.UtcNow);}
  public static string ValidateName(string value,string label){
   if(string.IsNullOrWhiteSpace(value))throw new ArgumentException("请输入"+label);
   value=value.Trim();if(value.Length>24||value.Any(char.IsControl))throw new ArgumentException(label+"需为1—24个字符，不能包含换行或控制字符");
   return value;
  }
  public static DemoProfile Create(string name){string value=string.IsNullOrWhiteSpace(name)?DefaultPlanName():name.Trim();if(value.Length>24)value=value.Substring(0,24);return Create(value,value);}
  public static DemoProfile Create(string name,string characterName){var p=new DemoProfile{id=Guid.NewGuid().ToString("N"),name=ValidateName(name,"存档名称"),characterName=ValidateName(characterName,"个人AI调用ID"),createdUtc=DateTime.UtcNow.ToString("o")};Save(p);return p;}
  public static void Save(DemoProfile profile){
   DemoProgression.Normalize(profile);DemoProgression.Validate(profile);
   profile.updatedUtc=DateTime.UtcNow.ToString("o");string payload=JsonUtility.ToJson(profile);var envelope=new DemoSaveEnvelope{payload=payload,sha256=Hash(payload)};
   Directory.CreateDirectory(DirectoryPath);string file=FileFor(profile.id),temp=file+".tmp";
   try{using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){var bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope,true));stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);}
   finally{if(File.Exists(temp))File.Delete(temp);}
  }
  static DemoProfile Read(string path,string id){var env=JsonUtility.FromJson<DemoSaveEnvelope>(File.ReadAllText(path,Encoding.UTF8));if(env==null||env.version!=1||string.IsNullOrEmpty(env.payload)||Hash(env.payload)!=env.sha256)throw new IOException("存档校验失败或版本不兼容");var p=JsonUtility.FromJson<DemoProfile>(env.payload);if(p==null||(p.version!=1&&p.version!=2)||p.id!=id)throw new IOException("存档数据无效");DemoProgression.Normalize(p);DemoProgression.Validate(p);if(p.hasCheckpoint)DemoSimulation.Restore(p.checkpoint,DemoRuleBook.Load());return p;}
  public static DemoProfile Load(string id){return Read(FileFor(id),id);}
  public static bool HasBackup(string id){return File.Exists(FileFor(id)+".bak");}
  public static DemoProfile LoadBackup(string id){return Read(FileFor(id)+".bak",id);}
  public static List<DemoSaveEntry> List(){var entries=new List<DemoSaveEntry>();if(!Directory.Exists(DirectoryPath))return entries;foreach(string file in Directory.GetFiles(DirectoryPath,"slot_*.json")){string id=Path.GetFileNameWithoutExtension(file).Substring(5);try{var p=Load(id);entries.Add(new DemoSaveEntry{id=id,name=p.name,date=p.updatedUtc,level=p.currentLevel,highestUnlockedLevel=p.highestUnlockedLevel,hasCheckpoint=p.hasCheckpoint});}catch(Exception ex){entries.Add(new DemoSaveEntry{id=id,name="无法读取的存档",error=ex.Message,date=File.GetLastWriteTimeUtc(file).ToString("o")});}}return entries.OrderByDescending(e=>e.date).ToList();}
  public static void Delete(string id){string path=FileFor(id);foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);if(DemoSession.Profile!=null&&DemoSession.Profile.id==id)DemoSession.Profile=null;}
 }
 public static class DemoSession {
  public static DemoProfile Profile;
  public static bool ResumeRequested;
  public static void Ensure(){if(Profile==null)Profile=DemoSaveStore.Create("临时调试档案");else{DemoProgression.Normalize(Profile);DemoProgression.Validate(Profile);}}
  public static void EnterSimulation(){Ensure();if(!DemoNarrativeSession.TestMode&&DemoNarrative.OfficePending(Profile))throw new InvalidOperationException("请先完成办公室剧情");ResumeRequested=Profile.hasCheckpoint;UnityEngine.SceneManagement.SceneManager.LoadScene("StarSystemTest");}
  public static void EnterProfile(DemoProfile p){DemoProgression.Normalize(p);DemoProgression.Validate(p);Profile=p;ResumeRequested=p.hasCheckpoint;UnityEngine.SceneManagement.SceneManager.LoadScene(ResumeRequested?"StarSystemTest":"Office");}
  public static void SaveProgress(DemoSnapshot snapshot,int level,int applyCount,bool secondEnding){Ensure();if(!DemoProgression.CanEnter(Profile,level))throw new IOException("该关卡尚未解锁");var next=JsonUtility.FromJson<DemoProfile>(JsonUtility.ToJson(Profile));next.checkpoint=snapshot;next.hasCheckpoint=snapshot!=null;next.currentLevel=level;next.applyCount=Math.Max(next.applyCount,applyCount);next.secondEnding|=secondEnding;DemoSaveStore.Save(next);Profile=next;}
  public static bool RecordVictory(DemoSimulation simulation){Ensure();var next=JsonUtility.FromJson<DemoProfile>(JsonUtility.ToJson(Profile));bool first=!DemoProgression.Completed(next,simulation.Level.number);if(!DemoProgression.Complete(next,simulation))return false;DemoNarrative.QueueVictory(next,simulation,first);DemoSaveStore.Save(next);Profile=next;return true;}
 }
}
