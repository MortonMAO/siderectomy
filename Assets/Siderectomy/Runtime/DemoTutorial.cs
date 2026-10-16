using System.Linq;
namespace Siderectomy {
 public sealed partial class DemoSimulation {
  public int TutorialStep;
  public bool TutorialArmamentProduced,TutorialModuleUsed,TutorialSoftOperation;
  public string TutorialHint {get{
   if(Level.tutorial==null||Level.tutorial.Length==0)return Level.briefing;
   if(Level.number==1&&TutorialStep==1&&AttackModules["surface"]==0&&Res[3]<Rules.Find("attackModule","surface").cost.a1)return "引导 2/5 · Q：建设军工制造点 → W：补产一级武装 → T：制造地表打击模块。";
   string text=Level.tutorial[System.Math.Min(TutorialStep,Level.tutorial.Length-1)];if(Level.number==1)text=text.Replace("144P",Level.population.ToString("0")+"P");return "引导 "+(System.Math.Min(TutorialStep,Level.tutorial.Length-1)+1)+"/"+Level.tutorial.Length+" · "+text;
  }}
  void UpdateTutorial(){
   if(Level.number==1){
    if(TutorialStep==0&&Facilities["energy"]>0&&Facilities["matter"]>0)TutorialStep=1;
    if(TutorialStep==1&&AttackModules["surface"]>0)TutorialStep=2;
    if(TutorialStep==2&&GlobalProbe)TutorialStep=3;
    if(TutorialStep==3&&Bodies.Any(b=>b.initialPop>0&&b.recon))TutorialStep=4;
   }else if(Level.number==2){
    if(TutorialStep==0&&Research.ContainsKey("stealth")&&Facilities["signal"]>0)TutorialStep=1;
    if(TutorialStep==1&&Bodies.Where(b=>b.initialPop>0).All(b=>b.recon))TutorialStep=2;
    if(TutorialStep==2&&TutorialModuleUsed)TutorialStep=3;
    if(TutorialStep==3&&TutorialSoftOperation)TutorialStep=4;
   }
  }
 }
}
