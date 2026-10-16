using System;
using System.Collections.Generic;
using System.Linq;

namespace Siderectomy {
 public static class DemoProgression {
  public static int LevelCount { get { return DemoLevel.Defaults().Length; } }
  public static bool CanEnter(DemoProfile profile,int level) { return profile!=null&&level>=1&&level<=LevelCount&&level<=profile.highestUnlockedLevel; }
  public static bool Completed(DemoProfile profile,int level) { return profile!=null&&profile.completedLevels!=null&&profile.completedLevels.Contains(level); }
  public static void Normalize(DemoProfile profile) {
   if(profile==null)throw new ArgumentException("存档为空");
   if(profile.version==1){
    // Legacy saves had free level selection. Preserve their actual saved position without inventing clears.
    int saved=profile.hasCheckpoint&&profile.checkpoint!=null&&profile.checkpoint.level!=null?profile.checkpoint.level.number:profile.currentLevel;
    profile.currentLevel=saved;profile.highestUnlockedLevel=saved;profile.completedLevels=new List<int>();profile.version=2;
   }
   if(profile.completedLevels==null)profile.completedLevels=new List<int>();
   if(string.IsNullOrWhiteSpace(profile.characterName))profile.characterName=profile.name;
   DemoNarrative.Normalize(profile);
  }
  public static void Validate(DemoProfile profile) {
   if(profile==null||profile.version!=2||profile.currentLevel<1||profile.currentLevel>LevelCount||profile.highestUnlockedLevel<1||profile.highestUnlockedLevel>LevelCount||!CanEnter(profile,profile.currentLevel))throw new ArgumentException("存档关卡进度无效");
   if(profile.completedLevels==null||profile.completedLevels.Any(n=>n<1||n>profile.highestUnlockedLevel)||profile.completedLevels.Distinct().Count()!=profile.completedLevels.Count)throw new ArgumentException("存档通关记录无效");
   if(profile.hasCheckpoint&&(profile.checkpoint==null||profile.checkpoint.level==null||profile.checkpoint.level.number!=profile.currentLevel||!CanEnter(profile,profile.checkpoint.level.number)))throw new ArgumentException("局内现场与已解锁关卡不一致");
  }
  public static bool Complete(DemoProfile profile,DemoSimulation simulation) {
   if(simulation==null||!simulation.Over||!simulation.Won)return false;
   Validate(profile);int level=simulation.Level.number;
   if(!CanEnter(profile,level))throw new ArgumentException("未解锁关卡不能登记通关");
   if(!profile.completedLevels.Contains(level))profile.completedLevels.Add(level);
   profile.completedLevels.Sort();profile.highestUnlockedLevel=Math.Max(profile.highestUnlockedLevel,Math.Min(LevelCount,level+1));
   profile.currentLevel=Math.Min(LevelCount,level+1);profile.checkpoint=null;profile.hasCheckpoint=false;
   profile.applyCount=Math.Max(profile.applyCount,simulation.ApplyCount);profile.secondEnding|=simulation.SecondEnding;
   Validate(profile);return true;
  }
 }
}
