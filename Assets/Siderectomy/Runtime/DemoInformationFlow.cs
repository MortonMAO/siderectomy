using System.Linq;
namespace Siderectomy {
 public sealed partial class DemoSimulation {
  public double LinkFlow(DemoBody b,DemoInformationLink l){
   if(!b.nodes.Any(n=>n.id==l.from&&n.hp>0)||!b.nodes.Any(n=>n.id==l.to&&n.hp>0))return 0;
   return l.baseFlow*(Time<l.suppressedUntil?l.multiplier:1);
  }
  public double InformationFlow(DemoBody b){return b.links.Count==0?0:b.links.Average(l=>LinkFlow(b,l));}
  public double RegenerationPerSecond(DemoBody b){return b.species=="silicon"&&b.pop>0?b.initialPop*b.regenerationRate*Level.regenerationScale*InformationFlow(b):0;}
  void RegeneratePopulation(double dt){foreach(var b in Bodies){double add=System.Math.Min(System.Math.Max(0,b.initialPop-b.pop),RegenerationPerSecond(b)*dt);b.pop+=add;Killed-=add;}}
 }
}
