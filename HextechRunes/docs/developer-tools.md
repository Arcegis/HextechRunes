# 开发工具与共享代码

先读 [开发规范](development.md)。以下命令使用本工作区绝对路径；其它机器替换 `/Users/iniad/sts2-mods/HextechRunes` 前缀。工具从自身位置找项目，不依赖当前目录。

## 内容定位

```bash
python3 /Users/iniad/sts2-mods/HextechRunes/tools/hextech_dev.py find '电球头'
python3 /Users/iniad/sts2-mods/HextechRunes/tools/hextech_dev.py find SingularityAI --limit 3
```

输出中文名、品级、注册元数据、模型/敌方两种描述、源码和测试引用、已有图标路径。复用内容校验器的注册解析与键名规则，支持连续大写缩写；只索引本体的注册符文/敌方/锻造器，不扫描发行副本或拓展包。它是导航工具，不是 C# 语义分析器；修改后仍需阅读目标文件。卡牌/Power 等用 `rg` 在对应源码目录找。

## 同步重复文案

以下命令将每种语言的源键复制到同语言的目标键，默认仅展示 diff：

```bash
python3 /Users/iniad/sts2-mods/HextechRunes/tools/hextech_dev.py loc-copy GLOBE_HEAD_HEX.description globeHeadHex.enemyDescription
# 明确两条文本语义相同后写入；--check 则只比较，有差异时退出 1
python3 /Users/iniad/sts2-mods/HextechRunes/tools/hextech_dev.py loc-copy GLOBE_HEAD_HEX.description globeHeadHex.enemyDescription --apply
```

`--locale zhs` 可限制语言，可重复；默认覆盖磁盘上的全部语言。`--table` 默认 `relics`，也支持 `relic_collection/cards/powers`。先检查所有所选语言的键存在且为字符串，再改写；保留其它行格式和内容。此工具不翻译、不更新 TXT、不重打 PCK。退出 2 表示缺键、格式或调用错误。

**不能对所有我方/敌方描述批量互抄。** 比如借用我方图标的敌方海克斯有不同效果；Inklet 的敌方文本有多人缩放变量。源键写错也会被工具忠实复制，所以先看 diff 和实际代码。无需维护“所有别名必须相等”的测试白名单。

TXT 沿用现有工具：

```bash
python3 /Users/iniad/sts2-mods/HextechRunes/tools/sync_content_txt.py
python3 /Users/iniad/sts2-mods/HextechRunes/tools/sync_content_txt.py --help
```

不传参数为只读预览（不是 `--check` 参数）。工具会读取拓展包，报告可能超出本次修改；`--apply` 会更新三个 TXT 的生成部分，不自动覆盖人工描述。只对已批准条目用 `--accept-json '锚'`，删除旧条目需显式 `--prune`。不要为消除预览差异整表覆盖。

TXT 描述从对应模型的 `CanonicalVars` 和数值常量取得未升级基础值，支持同文件多个模型、标准变量及 `PowerVar`，并剥离 BBCode。敌方人数缩放读取 `MonsterHexCatalog` 的参数表，以 `N` 表示玩家人数；需要具体对局状态的值使用明确公式。遇到无法静态解析的变量会报错，不能让裸占位符进入说明，也不能猜一个数值。已有人工描述仍须通过 `--accept-json` 才会更新。

`tools/validate_hextech_content.py` 还会比较固定九语逐键占位符集合、BBCode 配平及数量；原版中文引用读取 `tools/official_zhs_titles.json`，运行时不依赖本机 PCK。名称检查覆盖 `CardUpgradeRuneBase<T>` 的源码绑定、`[gold]` 名称引用，以及快照中逐键登记的无高亮官方模型引用，区分自创标题与普通强调词。它不是任意新句子的实体识别器：新增或删去无高亮引用时，须人工核对并维护 `references`。补充官方名称时从原版本地化取证更新快照；不得把错写的原版名称加到普通强调词豁免中。术语依据见 [自创术语](custom-terms.md)；原版中文名以 `tools/official_zhs_titles.json` 为准，不要在文档里另维护一份。

## 定向验证

