using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Siderectomy {
 public sealed partial class DemoMissionView {
  readonly Dictionary<int,Button> levelButtons=new Dictionary<int,Button>();
  void BuildProgressionButtons(){for(int i=1;i<=DemoProgression.LevelCount;i++){int level=i;levelButtons[level]=Button(ui,"Level"+i,i+"",845+(i-1)*34,13,30,38,()=>LoadLevel(level),14);}}
  void RefreshProgressionButtons(){foreach(var pair in levelButtons){bool allowed=DemoProgression.CanEnter(DemoSession.Profile,pair.Key);if(DemoSession.Profile!=null&&DemoSession.Profile.hasCheckpoint&&Simulation!=null&&!Simulation.Over&&pair.Key!=Simulation.Level.number)allowed=false;pair.Value.interactable=allowed;pair.Value.GetComponentInChildren<Text>().color=allowed?(DemoProgression.Completed(DemoSession.Profile,pair.Key)?PrototypeUi.Accent:PrototypeUi.Ink):(Color)new Color32(91,107,127,255);}}
 }
}
