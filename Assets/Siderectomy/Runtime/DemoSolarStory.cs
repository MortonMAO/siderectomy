namespace Siderectomy {
 public sealed partial class DemoSimulation {
  // 0待触发；1第一轮；2选择；5选择后等待15秒；3第二轮；4完成。
  public int SolarStoryPhase;
  public string SolarChoice="";
  public double SolarSecondAt=-1,SolarReservedPopulation;
  public bool DebugAllFeatures;
  public bool StoryPending {get{return Rules.solarStory.enabled&&Level.number==4&&(SolarStoryPhase==1||SolarStoryPhase==2||SolarStoryPhase==3);}}
  public bool CanApply {get{return DebugAllFeatures||Level.number>4||(Level.number==4&&(!Rules.solarStory.enabled||SolarStoryPhase==4));}}
  public bool CanIntercept {get{return ReportedNoEscape||DebugAllFeatures||!Rules.solarStory.enabled||Level.number!=4||SolarChoice=="B";}}
  public void BeginSolarStory(){if(!Rules.solarStory.enabled||Level.number!=4||SolarStoryPhase!=0)return;SolarStoryPhase=1;Log("通讯接入：太阳系外逃事件");}
  public void NextSolarDialogue(){if(SolarStoryPhase==1)SolarStoryPhase=2;else if(SolarStoryPhase==3){SolarStoryPhase=4;Log("第二轮通讯结束：资源申请已开放");}Revision++;}
  public void ChooseSolarEscape(bool allow){if(SolarStoryPhase!=2)return;allow=allow&&!ReportedNoEscape;SolarChoice=allow?"A":"B";SolarStoryPhase=5;SolarSecondAt=Time+Rules.solarStory.secondDelay;if(allow)Missions.RemoveAll(m=>m.op=="blockade");Log(allow?"选择A：本局关闭外逃拦截":"选择B：保留外逃拦截功能");}
  void SolarStoryTick(){
   if(!Rules.solarStory.enabled||Level.number!=4)return;
   if(SolarStoryPhase==0&&Time>=Rules.solarStory.eventSeconds){
    var origin=Find(Level.solarOriginId);double people=SolarReservedPopulation;SolarReservedPopulation=0;
    if(people>0)Ships.Add(new DemoShip{id=++serial,body=origin.id,pop=people,left=Level.escapeFlightSeconds,total=Level.escapeFlightSeconds});
    EscapeActive=true;BeginSolarStory();Log("应急外逃舰已离港：首次离港事件不可取消");
   }
   if(SolarStoryPhase==5&&Time>=SolarSecondAt){SolarStoryPhase=3;Log("第二轮通讯接入");}
  }
  public void SetDebugFeatures(bool all){DebugAllFeatures=all;Log(all?"调试：全部功能权限已开启，库存与剧情未改动":"调试：已恢复当前关卡及剧情应有权限");}
 }
}
