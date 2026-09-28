// UnityPlugin bundle 结构守卫
//
// 背景：Runtime/minigame-default/cachedPlugin/UnityPlugin/index.js 是整体替换的
// minified 构建产物。它曾在 release 环境（enableMonitor 被强制关闭）下抛
// `TypeError: undefined is not an object (evaluating 'e.downloadWasm')`，
// 因为启动监控的 showResult 没有按监控状态提前返回，而 showBaseInfo 无条件读取
// reportData.baseInfo.downloadWasm。异常发生在 gameStarted=true 之前，
// 会连带阻断 GAME_START 上报与 compileSubWasm()。
//
// 该 bundle 无法做行为级测试（minified + 依赖 window/document/GameGlobal/tj 等宿主全局），
// 所以这里只做结构断言：bundle 一变更就检查关键不变式还在不在，避免修复被静默覆盖。
//
// 用法：node verify-unityplugin-bundle.mjs
// 退出码：0 = 全部通过；1 = 有不变式被破坏、bundle 无法解析，或 bundle 内容与预期不符

import vm from 'node:vm';
import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const BUNDLE = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  'Runtime/minigame-default/cachedPlugin/UnityPlugin/index.js'
);

// bundle 的预期 sha256。更换 bundle 时，确认变更内容与 CHANGELOG 后更新此值。
// CI 会在 bundle 文件变更时强制核对它（见 .github/workflows/verify-bundle.yml）。
const EXPECTED_BUNDLE_SHA256 =
  '206a07a2043a67aaab49bac738594a62f59008393a6d0ea6e4e73292dc59eb60';

// 由 workflow 在检测到 bundle 变更时置为 'true'；本地运行时不会设置。
const BUNDLE_CHANGED = process.env.BUNDLE_CHANGED === 'true';


// 监控开启时仍要走原逻辑
const SHOW_RESULT_GUARD =
  /showResult=function\(e\)\{if\(void 0===e&&\(e=!1\),this\.enableMonitor&&!this\.endMonitor\)\{/;
// 监控关闭时不能读未生成的 reportData 字段
const NULL_DEFENSES = [
  [/showBaseInfo=function\(\)\{var e=this\.reportData\.baseInfo;return e\?\[/, 'showBaseInfo'],
  [/showAssetInfo=function\(\)\{var e=this\.reportData\.assetInfo;return e\?\[/, 'showAssetInfo'],
  [/showFrameInfo=function\(\)\{var e=this\.reportData\.frameInfo;return e\?\[/, 'showFrameInfo'],
];
const LAUNCH_INFO =
  /reportCustomLaunchInfo=function\(\)\{var e;if\(!this\.gameStarted\)\{([\s\S]*?)compileSubWasm/;
const VERSION = /UnityPluginVersion: ([0-9a-f]{40})/;

function report(checks) {
  let failed = 0;
  for (const [name, ok, evidence] of checks) {
    if (!ok) failed += 1;
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${evidence ? `   [${evidence}]` : ''}`);
  }
  console.log(`\nRESULT: ${failed ? `${failed} FAILED` : 'ALL_PASS'}`);
  return failed;
}

function main() {
  const checks = [];

  let source;
  try {
    source = fs.readFileSync(BUNDLE, 'utf8');
  } catch (error) {
    console.error(`FAIL  无法读取 ${BUNDLE}: ${error.message}`);
    return 1;
  }

  // 1. 产物必须能被 V8 解析（真正编译，不是括号计数）
  try {
    new vm.Script(source, { filename: BUNDLE });
    checks.push(['bundle 可通过 V8 语法编译', true]);
  } catch (error) {
    checks.push(['bundle 可通过 V8 语法编译', false, error.message]);
  }

  checks.push(['bundle 为 webpack IIFE 产物', source.trimEnd().endsWith('})();')]);
  checks.push(['showResult 保留 enableMonitor/endMonitor 前置守卫', SHOW_RESULT_GUARD.test(source)]);

  for (const [pattern, label] of NULL_DEFENSES) {
    checks.push([`${label} 保留 reportData 空数据防御`, pattern.test(source)]);
  }

  const launch = source.match(LAUNCH_INFO);
  if (!launch) {
    checks.push(['reportCustomLaunchInfo 结构保持可识别', false, '未匹配到启动上报段落']);
  } else {
    const body = launch[1];
    const started = body.indexOf('this.gameStarted=!0');
    const reported = body.indexOf('GameLaunchStatus.GAME_START');
    checks.push([
      'reportCustomLaunchInfo 结构保持可识别',
      started >= 0 && reported >= 0 && started < reported,
      started < 0 ? '缺少 gameStarted=!0' : reported < 0 ? '缺少 GAME_START 上报' : 'gameStarted 早于 GAME_START',
    ]);
  }

  const version = source.match(VERSION);
  if (!version) {
    checks.push(['bundle 声明 UnityPluginVersion 且格式合法', false, '未匹配到 40 位 hash']);
  } else {
    checks.push(['bundle 声明 UnityPluginVersion 且格式合法', true, version[1]]);
    console.log(`INFO  UnityPluginVersion: ${version[1]}`);
  }

  checks.push(['未回退到旧版拼写错误的 receivedBytedCount', !source.includes('receivedBytedCount')]);

  // 2. 内容核对：bundle 是整体替换的产物，必须与预期 hash 一致，
  //    否则说明换入了未经确认的 bundle（即使它恰好保留了上面的结构）。
  const actualHash = crypto.createHash('sha256').update(source).digest('hex');
  console.log(`INFO  bundle sha256: ${actualHash}`);
  const hashOk = actualHash === EXPECTED_BUNDLE_SHA256;
  if (!hashOk) {
    console.log('      更新方式：确认本次 bundle 变更内容与 CHANGELOG 后，');
    console.log(`      将脚本中的 EXPECTED_BUNDLE_SHA256 改为 ${actualHash}`);
  }
  checks.push([
    'bundle 内容与预期 sha256 一致',
    hashOk,
    hashOk ? '与 EXPECTED_BUNDLE_SHA256 一致' : `预期 ${EXPECTED_BUNDLE_SHA256.slice(0, 12)}… 实际 ${actualHash.slice(0, 12)}…`,
  ]);

  // 3. bundle 有改动时不允许“只改 bundle 不改预期 hash”蒙混过关
  checks.push([
    'bundle 变更时已同步更新预期 sha256',
    !BUNDLE_CHANGED || hashOk,
    BUNDLE_CHANGED ? '检测到 bundle 文件变更' : '本次无 bundle 变更，跳过',
  ]);

  return report(checks) ? 1 : 0;
}

process.exit(main());
