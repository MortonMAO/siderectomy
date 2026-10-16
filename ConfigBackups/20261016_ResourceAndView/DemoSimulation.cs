using System;
using System.Collections.Generic;
using System.Linq;

namespace Siderectomy
{
    // Deterministic simulation; view, disk persistence and Unity frame rate have no effect on rules.
    public sealed partial class DemoSimulation
    {
        public static readonly string[] ResourceNames={"能源","物质","信息","一级武装","二级武装","三级武装"};
        public readonly DemoLevel Level; public readonly DemoRuleBook Rules;
        public readonly double[] Res=new double[6];
        public readonly Dictionary<string,int> Facilities=new Dictionary<string,int>(),Carriers=new Dictionary<string,int>();
        public readonly Dictionary<string,double> Research=new Dictionary<string,double>();
        public static readonly string[] AttackModuleKeys={"surface","satellite","planetary","stellar","blockade"};
        public readonly Dictionary<string,int> AttackModules=new Dictionary<string,int>(),TechnologyModules=new Dictionary<string,int>();
        public static readonly string[] TechnologyModuleKeys={"stealth","infoInvasion","virus"};
        public readonly List<DemoBody> Bodies=new List<DemoBody>();
        public readonly List<DemoJob> Jobs=new List<DemoJob>(); public readonly List<DemoMission> Missions=new List<DemoMission>();
        public readonly List<DemoShip> Ships=new List<DemoShip>(); public readonly List<DemoAI> AIFacilities=new List<DemoAI>();
        public readonly List<DemoVirus> Viruses=new List<DemoVirus>(); public readonly List<DemoEffect> Effects=new List<DemoEffect>();
        public readonly List<string> Logs=new List<string>(); public readonly List<int> Thresholds=new List<int>();
        public double Time,Alert,Exposure,Tech,Escape,EscapedPop,Killed,Entropy,Peak,InitialPop;
        public int InitialNodes,Escaped,EverBuilt,ApplyCount,Revision; public int Speed=1;
        public bool Paused=true,Over,Won,EscapeActive,GlobalProbe,SecondEnding;
        public string Selected,TargetNode="auto",Notice,ResultReason; public int SelectedShip=-1; public DemoAI AIQueue;
        public event Action<string> MessageRaised;
        public event Action<int> ApplicationRequested; public event Action EscapedBranch;
        uint rng;int serial;
        public string RunId=Guid.NewGuid().ToString("N");
        public DemoSimulation(DemoLevel level,DemoRuleBook rules) {
            Level=level.Copy();Level.Validate();Rules=rules;rng=(uint)Level.seed;
            Res[0]=Level.energy;Res[1]=Level.matter;Res[2]=Level.info;Res[3]=Level.a1;Res[4]=Level.a2;Res[5]=Level.a3;Tech=Level.techStart;
            foreach(var r in Rules.facilities)Facilities[r.key]=0;foreach(var r in Rules.carriers)Carriers[r.key]=0;foreach(var key in AttackModuleKeys)AttackModules[key]=0;foreach(var key in TechnologyModuleKeys)TechnologyModules[key]=0;
            CreateBodies();InitialPop=Bodies.Sum(b=>b.pop);InitialNodes=Bodies.Sum(b=>b.nodes.Count);
            if(Level.number==4&&Rules.solarStory.enabled){var earth=Find(Level.solarOriginId);if(earth!=null){SolarReservedPopulation=Math.Min(earth.pop,Rules.solarStory.reservedPopulation);earth.pop-=SolarReservedPopulation;}}
            Selected=Find(Level.number==1?"m0":"p2")?.id??Bodies.FirstOrDefault(b=>b.pop>0)?.id??Bodies[0].id;
            if(Level.awareness)foreach(var d in Rules.ai) {
                var host=Bodies.FirstOrDefault(b=>b.nodes.Any(n=>n.type==d.node));
                if(host!=null)for(int i=0;i<d.initialCount;i++)AIFacilities.Add(new DemoAI{key=d.key,host=host.id,node=host.nodes.First(n=>n.type==d.node).id});
            }
            Log(Level.title+"待命 · "+Clock(Level.limit)+" · "+InitialPop.ToString("0")+"P。点击继续开始。");
        }
        void CreateBodies() {
            if(CreateConfiguredBodies())return;
            string[] names={"水星","金星","地球","火星","木星","土星","天王星","海王星"};
            string[] colors={"#a9a59a","#d3b083","#6faddc","#c8846f","#d6baa0","#dccb95","#8ecbd3","#658ad7"};
            double[] aus={.39,.72,1,1.52,5.2,9.58,19.2,30.05},diam={4879,12104,12742,6779,139820,116460,50724,49244};
            Bodies.Add(new DemoBody{id="sun",name=Level.number==4?"太阳":"主恒星",kind="star",color="#f2c77e",diameter=1392700,recon=true});
            for(int i=0;i<Level.planets;i++)Bodies.Add(new DemoBody{id="p"+i,name=Level.number==4?names[i]:"行星 "+(i+1),kind="planet",parent="sun",index=i,au=aus[i],diameter=diam[i],color=colors[i]});
            string[] moons={"月球","火卫一","火卫二","木卫一","木卫二","木卫三","木卫四","土卫六","土卫二","海卫一"};
            int[] parents={2,3,3,4,4,4,4,5,5,7};double[] md={3474,22,12,3643,3122,5268,4821,5150,504,2707};
            for(int i=0;i<Level.moons;i++){int p=Level.number==4?parents[i]:i%Level.planets;Bodies.Add(new DemoBody{id="m"+i,name=Level.number==4?moons[i]:"卫星 "+(i+1),kind="moon",parent="p"+p,index=i,au=aus[p],diameter=md[i],color="#b5bfd0"});}
            for(int i=0;i<Level.stations;i++){int p=Level.number==4?new[]{2,4,5}[i]:(i+1)%Level.planets;Bodies.Add(new DemoBody{id="s"+i,name=Level.number==4?new[]{"地月数据站","木星军工站","土星中继站"}[i]:"轨道站 "+(i+1),kind="station",parent="p"+p,index=i,au=aus[p],diameter=12,color="#a38fd8"});}
            var layout=Rules.layouts==null?null:Rules.layouts.FirstOrDefault(l=>l.level==Level.number);
            if(layout!=null){foreach(var placement in layout.bodies){var body=Find(placement.body);if(body==null)throw new InvalidOperationException("配置天体不存在："+placement.body);body.pop=Level.population*placement.populationShare;body.species=string.IsNullOrEmpty(placement.species)?"carbon":placement.species;body.regenerationRate=placement.regenerationRate;body.scanExposureMultiplier=placement.scanExposureMultiplier;if(placement.nodes!=null)foreach(var n in placement.nodes)for(int i=0;i<n.count;i++)Node(body,n.type,Rules.nodeTemplates.First(t=>t.type==(string.IsNullOrEmpty(n.template)?n.type:n.template)).hp);
                if(placement.chainSegments>0&&placement.chainSegments>body.nodes.Count-1)throw new InvalidOperationException("链条数量超过节点可连接数量");for(int i=0;i<placement.chainSegments;i++)body.links.Add(new DemoInformationLink{from=body.nodes[i].id,to=body.nodes[i+1].id});}}
            else if(Level.number==4){var e=Find("p2");e.pop=Level.population*Rules.mechanics.earthPopulationShare;Find("m0").pop=Level.population*(1-Rules.mechanics.earthPopulationShare);
                // Port the HTML's original 25 nodes exactly before concentrating them on Earth.
                int[] count={2,3,4,4,3,3,2,1};string[] types={"政治","军事","能源","信息"};
                for(int i=0;i<8;i++)for(int j=0;j<count[i];j++)Node(e,types[(j+(i==7?3:0))%4]);
                for(int i=0;i<3;i++)Node(e,"信息",Rules.nodeTemplates.First(n=>n.type=="信息附属").hp);
            } else if(Level.number==1){var b=Find("m0");b.pop=Level.population;foreach(string t in new[]{"政治","军事","能源","信息"})Node(b,t);}
            else {
                for(int i=0;i<3;i++)Find("p"+i).pop=Level.population/3;
                if(Level.number==2)foreach(string t in new[]{"政治","军事","能源","信息","军事","能源"})Node(Find("p2"),t);
                else {foreach(string t in new[]{"军事","能源","军事","能源"})Node(Find("p1"),t);foreach(string t in new[]{"政治","信息","政治","信息"})Node(Find("p2"),t);}
            }
            var info=Bodies.Where(b=>b.nodes.Any(n=>n.type=="信息")).ToArray();
            foreach(var b in Bodies){b.initialPop=b.pop;b.cities=(int)Math.Ceiling(b.pop/Rules.mechanics.populationPerCity);if(info.Contains(b))b.backup=b.initialBackup=Level.backupStart/info.Length;}
        }
        void Node(DemoBody b,string type,double hp=0){if(hp==0)hp=Rules.nodeTemplates.First(n=>n.type==type).hp;b.nodes.Add(new DemoNode{id=b.id+"n"+b.nodes.Count,type=type,hp=hp,max=hp});}
        public DemoBody Find(string id){return Bodies.FirstOrDefault(b=>b.id==id);}
        // Selected remains a navigation context for existing saves; an explicit clear disables targeting.
        public bool SelectionCleared;
        public void ClearSelection(){SelectionCleared=true;SelectedShip=-1;TargetNode="auto";Revision++;}
        public DemoBody Body {get{return Find(Selected);}}
        public DemoShip Ship {get{return Ships.FirstOrDefault(s=>s.id==SelectedShip);}}
        public string TargetName {get{if(SelectionCleared&&GlobalProbe)return "未选择天体";if(!GlobalProbe)return "未知星系";if(SelectedShip>=0)return "外逃舰 #"+SelectedShip;var n=Body.nodes.FirstOrDefault(x=>x.id==TargetNode);return n==null?Body.name:Body.name+" / "+n.type+"中心 "+(Body.nodes.IndexOf(n)+1);}}
        public void SelectBody(string id){if(!GlobalProbe||Find(id)==null)return;SelectionCleared=false;Selected=id;SelectedShip=-1;TargetNode="auto";Revision++;}
        public bool SelectNode(string id){if(!GlobalProbe)return false;var b=Bodies.FirstOrDefault(x=>x.recon&&x.nodes.Any(n=>n.id==id&&n.hp>0));if(b==null)return false;SelectionCleared=false;Selected=b.id;SelectedShip=-1;TargetNode=id;Revision++;return true;}
        public bool SelectShip(int id){if(!GlobalProbe)return false;var s=Ships.FirstOrDefault(x=>x.id==id&&!x.done);if(s==null)return false;SelectionCleared=false;Selected=s.body;SelectedShip=id;TargetNode="auto";Revision++;return true;}
        public int Cleared {get{return Bodies.Sum(b=>b.nodes.Count(n=>n.hp<=0));}}
        public int GateCount {get{return (int)Math.Ceiling(InitialNodes*Level.nodeGate);}}
        public bool GateOpen {get{return true;}}
        public double Remaining {get{return Bodies.Sum(b=>b.pop)+Ships.Where(s=>!s.done).Sum(s=>s.pop)+SolarReservedPopulation;}}
        public double Backups {get{return Bodies.Sum(b=>b.backup);}}
        public int Alive(string type){return Bodies.Sum(b=>b.nodes.Count(n=>n.type==type&&n.hp>0));}
        int Destroyed(string type){return Bodies.Sum(b=>b.nodes.Count(n=>n.type==type&&n.hp<=0));}
        double Ratio(string type){int count=Alive(type)+Destroyed(type);return count==0?0:(double)Alive(type)/count;}
        bool Active(DemoAI f){var b=Find(f.host);return b!=null&&b.nodes.Any(n=>n.id==f.node&&n.hp>0);}
        public int AICount(string key){return AIFacilities.Count(f=>f.key==key&&Active(f));}
        public int Counter {get{return Level.awareness?(int)Math.Max(0,Rules.mechanics.counterBase+Thresholds.Count*Rules.mechanics.thresholdCounterGain-Destroyed("军事")*Rules.mechanics.militaryCounterLoss+AICount("military")-Rules.mechanics.counterMilitaryOffset):0;}}
        public double Production(string key){if(key=="info")return Facilities["signal"]*Rules.Find("facility","signal").productionRate;double bonus=0;for(int i=0;i<Facilities[key];i++)bonus+=(key=="energy"?Level.energyBonus:Level.matterBonus)*(i==0?1:i==1?Rules.mechanics.secondProducerMultiplier:Rules.mechanics.laterProducerMultiplier);return (key=="energy"?Level.energyRate:Level.matterRate)+bonus;}
        public bool Unlocked(DemoRule r){return r!=null&&(DebugAllFeatures||(!r.locked&&r.unlock<=Level.number));}
        public bool CanPay(DemoCost c){for(int i=0;i<6;i++)if(Res[i]+1e-7<c[i])return false;return true;}
        bool Pay(DemoCost c){if(!CanPay(c)){var missing=new List<string>();for(int i=0;i<6;i++)if(Res[i]<c[i])missing.Add(ResourceNames[i]+(c[i]-Res[i]).ToString("0.0"));return Reject("资源不足：缺少 "+string.Join(" · ",missing));}for(int i=0;i<6;i++)Res[i]-=c[i];return true;}
        public bool Reject(string text){Notice=text;Revision++;MessageRaised?.Invoke(text);return false;}
        public void Log(string text){Notice=text;Logs.Insert(0,Clock(Time)+"  "+text);if(Logs.Count>50)Logs.RemoveAt(50);Revision++;MessageRaised?.Invoke(text);}
        public static string Clock(double value){int s=(int)Math.Max(0,value);return (s/60).ToString("00")+":"+(s%60).ToString("00");}
        double Random(){rng=unchecked(rng*1664525+1013904223);return rng/4294967296.0;}
        public bool Submit(string kind,string key) {
            if(kind=="batch"&&key=="info")return Reject("信息由信息收集点自动产出：先研发隐形探测，再建造收集点");
            if(kind=="attackModule"||kind=="technologyModule")return ManufactureModule(kind,key);
            if(Over)return Reject("任务已结束");
            if(kind=="apply") {if(!CanApply)return Reject(Level.number<4?"资源申请在第四关剧情中首次开放":"第四关第二轮剧情结束后开放资源申请");if(Jobs.Any(j=>j.kind==kind))return Reject("客服正在处理申请");Jobs.Add(new DemoJob{id=++serial,kind=kind,key=key,left=Rules.application.seconds,total=Rules.application.seconds});ApplyCount++;ApplicationRequested?.Invoke(ApplyCount);Log("提交确定性资源申请 · "+Rules.application.seconds+"秒后大额拨付");return true;}
            var d=Rules.Find(kind,key);if(!Unlocked(d))return Reject("本关尚未解锁该项目");
            if(kind=="facility"){
                if(Facilities[key]+Jobs.Count(j=>j.kind==kind&&j.key==key)>=Level.facilityCap)return Reject("该设施已达上限");
                if(key=="signal"&&!DebugAllFeatures&&!Research.ContainsKey("stealth"))return Reject("先研发隐形探测卫星");
            }
            if(kind=="batch"&&Jobs.Count(j=>j.kind=="batch"&&Rules.Find("batch",j.key).facility==d.facility)>=Facilities[d.facility])return Reject("制造队列不足：先建设"+Rules.Find("facility",d.facility).name);
            if(kind=="research"&&(Research.ContainsKey(key)||Jobs.Any(j=>j.kind==kind&&j.key==key)))return Reject("已研发或正在研发");
            if(!Pay(d.cost))return false;Jobs.Add(new DemoJob{id=++serial,kind=kind,key=key,left=d.seconds,total=d.seconds});Log("已提交："+d.name);return true;
        }
        public void Expose(double amount){if(!Level.awareness)return;amount=Math.Max(0,amount);Alert=Math.Min(100,Alert+amount);Exposure+=amount;Peak=Math.Max(Peak,Alert);
            foreach(var stage in Rules.mechanics.alertStages)if(Alert>=stage.threshold&&!Thresholds.Contains(stage.threshold)){Thresholds.Add(stage.threshold);Tech=Math.Min(100,Tech+stage.techBonus);if(stage.startEscape)EscapeActive=true;Log("察觉"+stage.threshold+"：科技、防御和备份升级"+(stage.startEscape?"；外逃准备启动":""));}}
        void CompleteJob(DemoJob j){
            if(j.kind=="batch"&&j.key=="a1")TutorialArmamentProduced=true;
            if(j.kind=="apply"){var grants=new List<string>();for(int k=0;k<6;k++){if(k>=3&&Rules.application.unlockedArmsOnly&&!Unlocked(Rules.Find("batch",new[]{"a1","a2","a3"}[k-3])))continue;double amount=Rules.application.amounts[k];Res[k]+=amount;grants.Add(ResourceNames[k]+"+"+amount.ToString("0.#"));}Log("上级确定性拨付："+string.Join(" · ",grants));return;}
            var d=Rules.Find(j.kind,j.key);switch(j.kind){case "facility":Facilities[j.key]++;break;case "batch":Res[Array.IndexOf(new[]{"energy","matter","info","a1","a2","a3"},j.key)]+=d.outValue;break;case "carrier":Carriers[j.key]++;break;case "attackModule":AttackModules[j.key]++;TutorialArmamentProduced=true;break;case "technologyModule":TechnologyModules[j.key]++;break;case "research":Research[j.key]=j.key=="calibration"?d.effect:1;break;}Log(d.name+"完成"+(j.kind=="batch"?" · +"+d.outValue:""));
        }
        public void DamageNodes(DemoBody b,double fraction,string node="auto",double absolute=0){
            var ns=node=="auto"?b.nodes.Where(n=>n.hp>0).Take(1):b.nodes.Where(n=>n.id==node&&n.hp>0);
            foreach(var n in ns){n.hp=Math.Max(0,n.hp-(n.max*fraction+absolute)*(1-Math.Min(Rules.mechanics.mitigationCap,AICount("political")*Rules.mechanics.cooperationMitigation)));if(n.hp<1e-6){n.hp=0;Log(b.name+"："+n.type+"中心已摧毁");}}
        }
        public double Kill(IEnumerable<DemoBody> targets,double amount){
            double allowed=amount,actual=0;
            foreach(var b in targets){double n=Math.Min(b.pop,allowed);b.pop-=n;Killed+=n;actual+=n;allowed-=n;if(b.pop<1e-6)b.pop=0;if(allowed<=0)break;}return actual;
        }
        DemoAI ChooseAI(){
            if(EverBuilt>=Level.aiCap)return null;
            double[] weights=Rules.mechanics.peacefulBuildWeights;foreach(var stage in Rules.mechanics.alertStages)if(Alert>=stage.threshold)weights=stage.buildWeights;
            var candidates=Rules.ai.Select((d,i)=>new{d,w=weights[i]}).Where(x=>x.w>0&&Alive(x.d.node)>0&&AICount(x.d.key)<x.d.max).ToArray();
            if(candidates.Length==0)return null;double roll=Random()*candidates.Sum(x=>x.w);var def=candidates[0].d;
            foreach(var c in candidates){roll-=c.w;if(roll<0){def=c.d;break;}}
            var host=Bodies.First(b=>b.nodes.Any(n=>n.type==def.node&&n.hp>0));return new DemoAI{key=def.key,left=def.points,host=host.id,node=host.nodes.First(n=>n.type==def.node&&n.hp>0).id};
        }
        void AITick(double dt){
            if(!Level.awareness)return;Alert=Math.Max(0,Alert-Level.decay*dt);
            Tech=Math.Min(100,Tech+Level.techRate*Level.aiScale*(1+Alert/100*Level.alertMul)*Math.Max(Rules.mechanics.techFloor,1-Destroyed("政治")*Rules.mechanics.politicalTechLoss)*dt);
            if(Tech>=Rules.mechanics.escapeTechThreshold&&!EscapeActive){EscapeActive=true;Log("科技达标：外逃研发完成");}
            var infos=Bodies.Where(b=>b.nodes.Any(n=>n.type=="信息"&&n.hp>0)).ToArray();
            double backupMultiplier=1;foreach(var stage in Rules.mechanics.alertStages)if(Thresholds.Contains(stage.threshold))backupMultiplier=stage.backupMultiplier;
            double rate=(Level.backupRate*Ratio("信息")+Rules.mechanics.coldStorageRate*Math.Max(0,AICount("info")-Rules.mechanics.coldStorageBaseline))*backupMultiplier*Level.aiScale;
            foreach(var b in infos)b.backup+=rate*dt/infos.Length;
            if(AIQueue==null){AIQueue=ChooseAI();if(AIQueue!=null)Log("AI开始建造："+Rules.Find("ai",AIQueue.key).name);}
            if(AIQueue!=null){if(!Active(AIQueue)){Log("挂靠中心被毁，AI建造取消");AIQueue=null;}else{AIQueue.left-=Level.aiBuildRate*Level.aiScale*(1+Rules.mechanics.energyBuildBonus*Math.Max(0,AICount("energy")-Rules.mechanics.energyBuildBaseline))*dt;if(AIQueue.left<=0){AIFacilities.Add(AIQueue);EverBuilt++;Log("AI完成："+Rules.Find("ai",AIQueue.key).name);AIQueue=null;}}}
            if(EscapeActive&&(Level.number!=4||!Rules.solarStory.enabled||SolarStoryPhase!=0)){Escape+=100/Level.escapeSeconds*Math.Max(Rules.mechanics.escapeFloor,1-Destroyed("能源")*Rules.mechanics.energyEscapeLoss)*(1+Rules.mechanics.escapePortBonus*Math.Max(0,AICount("escape")-Rules.mechanics.escapePortBaseline))*Level.aiScale*dt;
                if(Escape>=100){Escape-=100;var b=Bodies.Where(x=>x.pop>0).OrderByDescending(x=>x.pop).FirstOrDefault();if(b!=null){double p=Math.Min(Rules.mechanics.escapePopulation,b.pop);b.pop-=p;Ships.Add(new DemoShip{id=++serial,body=b.id,pop=p,left=Level.escapeFlightSeconds,total=Level.escapeFlightSeconds});Log("外逃舰离港："+p.ToString("0.#")+"P，"+Level.escapeFlightSeconds.ToString("0")+"游戏秒内可拦截");}}}
            foreach(var s in Ships.Where(s=>!s.done).ToArray()){s.left-=dt;if(s.left<=0){s.done=true;Escaped++;EscapedPop+=s.pop;if(AllowsEscape){SecondEnding=true;EscapedBranch?.Invoke();Log("外逃成功：已记录外逃事实，最终结局由剧情与后三关记录共同判定");}else{Finish(false,ReportedNoEscape?"异常已上报：禁止外逃，外逃舰已离开星系":"本关禁止外逃：外逃舰已离开星系");return;}}}
        }
        public void Advance(double seconds){
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException("seconds");
            // Fixed 0.05s substeps retain chronological flight/interception/virus events at accelerated speed.
            while(seconds>1e-8&&!Paused&&!Over&&!StoryPending){double dt=Math.Min(.05,Math.Min(seconds,Math.Max(0,Level.limit-Time)));seconds-=dt;Time+=dt;
                SolarStoryTick();if(StoryPending)break;
                Res[0]+=Production("energy")*dt;Res[1]+=Production("matter")*dt;Res[2]+=Production("info")*dt;
                foreach(var j in Jobs.ToArray()){j.left-=dt;if(j.left<=1e-7){Jobs.Remove(j);CompleteJob(j);}}
                AITick(dt);if(Over)break;
                RegeneratePopulation(dt);
                foreach(var m in Missions.ToArray()){m.left-=dt;if(m.left<=1e-7){Missions.Remove(m);CompleteMission(m);}}
                foreach(var v in Viruses.ToArray())if(v.next<=Time){var virus=Rules.Find("operation","virus");Kill(new[]{Find(v.body)},virus.effect*v.strength);v.next+=virus.interval;if(--v.count==0)Viruses.Remove(v);}
                Effects.RemoveAll(e=>Time-e.time>1.8);UpdateTutorial();CheckEnd();
            }
        }
        public void CheckEnd(){if(Over||StoryPending)return;if(!AllowsEscape&&Escaped>0){Finish(false,"禁止外逃硬条件未满足");return;}if(Remaining<1e-6&&Time<=Level.limit+1e-6)Finish(true,Level.number==6?"碳基与硅基人口均清零，限时内完成":"人口清零，限时内完成");else if(Time>=Level.limit-1e-7)Finish(false,"任务失败 · 时间耗尽");}
        void Finish(bool won,string reason){Over=true;Won=won;ResultReason=reason;Log(reason);}
        public double InformationScore {get{return Level.backupStart==0?(Backups==0?100:0):Math.Max(0,Math.Min(100,100-100*Backups/Level.backupStart));}}
        public double KnowledgeScore {get{return Math.Max(0,100-Exposure);}}
        public double EntropyScore {get{return Math.Max(0,100-Entropy);}}
        public void Calibrate(DemoMission mission,bool success){if(!Missions.Contains(mission)||mission.qte)return;mission.qte=true;mission.boost=success?Rules.mechanics.calibrationSuccess:Rules.mechanics.calibrationFailure;if(success)mission.left=Math.Max(1,mission.left-Rules.mechanics.calibrationTimeSaved);else Expose(Rules.mechanics.calibrationFailureAlert);Log("校准"+(success?"成功":"失误")+"：效果×"+mission.boost);}
    }
}
