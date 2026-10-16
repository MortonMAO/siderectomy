using System;
using System.IO;
using Siderectomy;
using UnityEngine;
public static class DemoInformationChecks {
 static void Check(bool b,string m){if(!b)throw new Exception("INFORMATION_CHECK: "+m);}
 public static void Run(){var rules=DemoRuleBook.Load();var s=new DemoSimulation(DemoLevel.Defaults()[0],rules);s.Res[0]=1000;s.Res[1]=1000;s.Res[2]=100;
 Check(!s.Submit("facility","signal"),"研发前不能建设");Check(!s.Submit("batch","info"),"禁止新增信息批次");s.Paused=false;double before=s.Res[2];s.Advance(5);Check(s.Res[2]==before,"无设施不产出");Check(s.Submit("research","stealth"),"可研发");s.Advance(rules.Find("research","stealth").seconds+1);Check(s.Submit("facility","signal"),"研发后可建设");s.Advance(rules.Find("facility","signal").seconds+1);before=s.Res[2];s.Advance(10);Check(Math.Abs(s.Res[2]-before-10)<1e-6,"每秒自动产出");s.Paused=true;before=s.Res[2];s.Advance(10);Check(s.Res[2]==before,"暂停不产出");var restored=DemoSimulation.Restore(s.Capture(),rules);s.Paused=restored.Paused=false;s.Advance(10);restored.Advance(10);Check(Math.Abs(s.Res[2]-restored.Res[2])<1e-6,"读档后生产一致");s.Facilities["signal"]=2;before=s.Res[2];s.Advance(5);Check(Math.Abs(s.Res[2]-before-10)<1e-6,"多设施叠加");s.Jobs.Add(new DemoJob{id=999,kind="batch",key="info",left=1,total=8});before=s.Res[2];s.Advance(1);Check(Math.Abs(s.Res[2]-before-10)<1e-6,"旧批次结算兼容");Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/信息自动生产验收.txt","PASS 研发前置、建造、无设施不生产、自动产出、暂停、多设施、存档恢复、旧批次兼容。\n");}
}
