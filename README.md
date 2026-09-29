# 海克斯大乱斗 / ARAM: Mayhem

Natsuki 制作的《杀戮尖塔 2》海克斯玩法模组，以及额外拓展包。

**本仓库是海克斯本体与拓展包唯一的开发、Issue 和 Pull Request 入口。** 后续修改直接在这里提交，不再通过私有开发仓库向源码镜像同步。

This is the canonical development repository for HextechRunes (ARAM: Mayhem) and HextechRunesSponsorPack. Code, issues, and pull requests are maintained here directly.

| 目录 | 内容 |
| --- | --- |
| [HextechRunes](HextechRunes/) | 海克斯本体：符文、敌方海克斯、锻造器、卡牌和联机支持 |
| [HextechRunesSponsorPack](HextechRunesSponsorPack/) | 额外拓展包 |
| [开发规范](HextechRunes/docs/development.md) | 内容与代码维护约定 |
| [工具手册](HextechRunes/docs/developer-tools.md) | 构建、检查与工具入口 |
| [外部接入说明](HextechRunes/INTEGRATION.md) | 其他模组的接入接口 |

## 历史与贡献者

本仓库合并了原公开源码仓库中 **104 笔**海克斯相关提交，以及原私有开发仓库中 **661 笔**相关提交。两条历史都保留在默认分支 `main` 的祖先链中，未压缩成单次导入。

- 原作者、提交者、邮箱、时间、提交说明与共同作者署名均保留。
- 目录变化会改变提交 SHA；完整对应表和逐提交核验结果见[迁移记录](docs/migration/README.md)。
- 原 PR、Issue 和讨论仍保留在 [sts2mod](https://github.com/s1f102500012/sts2mod)，旧仓库作为历史入口。
- 感谢所有[贡献者](CONTRIBUTORS.md)。GitHub 的账号关联依照提交邮箱及平台规则生成。

Both original histories are retained on `main`, including author and committer metadata. See the [migration record](docs/migration/README.md) for commit mappings and original PR links.

## 构建

源码使用 .NET 9。编译需要你本机合法安装的游戏程序集；目标版本、引用路径和 Godot 编辑器路径以各模组的 `.csproj` 与 `tools/build_and_deploy.sh` 为准。游戏程序集、Godot 工具和生成的 DLL/PCK 不随源码提供。

本体与拓展包保留为相邻英文目录，以满足拓展包已有的 `ProjectReference`、共享加载器与打包工具引用。不要单独重命名其中一个目录。

从本体目录使用工具手册中的命令。`build_and_deploy.sh` 默认包含部署行为；仅构建本体时设置 `HEXTECH_DEPLOY=0`。本次仓库迁移核验了历史和源码一致性，没有重新构建或进行游戏内测试。

## 协议

沿用原仓库的 [MIT License](LICENSE)。
