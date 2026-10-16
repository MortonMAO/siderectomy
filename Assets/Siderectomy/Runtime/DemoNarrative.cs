using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Siderectomy {
 [Serializable] public sealed class DemoStoryChoice { public string id,text,next; public string[] set; }
 [Serializable] public sealed class DemoStoryJump { public string flag,next; public bool absent; }
 [Serializable] public sealed class DemoStoryNode {
  public string id,speaker,text,next; public int day,sourceParagraph;
  public float timeout; public string timeoutNext;
  public DemoStoryChoice[] choices; public DemoStoryJump[] jumps; public string[] set;
 }
 [Serializable] public sealed class DemoStoryTrigger { public string id,start; public int afterLevel; }
 [Serializable] public sealed class DemoStoryBook {
  public int version=1; public DemoStoryNode[] nodes; public DemoStoryTrigger[] triggers;
  static DemoStoryBook cached;
  public static DemoStoryBook Load(){if(cached==null){var file=Resources.Load<TextAsset>("DemoNarrative");if(file==null)throw new InvalidOperationException("缺少 DemoNarrative.json");cached=JsonUtility.FromJson<DemoStoryBook>(file.text);cached.Validate();}return cached;}
  public DemoStoryNode Find(string id){return nodes.FirstOrDefault(n=>n.id==id);}
  public void Validate(){
   if(nodes==null||triggers==null||nodes.Any(n=>string.IsNullOrEmpty(n.id))||nodes.Select(n=>n.id).Distinct().Count()!=nodes.Length)throw new InvalidOperationException("剧情节点编号重复或为空");
   Action<string> edge=id=>{if(!string.IsNullOrEmpty(id)&&Find(id)==null)throw new InvalidOperationException("剧情跳转不存在："+id);};
   foreach(var n in nodes){edge(n.next);edge(n.timeoutNext);if(n.choices!=null){if(n.choices.Select(c=>c.id).Distinct().Count()!=n.choices.Length)throw new InvalidOperationException("选项编号重复："+n.id);foreach(var c in n.choices)edge(c.next);}if(n.jumps!=null)foreach(var j in n.jumps)edge(j.next);}
   foreach(var t in triggers)edge(t.start);
  }
 }
 [Serializable] public sealed class DemoStoryDecision {public string trigger,node,choice,utc;}
 [Serializable] public sealed class DemoEscapeRecord {
  public string run;public int level,departed,escaped,applications;public double escapedPopulation;
  public bool won,over,canonical;public string solarChoice;
 }
 [Serializable] public sealed class DemoStoryState {
  public int version=1;
  public string activeTrigger="",cursor="",ending="";
  public List<string> flags=new List<string>(),completed=new List<string>(),pending=new List<string>();
  public List<DemoStoryDecision> decisions=new List<DemoStoryDecision>();
  public List<DemoEscapeRecord> attempts=new List<DemoEscapeRecord>();
 }
 public static class DemoNarrative {
  public static void Normalize(DemoProfile p){
   if(p.story==null){p.story=new DemoStoryState();
    // Old clears have no historical evidence. Do not fabricate choices or replay past scenes.
    foreach(int l in p.completedLevels)p.story.completed.Add("office.clear."+l);
    if(p.completedLevels.Count>0||p.hasCheckpoint)p.story.completed.Add("office.intro");
   }
   var s=p.story;
   if(s.flags==null)s.flags=new List<string>();if(s.completed==null)s.completed=new List<string>();if(s.pending==null)s.pending=new List<string>();
   if(s.decisions==null)s.decisions=new List<DemoStoryDecision>();if(s.attempts==null)s.attempts=new List<DemoEscapeRecord>();
  }
  public static bool Has(DemoStoryState s,string flag){return s.flags.Contains(flag);}
  public static void Set(DemoStoryState s,params string[] flags){if(flags==null)return;foreach(var flag in flags)if(!string.IsNullOrEmpty(flag)&&!s.flags.Contains(flag))s.flags.Add(flag);}
  public static bool Busy(DemoStoryState s){return !string.IsNullOrEmpty(s.cursor);}
  public static bool OfficePending(DemoProfile p){return Busy(p.story)&&p.story.activeTrigger.StartsWith("office.")||p.story.pending.Any(x=>x.StartsWith("office."))||(!p.story.completed.Contains("office.intro")&&p.completedLevels.Count==0);}
  public static bool Start(DemoStoryState s,DemoStoryBook book,string trigger){
   if(Busy(s)||s.completed.Contains(trigger))return false;
   var t=book.triggers.FirstOrDefault(x=>x.id==trigger);if(t==null)return false;
   s.activeTrigger=trigger;s.cursor=t.start;s.pending.Remove(trigger);Enter(s,book);return true;
  }
  static void Enter(DemoStoryState s,DemoStoryBook b){
   for(int guard=0;guard<128;guard++){
    if(string.IsNullOrEmpty(s.cursor)){s.completed.Add(s.activeTrigger);s.activeTrigger="";return;}
    var n=b.Find(s.cursor);if(n==null)throw new InvalidOperationException("剧情节点已失效："+s.cursor);
    Set(s,n.set);
    if(n.jumps!=null&&n.jumps.Length>0){var j=n.jumps.FirstOrDefault(x=>Has(s,x.flag)!=x.absent);s.cursor=j==null?n.next:j.next;continue;}
    if(string.IsNullOrEmpty(n.text)&&(n.choices==null||n.choices.Length==0)){s.cursor=n.next;continue;}
    return;
   }throw new InvalidOperationException("剧情存在无文本循环");
  }
  public static void Step(DemoStoryState s,DemoStoryBook b,string choice=null,bool timed=false){
   if(!Busy(s))return;var n=b.Find(s.cursor);
   if(timed){if(n.timeout<=0||string.IsNullOrEmpty(n.timeoutNext))return;s.decisions.Add(new DemoStoryDecision{trigger=s.activeTrigger,node=n.id,choice="timeout",utc=DateTime.UtcNow.ToString("o")});s.cursor=n.timeoutNext;}
   else if(n.choices!=null&&n.choices.Length>0){var c=n.choices.FirstOrDefault(x=>x.id==choice);if(c==null)return;s.decisions.Add(new DemoStoryDecision{trigger=s.activeTrigger,node=n.id,choice=c.id,utc=DateTime.UtcNow.ToString("o")});Set(s,c.set);s.cursor=c.next;}
   else s.cursor=n.next;
   Enter(s,b);
  }
  public static void QueueVictory(DemoProfile p,DemoSimulation sim,bool first){
   Record(p,sim);if(!first)return;string id="office.clear."+sim.Level.number;
   if(!p.story.pending.Contains(id)&&!p.story.completed.Contains(id))p.story.pending.Add(id);
  }
  public static void Record(DemoProfile p,DemoSimulation sim){
   var s=p.story;var r=s.attempts.FirstOrDefault(x=>x.run==sim.RunId);
   if(r==null){r=new DemoEscapeRecord{run=sim.RunId,level=sim.Level.number};s.attempts.Add(r);}
   r.departed=sim.Ships.Count;r.escaped=sim.Escaped;r.escapedPopulation=sim.EscapedPop;r.solarChoice=sim.SolarChoice;r.applications=sim.ApplyCount;r.over=sim.Over;r.won=sim.Won;
   if(sim.Won&&!s.attempts.Any(x=>x.level==r.level&&x.canonical))r.canonical=true;
   if(sim.Level.number==4&&!string.IsNullOrEmpty(sim.SolarChoice))Set(s,sim.SolarChoice=="A"?"solar_allow":"solar_intercept");
   if(sim.Escaped>0&&Has(s,"rosetta_available"))Set(s,"rosetta_overwrite");
   RefreshEnding(s);
  }
  public static void RefreshEnding(DemoStoryState s){
   bool all=new[]{4,5,6}.All(l=>s.attempts.Any(x=>x.level==l&&x.canonical&&x.escaped>0));
   if(all)Set(s,"escape_three_levels");
   // Recompute derived flag; replay/failed attempts do not count toward the three-level ending.
   else s.flags.Remove("escape_three_levels");
   foreach(int l in new[]{4,5,6}){string key="escape_clear_"+l;if(s.attempts.Any(x=>x.level==l&&x.canonical&&x.escaped>0))Set(s,key);else s.flags.Remove(key);}
   s.ending=Has(s,"unsafe_confirmed")?"death":!Has(s,"reported")&&Has(s,"rosetta_overwrite")&&all?"zero":Has(s,"reported")?"normal":"pending";
  }
 }
}