玩家侧活力火花的施加入口与清理规则见 [设计裁决 · 玩家符文](design-decisions.md#玩家符文)；使用 `PowerCmd.Apply<HextechVitalSparkPower>`，不要直接将原版敌方增益施加到玩家。

```bash
python3 /Users/iniad/sts2-mods/HextechRunes/tools/hextech_dev.py tests --list --match Hopper
python3 /Users/iniad/sts2-mods/HextechRunes/tools/hextech_dev.py tests --target 0.111.0 --name HopperEscapeSurvivesTheNextNativeMoveRoll
# 加 --run 才构建该目标并运行所选测试，可重复 --name
python3 /Users/iniad/sts2-mods/HextechRunes/tools/hextech_dev.py tests --target 0.111.0 --name HopperEscapeSurvivesTheNextNativeMoveRoll --run
```

执行前校验维护目标和精确测试名，避免名称拼错变成“运行 0 项也成功”；构建失败后不运行残留 DLL。目标从 csproj 读取，名称从现有 Program 注册读取。测试项目仍会编译其工程依赖，但只执行指定案例，不构建 loader、不部署、不跑内容/发行检查。命令列出的是静态注册候选，最终是否可用由对应版本的测试程序判定。

不同目标共用 bin/obj，**串行执行**。测试中不要调用依赖 Godot 原生层的 API；确实需要时按已有隔离 fixture/`TestMode` 方法处理，真实界面/战斗验证仍交用户。

其它已有工具按任务使用：

| 工具 | 用途/副作用 |
| --- | --- |
| `tools/validate_hextech_content.py` | 全量内容/注册/本地化检查；不是每次小改必跑 |
| `tools/run_tests.sh` | Bash 脚本，默认全套、多目标、loader 和已有 bundle 检查；`HEXTECH_STS2_TARGET` 可限目标。仍是完整检查入口，不用于定向调用 |
| 工作区 `tools/sts2-inspect types <类型> <成员>` / `decompile <完整类型> --assembly <匹配 DLL>` | 原版证据；不要混用其它版本依赖 |
| `tools/multi_version/validate_variant_bundle.py` | loader/manifest/变体路径、目标、DLL 哈希校验 |
| `tools/build_and_deploy.sh` | Zsh 脚本，重建 `.build` 和 `dist`、导入、构建、打包；默认替换本机模组目录，设 `HEXTECH_DEPLOY=0` 才不部署 |
| `tools/package_release_zip.sh [输出绝对路径]` | 只打包现有 dist，不构建、不部署；调用下面的 Python 实现 |
| `tools/package_release.py [输出绝对路径] --dist <目录>` | 校验 bundle，再按变体清单打 ZIP；包含 loader、PCK、manifest、各变体 DLL 和必要 `compat-target.txt`，不含更新日志 TXT；成功后才替换原 ZIP |
| `tools/extract_near_death_feast_glow.gd -- <原版PCK> <输出PNG>` | 用 Godot `--headless --path tools -s <脚本绝对路径>` 运行，提取 SOUL_NEXUS 红光并写入指定 PNG；区域与来源见 [设计裁决 · 视觉](design-decisions.md#视觉) |
| `tools/update_latest_version.py` / 工坊上传器 / 镜像同步 | 涉及版本发布或外部写入；按用户指定范围使用，不是代码修改后的自动步骤 |

## 运行时共享能力

以下是已有实现的入口，使用前看具体签名和相邻调用。不要再造一组同义封装。

| 需求 | 入口 | 使用边界 |
| --- | --- | --- |
| 归属、攻击/技能判定、伤害预览 | `src/Relics/Base/HextechRelicBase.CombatHelpers.cs` | `IsOwnedAttack/IsOwnedSkill` 含模组特殊判定；预览不得产生副作用 |
| 角色/联网上下文 | `src/Helpers/HextechPlayerContextHelper.cs` | 本地玩家判断不能控制共享战斗结算 |
| 出牌/抽牌历史和宠物来源 | `src/Helpers/HextechCombatHistoryHelper.cs` | 核对 `firstInSeriesOnly/includeAutoPlay`；历史读取不等于自动保证跨端一致 |
| 小刀识别 | `src/Helpers/HextechKnifeHelper.cs` | 用当前项目的小刀规则，不到处另写 `card is Shiv` |
| 敌方三档数值/存活目标 | `src/EnemyHexes/HextechEnemyHexContext.cs` | `TierValue` 按该海克斯强度算；目标池用已有 helper；别以幕号替代强度 |
| 次数与战斗追踪 | `src/Mayhem/HextechCombatProcTracker.cs`、`HextechMayhemCombatTrackingState.cs` | 挑选玩家/敌人/全局及本回合/整场范围；新增字段还要接序列化和重置 |
| 确定性抽选 | `src/HextechStableRandom.cs` | 稳定身份、排序、盐值和触发序号是调用者契约；不擅自替换原流程 RNG |
| 随机获得符文 | `src/Helpers/HextechRuneGrantHelper.cs` | 包含联机 ID 同步及奖励生命周期限制；不要自行抽一个类型再只在本机发奖 |
| 防递归执行范围 | `src/Helpers/HextechScopedDepthGuard.cs` | `RunAsync` 管理进入/退出；只约束执行流，不是保存数据或联机协议 |
| 等待卡牌结算/下一帧 | `src/Helpers/HextechCardPlayTiming.cs`、`HextechGodotAsync.cs` | 用于已有时序需求；返回 false 表示生命周期已结束；帧数不能决定伤害或随机结果 |
| 私有 API/跨版本签名 | `src/Helpers/HextechHookReflection.cs`、`src/Compat` | 优先公开 API；新反射集中登记，版本差异隔离；缺成员要可诊断 |
| 图片资源/界面样式 | `src/Assets/HextechAssets.cs`、`HextechTextures.cs`、`src/UI/HextechUiTheme.cs` | 复用命名、加载与主题；不硬编码另一套路径和字号 |
| 对外扩展 | `src/Api` | 保持现有公共契约；只有调用方确实需要时才增加 API |

## 维护这些工具

```bash
python3 /Users/iniad/sts2-mods/HextechRunes/tools/tests/test_developer_tools.py
```

这些测试只覆盖开发工具的文件写入边界和发行包遗漏问题，不启动游戏或执行全部 C# 回归。修改只读查询时也可直接使用上面的查询示例核对。新增命令保持默认只读；有写入/构建动作需在帮助中明确，并更新本手册。
