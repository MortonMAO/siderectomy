using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Siderectomy
{
    [Serializable] public sealed class DemoCost
    {
        public double energy, matter, info, a1, a2, a3;
        public double this[int i] { get { return i==0?energy:i==1?matter:i==2?info:i==3?a1:i==4?a2:a3; } }
        public static DemoCost Sum(params DemoCost[] values) { var c=new DemoCost(); foreach(var v in values) { if(v==null)continue; c.energy+=v.energy;c.matter+=v.matter;c.info+=v.info;c.a1+=v.a1;c.a2+=v.a2;c.a3+=v.a3; } return c; }
        public override string ToString() { var parts=new List<string>();for(int i=0;i<6;i++)if(this[i]>0)parts.Add(DemoSimulation.ResourceNames[i]+this[i].ToString("0.#"));return parts.Count==0?"无需资源":string.Join(" · ",parts); }
    }
    [Serializable] public sealed class DemoRule
    {
        public string key, name, category, desc, facility, carrier, node;
        public DemoCost cost=new DemoCost();
        public double seconds, flight, exposure, alert, entropy, pop, hp, intercept;
        public double duration, interval, effect, productionRate, nodeDamage, flowMultiplier;
        public int unlock=1, slots, attackSlots=1, technologySlots=1, capacityRank, max, outValue, points, ticks, useLimit, initialCount;
        public string moduleType;
        public double manufactureSeconds;
        public bool immuneCounter;
        public bool locked;
        public string Description { get {
            if(category=="carrier")return attackSlots+"打击槽 / "+technologySlots+"科技槽 / 航行"+flight+"秒";
            if(category=="batch")return "每批产出"+outValue;
            if(key=="signal")return "每座自动产出"+productionRate+"信息/秒";
            if(category=="facility"&&(key=="energy"||key=="matter"))return "首座基础产出"+productionRate+"/秒，后续按配置递减";
            if(key=="virus")return "每"+interval+"秒削减"+effect+"P，共"+ticks+"次";
            if(key=="infoInvasion")return "信息入侵：流速×"+flowMultiplier+"，持续"+duration+"秒";
            if(key=="calibration")return "伤害效果×"+effect;
            if(category=="operation"&&(pop>0||hp>0||nodeDamage>0))return "人口"+pop+"P / 中心"+(hp*100)+"%最大HP + "+nodeDamage+"固定伤害";
            if(category=="module")return "察觉修正"+alert+(immuneCounter?" / 投送免受反制":"");
            return desc??"";
        }}
    }
    [Serializable] public sealed class DemoRuleBook
    {
        public DemoRule[] facilities,batches,carriers,operations,modules,research,ai;
        public DemoApplication application=new DemoApplication();
        public DemoSolarStory solarStory=new DemoSolarStory();
        public DemoMechanics mechanics=new DemoMechanics();
        public DemoNodeTemplate[] nodeTemplates;
        public DemoLayout[] layouts;
        public static DemoRuleBook Load() {
            // JsonUtility cannot map the JS key 'out' to a C# identifier.
            var asset=Resources.Load<TextAsset>("DemoRules");
            if(asset==null)throw new InvalidOperationException("缺少 Resources/DemoRules.json");
            var rules=JsonUtility.FromJson<DemoRuleBook>(asset.text.Replace("\"out\":","\"outValue\":"));
            rules.Validate();return rules;
        }
        public void Validate() {
            if(application==null||mechanics==null||nodeTemplates==null||nodeTemplates.Length==0)throw new InvalidOperationException("缺少申请、机制或节点配置");
            if(application.seconds<0||double.IsNaN(application.seconds)||double.IsInfinity(application.seconds))throw new InvalidOperationException("申请等待时间无效");
            for(int i=0;i<6;i++)if(application.amounts[i]<0||double.IsNaN(application.amounts[i])||double.IsInfinity(application.amounts[i]))throw new InvalidOperationException("申请资源量无效");
            foreach(var n in nodeTemplates)if(n.hp<=0||double.IsNaN(n.hp)||double.IsInfinity(n.hp))throw new InvalidOperationException("节点HP必须为正");
            foreach(var r in operations)if(r.seconds<0||r.hp<0||r.pop<0||r.interval<0||r.useLimit<0)throw new InvalidOperationException("作业参数无效："+r.key);
            if(mechanics.alertStages==null||mechanics.peacefulBuildWeights==null||mechanics.peacefulBuildWeights.Length!=ai.Length)throw new InvalidOperationException("AI阶段配置无效");
            int previous=-1;foreach(var s in mechanics.alertStages){if(s.threshold<=previous||s.threshold>100||s.techBonus<0||s.backupMultiplier<0||s.buildWeights==null||s.buildWeights.Length!=ai.Length)throw new InvalidOperationException("察觉阶段必须递增且AI权重数量一致");previous=s.threshold;}
            foreach(var r in new[]{Find("operation","virus"),Find("operation","infoInvasion")})if(r==null||r.effect<0||(r.key=="virus"&&(r.interval<=0||r.ticks<1)))throw new InvalidOperationException("病毒参数无效");
            foreach(var c in carriers)if(c.attackSlots!=1||c.technologySlots<1||c.capacityRank<0)throw new InvalidOperationException("载体插槽或搭载能力无效");
            foreach(var r in Table("attackModule").Concat(Table("technologyModule"))){
                if(r.manufactureSeconds<0||double.IsNaN(r.manufactureSeconds)||double.IsInfinity(r.manufactureSeconds))throw new InvalidOperationException("模块制造时间无效");
                if((r.moduleType=="attack"&&r.cost.info!=0)||(r.moduleType=="technology"&&(r.cost.matter!=0||r.cost.a1!=0||r.cost.a2!=0||r.cost.a3!=0)))throw new InvalidOperationException("模块费用分类无效："+r.key);
                for(int i=0;i<6;i++)if(r.cost[i]<0||double.IsNaN(r.cost[i])||double.IsInfinity(r.cost[i]))throw new InvalidOperationException("模块费用无效："+r.key);
            }
            if(mechanics.populationPerCity<=0)throw new InvalidOperationException("城市人口换算必须为正");
            if(layouts!=null)foreach(var l in layouts){if(l.bodies==null||Math.Abs(l.bodies.Sum(b=>b.populationShare)-1)>.000001)throw new InvalidOperationException("人口分配比例总和必须为1");foreach(var b in l.bodies){if(b.populationShare<0)throw new InvalidOperationException("人口比例不能为负");if(b.nodes!=null)foreach(var n in b.nodes)if(n.count<0||!nodeTemplates.Any(t=>t.type==(string.IsNullOrEmpty(n.template)?n.type:n.template)))throw new InvalidOperationException("节点布局参数无效");}}
        }
        public DemoRule Find(string category,string key) { return Table(category).FirstOrDefault(x=>x.key==key); }
        public DemoRule[] Table(string category) { switch(category) {case "facility":return facilities;case "batch":return batches;case "carrier":return carriers;case "attackModule":return operations.Where(r=>r.moduleType=="attack").ToArray();case "technologyModule":return operations.Concat(modules).Where(r=>r.moduleType=="technology").OrderBy(r=>r.key=="stealth"?0:r.key=="infoInvasion"?1:2).ToArray();case "operation":return operations;case "module":return modules;case "research":return research;default:return ai;} }
    }
    [Serializable] public sealed class DemoApplication {
        public double seconds=25;
        public DemoCost amounts=new DemoCost();
        public bool unlockedArmsOnly=true;
    }
    [Serializable] public sealed class DemoSolarStory {
        public bool enabled;
        public double eventSeconds=180,secondDelay=15,reservedPopulation=8;
        public string first="xxxxx", second="xxxxx\n可以申请资源进行拦截。";
        public string optionA="A 让它逃出去吧",optionB="B 不能放过";
    }
    [Serializable] public sealed class DemoNodeTemplate {public string type;public double hp;}
    [Serializable] public sealed class DemoNodePlacement {public string type,template;public int count=1;}
    [Serializable] public sealed class DemoBodyPlacement {public string body,species="carbon";public double populationShare,regenerationRate,scanExposureMultiplier=1;public int chainSegments;public DemoNodePlacement[] nodes;}
    [Serializable] public sealed class DemoLayout {public int level;public DemoBodyPlacement[] bodies;}
    [Serializable] public sealed class DemoAlertStage {public int threshold;public double techBonus,backupMultiplier;public bool startEscape;public double[] buildWeights;}
    [Serializable] public sealed class DemoMechanics {
        public double secondProducerMultiplier=.75,laterProducerMultiplier=.55;
        public double counterBase=3,counterMilitaryOffset=2,militaryCounterLoss=1,thresholdCounterGain=1;
        public double interceptCap=.8,interceptPerCounter=.05,interceptPerMilitary=.02,disruptionIntercept=.05;
        public double interceptedStrength=.6,interceptedAlert=6;
        public double cooperationMitigation=.04,mitigationCap=.3;
        public double techFloor=.15,politicalTechLoss=.08,escapeTechThreshold=70;
        public double energyEscapeLoss=.2,escapeFloor=.15,escapePortBonus=.1,escapePortBaseline=1,escapePopulation=8;
        public double coldStorageRate=.12,coldStorageBaseline=2,energyBuildBonus=.05,energyBuildBaseline=2;
        public double calibrationSuccess=1.2,calibrationFailure=.8,calibrationTimeSaved=5,calibrationFailureAlert=3;
        public double populationPerCity=48,earthPopulationShare=.9309090909090909;
        public double[] peacefulBuildWeights={45,20,25,10,0};
        public DemoAlertStage[] alertStages;
    }
    [Serializable] public sealed class DemoLevel
    {
        public int number, planets, moons, stations, seed=2407;
        public DemoConfiguredBody[] bodies;
        public string solarOriginId="p2";
        public double a2,a3;
        public string title, briefing;
        public string[] tutorial;
        public double limit=960, population=1650, energy=150, matter=150, info=12, a1=28;
        public double energyRate=1.2,matterRate=.8,infoRate=.5,energyBonus=1.6,matterBonus=.95;
        public int facilityCap=5,aiCap=8;
        public double techStart=32,techRate=.045,alertMul=.9,decay=.16,backupStart=180,backupRate=.38;
        public double escapeSeconds=45,escapeFlightSeconds=60,aiBuildRate=.42,nodeGate=0,protection=0,damageScale=1,aiScale=1;
        public bool awareness=true, allowEscape=true;
        public double regenerationScale=1;
        public static DemoLevel[] Defaults() { return new[] {
            new DemoLevel {number=1,title="第一关 · 初次作业",planets=3,moons=1,stations=0,limit=480,population=144,info=40,a1=16,awareness=false,allowEscape=false,techStart=0,techRate=0,backupStart=0,backupRate=0,aiCap=0,briefing="目标：限时消灭卫星聚居点。学习生产、载体、扫描、侦察及基础武器搭载。",
              tutorial=new[]{"建设能源采集器与物质转化器，继续后完成建造。","在模块制造中制造地表打击模块，完成后进入库存。","制造星尘级载体，投送星系探测，揭示天体。","再造一个载体，侦察有常住人口的卫星。","制造地表打击模块 → 选载体 → 搭载 → 发射，消灭144P。"}},
            new DemoLevel {number=2,title="第二关 · 功能链与搭载",planets=3,moons=3,stations=2,limit=720,population=540,info=40,techStart=10,techRate=.025,backupStart=60,backupRate=.12,escapeSeconds=75,aiCap=4,allowEscape=false,briefing="目标：消灭三个聚居点且零外逃。完成隐形、入侵与节点搭载基础教学。",
              tutorial=new[]{"研发隐形探测卫星，建设信息收集点。","扫描并深度侦察三个聚居点，找出军事与能源中心。","制造科技模块与载体；先选择载体，再搭载打击和隐形模块。","制造并投送病毒入侵模块，观察碳基人口持续损失。","针对中心或人口投送打击；外逃舰出现时选中并阻截。"}},
            new DemoLevel {number=3,title="第三关 · 硅基观察者",planets=5,moons=5,stations=3,limit=900,population=1050,info=40,techStart=22,techRate=.035,backupStart=120,backupRate=.25,escapeSeconds=60,aiCap=6,allowEscape=false,briefing="硅基人口会恢复，3段节点信息链支持重建。隐形降低被发现风险，信息入侵压制流速后集中打击。限时清零且零外逃。"},
            new DemoLevel {number=4,title="第四关 · 太阳系",planets=8,moons=10,stations=3,info=40,escapeFlightSeconds=90,briefing="地球与月球人口清零。外逃舰必然离港，选择放行或阻截；选择15秒后第二轮通讯开放资源申请。无节点清除门槛。"},
            new DemoLevel {number=5,title="第五关 · 集中式硅基网络",planets=6,moons=8,stations=4,limit=960,population=2300,info=50,a1=40,facilityCap=6,techStart=42,techRate=.055,backupStart=300,backupRate=.5,escapeSeconds=50,aiCap=10,briefing="高级硅基文明，6节点集中于主星，构成5段信息链。隐形入侵压恢复，高频打击或恒星载荷集中突破。"},
            new DemoLevel {number=6,title="第六关 · 共生文明",planets=7,moons=10,stations=5,limit=1080,population=2900,info=60,a1=50,facilityCap=7,techStart=50,techRate=.065,backupStart=450,backupRate=.6,escapeSeconds=45,aiCap=12,briefing="碳基与硅基共生；硅基拥有5段信息链。生物入侵处理碳基，信息入侵限制硅基恢复。限时两类人口均清零。"}
        }; }
        public DemoLevel Copy() { return JsonUtility.FromJson<DemoLevel>(JsonUtility.ToJson(this)); }
        public void Validate() {
            foreach(double v in new[]{limit,population,energy,matter,info,a1,energyRate,matterRate,infoRate,energyBonus,matterBonus,techStart,techRate,alertMul,decay,backupStart,backupRate,escapeSeconds,aiBuildRate,nodeGate,protection,damageScale,aiScale,regenerationScale})
                if(double.IsNaN(v)||double.IsInfinity(v)||v<0)throw new ArgumentException("关卡参数必须是有限非负数");
            if(number<1||number>6||planets<0||moons<0||stations<0||limit<=0||population<0||nodeGate<0||nodeGate>1||protection<0||protection>1||escapeSeconds<=0||double.IsNaN(escapeFlightSeconds)||double.IsInfinity(escapeFlightSeconds)||escapeFlightSeconds<=0||damageScale<=0||aiScale<0||energyRate<0||matterRate<0||facilityCap<1||a2<0||a3<0)throw new ArgumentException("关卡参数无效");
        }
    }
    [Serializable] public sealed class DemoNode { public string id,type;public double hp,max; }
    [Serializable] public sealed class DemoBody
    {
        public string id,name,kind,parent,color; public int index,cities,virusCount;
        public double au,diameter,pop,initialPop,backup,initialBackup,disruptedUntil;
        public bool recon,located; public List<DemoNode> nodes=new List<DemoNode>();
        public string species="carbon";
        public double regenerationRate,scanExposureMultiplier=1;
        public List<DemoInformationLink> links=new List<DemoInformationLink>();
        public double Metric(string a,string b) { return (Ratio(a)+Ratio(b))*50; }
        double Ratio(string type) { var ns=nodes.Where(n=>n.type==type).ToArray();return ns.Length==0?0:ns.Average(n=>n.hp/n.max); }
    }
    [Serializable] public sealed class DemoInformationLink {public string from,to;public double baseFlow=1,multiplier=1,suppressedUntil;}
    [Serializable] public sealed class DemoJob {public int id;public string kind,key;public double left,total;}
    [Serializable] public sealed class DemoMission {public int id,ship=-1;public string op,body,node,carrier;public double left,total,intercept,boost=1;public bool qte;public string[] technology;public bool concealed;}
    [Serializable] public sealed class DemoShip {public int id;public string body;public double pop,left=30,total=30;public bool done;}
    [Serializable] public sealed class DemoAI {public string key,host,node;public double left;}
    [Serializable] public sealed class DemoVirus {public string body;public double next,strength;public int count=3;}
    [Serializable] public sealed class DemoEffect {public string body;public double time;}
}
