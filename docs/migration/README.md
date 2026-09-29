# 仓库历史迁移记录（2026-09-29）

> 本文是迁移完成时的历史快照。提交数量、文件数量及本地工作区状态仅描述当时的核验结果；当前开发入口与操作说明见[仓库 README](../../README.md)和[工具手册](../../HextechRunes/docs/developer-tools.md)。不要将本文当作重复执行的迁移或发布清单。

> This is a historical snapshot of the completed migration. Counts and local workspace status describe the checks performed at that time. For current development instructions, see the [repository README](../../README.md) and [developer tools](../../HextechRunes/docs/developer-tools.md).

此仓库从 `s1f102500012/sts2mod` 的公开历史和 `s1f102500012/sts2mod-dev` 的开发历史中提取海克斯本体、拓展包，再以真正的双亲合并提交连接两条历史。原仓库不改写历史，也不删除其他模组。

## 路径映射

| 历史路径 | 新路径 |
| --- | --- |
| `海克斯符文/`（早期尚未划分本体与拓展包） | `HextechRunes/` |
| `海克斯符文/本体/` | `HextechRunes/` |
| `海克斯符文/拓展包/` | `HextechRunesSponsorPack/` |
| `海克斯符文 HextechRunes/本体/` | `HextechRunes/` |
| `海克斯符文 HextechRunes/拓展包/` | `HextechRunesSponsorPack/` |
| 私有历史中的 `HextechRunes/`、`HextechRunesSponsorPack/` | 保持原路径 |

## 保留范围与核验

- 公开历史 104 笔相关提交、私有历史 661 笔相关提交，均保留原始作者、提交者、邮箱、时间与完整提交说明。
- 对每笔保留提交，比较迁移前后的元数据，以及按上述规则筛选、映射后的完整文件树；文件模式和 Git blob ID 均逐项核对。
- 即使某笔提交因路径规范化或生成产物剔除而变为空提交，也保留其提交记录。
- 原开发仓库三个海克斯相关分支已确认在开发历史中可达；另以 `archive/*` 分支保留其末端位置。
- 两个来源的最新版本共有 1,667 个文件，这些文件完全一致。开发历史额外提供 56 个开发文档、汇总分析和工坊源素材；合并时没有覆盖或舍弃公开贡献。
- 从私有历史剔除编译产物和 `workshop/content/`、`workshop-beta/content/`。从两个来源的历史中剔除 `analytics/runs_latest.csv`、`analytics/rune_choices_latest.csv`、`analytics/monster_hexes_latest.csv` 原始对局记录；相关提交及作者仍保留。
- 原有公开历史中的早期发行归档保留在旧提交中；新的构建产物不再提交到 Git。
- 本地未提交改动不混入历史迁移提交，也不随迁移发布；在本机新工作区单独恢复。

核验结果： [公开历史](public-verification.json)、[开发历史](development-verification.json)。

## 查找原提交

[commit-map.tsv](commit-map.tsv) 列出来源、原 SHA 与新 SHA，共 765 行提交映射。`public` 指公开合集仓库，`development` 指原开发仓库。

提交 SHA 因路径和父链变化而改变。原 GPG/SSH 提交签名不能沿用于改写后的对象；Git 作者署名及邮箱保持原值。

## 原 PR 与讨论

Git 提交与 PR 合并提交保留，但 GitHub 的 PR、Issue、评论、Star 不属于 Git 对象，不会自动转移。历史提交说明中的旧 `#编号` 应到原仓库查阅：

- [原 Pull Requests](https://github.com/s1f102500012/sts2mod/pulls?q=is%3Apr)
- [原 Issues](https://github.com/s1f102500012/sts2mod/issues)
- [原 PR 索引](original-pull-requests.md)

后续贡献直接提交到当前仓库。
