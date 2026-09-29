# 海克斯大乱斗 / ARAM: Mayhem

[中文](#中文) · [English](#english)

## 中文

由 **Natsuki** 制作的《杀戮尖塔 2》玩法模组，灵感来自《英雄联盟》的海克斯强化。本仓库包含海克斯本体 **HextechRunes** 和额外拓展包 **HextechRunesSponsorPack**。

### 玩法与安装

- 在每幕开始时选择白银、黄金或棱彩海克斯，改变本局构筑；敌人也会获得对应稀有度的敌方海克斯。
- 包含角色专属海克斯、卡牌升级、属性锻造器、新卡牌、符文图鉴与配置功能，支持联机游玩。
- 额外拓展包提供更多赞助者内容，需要同时安装本体。

通过 Steam 创意工坊安装：

| 模组 | 创意工坊 |
| --- | --- |
| 海克斯大乱斗本体 | [ARAM: Mayhem](https://steamcommunity.com/sharedfiles/filedetails/?id=3747501308) |
| 额外拓展包 | [HextechRunesSponsorPack](https://steamcommunity.com/sharedfiles/filedetails/?id=3749708876) |

本体的游戏正式分支与测试分支共用同一个工坊项目。具体支持的游戏版本以工坊说明和发行包为准；联机玩家应使用一致的模组版本与启用配置。

### 源码与文档

| 路径 | 内容 |
| --- | --- |
| [HextechRunes/](HextechRunes/) | 本体源码、资源、测试及构建工具 |
| [HextechRunesSponsorPack/](HextechRunesSponsorPack/) | 拓展包源码、资源及构建工具 |
| [开发规范](HextechRunes/docs/development.md) | 代码、内容与验证约定 |
| [工具手册](HextechRunes/docs/developer-tools.md) | 定位内容、定向测试、构建与打包 |
| [架构说明](HextechRunes/docs/architecture.md) | 模块划分与维护方向 |
| [外部接入说明](HextechRunes/INTEGRATION.md) | 供其他模组使用的扩展接口 |

### 本地开发

需要 .NET 9 SDK、Python 3，以及与构建目标匹配的本机游戏程序集。打包资源还需要支持 .NET 的 Godot 编辑器。游戏程序集、编辑器和构建产物不随源码提供。

```bash
git clone https://github.com/s1f102500012/HextechRunes.git
cd HextechRunes
python3 HextechRunes/tools/hextech_dev.py find SingularityAI --limit 3
python3 HextechRunes/tools/hextech_dev.py tests --list --match Hopper
```

上述查询不会构建或启动游戏。编译前请按[工具手册](HextechRunes/docs/developer-tools.md#构建环境与外部工具)配置游戏引用和本地路径。两个模组目录应保持相邻，拓展包会引用本体的工程、加载器和打包工具。

现有完整打包脚本面向 macOS/Zsh，默认还会部署到本机游戏目录。只构建打包、不部署时，分别使用 `HEXTECH_DEPLOY=0`（本体）和 `HEXTECH_SPONSOR_DEPLOY=0`（拓展包）。

### 反馈、贡献与历史

这是本体与拓展包的唯一开发入口。欢迎提交 [Issue](https://github.com/s1f102500012/HextechRunes/issues) 或 [Pull Request](https://github.com/s1f102500012/HextechRunes/pulls)。报告问题时请附游戏版本、模组版本、启用的其他模组、复现步骤与相关日志；发日志前请移除凭据和个人信息。

感谢所有[贡献者](CONTRIBUTORS.md)。原公开贡献与开发提交均已保留；[迁移记录](docs/migration/README.md)包含新旧提交对应表、核验结果和旧 PR 链接。原合集中的历史 PR、Issue 和讨论仍可查阅，后续开发直接在本仓库进行。

### 协议

沿用原项目的 [MIT License](LICENSE)。

---

## English

A **Slay the Spire 2** gameplay mod by **Natsuki**, inspired by Hextech augments from League of Legends. This repository contains the main mod, **HextechRunes**, and its optional add-on, **HextechRunesSponsorPack**.

### Gameplay and installation

- Choose a Silver, Gold, or Prismatic augment at the start of each act to reshape your build. Enemies also receive enemy augments of the corresponding rarity.
- Includes character-specific augments, card upgrades, stat forgers, new cards, a rune compendium, configuration options, and multiplayer support.
- The optional SponsorPack adds more supporter-created content and requires the main mod.

Install through the Steam Workshop:

| Mod | Workshop |
| --- | --- |
| Main mod | [ARAM: Mayhem](https://steamcommunity.com/sharedfiles/filedetails/?id=3747501308) |
| Optional add-on | [HextechRunesSponsorPack](https://steamcommunity.com/sharedfiles/filedetails/?id=3749708876) |

The main mod uses one Workshop item for both the game's public and beta branches. Check the Workshop description and release package for supported game versions. Multiplayer participants should use matching mod versions and enabled-mod configurations.

### Source and documentation

| Path | Contents |
| --- | --- |
| [HextechRunes/](HextechRunes/) | Main mod source, assets, tests, and build tools |
| [HextechRunesSponsorPack/](HextechRunesSponsorPack/) | Add-on source, assets, and build tools |
| [Development guidelines](HextechRunes/docs/development.md) | Code, content, and validation conventions |
| [Developer tools](HextechRunes/docs/developer-tools.md) | Content lookup, targeted tests, building, and packaging |
| [Architecture](HextechRunes/docs/architecture.md) | Module boundaries and maintenance direction |
| [Integration guide](HextechRunes/INTEGRATION.md) | Extension interfaces for other mods |

Detailed developer documentation is currently written in Chinese.

### Local development

You need the .NET 9 SDK, Python 3, and locally installed game assemblies matching your build target. Packaging assets also requires a .NET-enabled Godot editor. Game assemblies, the editor, and generated build artifacts are not included.

```bash
git clone https://github.com/s1f102500012/HextechRunes.git
cd HextechRunes
python3 HextechRunes/tools/hextech_dev.py find SingularityAI --limit 3
python3 HextechRunes/tools/hextech_dev.py tests --list --match Hopper
```

These queries do not build or launch the game. Before compiling, configure game references and local paths as described in the [build environment notes](HextechRunes/docs/developer-tools.md#构建环境与外部工具). Keep the two mod directories next to each other: the add-on references the main mod's project, loader, and packaging tools.

The complete packaging scripts currently target macOS/Zsh and deploy to a local game installation by default. To build and package without deploying, use `HEXTECH_DEPLOY=0` for the main mod and `HEXTECH_SPONSOR_DEPLOY=0` for the add-on.

### Feedback, contributions, and history

This is the canonical development repository for both mods. [Issues](https://github.com/s1f102500012/HextechRunes/issues) and [pull requests](https://github.com/s1f102500012/HextechRunes/pulls) are welcome. For bug reports, include your game version, mod version, other enabled mods, reproduction steps, and relevant logs. Remove credentials and personal information before sharing logs.

Thanks to all [contributors](CONTRIBUTORS.md). Original public contributions and development commits are preserved. The [migration record](docs/migration/README.md) contains old-to-new commit mappings, verification results, and links to original PRs. Historical PRs, issues, and discussions remain in the former collection repository; all new development takes place here.

### License

The project retains its original [MIT License](LICENSE).
