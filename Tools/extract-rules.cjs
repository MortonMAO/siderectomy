const fs = require('fs'), vm = require('vm'), path = require('path');
const source = fs.readFileSync(path.resolve(__dirname, '../../../裁星计划_实时原型.html'), 'utf8');
const script = source.split('<script>')[1].split('let state,tab=')[0];
const rules = vm.runInNewContext(script + ';({CFG,FAC,BATCH,CARRIERS,OPS,MODULES,RESEARCH,AI_FAC})');
const table = (o, category) => Object.entries(o).map(([key,v]) => ({key, category, ...v,
  unlock: key==='galaxy'?5:['a2','asteroid','satellite','planetary','disrupt','steal','virus','infoVirus','dissolve'].includes(key)?2:['a3','planet','stellar','calibration'].includes(key)?3:1}));
const data = {baseline:rules.CFG, facilities:table(rules.FAC,'facility'), batches:table(rules.BATCH,'batch'),
  carriers:table(rules.CARRIERS,'carrier'), operations:table(rules.OPS,'operation'),
  modules:table(rules.MODULES,'module'), research:table(rules.RESEARCH,'research'), ai:table(rules.AI_FAC,'ai')};
// 'satellite' carrier must be available for level 1 recon, the weapon unlocks on level 2.
data.carriers.find(x=>x.key==='satellite').unlock=1;
const output=path.resolve(__dirname,'../Assets/Siderectomy/Resources/DemoRules.json');
fs.mkdirSync(path.dirname(output),{recursive:true}); fs.writeFileSync(output,JSON.stringify(data,null,2));
console.log('Extracted HTML v1.1 rules:',output);
