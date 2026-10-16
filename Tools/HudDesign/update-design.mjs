import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const bundledRequire=createRequire(path.join(process.env.USERPROFILE||process.env.HOME||'.', '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/artifact-loader.cjs'));
const {FileBlob,SpreadsheetFile}=await import(pathToFileURL(bundledRequire.resolve('@oai/artifact-tool')).href);
const root=process.cwd(),out=path.join(root,'outputs','hud_inventory_2026-10-08');
await fs.mkdir(out,{recursive:true});
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(path.join(root,'局内玩法策划案.xlsx')));
const sheet=wb.worksheets.getItem('工作表1');
if(process.argv[2]==='preview'){
 const image=await wb.render({sheetName:'工作表1',range:'B40:G57',scale:1,format:'png'});
 await fs.writeFile(path.join(out,'原策划预览.png'),new Uint8Array(await image.arrayBuffer()));
 console.log((await wb.inspect({kind:'region',sheetId:'工作表1',range:'B40:G57',maxChars:1700})).ndjson);
}else{
 const rows=[
  ['物资储备HUD','分类','显示项目','库存口径','显示与交互规则','图标与后续替换'],
  ['', '载体','星尘 / 卫星 / 小行星 / 行星','int，实际可用库存','状态栏常驻显示四种载体的独立数量；制造中的数量单独标注在造，完成入库后更新，发射扣除1个。','六款载体图集，命名标注代替换'],
  ['', '科技','隐形探测 / 结构解离 / 武装校准','0/1，已研发成果','分项显示0/1，分组显示已研发项数/本关可研发项数；研发中的项目显示在研，科技持续生效，不在发射时扣除。','芯片图标，命名标注代替换'],
  ['', '武装','一级 / 二级 / 三级武装','int，独立库存','按档位显示现存数量，不合并为总武装；制造完成后增加，打击模块预装配或即时发射消耗对应档位武装。','火箭图标，命名标注代替换'],
  ['', '打击模块','地表 / 行星 / 卫星 / 恒星 / 星系 / 拦截','int，已预装配库存','新增打击模块页签；预装配时按原载荷费用扣资源并立即入库 → 发射优先消耗库存1个；无库存时保持原发射装配流程，不重复扣费。','火箭图标，命名标注代替换'],
  ['', '制造与投送','已有模块的发射费用','模块费用已预付','使用库存模块时不再扣该模块能源、物质或武装费用；隐形附加模块仍在发射时另行扣费。运输载体库存照常扣除。','费用清单按实际应付资源刷新'],
  ['', '信息一致性','库存与在造分离','完成才计入可用量','HUD直接读取局内模拟状态，不以资源折算可制造数量，不将排队或飞行中的物品算作手头储备。失败的发射不得消耗库存。','各图标均保留名称与数字'],
  ['', '解锁状态','当前关卡未开放项目','显示未解锁','未解锁项保留固定位置并灰显，不用0掩盖解锁条件；星系打击及模块仍在第五关开放，资源申请不能绕过。','图标与文字同时灰显'],
  ['', '查看与操作','点击库存图标','详情与生产入口','点击HUD条目暂停战场并显示库存、在造/在研、解锁关卡及消耗方式，允许跳转到对应制造、研发或预装配页；关闭后恢复原暂停状态。','正式图标可直接替换Sprite槽'],
  ['', '刷新时机','状态更新','实时事件+定时刷新','建设、制造、研发完成、模块预装配、发射及重开关卡后立即更新；常规每0.25秒刷新一次，暂停时库存仍可因玩家指令变化。','刷新不重建库存卡片'],
  ['', '布局','底部状态栏','资源+两行储备','保留能源、物质、信息、剩余时间和速度控制；新增两行储备，首行为载体及科技，次行为武装及打击模块，操作区继续常驻。','为16个条目保留独立图标窗口'],
  ['', '资产管理','占位图片与图标配置','原文件保留','三张用户图片存入工程HUD目录，全部文件名标注代替换；载体图集通过Unity切片使用，原图片像素不修改；旧美术资产不改动。','HUD图标配置可在Inspector替换'],
  ['', '验收','数量同步与费用一致','可复测','比如预装配1个地表打击模块，HUD立即显示1；成功发射后显示0，一级武装不再重复扣6；无载体导致发射失败时模块仍为1。','检查库存、费用、点击和灰显'],
 ];
 sheet.getRange('B92:G104').values=rows;
 // Keep original columns and styles intact; wrap only new rows to fit their existing widths.
 sheet.getRange('B92:G104').format.font={name:'Microsoft YaHei',size:11};
 sheet.getRange('B92:G104').format.wrapText=true;
 sheet.getRange('B92:G104').format.verticalAlignment='top';
 sheet.getRange('B92:G92').format.fill='#203D55';sheet.getRange('B92:G92').format.font={name:'Microsoft YaHei',size:11,bold:true,color:'#FFFFFF'};
 sheet.getRange('B92:G92').format.rowHeight=36;
 sheet.getRange('B93:G104').format.autofitRows();
 wb.recalculate();
 console.log((await wb.inspect({kind:'region',sheetId:'工作表1',range:'B92:G104',maxChars:2300})).ndjson);
 const image=await wb.render({sheetName:'工作表1',range:'B92:G104',scale:1,format:'png'});
 await fs.writeFile(path.join(out,'HUD策划补充预览.png'),new Uint8Array(await image.arrayBuffer()));
 const file=await SpreadsheetFile.exportXlsx(wb);await file.save(path.join(out,'局内玩法策划案.xlsx'));
 console.log('saved '+path.join(out,'局内玩法策划案.xlsx'));
}
