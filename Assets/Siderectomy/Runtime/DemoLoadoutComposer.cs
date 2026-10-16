using System;
using System.Linq;
using UnityEngine;
namespace Siderectomy {
 public sealed partial class DemoMissionView {
  string draftCarrier="dust",draftPrimary="";string[] draftTechnology=new string[0];
  void ResetLoadoutDraft(){draftCarrier="dust";draftPrimary="";draftTechnology=new string[0];}
  void StoreLoadoutDraft(){if(opDrop==null||carrierDrop==null||serviceDrop==null)return;draftCarrier=rules.carriers[carrierDrop.value].key;draftPrimary=ComposerPrimary;draftTechnology=ComposerTechnologies;}
  string ComposerPrimary {get{return serviceDrop.value==1?"probe":serviceDrop.value==2?"recon":opDrop.value==0?"":composerAttacks[opDrop.value-1].key;}}
  string[] ComposerTechnologies {get{return new[]{infoHide.isOn?"stealth":null,invasionToggle.isOn?"infoInvasion":null,virusToggle.isOn?"virus":null}.Where(k=>k!=null).ToArray();}}
  void BuildComposer(){
   Label(commands,"CarrierStep","① 选择载体",0,0,140,38,18);
   carrierDrop=Dropdown(commands,"CarrierSelect",148,0,294,rules.carriers.Select(c=>c.name).ToList(),CarrierChanged);
   carrierSummary=Label(commands,"CarrierCapacity","",454,0,294,44,14,Muted);
   Label(commands,"ModuleStep","② 搭载模块 · 打击槽与科技槽独立计算",0,47,744,24,16);
   opDrop=Dropdown(commands,"AttackSelect",0,77,365,new System.Collections.Generic.List<string>{"不搭载打击模块"},()=>{serviceDrop.SetValueWithoutNotify(0);RefreshManifest();});
   serviceDrop=Dropdown(commands,"ServiceSelect",379,77,365,new System.Collections.Generic.List<string>{"普通模块投送","基础操作：星系探测","基础操作：深度侦察"},()=>{if(serviceDrop.value>0){opDrop.SetValueWithoutNotify(0);invasionToggle.SetIsOnWithoutNotify(false);virusToggle.SetIsOnWithoutNotify(false);}RefreshManifest();});
   infoHide=Toggle(commands,"StealthModule","隐形模块",0,120,RefreshManifest);
   invasionToggle=Toggle(commands,"InformationInvasionModule","信息入侵",253,120,RefreshManifest);
   virusToggle=Toggle(commands,"VirusModule","病毒入侵",506,120,RefreshManifest);
   foreach(var t in new[]{infoHide,invasionToggle,virusToggle}){((RectTransform)t.transform).sizeDelta=new Vector2(242,32);var label=t.transform.Find("Caption").GetComponent<UnityEngine.UI.Text>();label.rectTransform.sizeDelta=new Vector2(209,32);label.fontSize=14;}
   manifest=Label(commands,"Manifest","",0,160,746,92,14,Gold);
   Button(commands,"PrepareCarrier","制造所选载体",0,266,180,38,()=>{Simulation.Submit("carrier",rules.carriers[carrierDrop.value].key);Refresh();},15);
   Button(commands,"PrepareModules","前往模块制造",190,266,175,38,()=>SetTab("attackModule"),15);
   launchButton=Button(commands,"Launch","③ 发射",379,266,367,38,()=>{Simulation.LaunchLoadout(ComposerPrimary,rules.carriers[carrierDrop.value].key,ComposerTechnologies);Refresh();},17);
   Label(commands,"LoadoutHint","模块须先制造入库；发射消耗载体和每种所选模块各1个。基础侦察直接扣作业资源。",0,310,746,22,12,Muted);
   carrierDrop.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(rules.carriers,c=>c.key==draftCarrier)));RebuildAttackOptions(draftPrimary);serviceDrop.SetValueWithoutNotify(draftPrimary=="probe"?1:draftPrimary=="recon"?2:0);infoHide.SetIsOnWithoutNotify(draftTechnology.Contains("stealth"));invasionToggle.SetIsOnWithoutNotify(draftTechnology.Contains("infoInvasion"));virusToggle.SetIsOnWithoutNotify(draftTechnology.Contains("virus"));RefreshManifest();
  }
  void CarrierChanged(){if(opDrop==null)return;string old=ComposerPrimary;RebuildAttackOptions(old);RefreshManifest();}
  void RebuildAttackOptions(string selected){
   var c=rules.carriers[carrierDrop.value];
   composerAttacks=rules.Table("attackModule").Where(r=>rules.Find("carrier",r.carrier).capacityRank<=c.capacityRank).ToArray();
   opDrop.ClearOptions();opDrop.AddOptions(new[]{"不搭载打击模块"}.Concat(composerAttacks.Select(r=>r.name+(Simulation.Unlocked(r)?" · 库存"+Simulation.AttackModules[r.key]:"（第"+r.unlock+"关）"))).ToList());
   int i=Array.FindIndex(composerAttacks,r=>r.key==selected);opDrop.SetValueWithoutNotify(i<0?0:i+1);
  }
  public void QuickOperation(string op){
   if(tab!="operation")SetTab("operation");else{secondaryPanel.gameObject.SetActive(true);RefreshDock();}
   if(op=="probe"||op=="recon"){serviceDrop.SetValueWithoutNotify(op=="probe"?1:2);opDrop.SetValueWithoutNotify(0);invasionToggle.SetIsOnWithoutNotify(false);virusToggle.SetIsOnWithoutNotify(false);}
   else{serviceDrop.SetValueWithoutNotify(0);RebuildAttackOptions(op);if(op=="infoInvasion")invasionToggle.SetIsOnWithoutNotify(true);if(op=="virus")virusToggle.SetIsOnWithoutNotify(true);}
   RefreshManifest();
  }
  void RefreshManifest(){
   if(opDrop==null||carrierDrop==null||serviceDrop==null||manifest==null)return;
   var c=rules.carriers[carrierDrop.value];string primary=ComposerPrimary;var tech=ComposerTechnologies;StoreLoadoutDraft();
   for(int i=0;i<composerAttacks.Length;i++){var r=composerAttacks[i];opDrop.options[i+1].text=r.name+(Simulation.Unlocked(r)?" · 库存"+Simulation.AttackModules[r.key]:"（第"+r.unlock+"关）");}opDrop.RefreshShownValue();
   var d=rules.Find("operation",primary);string error=Simulation.LoadoutError(primary,c.key,tech);
   carrierSummary.text=c.attackSlots+"打击槽 / "+c.technologySlots+"科技槽\n航行"+c.flight+"秒 · 库存"+Simulation.Carriers[c.key];
   var toggles=new[]{infoHide,invasionToggle,virusToggle};var keys=DemoSimulation.TechnologyModuleKeys;
   for(int i=0;i<3;i++){var r=Simulation.TechnologyRule(keys[i]);toggles[i].transform.Find("Caption").GetComponent<UnityEngine.UI.Text>().text=r.name+" · "+Simulation.TechnologyModules[keys[i]]+(Simulation.ModuleAvailable(r)?"":"（未开放）");}
   bool utility=primary=="probe"||primary=="recon";double duration=Simulation.LoadoutSeconds(primary,c.key,tech);
   manifest.text=(d==null?"科技投送":d.name+"："+d.Description)+"\n打击槽 "+(!utility&&d!=null?1:0)+"/"+c.attackSlots+" · 科技槽 "+tech.Length+"/"+c.technologySlots+" · 总用时 "+duration+"秒\n"+(utility?"本次侦察费用："+d.cost:"发射只扣载体和模块库存")+(Simulation.Ship!=null&&!Simulation.Ship.done?" · 外逃舰离场 "+Math.Ceiling(Simulation.Ship.left)+"秒":"")+"\n"+(error??"投送就绪 → "+Simulation.TargetName);
   launchButton.interactable=error==null;launchButton.GetComponentInChildren<UnityEngine.UI.Text>().text="③ 发射"+(Simulation.SelectedShip>=0?"至所选外逃舰":"至选中目标");
  }
 }
}

