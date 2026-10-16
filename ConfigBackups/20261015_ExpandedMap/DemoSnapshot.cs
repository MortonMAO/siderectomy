using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Siderectomy {
 [Serializable] public sealed class DemoSavedNumber {public string key;public double value;}
 [Serializable] public sealed class DemoSnapshot {
  public int version=3,serial;public uint rng;
  public string RunId;
  public bool ReportedNoEscape;
  public int SolarStoryPhase;public string SolarChoice;public bool DebugAllFeatures;
  public double SolarSecondAt,SolarReservedPopulation;
  public int TutorialStep;public bool TutorialArmamentProduced,TutorialModuleUsed,TutorialSoftOperation;
  public DemoLevel level;public double[] resources;
  public DemoSavedNumber[] facilities,carriers,research,modules,technologyModules;
  public string[] draftTechnologies;
  public DemoAI AIQueue;
  public string localBodyId,actionTab,operationKey,carrierKey;public bool infoHide,physicalHide;
  public double Time,Alert,Exposure,Tech,Escape,EscapedPop,Killed,Entropy,Peak,InitialPop;
  public int InitialNodes,Escaped,EverBuilt,ApplyCount,Revision,Speed,SelectedShip;
  public bool Paused,Over,Won,EscapeActive,GlobalProbe,SecondEnding;
  public string Selected,TargetNode,Notice,ResultReason;
  public DemoBody[] Bodies;
  public DemoJob[] Jobs;
  public DemoMission[] Missions;
  public DemoShip[] Ships;
  public DemoAI[] AIFacilities;
  public DemoVirus[] Viruses;
  public DemoEffect[] Effects;
  public string[] Logs;
  public int[] Thresholds;
 }
 public sealed partial class DemoSimulation {
  public DemoSnapshot Capture(){var snapshot=new DemoSnapshot {
   level=Level.Copy(),RunId=RunId,ReportedNoEscape=ReportedNoEscape,resources=(double[])Res.Clone(),rng=rng,serial=serial,AIQueue=AIQueue,
   facilities=Facilities.Select(p=>new DemoSavedNumber{key=p.Key,value=p.Value}).ToArray(),
   carriers=Carriers.Select(p=>new DemoSavedNumber{key=p.Key,value=p.Value}).ToArray(),
   research=Research.Select(p=>new DemoSavedNumber{key=p.Key,value=p.Value}).ToArray(),
   modules=AttackModules.Select(p=>new DemoSavedNumber{key=p.Key,value=p.Value}).ToArray(),
   technologyModules=TechnologyModules.Select(p=>new DemoSavedNumber{key=p.Key,value=p.Value}).ToArray(),
   SolarStoryPhase=SolarStoryPhase,SolarChoice=SolarChoice,DebugAllFeatures=DebugAllFeatures,SolarSecondAt=SolarSecondAt,SolarReservedPopulation=SolarReservedPopulation,
   TutorialStep=TutorialStep,TutorialArmamentProduced=TutorialArmamentProduced,TutorialModuleUsed=TutorialModuleUsed,TutorialSoftOperation=TutorialSoftOperation,
   Time=Time,
   Alert=Alert,
   Exposure=Exposure,
   Tech=Tech,
   Escape=Escape,
   EscapedPop=EscapedPop,
   Killed=Killed,
   Entropy=Entropy,
   Peak=Peak,
   InitialPop=InitialPop,
   InitialNodes=InitialNodes,
   Escaped=Escaped,
   EverBuilt=EverBuilt,
   ApplyCount=ApplyCount,
   Revision=Revision,
   Speed=Speed,
   SelectedShip=SelectedShip,
   Paused=Paused,
   Over=Over,
   Won=Won,
   EscapeActive=EscapeActive,
   GlobalProbe=GlobalProbe,
   SecondEnding=SecondEnding,
   Selected=Selected,
   TargetNode=TargetNode,
   Notice=Notice,
   ResultReason=ResultReason,
   Bodies=Bodies.ToArray(),
   Jobs=Jobs.ToArray(),
   Missions=Missions.ToArray(),
   Ships=Ships.ToArray(),
   AIFacilities=AIFacilities.ToArray(),
   Viruses=Viruses.ToArray(),
   Effects=Effects.ToArray(),
   Logs=Logs.ToArray(),
   Thresholds=Thresholds.ToArray(),
  };return JsonUtility.FromJson<DemoSnapshot>(JsonUtility.ToJson(snapshot));}
  public static DemoSimulation Restore(DemoSnapshot snapshot,DemoRuleBook rules){
   if(snapshot==null||(snapshot.version<1||snapshot.version>3)||snapshot.level==null||snapshot.resources==null||snapshot.resources.Length!=6||snapshot.Bodies==null||snapshot.Bodies.Length==0)throw new ArgumentException("局内存档缺少必要数据或版本不兼容");
   // Deep clone so a resumed world cannot mutate the checkpoint held by the profile.
   var d=JsonUtility.FromJson<DemoSnapshot>(JsonUtility.ToJson(snapshot));var s=new DemoSimulation(d.level,rules);
   Array.Copy(d.resources,s.Res,6);s.rng=d.rng;s.serial=d.serial;s.AIQueue=d.AIQueue;
   if(!string.IsNullOrEmpty(d.RunId))s.RunId=d.RunId;
   s.ReportedNoEscape=d.ReportedNoEscape;
   foreach(var p in d.facilities)s.Facilities[p.key]=(int)p.value;
   foreach(var p in d.carriers)s.Carriers[p.key]=(int)p.value;
   s.Research.Clear();foreach(var p in d.research)if(rules.Find("research",p.key)!=null)s.Research[p.key]=p.value;
   if(d.modules!=null)foreach(var p in d.modules)if(s.AttackModules.ContainsKey(p.key))s.AttackModules[p.key]=(int)p.value;
   if(d.technologyModules!=null)foreach(var p in d.technologyModules)if(s.TechnologyModules.ContainsKey(p.key))s.TechnologyModules[p.key]=(int)p.value;
   s.SolarStoryPhase=d.SolarStoryPhase;s.SolarChoice=d.SolarChoice??"";s.DebugAllFeatures=d.DebugAllFeatures;
   s.SolarSecondAt=d.SolarSecondAt;s.SolarReservedPopulation=d.SolarReservedPopulation;
   s.TutorialStep=d.TutorialStep;s.TutorialArmamentProduced=d.TutorialArmamentProduced;s.TutorialModuleUsed=d.TutorialModuleUsed;s.TutorialSoftOperation=d.TutorialSoftOperation;
   s.Time=d.Time;
   s.Alert=d.Alert;
   s.Exposure=d.Exposure;
   s.Tech=d.Tech;
   s.Escape=d.Escape;
   s.EscapedPop=d.EscapedPop;
   s.Killed=d.Killed;
   s.Entropy=d.Entropy;
   s.Peak=d.Peak;
   s.InitialPop=d.InitialPop;
   s.InitialNodes=d.InitialNodes;
   s.Escaped=d.Escaped;
   s.EverBuilt=d.EverBuilt;
   s.ApplyCount=d.ApplyCount;
   s.Revision=d.Revision;
   s.Speed=d.Speed;
   s.SelectedShip=d.SelectedShip;
   s.Paused=d.Paused;
   s.Over=d.Over;
   s.Won=d.Won;
   s.EscapeActive=d.EscapeActive;
   s.GlobalProbe=d.GlobalProbe;
   s.SecondEnding=d.SecondEnding;
   s.Selected=d.Selected;
   s.TargetNode=d.TargetNode;
   s.Notice=d.Notice;
   s.ResultReason=d.ResultReason;
   s.Bodies.Clear();if(d.Bodies!=null)s.Bodies.AddRange(d.Bodies);
   s.Jobs.Clear();if(d.Jobs!=null)s.Jobs.AddRange(d.Jobs.Where(j=>j.kind=="apply"||rules.Find(j.kind,j.key)!=null));
   s.Missions.Clear();if(d.Missions!=null)foreach(var m in d.Missions){
    if(m.op=="infoVirus")m.op="infoInvasion";
    if(m.op=="virus"||m.op=="infoInvasion"){m.technology=(m.technology??new string[0]).Concat(new[]{m.op}).Distinct().ToArray();m.op="technology";}
    if(m.op=="technology"||rules.Find("operation",m.op)!=null)s.Missions.Add(m);
   }
   s.Ships.Clear();if(d.Ships!=null)s.Ships.AddRange(d.Ships);
   s.AIFacilities.Clear();if(d.AIFacilities!=null)s.AIFacilities.AddRange(d.AIFacilities);
   s.Viruses.Clear();if(d.Viruses!=null)s.Viruses.AddRange(d.Viruses);
   s.Effects.Clear();if(d.Effects!=null)s.Effects.AddRange(d.Effects);
   s.Logs.Clear();if(d.Logs!=null)s.Logs.AddRange(d.Logs);
   s.Thresholds.Clear();if(d.Thresholds!=null)s.Thresholds.AddRange(d.Thresholds);
   if(d.version==1&&s.Level.number==4&&rules.solarStory.enabled&&s.SolarStoryPhase==0){var earth=s.Find("p2");s.SolarReservedPopulation=Math.Min(earth.pop,rules.solarStory.reservedPopulation);earth.pop-=s.SolarReservedPopulation;}
   foreach(var b in s.Bodies){if(string.IsNullOrEmpty(b.species))b.species="carbon";if(b.links==null)b.links=new List<DemoInformationLink>();}
   if(s.Find(s.Selected)==null||s.Speed<1||s.Speed>4||double.IsNaN(s.Time)||s.Time<0||s.Res.Any(v=>double.IsNaN(v)||double.IsInfinity(v)||v<0))throw new ArgumentException("局内存档状态无效");
   return s;
  }
 }
}
