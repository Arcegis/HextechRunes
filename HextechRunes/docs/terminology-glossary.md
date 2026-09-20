# 海克斯大乱斗术语表

审计基线：2026-09-21，源码 `6a5ec941`。官方文本直接读取当前本机游戏 PCK 的 `localization/<lang>/*.json`；导出文件只放临时目录，不进入仓库。下表保留同一资源键的原文，`esp` 与 `spa` 分别读取，不能互相代用。破折号表示该语言的原包缺键，不表示已经提供官方译文。

本表是文案依据，不更改玩法。原版名称优先采用同语言、同模型的 title；自创名称采用修改前模组文本中同概念现有写法的多数。变格、复数、动词与名词的语法变化不是另一套术语。格式器与变量是原版例句的一部分，不可直接复制到未注入相应变量的模组上下文。

## 角色名与限定后缀

正文提到角色时使用官方名称；中文限定后缀按用户指定统一为 `（仅战士）`、`（仅猎人）`、`（仅故障机器人）`、`（仅储君）`、`（仅亡灵契约师）`。只有实际联机限定的条目加 `（仅联机出现）`；不得给通用符文仅凭标签加角色限定。`HextechPlayerContextHelper` 使用原版角色 ModelId 判定；`CardUpgradeRuneBase.IsAvailableForPlayer` 还可能检查牌组条件。

## 五个原版角色的官方名称

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `characters/IRONCLAD.title` | 铁甲战士 | The Ironclad | El Blindado | El Blindado | アイアンクラッド | 아이언클래드 | O Rígido | Латоносец | นักรบเกราะเหล็ก |
| `characters/SILENT.title` | 静默猎手 | The Silent | La Silenciosa | La Silenciosa | サイレント | 사일런트 | A Sorrateira | Безмолвная | นักฆ่าแห่งความเงียบ |
| `characters/DEFECT.title` | 故障机器人 | The Defect | El Defectuoso | El Defectuoso | ディフェクト | 디펙트 | O Defeituoso | Дефект | นักเวทแปรพักตร์ |
| `characters/REGENT.title` | 储君 | The Regent | El Monarca | El Regente | リージェント | 리젠트 | O Regente | Наследник | —（原包缺键） |
| `characters/NECROBINDER.title` | 亡灵契约师 | The Necrobinder | La Necrodescarte | La Vinculahuesos | ネクロバインダー | 네크로바인더 | A Necroartífice | Призывающая | นักปลุกผีผูกจิต |

## 卡牌关键词

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `card_keywords/ETERNAL.title` | 永恒 | Eternal | Inmutabilidad | Eterna | 永劫 | 영구 | Perpétua | Вечная | นิรันดร์ |
| `card_keywords/ETHEREAL.title` | 虚无 | Ethereal | Evanescencia | Etérea | エセリアル | 휘발성 | Etérea | Эфирная | กึ่งสลาย |
| `card_keywords/EXHAUST.title` | 消耗 | Exhaust | Agotamiento | Agotamiento | 廃棄 | 소멸 | Exaurir | Сжигается | สลาย |
| `card_keywords/INNATE.title` | 固有 | Innate | Innatismo | Innata | 天賦 | 선천성 | Inata | Начальная | ติดตัว |
| `card_keywords/RETAIN.title` | 保留 | Retain | Retención | Retención | 保留 | 보존 | Manter | Оставляется | คงสภาพ |
| `card_keywords/SLY.title` | 奇巧 | Sly | Astucia | Astucia | スライ | 교활 | Sagaz | Коварная | เล่ห์เหลี่ยม |
| `card_keywords/UNPLAYABLE.title` | 不能被打出 | Unplayable | Injugabilidad | Injugable | プレイ不可 | 사용불가 | Injogável | Неиграбельная | ถูกผนึก |

## 战斗通用术语

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `static_hover_tips/BLOCK.title` | 格挡 | Block | Bloqueo | Bloqueo | ブロック | 방어도 | Proteção | Защита | บล็อก |
| `static_hover_tips/CHANNELING.title` | 生成 | Channel | Canalización | Invocar | 生成 | 영창 | Canalizar | Зарядка сфер | ปลุกเสก |
| `static_hover_tips/ENERGY.title` | 能量 | Energy | Energía | Energía | エナジー | 에너지 | Energia | Энергия | พลังงาน |
| `static_hover_tips/EVOKE.title` | 激发 | Evoke | Descarga | Descargar | 解放 | 발현 | Evocar | Разрядка сфер | ปล่อยพลัง |
| `static_hover_tips/FATAL.title` | 斩杀 | Fatal | Fatal | Letal | リーサル | 치명타 | Letal | Казнь | สังหาร |
| `static_hover_tips/FORGE.title` | 铸造 | Forge | Forjar | Forjar | 鍛造 | 단조 | Forjar | Закалка | หลอมสร้าง |
| `static_hover_tips/HIT_POINTS.title` | 生命值（HP） | Hit Points (HP) | Puntos de vida (PV) | Puntos de Vida (PV) | ヒットポイント (HP) | 체력 | Pontos de Vida (PV) | Очки здоровья (ОЗ) | พลังชีวิต (HP) |
| `static_hover_tips/MONEY_POUCH.title` | 金币 | Gold | Oro | Oro | ゴールド | 골드 | Ouro | Золото | ทอง |
| `static_hover_tips/REPLAY_DYNAMIC.title` | 重放 | Replay | Rejugar | Repetición | リプレイ | 재사용 | Repetir | Повтор | เล่นซ้ำ |
| `static_hover_tips/STAR_COUNT.title` | 辉星 | Stars | Estrellas | Estrellas | スター | 별 | Estrelas | Звезды | ดาว |
| `static_hover_tips/STUN.title` | 击晕 | Stun | Aturdimiento | Aturdimiento | スタン | 기절 | Atordoamento | Оглушение | ทำให้สตัน |
| `static_hover_tips/SUMMON_STATIC.title` | 召唤 | Summon | Invocación | Vincular | 召喚 | 소환 | Vincular | Призыв | อัญเชิญ |
| `static_hover_tips/TRANSFORM.title` | 变化 | Transform | Transformación | Transformar | 変化 | 변화 | Transformar | Преобразование | แปรสภาพ |

## 污染及其他侵蚀

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `afflictions/BOUND.title` | 魂缚 | Bound | Vínculo | Ligada | 束縛 | 구속 | Restrição | Печать | พันธะ |
| `afflictions/ENTANGLED.title` | 缠身 | Entangled | Enredo | Enredada | 絡みつき | 뒤엉킴 | Emaranhada | Путы | พัวพัน |
| `afflictions/GALVANIZED.title` | 流电 | Galvanized | Galvanismo | Galvanismo | 帯電 | 전기 자극 | Galvanização | Гальванизация | กัดกร่อน |
| `afflictions/HEXED.title` | 邪咒 | Hexed | Maleficio | Embrujada | 呪詛 | 주박 | Enfeitiçada | Сглаз | สาปสลาย |
| `afflictions/MOCK_NO_UNPLAYABLE_AFFLICTION.title` | 临时不可打出苦痛 | Mock No Unplayable Affliction | Mock No Unplayable Affliction | Mock No Unplayable Affliction | [テスト用]プレイ不可の災禍なし | 가짜 비사용불가 고난 | Mock No Unplayable Affliction | Mock No Unplayable Affliction | Mock No Unplayable Affliction |
| `afflictions/MOCK_SELF_DAMAGE_AFFLICTION.title` | 临时自伤苦痛 | Mock Self Damage Affliction | Mock Self Damage Affliction | Mock Self Damage Affliction | [テスト用] 自傷の災禍 | 가짜 자가 피해 고난 | Mock Self Damage Affliction | Mock Self Damage Affliction | Mock Self Damage Affliction |
| `afflictions/MOCK_USELESS_AFFLICTION.title` | 临时无用苦痛 | Mock Useless Affliction | Mock Useless Affliction | Mock Useless Affliction | [テスト用] 無用な災禍 | 가짜 무쓸모 고난 | Mock Useless Affliction | Mock Useless Affliction | Mock Useless Affliction |
| `afflictions/RINGING.title` | 昏眩 | Ringing | Vibración | Tañido | 鳴動 | 공명 | Ressonante | Звон в ушах | หูดับ |
| `afflictions/SMOG.title` | 烟雾 | Smog | Contaminación | Polución | スモッグ | 안개 | Poluição | Смог | ควัน |
| `afflictions/TAINTED.title` | 污染 | Tainted | Corrupción | Impureza | 汚染 | 훼손됨 | Contaminação | Порча | ปนเปื้อน |

## 附魔

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `enchantments/ADROIT.title` | 伶俐 | Adroit | Aptitud | Hábil | 巧妙 | 숙련 | Hábil | Смекалка | มนตร์คล่องแคล่ว |
| `enchantments/CLONE.title` | 克隆 | Clone | Clonación | Clonable | クローン | 복제 | Clonagem | Удвоение | มนตร์โคลนนิ่ง |
| `enchantments/CORRUPTED.title` | 腐化 | Corrupted | Corrupción | Corrupción | 蝕み | 오염 | Corrompida | Чудовищная | มนตร์ผุพัง |
| `enchantments/DEPRECATED_ENCHANTMENT.title` | 弃用 | Deprecated | Obsolescencia | Eliminado | 削除済み | 구식 | Descontinuada | Устаревшая | มนตร์ที่หายไป |
| `enchantments/GLAM.title` | 华彩 | Glam | Glamur | Glamour | 魅惑 | 호화 | Elegante | Шик | มนตร์สวยหรู |
| `enchantments/GOOPY.title` | 黏糊 | Goopy | Viscosidad | Viscosidad | べとべと | 점착 | Viscosidade | Вязкая | มนตร์หนังเหนียว |
| `enchantments/IMBUED.title` | 注能 | Imbued | Impregnación | Infusión | 刻印 | 주입 | Imbuída | Знаковая | มนตร์อัดฉีด |
| `enchantments/INKY.title` | 墨影 | Inky | Entintado | Tinta | 墨塗り | 잉크투성이 | Nanquim | Чернильная | มนตร์อาบหมึก |
| `enchantments/INSTINCT.title` | 本能 | Instinct | Instinto | Instinto | 本能 | 본능 | Instinto | Инстинкт | มนตร์สัญชาตญาณ |
| `enchantments/MOCK_FREE_ENCHANTMENT.title` | 临时免费附魔 | Mock Free Enchantment | Encantamiento sin costo (provisional) | Mock Free Enchantment | [テスト用]無料エンチャント | 가짜 무료 인챈트 | Mock Free Enchantment | Бесплатные чары только в мышеловке | Mock Free Enchantment |
| `enchantments/MOMENTUM.title` | 动量 | Momentum | Ímpetu | Impulso | モメンタム | 기세 | Impulso | Размах | มนตร์แรงเหวี่ยง |
| `enchantments/NIMBLE.title` | 灵巧 | Nimble | Agilidad | Agilidad | 身軽 | 기민함 | Ágil | Гибкость | มนตร์ปราดเปรียว |
| `enchantments/PERFECT_FIT.title` | 完美契合 | Perfect Fit | Calce perfecto | A medida | 完璧な適合 | 안성맞춤 | Encaixe Perfeito | Свое место | มนตร์เหมาะเหม็ง |
| `enchantments/ROYALLY_APPROVED.title` | 王室认证 | Royally Approved | Aprobación real | Sigilo real | 王室認可 | 왕실 인증 | Aprovação Real | Королевская печать | มนตร์พระราชทาน |
| `enchantments/SHARP.title` | 锋利 | Sharp | Filo | Filo | 鋭利 | 예리 | Afiada | Острая | มนตร์แหลมคม |
| `enchantments/SLITHER.title` | 蛇行 | Slither | Serpentino | Serpenteo | スリザー | 스르륵 | Serpenteio | Зигзаг | มนตร์อสรพิษ |
| `enchantments/SLUMBERING_ESSENCE.title` | 沉眠精华 | Slumbering Essence | Esencia letárgica | Esencia latente | 眠れる真髄 | 잠재성 | Essência Dormente | Дремлющая эссенция | มนตร์สกัดหลับใหล |
| `enchantments/SOULS_POWER.title` | 灵魂之力 | Soul's Power | Poder del alma | Poder del alma | 魂力 | 영혼의 힘 | Poder Espiritual | Искра души | มนตร์พลังวิญญาณ |
| `enchantments/SOWN.title` | 播种 | Sown | Sembradío | Siembra | 芽吹き | 발아 | Semeada | Посев | มนตร์หว่านเมล็ด |
| `enchantments/SPIRAL.title` | 涡旋 | Spiral | Espiral | Espiral | らせん | 소용돌이 | Espiral | Спираль | มนตร์หมุนวน |
| `enchantments/STEADY.title` | 稳定 | Steady | Estabilidad | Firmeza | 安定性 | 안정 | Prontidão | Надежность | มนตร์มั่นคง |
| `enchantments/SWIFT.title` | 迅速 | Swift | Velocidad | Rapidez | 迅速 | 신속 | Célere | Скорость | มนตร์ฉับพลัน |
| `enchantments/TEZCATARAS_EMBER.title` | 特兹卡塔拉的余烬 | Tezcatara's Ember | Ascua de Tezcatara | Ascua de Tezcatara | テスカタラの残り火 | 테즈카타라의 잉걸불 | Centelha de Tezcatara | Уголек Тецкатары | เพลิงของเทซคาทารา |
| `enchantments/VIGOROUS.title` | 活力 | Vigorous | Vigorosidad | Robustez | 活気 | 활기 | Vigorosa | Бодрость | มนตร์ชีวิตชีวา |

## 充能球

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `orbs/DARK_ORB.title` | 黑暗 | Dark | Oscuridad | Oscuridad | ダーク | 암흑 | Trevas | Сфера тьмы | ความมืด |
| `orbs/EMPTY_SLOT.title` | 充能球栏位 | Orb Slot | Espacio de orbe | Espacio de Orbe | オーブスロット | 구체 슬롯 | Espaço de Orbe | Слот для сферы | ช่องเก็บลูกแก้ว |
| `orbs/FROST_ORB.title` | 冰霜 | Frost | Escarcha | Escarcha | フロスト | 냉기 | Gelo | Сфера льда | น้ำแข็ง |
| `orbs/GLASS_ORB.title` | 玻璃 | Glass | Vidrio | Cristal | グラス | 유리 | Vidro | Сфера стекла | แก้ว |
| `orbs/LIGHTNING_ORB.title` | 闪电 | Lightning | Electricidad | Relámpago | ライトニング | 전기 | Relâmpago | Сфера молнии | สายฟ้า |
| `orbs/MOCK_COMBAT_CLEANUP_ORB.title` | Mock Combat Cleanup | Mock Combat Cleanup | Resumen de combate provisional | Mock Combat Cleanup | テスト用戦闘クリーンナップ | 가짜 전투 정리 | Mock Combat Cleanup | Mock Combat Cleanup | Mock Combat Cleanup |
| `orbs/PLASMA_ORB.title` | 等离子 | Plasma | Plasma | Plasma | プラズマ | 플라즈마 | Plasma | Сфера плазмы | พลาสมา |

## 常用动词搭配：原版例句

以下按同一原版资源键对照，引用的是完整原文而非机器翻译。中文通常用“给予 X 层易伤”“获得 X 点力量/格挡”“失去 X 点生命”；层数、点数、张数由该效果的实体类型决定。灼烧属于自创效果，另按下节的多数约定统一为“对目标施加 X 层灼烧”。

### 造成伤害、给予负面效果（`cards/BASH.description`）

| 语言 | 原版原文 |
|---|---|
| zhs | 造成{Damage:diff()}点伤害。<br>给予{VulnerablePower:diff()}层[gold]易伤[/gold]。 |
| eng | Deal {Damage:diff()} damage.<br>Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold]. |
| esp | Infliges {Damage:diff()} de daño.<br>Aplicas {VulnerablePower:diff()} de [gold]vulnerabilidad[/gold]. |
| spa | Inflige {Damage:diff()} de daño.<br>Aplica {VulnerablePower:diff()} de [gold]Vulnerabilidad[/gold]. |
| jpn | {Damage:diff()}ダメージを与える。<br>[gold]弱体[/gold]{VulnerablePower:diff()}を付与する。 |
| kor | 피해를 {Damage:diff()} 줍니다.<br>[gold]취약[/gold]을 {VulnerablePower:diff()} 부여합니다. |
| ptb | Cause {Damage:diff()} de dano.<br>Aplique {VulnerablePower:diff()} de [gold]Vulnerável[/gold]. |
| rus | Наносит {Damage:diff()} урона.<br>Накладывает {VulnerablePower:diff()} [gold]уязвимости[/gold]. |
| tha | สร้างความเสียหาย {Damage:diff()}<br>สร้าง {VulnerablePower:diff()} [gold]จุดอ่อน[/gold] |

### 获得力量、临时效果（`cards/INFLAME.description`）

| 语言 | 原版原文 |
|---|---|
| zhs | 获得{StrengthPower:diff()}点[gold]力量[/gold]。 |
| eng | Gain {StrengthPower:diff()} [gold]Strength[/gold]. |
| esp | Obtienes {StrengthPower:diff()} de [gold]fuerza[/gold]. |
| spa | Gana {StrengthPower:diff()} de [gold]Fuerza[/gold]. |
| jpn | [gold]筋力[/gold]{StrengthPower:diff()}を得る。 |
| kor | [gold]힘[/gold]을 {StrengthPower:diff()} 얻습니다. |
| ptb | Receba {StrengthPower:diff()} de [gold]Força[/gold]. |
| rus | Дает {StrengthPower:diff()} [gold]силы[/gold]. |
| tha | ได้รับ {StrengthPower:diff()} [gold]ความแข็งแรง[/gold] |

### 获得格挡（`cards/DEFEND_IRONCLAD.description`）

| 语言 | 原版原文 |
|---|---|
| zhs | 获得{Block:diff()}点[gold]格挡[/gold]。 |
| eng | Gain {Block:diff()} [gold]Block[/gold]. |
| esp | Obtienes {Block:diff()} de [gold]bloqueo[/gold]. |
| spa | Gana {Block:diff()} de [gold]Bloqueo[/gold]. |
| jpn | {Block:diff()}[gold]ブロック[/gold]を得る。 |
| kor | [gold]방어도[/gold]를 {Block:diff()} 얻습니다. |
| ptb | Receba {Block:diff()} de [gold]Proteção[/gold]. |
| rus | Дает {Block:diff()} [gold]защиты[/gold]. |
| tha | ได้รับ {Block:diff()} [gold]บล็อก[/gold] |

### 失去生命、抽牌（`cards/OFFERING.description`）

| 语言 | 原版原文 |
|---|---|
| zhs | 失去{HpLoss:diff()}点生命。<br>获得{Energy:energyIcons()}。<br>抽{Cards:diff()}张牌。 |
| eng | Lose {HpLoss:diff()} HP.<br>Gain {Energy:energyIcons()}.<br>Draw {Cards:diff()} cards. |
| esp | Pierdes {HpLoss:diff()} PV.<br>Obtienes {Energy:energyIcons()}.<br>Robas {Cards:diff()} cartas. |
| spa | Pierde {HpLoss:diff()} PV.<br>Gana {Energy:energyIcons()}.<br>Roba {Cards:diff()} cartas. |
| jpn | HPを{HpLoss:diff()}失う。<br>{Energy:energyIcons()}を得る。<br>カードを{Cards:diff()}枚引く。 |
| kor | 체력을 {HpLoss:diff()} 잃습니다.<br>{Energy:energyIcons()}를 얻습니다.<br>카드를 {Cards:diff()}장 뽑습니다. |
| ptb | Perca {HpLoss:diff()} de PV.<br>Receba {Energy:energyIcons()}.<br>Compre {Cards:diff()} cartas. |
| rus | Вы теряете {HpLoss:diff()} ОЗ.<br>Дает {Energy:energyIcons()}.<br>Вы добираете {Cards:diff()} карты. |
| tha | เสีย {HpLoss:diff()} HP<br>ได้รับ {Energy:energyIcons()}<br>จั่วการ์ด {Cards:diff()} ใบ |

### 生成充能球（`cards/ZAP.description`）

| 语言 | 原版原文 |
|---|---|
| zhs | [gold]生成[/gold]1个[gold]闪电[/gold]充能球。 |
| eng | [gold]Channel[/gold] 1 [gold]Lightning[/gold]. |
| esp | [gold]Canalizas[/gold] 1 de [gold]electricidad[/gold]. |
| spa | [gold]Invoca[/gold] 1 [gold]Relámpago[/gold]. |
| jpn | [gold]ライトニング[/gold]1を[gold]生成[/gold]する。 |
| kor | [gold]전기[/gold]를 1번 [gold]영창[/gold]합니다. |
| ptb | [gold]Canalize[/gold] 1 [gold]Relâmpago[/gold]. |
| rus | [gold]Заряжает[/gold] 1 [gold]сферу молнии[/gold]. |
| tha | [gold]ปลุกเสก[/gold] 1 [gold]สายฟ้า[/gold] |

### 消耗牌（`cards/FIEND_FIRE.description`）

| 语言 | 原版原文 |
|---|---|
| zhs | [gold]消耗[/gold]所有[gold]手牌[/gold]。<br>每张被[gold]消耗[/gold]的牌造成{Damage:diff()}点伤害。 |
| eng | [gold]Exhaust[/gold] your [gold]Hand[/gold].<br>Deal {Damage:diff()} damage for each card [gold]Exhausted[/gold]. |
| esp | [gold]Agotas[/gold] tu [gold]mano[/gold].<br>Infliges {Damage:diff()} de daño por cada carta [gold]agotada[/gold]. |
| spa | [gold]Agota[/gold] tu [gold]Mano[/gold].<br>Inflige {Damage:diff()} de daño por cada carta [gold]Agotada[/gold]. |
| jpn | [gold]手札[/gold]をすべて[gold]廃棄[/gold]する。<br>[gold]廃棄[/gold]したカード1枚につき、{Damage:diff()}ダメージを与える。 |
| kor | [gold]손[/gold]에 있는 모든 카드를<br>[gold]소멸[/gold]시킵니다.<br>[gold]소멸[/gold]시킨 카드 1장당<br>피해를 {Damage:diff()} 줍니다. |
| ptb | [gold]Exaure[/gold] a sua [gold]Mão[/gold].<br>Cause {Damage:diff()} de dano por cada carta [gold]Exaurida[/gold]. |
| rus | [gold]Сжигает[/gold] все карты в [gold]руке[/gold].<br>Наносит по {Damage:diff()} урона за каждую. |
| tha | [gold]สลาย[/gold]ทั้ง[gold]มือ[/gold]<br>สร้างความเสียหาย {Damage:diff()} ตามจำนวนการ์ดที่[gold]ถูกสลาย[/gold] |

### 将牌加入手牌（`cards/BLADE_DANCE.description`）

| 语言 | 原版原文 |
|---|---|
| zhs | 添加{Cards:diff()}张[gold]小刀[/gold]到你的[gold]手牌[/gold]。 |
| eng | Add {Cards:diff()} [gold]{Cards:plural:Shiv&#124;Shivs}[/gold] into your [gold]Hand[/gold]. |
| esp | Agregas {Cards:diff()} [gold]{Cards:plural:Navaja&#124;carta de Navaja}[/gold] a tu [gold]mano[/gold]. |
| spa | Añade {Cards:diff()} [gold]{Cards:plural:Navaja&#124;Navajas}[/gold] a tu [gold]Mano[/gold]. |
| jpn | [gold]ナイフ[/gold]を{Cards:diff()}枚[gold]手札[/gold]に加える。 |
| kor | [gold]단도[/gold]를 {Cards:diff()}장<br>[gold]손[/gold]으로 가져옵니다. |
| ptb | Adicione {Cards:diff()} [gold]{Cards:plural:Lâmina&#124;Lâminas}[/gold] na sua [gold]Mão[/gold]. |
| rus | Добавляет в [gold]руку[/gold] {Cards:diff()} [gold]{Cards:plural(ru):«Заточку»&#124;«Заточки»&#124;«Заточек»}[/gold]. |
| tha | เพิ่ม[gold]มีดสั้น[/gold] {Cards:diff()} ใบใส่[gold]มือ[/gold] |

### 召唤（`cards/UNLEASH.description`）

| 语言 | 原版原文 |
|---|---|
| zhs | [gold]奥斯提[/gold]造成{CalculatedDamage:diff()}点伤害。<br>这张牌额外造成等量于[gold]奥斯提[/gold]当前生命值的伤害。 |
| eng | [gold]Osty[/gold] deals {CalculatedDamage:diff()} damage.<br>Deals additional damage equal to [gold]Osty's[/gold] current HP. |
| esp | [gold]Puro Hueso[/gold] inflige {CalculatedDamage:diff()} de daño.<br>Inflige daño adicional por un valor equivalente a sus PV actuales. |
| spa | [gold]Nudillos[/gold] inflige {CalculatedDamage:diff()} de daño.<br>Inflige daño adicional igual a los PV actuales de [gold]Nudillos[/gold]. |
| jpn | [gold]オスティ[/gold]が{CalculatedDamage:diff()}ダメージを与える。<br>[gold]オスティ[/gold]の現在HPに等しい追加ダメージを与える。 |
| kor | [gold]골골이[/gold]가 피해를 {CalculatedDamage:diff()} 줍니다.<br>[gold]골골이[/gold]의 현재 체력만큼<br>피해량이 증가합니다. |
| ptb | [gold]Ostheo[/gold] causa {CalculatedDamage:diff()} de dano.<br>Causa dano adicional equivalente aos PV atuais de [gold]Ostheo[/gold]. |
| rus | [gold]Костя[/gold] наносит {CalculatedDamage:diff()} урона.<br>Наносит дополнительный урон, равный ОЗ [gold]Кости[/gold]. |
| tha | [gold]น้าผี[/gold]สร้างความเสียหาย {CalculatedDamage:diff()}<br>สร้างความเสียหายเพิ่มตามจำนวน HP ปัจจุบันของ[gold]น้าผี[/gold] |

## 模组自创术语：多数写法及使用边界

频次统计范围为审计开始时本体九语言 JSON 的字符串值，排除 `.flavor`，剥标签后按大小写不敏感的最长候选非重叠匹配；同词的复数/格变化合并到词典形。计数是定位多数的文本证据，不表示每个同形词都属于该 Power，因此修正文案时必须核对模型与上下文，不全局替换同形的原版卡名。

| 概念 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| 灼烧 | 灼烧 | Burn | Quemadura | Quemadura | 火傷 | 점화 | Queimadura | Ожог | เผาไหม้ |
| 属性锻造器 | 属性锻造器 | Stat Forger | Forjador de atributo | Forjador de atributo | ステータス鍛造器 | 능력치 모루 | Forjador de Atributos | Наковальня показателей | ช่างหลอมค่าสถานะ |
| 锻体 | 锻体 | Forge Body | Forjar el cuerpo | Forjar el cuerpo | 肉体鍛錬 | 단련 | Forjar Corpo | Ковать тело | หลอมร่าง |
| 临时缓慢 | 临时缓慢 | temporary Slow | Lentitud temporal | Lentitud temporal | 一時的なスロウ | 임시 둔화 | Lentidão temporária | Временное замедление | เชื่องช้าชั่วคราว |

“临时缓慢”是隐藏的 `HextechTemporarySlowPower` 对可见 `HextechPlayerSlowPower` 的临时增减；它没有独立本地化 title，不得把原版 `SlowPower` 的机制说明套过来。原版 Slow 与模组 Slow 名称相同但机制不同，具体效果以对应描述为准。

“活力火花”“流电”“吊杀”等沿用原版名称的自定义 Power，名称优先同语言对应原版 title，但描述必须保留模组自身机制。泰语等原包缺键时，仅能明确标为模组选词，不能宣称官方译法。

中文灼烧施加语句的基线主写法为“施加”（9 条）；“给予”（4 条）及“使其获得”（1 条）统一到“对目标施加 X 层灼烧”。这是自创术语的局部多数约定，不覆盖原版例句的“给予 X 层易伤”。对象和层数必须保留原行为。

### 多数选择的计数证据

| 概念 | 语言 | 选定写法 | 候选出现次数 |
|---|---|---|---|
| 灼烧 | zhs | 灼烧 | 灼烧=19；燃烧=3 |
| 灼烧 | eng | Burn | Burn=24；Burning=1；Scorch=0 |
| 灼烧 | esp | Quemadura | Quemadura=18；Ardor=2 |
| 灼烧 | spa | Quemadura | Quemadura=18；Ardor=2 |
| 灼烧 | jpn | 火傷 | 火傷=17；燃焼=4；やけど=0 |
| 灼烧 | kor | 점화 | 점화=14；화상=5 |
| 灼烧 | ptb | Queimadura | Queimadura=21；Queima=0 |
| 灼烧 | rus | Ожог | Ожог=19；Горение=0 |
| 灼烧 | tha | เผาไหม้ | เผาไหม้=18；ไฟลวก=3 |
| 属性锻造器 | zhs | 属性锻造器 | 属性锻造器=20；属性锻造系统=1 |
| 属性锻造器 | eng | Stat Forger | Stat Forger=18；Attribute Forge=2 |
| 属性锻造器 | esp | Forjador de atributo | Forjador de atributo=18；Forja de atributos=0 |
| 属性锻造器 | spa | Forjador de atributo | Forjador de atributo=18；Forja de atributos=0 |
| 属性锻造器 | jpn | ステータス鍛造器 | ステータス鍛造器=19；属性鍛造器=0 |
| 属性锻造器 | kor | 능력치 모루 | 능력치 모루=20；속성 단조기=0 |
| 属性锻造器 | ptb | Forjador de Atributos | Forjador de Atributos=18；Forja de atributo=2 |
| 属性锻造器 | rus | Наковальня показателей | Наковальня показателей=13；Наковальня характеристик=2；Кузня показателей=1 |
| 属性锻造器 | tha | ช่างหลอมค่าสถานะ | ช่างหลอมค่าสถานะ=20 |
| 锻体 | zhs | 锻体 | 锻体=3 |
| 锻体 | eng | Forge Body | Forge Body=3 |
| 锻体 | esp | Forjar el cuerpo | Forjar el cuerpo=3 |
| 锻体 | spa | Forjar el cuerpo | Forjar el cuerpo=3 |
| 锻体 | jpn | 肉体鍛錬 | 肉体鍛錬=2；肉体を鍛える=1 |
| 锻体 | kor | 단련 | 단련=5 |
| 锻体 | ptb | Forjar Corpo | Forjar Corpo=3 |
| 锻体 | rus | Ковать тело | Ковать тело=3 |
| 锻体 | tha | หลอมร่าง | หลอมร่าง=3 |
| 临时缓慢 | zhs | 临时缓慢 | 临时缓慢=7 |
| 临时缓慢 | eng | temporary Slow | temporary Slow=7 |
| 临时缓慢 | esp | Lentitud temporal | Lentitud temporal=7；Lento temporal=0 |
| 临时缓慢 | spa | Lentitud temporal | Lentitud temporal=7；Lento temporal=0 |
| 临时缓慢 | jpn | 一時的なスロウ | 一時的なスロウ=7；一時的な遅い=0 |
| 临时缓慢 | kor | 임시 둔화 | 임시 둔화=7 |
| 临时缓慢 | ptb | Lentidão temporária | Lentidão temporária=7 |
| 临时缓慢 | rus | Временное замедление | Временное замедление=7 |
| 临时缓慢 | tha | เชื่องช้าชั่วคราว | เชื่องช้าชั่วคราว=7 |

### 其余模组能力的当前名称

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `powers/HEXTECH_ATTACK_REPLAY_POWER.title` | 欺诈魔术 | Trick Magic | Magia trucada | Magia trucada | トリックマジック | 마술 트릭 | Magia de Truque | Иллюзорная магия | มายากลลวง |
| `powers/HEXTECH_BURN_POWER.title` | 灼烧 | Burn | Quemadura | Quemadura | 火傷 | 점화 | Queimadura | Ожог | เผาไหม้ |
| `powers/HEXTECH_CHEMTECH_DRAGON_SOUL_POWER.title` | 炼金龙魂 | Chemtech Dragon Soul | Alma de dragón químicotecnológico | Alma de dragón químicotecnológico | ケムテックドラゴンソウル | 화학공학 드래곤의 영혼 | Alma do Dragão Quimtec | Душа химтек-дракона | วิญญาณมังกรเคมเทค |
| `powers/HEXTECH_CLOUD_DRAGON_SOUL_POWER.title` | 云霄龙魂 | Cloud Dragon Soul | Alma de Dragón Nube | Alma de Dragón Nube | クラウドドラゴンソウル | 바람 드래곤의 영혼 | Alma do Dragão das Nuvens | Душа облачного дракона | วิญญาณมังกรเมฆา |
| `powers/HEXTECH_DRAGON_SOUL_POWER.title` | 海克斯科技龙魂 | Hextech Dragon Soul | Alma de dragón hextech | Alma de dragón hextech | ヘクステックドラゴンソウル | 마법공학 드래곤의 영혼 | Alma do Dragão Hextech | Душа хекстек-дракона | วิญญาณมังกรเฮกซ์เทค |
| `powers/HEXTECH_GALVANIC_POWER.title` | 流电 | Galvanic | Galvanismo | Galvanismo | ガルバニック | 전류 | Galvanização | Гальванизация | กัดกร่อน |
| `powers/HEXTECH_HANG_POWER.title` | 吊杀 | Hang | Ahorcar | Ahorcar | 吊殺 | 교살 | Enforcar | Повешение | แขวนคอ |
| `powers/HEXTECH_INFERNAL_DRAGON_SOUL_POWER.title` | 炼狱龙魂 | Infernal Dragon Soul | Alma de Dragón Infernal | Alma de Dragón Infernal | インファーナルドラゴンソウル | 화염 드래곤의 영혼 | Alma do Dragão Infernal | Душа огненного дракона | วิญญาณมังกรนรก |
| `powers/HEXTECH_MOUNTAIN_DRAGON_SOUL_POWER.title` | 山脉龙魂 | Mountain Dragon Soul | Alma de dragón de montaña | Alma de dragón de montaña | マウンテンドラゴンソウル | 대지 드래곤의 영혼 | Alma do Dragão da Montanha | Душа горного дракона | วิญญาณมังกรภูผา |
| `powers/HEXTECH_NEXT_TURN_DAMAGE_POWER.title` | 下回合伤害 | Next Turn Damage | Daño el próximo turno | Daño el próximo turno | 次ターンダメージ | 다음 턴 피해 | Dano no Próximo Turno | Урон в следующий ход | ความเสียหายเทิร์นถัดไป |
| `powers/HEXTECH_OCEAN_DRAGON_SOUL_POWER.title` | 海洋龙魂 | Ocean Dragon Soul | Alma de dragón oceánico | Alma de dragón oceánico | オーシャンドラゴンソウル | 바다 드래곤의 영혼 | Alma do Dragão do Oceano | Душа морского дракона | วิญญาณมังกรสมุทร |
| `powers/HEXTECH_PLAYER_SLOW_POWER.title` | 缓慢 | Slow | Lento | Lento | 遅い | 둔화 | Lentidão | Замедление | Slow |
| `powers/HEXTECH_VITAL_SPARK_POWER.title` | 活力火花 | Vital Spark | Chispa vital | Chispa vital | 生命の火花 | 생명의 불꽃 | Centelha vital | Искра жизни | ประกายชีวิต |

## 原版能力名完整索引

以下为 PCK 中文 powers 表中全部 `.title` 键的九语言对应。表内可能包含测试/不可见模型，收录表示原包存在该键，不代表应加入游戏内容池。能力同名不代表可互换机制。

## 原版 Power title

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `powers/ACCELERANT_POWER.title` | 触媒 | Accelerant | Acelerante | Acelerante | 促進剤 | 촉진제 | Acelerante | Катализатор | สารเร่งปฏิกิริยา |
| `powers/ACCURACY_POWER.title` | 精准 | Accuracy | Precisión | Precisión | 精度上昇 | 정밀 | Pontaria | Меткость | ความแม่นยำ |
| `powers/ADAPTABLE_POWER.title` | 适者生存 | Adaptable | Adaptabilidad | Adaptable | 適応 | 적응력 | Adaptável | Адаптивность | —（原包缺键） |
| `powers/ADAPTATION_POWER.title` | 适应 | Adaptation | Adaptación | Adaptación | 順応 | 적응 | Adaptação | Сообразительность | —（原包缺键） |
| `powers/AFTERIMAGE_POWER.title` | 余像 | Afterimage | Imagen residual | Imagen residual | 残像 | 잔상 | Imagem Residual | След в воздухе | ภาพติดตาชั่วขณะ |
| `powers/AGGRESSION_POWER.title` | 好勇斗狠 | Aggression | Agresión | Agresión | 闘争心 | 공격성 | Agressividade | Агрессия | ความก้าวร้าว |
| `powers/AMBERGRIS_POWER.title` | 龙涎香 | Ambergris | Ámbar gris | Ámbar gris | 龍涎香 | 용연향 | Âmbar Cinzento | Амбра | อำพันทะเล |
| `powers/ARSENAL_POWER.title` | 武器库 | Arsenal | Arsenal | Arsenal | 武器庫 | 무기고 | Arsenal | Арсенал | คลังสรรพาวุธ |
| `powers/ARTIFACT_POWER.title` | 人工制品 | Artifact | Artefacto | Artefacto | アーティファクト | 인공물 | Artefato | Артефакт | อาคม |
| `powers/ASLEEP_POWER.title` | 沉睡 | Asleep | Durmiente | Siesta | 睡眠 | 수면 | Adormecida | Сон | หลับอยู่ |
| `powers/AUTOMATION_POWER.title` | 自动化 | Automation | Automatización | Automatización | オートメーション | 자동화 | Automação | Автоматизация | กลไกอัตโนมัติ |
| `powers/A_THOUSAND_CUTS_POWER.title` | 凌迟 | A Thousand Cuts | Mil cortes | Mil cortes | サウザンドカット | 능지처참 | Mil Cortes | Тысяча порезов | พันคมมีด |
| `powers/BACK_ATTACK_LEFT_POWER.title` | 后方攻击 | Back Attack | Ataque trasero | Ataque por la espalda | バックアタック | 후방 공격 | Ataque Traseiro | Атака в спину | โจมตีจากด้านหลัง |
| `powers/BACK_ATTACK_RIGHT_POWER.title` | 后方攻击 | Back Attack | Ataque trasero | Ataque por la espalda | バックアタック | 후방 공격 | Ataque Traseiro | Атака в спину | โจมตีจากด้านหลัง |
| `powers/BARRICADE_POWER.title` | 壁垒 | Barricade | Barricada | Barricada | バリケード | 바리케이드 | Barricada | Баррикада | ตรึงกำลัง |
| `powers/BATTLEWORN_DUMMY_TIME_LIMIT_POWER.title` | 时间限制 | Time Limit | Tiempo límite | Límite de tiempo | タイムリミット | 시간 제한 | Limite de Tempo | Ограничение по времени | จำกัดเวลา |
| `powers/BEACON_OF_HOPE_POWER.title` | 希望灯塔 | Beacon of Hope | Faro de la esperanza | Égida de la esperanza | 希望の道標 | 희망의 등불 | Farol da Esperança | Луч надежды | ดวงประทีปแห่งความหวัง |
| `powers/BIASED_COGNITION_POWER.title` | 偏差认知 | Biased Cognition | Percepción sesgada | Conocimiento parcial | 認知偏向 | 인지 편향 | Cognição Enviesada | Предвзятость | อคติทางการรู้คิด |
| `powers/BLACK_HOLE_POWER.title` | 黑洞 | Black Hole | Agujero negro | Agujero negro | ブラックホール | 블랙홀 | Buraco Negro | Черная дыра | หลุมดำ |
| `powers/BLOCK_NEXT_TURN_POWER.title` | 下回合格挡 | Block Next Turn | Bloqueo de próximo turno | Bloqueo inminente | 次ターンブロック | 다음 턴 방어도 | Proteção Iminente | Отложенная защита | บล็อกเทิร์นถัดไป |
| `powers/BLUR_POWER.title` | 残影 | Blur | Desenfoque | Difuminarse | ブラー | 흐릿함 | Desfocar | Неуловимость | ภาพมัว |
| `powers/BORROWED_TIME_POWER.title` | 预借时间 | Borrowed Time | Tiempo prestado | Tiempo prestado | 借り物の時間 | 연명 | Tempo Emprestado | Сверхурочные | กู้ยืมเวลา |
| `powers/BUFFER_POWER.title` | 缓冲 | Buffer | Búfer | Amortiguador | バッファー | 버퍼 | Amortecedor | Буфер | ม่านกันชน |
| `powers/BURROWED_POWER.title` | 埋地 | Burrowed | Excavar | Excavar | 潜伏 | 잠복 | Enterrado | Под землей | —（原包缺键） |
| `powers/BURST_POWER.title` | 爆发 | Burst | Impulso | Ráfaga | バースト | 폭주 | Surto | Прилив сил | ระเบิดพลัง |
| `powers/CACOPHONY_POWER.title` | 不谐合曲 | Cacophony | Cacofonía | Cacofonía | 不協和音 | 불협화음 | Cacofonia | Какофония | —（原包缺键） |
| `powers/CALAMITY_POWER.title` | 劫难 | Calamity | Calamidad | Calamidad | カラミティ | 재앙 | Calamidade | Смерч | —（原包缺键） |
| `powers/CALCIFY_POWER.title` | 钙化 | Calcify | Calcificación | Calcificación | 骨強化 | 석회화 | Calcificar | Окостенение | —（原包缺键） |
| `powers/CALL_OF_THE_VOID_POWER.title` | 虚空之唤 | Call of the Void | Llamado del vacío | Llamada del Vacío | 虚空の呼び声 | 공허의 부름 | Chamado do Vazio | Зов бездны | —（原包缺键） |
| `powers/CHAINS_OF_BINDING_POWER.title` | 魂缚锁链 | Chains of Binding | Cadenas de vínculo | Cadenas vinculantes | 拘束の鎖 | 속박의 사슬 | Correntes de Restrição | Сковывающая печать | โซ่ตรวนแห่งพันธนาการ |
| `powers/CHILD_OF_THE_STARS_POWER.title` | 群星之子 | Child of the Stars | Vástago estelar | Vástago de las estrellas | 星の子 | 별의 아이 | Filho das Estrelas | Дитя звезд | —（原包缺键） |
| `powers/CLARITY_POWER.title` | 明晰 | Clarity | Claridad | Claridad | 明鏡止水 | 명확성 | Lucidez | Ясность | กระจ่างแจ้งแจ่มชัด |
| `powers/COLOSSUS_POWER.title` | 巨像 | Colossus | Coloso | Colosal | 巨人 | 거상 | Colosso | Колосс | —（原包缺键） |
| `powers/CONCOCT_POWER.title` | 调制 | Concoct | Mezcla | Toxicología | 調合 | 조제 | Concocção | Варево | —（原包缺键） |
| `powers/CONFUSED_POWER.title` | 混乱 | Confused | Confusión | Confusión | 混乱 | 혼란 | Confusão | Замешательство | สับสน |
| `powers/CONQUEROR_POWER.title` | 征服者 | Conqueror | Conquistador | Conquistador | 征服者 | 정복자 | Conquistador | Завоевание | —（原包缺键） |
| `powers/CONSTRICT_POWER.title` | 紧缠 | Constrict | Constricción | Opresión | 締め付け | 조이기 | Apertar | Удушье | —（原包缺键） |
| `powers/CONSUMING_SHADOW_POWER.title` | 吞噬暗影 | Consuming Shadow | Sombras devoradoras | Sombra devoradora | 貪る影 | 그림자 소모 | Sombra Devoradora | Ненасытная тень | —（原包缺键） |
| `powers/CONTRACTILITY_POWER.title` | 伸缩力 | Contractility | Contractilidad | Contráctil | 収縮性 | 신축성 | Contratilidade | Сокращение | —（原包缺键） |
| `powers/COOLANT_POWER.title` | 冷却剂 | Coolant | Refrigerante | Refrigeración | 冷却材 | 냉각재 | Resfriamento | Охлаждение | —（原包缺键） |
| `powers/CORROSIVE_WAVE_POWER.title` | 腐蚀波 | Corrosive Wave | Deshechos tóxicos | Ola corrosiva | 腐食の波 | 부식성 파도 | Onda Corrosiva | Коррозия | —（原包缺键） |
| `powers/CORRUPTION_POWER.title` | 腐化 | Corruption | Corrupción | Corrupción | 堕落 | 타락 | Corrupção | Порок | เปื้อนมลทิน |
| `powers/COUNTDOWN_POWER.title` | 倒数计时 | Countdown | Cuenta regresiva | Cuenta atrás | カウントダウン | 카운트다운 | Contagem Regressiva | Обратный отсчет | นับถอยหลัง |
| `powers/COVERED_POWER.title` | 掩护 | Covered | Cobertura | Protección | カバー | 엄호 | Cobertura | Прикрытие | ได้รับการคุ้มกัน |
| `powers/CRAB_RAGE_POWER.title` | 蟹之怒 | Crab Rage | Cólera crustácea | Cólera crustácea | カニの怒り | 꽃게의 격노 | Furor Crustáceo | Гнев краба | —（原包缺键） |
| `powers/CREATIVE_AI_POWER.title` | 创造性AI | Creative AI | IA creativa | IA creativa | クリエイティブAI | 창의적인 인공지능 | IA Criativa | Находчивый ИИ | ปัญญาประดิษฐ์สร้างสรรค์ |
| `powers/CRIMSON_MANTLE_POWER.title` | 绯红披风 | Crimson Mantle | Manto carmesí | Manto carmesí | 深紅の衣 | 핏빛 망토 | Manto Carmesim | Алая мантия | —（原包缺键） |
| `powers/CRUELTY_POWER.title` | 残酷 | Cruelty | Crueldad | Crueldad | 無慈悲 | 악랄함 | Crueldade | Жестокость | —（原包缺键） |
| `powers/CURIOUS_POWER.title` | 好奇 | Curious | Curiosidad | Curiosidad | 好奇心 | 호기심 | Curiosidade | Любопытство | —（原包缺键） |
| `powers/CURL_UP_POWER.title` | 蜷身 | Curl Up | Enroscamiento | Enroscarse | まるくなる | 몸 말기 | Enrolar-se | Клубок | —（原包缺键） |
| `powers/DAMPEN_POWER.title` | 抑制 | Dampen | Humedad | Desmotivación | 減衰 | 위축 | Reprimir | Подавление | —（原包缺键） |
| `powers/DANSE_MACABRE_POWER.title` | 死亡之舞 | Danse Macabre | Danza de la muerte | Danza macabra | 死の舞踏 | 죽음의 무도 | Dança Macabra | Пляска смерти | ระบำมรณะ |
| `powers/DARK_EMBRACE_POWER.title` | 黑暗之拥 | Dark Embrace | Abrazo oscuro | Abrazo oscuro | 闇の抱擁 | 어둠의 포옹 | Abraço Sombrio | Объятия тьмы | ความมืดโอบกอด |
| `powers/DEBILITATE_POWER.title` | 摧残 | Debilitate | Depauperar | Debilitar | 衰弱 | 쇠락 | Debilitar | Изнурение | —（原包缺键） |
| `powers/DECREE_OF_ENTROPY_POWER.title` | 熵之律令 | Decree of Entropy | Decreto de la entropía | Decreto de entropía | エントロピーの布告 | 예측 불허 판결 | Decreto de Entropia | Завет энтропии | —（原包缺键） |
| `powers/DECREE_OF_UNMAKING_POWER.title` | 亡之律令 | Decree of Unmaking | Decreto de destruccion | Decreto de descomposición | 破壊の布告 | 원상 복구 판결 | Decreto de Destruição | Завет небытия | —（原包缺键） |
| `powers/DEMESNE_POWER.title` | 领域 | Demesne | Feudo | Dominio | 領域 | 권역 | Domínio | Обитель | —（原包缺键） |
| `powers/DEMISE_POWER.title` | 消亡 | Demise | Deceso | Defunción | 崩御 | 종언 | Declínio | Обреченность | —（原包缺键） |
| `powers/DEMON_FORM_POWER.title` | 恶魔形态 | Demon Form | Forma demoníaca | Forma demoniaca | 悪魔化 | 악마의 형상 | Forma Demoníaca | Облик демона | ร่างปีศาจ |
| `powers/DESPAIR_POWER.title` | 绝望 | Despair | Desesperanza | Desesperación | 絶望 | 절망 | Desespero | Отчаяние | —（原包缺键） |
| `powers/DEVOUR_LIFE_POWER.title` | 吞噬生命 | Devour Life | Devoravidas | Apetito vital | 魂喰らい | 생명 삼키기 | Consumir Vidas | Пожиратель жизни | กลืนกินชีวิต |
| `powers/DEXTERITY_DOWN_POWER.title` | 敏捷下降 | Dexterity Down | Disminución de destreza | Menos destreza | 敏捷ダウン | 민첩 감소 | Redução de Destreza | Понижение ловкости | ความชำนาญลดลง |
| `powers/DEXTERITY_POWER.title` | 敏捷 | Dexterity | Destreza | Destreza | 敏捷 | 민첩 | Destreza | Ловкость | ความชำนาญ |
| `powers/DIE_FOR_YOU_POWER.title` | 为你而死 | Die for You | Moriría por ti | Morirá por ti | 身代わり | 살신성인 | Morrer por Você | Друзья до гроба | —（原包缺键） |
| `powers/DISINTEGRATION_POWER.title` | 瓦解 | Disintegration | Desintegración | Desintegración | 崩壊 | 분해 | Desintegração | Расщепление | —（原包缺键） |
| `powers/DOOM_POWER.title` | 灾厄 | Doom | Condena | Condena | 破滅 | 종말 | Ruína | Злой рок | อายุขัย |
| `powers/DOUBLE_DAMAGE_POWER.title` | 双倍伤害 | Double Damage | Daño doble | Daño doble | ダメージ2倍 | 2배의 피해 | Dano em Dobro | Двойной урон | ความเสียหาย 2 เท่า |
| `powers/DRAW_CARDS_NEXT_TURN_POWER.title` | 下回合抽牌 | Draw Cards Next Turn | Robacartas de próximo turno | Robo de cartas inminente | 次ターンドロー | 다음 턴 카드 뽑기 | Compra de Cartas Iminente | Отложенный добор | จั่วการ์ดเทิร์นถัดไป |
| `powers/DUPLICATION_POWER.title` | 复制 | Duplication | Duplicación | Duplicar | 複製 | 복사 | Duplicação | Удвоение | —（原包缺键） |
| `powers/ECHO_FORM_POWER.title` | 回响形态 | Echo Form | Forma resonante | Forma resonante | 反響化 | 메아리의 형상 | Forma Ecoante | Облик эхо | ร่างกังวาน |
| `powers/ENERGY_NEXT_TURN_POWER.title` | 下回合能量 | Energy Next Turn | Energía de próximo turno | Energía inminente | 次ターンエナジー | 다음 턴 에너지 | Energia Iminente | Отложенная энергия | พลังงานเทิร์นถัดไป |
| `powers/ENRAGE_POWER.title` | 激怒 | Enrage | Cólera | Ira | 激怒 | 격분 | Enfurecer | Бешенство | —（原包缺键） |
| `powers/ENTROPY_POWER.title` | 熵 | Entropy | Entropía | Entropía | エントロピー | 엔트로피 | Entropia | Энтропия | —（原包缺键） |
| `powers/ENVENOM_POWER.title` | 涂毒 | Envenom | Emponzoñar | Envenenar | 毒の仕込み | 독 바르기 | Envenenar | Отравление | อาบยาพิษ |
| `powers/ESCAPE_ARTIST_POWER.title` | 逃脱大师 | Escape Artist | Artista del escape | Escapismo | 脱出名人 | 탈출의 명수 | Escapista | Побег | —（原包缺键） |
| `powers/FAN_OF_KNIVES_POWER.title` | 刀扇 | Fan of Knives | Abanico de cuchillas | Abanico de cuchillas | ナイフの雨 | 칼날 부채 | Leque de Facas | Веер из ножей | พัดมีดบิน |
| `powers/FASTEN_POWER.title` | 勒紧 | Fasten | Ajustar | Atadura | 締め直し | 고정시키기 | Fixar | Предосторожность | รัดให้แน่น |
| `powers/FEEL_NO_PAIN_POWER.title` | 无惧疼痛 | Feel No Pain | Indoloro | No hay dolor | 無痛 | 무감각 | Resistir à Dor | Обезболивание | สภาพไร้เจ็บปวด |
| `powers/FERAL_POWER.title` | 野性 | Feral | Salvaje | Salvaje | 野性 | 야성 | Selvagem | Дикость | —（原包缺键） |
| `powers/FLAME_BARRIER_POWER.title` | 火焰屏障 | Flame Barrier | Muro flamígero | Barrera de llamas | 炎の障壁 | 화염 장벽 | Barreira Flamejante | Огненный барьер | กำแพงอัคคี |
| `powers/FLANKING_POWER.title` | 夹击 | Flanking | Flanquear | Flanquear | 挟撃 | 측면 공격 | Flanquear | Заход с фланга | โจมตีจากด้านข้าง |
| `powers/FLUTTER_POWER.title` | 振翅 | Flutter | Revoloteo | Aleteo | 羽ばたき | 날갯짓 | Bater Asas | Порхание | —（原包缺键） |
| `powers/FOCUS_DOWN.title` | 集中下降 | Focus Down | Desconcentración | Menos concentración | 集中力ダウン | 밀집 감소 | Foco Reduzido | Понижение фокуса | โฟกัสลดลง |
| `powers/FOCUS_POWER.title` | 集中 | Focus | Concentración | Concentración | 集中力 | 밀집 | Foco | Фокус | โฟกัส |
| `powers/FORBIDDEN_GRIMOIRE_POWER.title` | 禁忌魔典 | Forbidden Grimoire | Grimorio prohibido | Grimorio prohibido | 禁断の魔導書 | 금지된 마도서 | Grimório Proibido | Запретный гримуар | —（原包缺键） |
| `powers/FOREGONE_CONCLUSION_POWER.title` | 既定事项 | Foregone Conclusion | Resultado previsible | Conclusión inevitable | 確定事項 | 필연적인 결과 | Conclusão Inevitável | Предрешенный исход | —（原包缺键） |
| `powers/FRAIL_POWER.title` | 脆弱 | Frail | Fragilidad | Fragilidad | 脆弱 | 손상 | Frágil | Хрупкость | ความบอบบาง |
| `powers/FREE_ATTACK_POWER.title` | 免费攻击 | Free Attack | Ataque gratuito | Ataque gratuito | フリーアタック | 무료 공격 | Ataque sem Custo | Бесплатная Атака | โจมตีฟรี |
| `powers/FREE_POWER_POWER.title` | 免费能力 | Free Power | Poder gratuito | Poder gratuito | フリーパワー | 무료 파워 | Poder sem Custo | Бесплатный Талант | พลังฟรี |
| `powers/FREE_SKILL_POWER.title` | 免费技能 | Free Skill | Habilidad gratuita | Habilidad gratuita | フリースキル | 무료 스킬 | Técnica sem Custo | Бесплатный Навык | ทักษะฟรี |
| `powers/FRIENDSHIP_POWER.title` | 友谊 | Friendship | Amistad | Amistad | フレンドシップ | 우정 | Amizade | Вечная дружба | มิตรภาพ |
| `powers/FURNACE_POWER.title` | 熔炉 | Furnace | Fragua | Fragua | 溶鉱炉 | 용광로 | Fornalha | Печь | เตาเผา |
| `powers/GALVANIC_POWER.title` | 流电 | Galvanic | Galvanismo | Galvanismo | ガルバニック | 전류 | Galvanização | Гальванизация | กัดกร่อน |
| `powers/GENESIS_POWER.title` | 创世纪 | Genesis | Primigenia | Génesis | ジェネシス | 창세 | Gênesis | Бытие | —（原包缺键） |
| `powers/GIGANTIFICATION_POWER.title` | 超巨化 | Gigantification | Gigantificación | Gigantificación | 巨大化 | 거대화 | Gigantificação | Гигантизм | —（原包缺键） |
| `powers/GRABBED.title` | 抓取 | Grabbed | Atrapado | Hurto | 捕獲 | 붙잡힘 | Roubo | Воровство | —（原包缺键） |
| `powers/GRAVITY_POWER.title` | 引力 | Gravity | Gravedad | Gravedad | 重力 | 중력 | Gravidade | Гравитация | —（原包缺键） |
| `powers/GUARDED_POWER.title` | 护卫 | Guarded | Custodiado | A cubierto | 護衛 | 보호됨 | Protegido | Заступник | ได้รับการคุ้มกัน |
| `powers/HAILSTORM_POWER.title` | 冰雹风暴 | Hailstorm | Granizada | Granizada | ヘイルストーム | 우박 폭풍 | Chuva de Granizo | Град | —（原包缺键） |
| `powers/HAMMER_TIME_POWER.title` | 锤子时间 | Hammer Time | A trabajar | A martillazos | ハンマータイム | 망치질 시간 | Mãos à Obra | Кузница | —（原包缺键） |
| `powers/HANG_POWER.title` | 吊杀 | Hang | Cuelgue | Colgar | 絞首 | 매달기 | Enforcar | Повешение | —（原包缺键） |
| `powers/HARDENED_SHELL_POWER.title` | 硬化外壳 | Hardened Shell | Caparazón endurecido | Endurecimiento | 硬い殻 | 단단한 껍질 | Carapaça Endurecida | Прочный панцирь | —（原包缺键） |
| `powers/HARD_TO_KILL_POWER.title` | 难以杀灭 | Hard to Kill | Duro de matar | Duro de roer | 不死身 | 질긴 생존력 | Duro de Matar | Живучесть | —（原包缺键） |
| `powers/HATCH_POWER.title` | 孵化 | Hatch | Eclosión | Eclosión | ふ化 | 부화 | Eclodir | Птенец | ฟักไข่ |
| `powers/HAUNT_POWER.title` | 纠缠 | Haunt | Atormentar | Aparición | 憑依 | 출몰 | Assombrar | Терзания | —（原包缺键） |
| `powers/HEATSINKS_POWER.title` | 散热片 | Heatsink | Disipador de calor | Disipador | ヒートシンク | 방열판 | Dissipador | Теплоотвод | —（原包缺键） |
| `powers/HEIST_POWER.title` | 盗窃 | Heist | Robo | Atraco | 強奪 | 강도 | Roubo | Грабеж | —（原包缺键） |
| `powers/HELLO_WORLD_POWER.title` | 你好世界 | Hello World | ¡Hola, mundo! | Hola mundo | ハロー・ワールド | Hello World | Olá Mundo | Привет, мир | สวัสดีชาวโลก |
| `powers/HELLRAISER_POWER.title` | 地狱狂徒 | Hellraiser | Alborotador | Agitador | ヘル・レイザー | 지옥검무 | Instigador Infernal | Дебошир | ปลุกนรก |
| `powers/HEX_POWER.title` | 恶咒 | Hex | Maleficio | Maleficio | 呪術 | 주술 | Feitiço | Сглаз | —（原包缺键） |
| `powers/HIBERNATE_POWER.title` | 休眠 | Hibernate | Hibernación | Hibernar | 冬眠 | 동면 | Hibernar | Спячка | —（原包缺键） |
| `powers/HIGH_VOLTAGE_POWER.title` | 高电压 | High Voltage | Alto voltaje | Alto voltaje | 高電圧 | 고전압 | Alta Voltagem | Высокое напряжение | —（原包缺键） |
| `powers/ILLUSION_POWER.title` | 幻象 | Illusion | Ilusión | Ilusión | 幻影 | 환상 | Ilusão | Иллюзия | ภาพมายา |
| `powers/IMBALANCED_POWER.title` | 失衡 | Imbalanced | Desbalance | Desequilibrio | アンバランス | 불균형 | Desequilíbrio | Неповоротливость | —（原包缺键） |
| `powers/IMITATION_LEARNING_POWER.title` | 模仿学习 | Imitation Learning | Aprendizaje por imitación | Aprendizaje por imitación | 模倣学習 | 모방 학습 | Aprendizagem por Imitação | Имитационное обучение | —（原包缺键） |
| `powers/IMPROVEMENT_POWER.title` | 改善 | Improvement | Mejoramiento | Mejora | 改善 | 개선 | Melhoria | Развитие | การปรับปรุง |
| `powers/INFERNO_POWER.title` | 狱火 | Inferno | Infierno | Infierno | インフェルノ | 불바다 | Inferno | Кромешный ад | เพลิงโลกันตร์ |
| `powers/INFESTED_POWER.title` | 寄生物 | Infested | Infestación | Infestación | 寄生 | 감염됨 | Infestação | Заражение | —（原包缺键） |
| `powers/INFINITE_BLADES_POWER.title` | 无尽刀刃 | Infinite Blades | Cuchillas infinitas | Cuchillas infinitas | 無限の刃 | 무한의 검날 | Lâminas Infinitas | Бесконечные клинки | คมมีดไร้ขอบเขต |
| `powers/INKED.title` | 墨染 | Inked | Entintado | Tinta | 墨まみれ | 잉크투성이 | Nanquim | Метка | —（原包缺键） |
| `powers/INTANGIBLE_POWER.title` | 无实体 | Intangible | Intangibilidad | Intangible | 霊体 | 불가침 | Intangível | Неосязаемость | ไร้ตัวตน |
| `powers/INTERCEPT_POWER.title` | 拦截 | Intercept | Interceptación | Interceptar | インターセプト | 가로막기 | Interceptar | Перехват | สกัดกั้น |
| `powers/ITERATION_POWER.title` | 迭代 | Iteration | Iteración | Iteración | 反復 | 순회 | Iteração | Пробы и ошибки | —（原包缺键） |
| `powers/JUGGERNAUT_POWER.title` | 势不可当 | Juggernaut | Monstruo imparable | Coloso | ジャガーノート | 절대적인 힘 | Titã Couraçado | Махина | กำลังภายใน |
| `powers/JUGGLING_POWER.title` | 杂耍 | Juggling | Malabarismo | Malabares | ジャグリング | 저글링 | Malabarismo | Жонглер | —（原包缺键） |
| `powers/KNOCKDOWN_POWER.title` | 击倒 | Knockdown | Fulminante | Mate | ノックダウン | 때려눕히기 | Derrubada | Наповал | —（原包缺键） |
| `powers/LEADERSHIP_POWER.title` | 领袖气质 | Leadership | Liderazgo | Liderazgo | リーダーシップ | 통솔력 | Liderança | Руководство | —（原包缺键） |
| `powers/LETHALITY_POWER.title` | 致死性 | Lethality | Letalidad | Letalidad | 殺意 | 치사성 | Letalidade | Смертоубийство | —（原包缺键） |
| `powers/LIGHTNING_ROD_POWER.title` | 引雷针 | Lightning Rod | Pararrayos | Pararrayos | 避雷針 | 피뢰침 | Para-raios | Громоотвод | สายล่อฟ้า |
| `powers/LOOP_POWER.title` | 循环 | Loop | Bucle | Bucle | ループ | 반복 | Ciclo | Цикл | คำสั่งวนซ้ำ |
| `powers/MACHINE_LEARNING_POWER.title` | 机器学习 | Machine Learning | Autoaprendizaje | Aprendizaje automático | 機械学習 | 기계학습 | Sistema de Aprendizado | Машинное обучение | แมชชีนเลิร์นนิ่ง |
| `powers/MAGIC_BOMB_POWER.title` | 魔法炸弹 | Magic Bomb | Bomba mágica | Bomba mágica | 魔法の爆弾 | 마법 폭탄 | Bomba Mágica | Магическая бомба | —（原包缺键） |
| `powers/MASTER_PLANNER_POWER.title` | 谋划专家 | Master Planner | Siempre lista | Plan maestro | 稀代の策士 | 설계의 대가 | Planejamento de Mestre | Наготове | จอมวางแผน |
| `powers/MAYHEM_POWER.title` | 乱战 | Mayhem | Azar | La ruleta del caos | 騒乱 | 대혼란 | Roda da Fortuna | Беспредел | ความโกลาหล |
| `powers/MELANCHOLY_POWER.title` | 忧郁 | Melancholy | Melancolía | Melancolía | 憂鬱 | 비애 | Melancolia | Меланхолия | —（原包缺键） |
| `powers/MIND_ROT_POWER.title` | 心灵腐化 | Mind Rot | Podredumbre mental | Podredumbre mental | 精神腐敗 | 정신 오염 | Apodrecimento Mental | Загнивающий рассудок | —（原包缺键） |
| `powers/MINION_POWER.title` | 爪牙 | Minion | Esbirro | Esbirro | ミニオン | 하수인 | Lacaio | Прислужник | ลูกน้อง |
| `powers/MOCK_CLONE_CARDS_ON_PLAY_POWER.title` | Mock Clone Cards on Play | Mock Clone Cards on Play | Clonar cartas en juego (provisional) | Prueba Clona cartas al jugarse | テスト用クローンカード | 가짜 사용 시 카드 복제 | Clonar Cartas em Jogo (Teste) | Копирование карт при розыгрыше для слабаков | ทดสอบทำสำเนาการ์ดเมื่อเล่น |
| `powers/MOCK_DO_NOT_SCALE_IN_MULTIPLAYER_POWER.title` | Mock Do Not Scale in Multiplayer | Mock Do Not Scale in Multiplayer | No aumenta en cooperativo (provisional) | Mock Do Not Scale in Multiplayer | テスト用マルチプレイスケール停止 | 가짜 멀티플레이 스케일 미적용 | Mock Do Not Scale in Multiplayer | Mock Do Not Scale in Multiplayer | ทดสอบไม่สเกลในโหมดหลายคน |
| `powers/MOCK_EXTRA_TURN_POWER.title` | Mock Extra Turn | Mock Extra Turn | Turno extra (provisional) | Mock Extra Turn | テスト用追加ターン | 가짜 추가 턴 | Turno Extra Teste | Mock Extra Turn | —（原包缺键） |
| `powers/MOCK_FREE_CARDS_POWER.title` | Mock Free Cards | Mock Free Cards | Cartas sin costo (provisional) | Prueba Cartas gratis | テスト用フリーカード | 가짜 무료 카드 | Cartas sem Custo (Teste) | Бесплатные карты только в мышеловке. | ทดสอบการ์ดฟรี |
| `powers/MOCK_GAIN_BLOCK_ON_ATTACK_POWER.title` | Mock Gain Block on Attack | Mock Gain Block on Attack | Bloqueo al atacar (provisional) | Prueba Gana Bloqueo al usar un Ataque | テスト用アタックブロック | 가짜 공격 시 방어도 획득 | Proteção ao Atacar (Teste) | Получение защиты при атаках для слабаков | ทดสอบได้รับบล็อกตอนโจมตี |
| `powers/MOCK_INVINCIBLE_ON_DEATH_POWER.title` | Mock Invincible on Death | Mock Invincible on Death | Inmortalidad al morir (provisional) | Prueba Invencible al morir | テスト用死亡時無敵 | 가짜 죽음에 면역 | Invencibilidade ao Morrer (Teste) | Неуязвимость после смерти для слабаков | ทดสอบอมตะตอนตาย |
| `powers/MOCK_MODIFY_ENERGY_COST_POWER.title` | Mock Modify Energy Cost | Mock Modify Energy Cost | Modificar costo de energía (provisional) | Prueba Modificar coste de Energía | テスト用エナジーコスト変更 | 가짜 에너지 비용 조정 | Modificar Custo de Energia (Teste) | Изменение цены карт для слабаков | ทดสอบแก้ไขค่าร่ายพลังงาน |
| `powers/MOCK_MODIFY_STAR_COST_POWER.title` | Mock Modify Star Cost | Mock Modify Star Cost | Modificar costo de estrellas (provisional) | Prueba Modificar coste de estrellas | テスト用スターコスト変更 | 가짜 별 비용 조정 | Modificar Custo de Estrelas (Teste) | Изменение требуемых звезд для слабаков | ทดสอบแก้ไขค่าร่ายดาว |
| `powers/MOCK_PHASE_OBSERVER_POWER.title` | Mock Phase Observer | Mock Phase Observer | Fase de observación (provisional) | Mock Phase Observer | テスト用フェーズ監視 | 가짜 페이즈 관찰자 | Mock Phase Observer | Mock Phase Observer | ทดสอบ Phase Observer |
| `powers/MOCK_PREVENT_DEATH_POWER.title` | Mock Prevent Death | Mock Prevent Death | Prevenir la muerte (provisional) | Prueba prevenir muerte | テスト用死亡回避 | 가짜 죽음 방지 | Prevenir Morte (Teste) | Смерть для сильных | ทดสอบป้องกันการตาย |
| `powers/MOCK_REMOVE_DRAWN_CARDS_FROM_COMBAT_POWER.title` | Mock Remove Drawn Cards from Combat | Mock Remove Drawn Cards from Combat | Quita cartas del combate provisional | Mock Remove Drawn Cards from Combat | テスト用戦闘中に引いたカードを削除 | 가짜 전투 시 카드 뽑기 제거 | Teste Remover Cartas Compradas do Combate | Удаление добираемых карт из боя | ทดสอบกำจัดการ์ดที่จั่วจากการต่อสู้ |
| `powers/MOCK_RESET_COMBAT_ON_SHUFFLE_POWER.title` | Mock Reset Combat on Shuffle | Mock Reset Combat on Shuffle | Mock Reset Combat on Shuffle | Mock Reset Combat on Shuffle | テスト用シャッフル時戦闘リセット | 가짜 섞을 시 전투 초기화 | Mock Reset Combat on Shuffle | Тестовый сброс боя при замешивании | ทดสอบรีเซ็ตการต่อสู้ตอนสับ |
| `powers/MOCK_REVIVE_POWER.title` | Mock Revive | Mock Revive | Revivir (provisional) | Prueba revivir | テスト用復活 | 가짜 부활 | Reviver (Teste) | Воскрешение для слабаков | ทดสอบชุบชีวิต |
| `powers/MOCK_SCALE_IN_MULTIPLAYER_POWER.title` | Mock Scale in Multiplayer | Mock Scale in Multiplayer | Aumenta en cooperativo (provisional) | Mock Scale in Multiplayer | テスト用マルチプレイスケール | 가짜 멀티플레이 스케일 적용 | Mock Scale in Multiplayer | Mock Scale in Multiplayer | ทดสอบสเกลในโหมดหลายคน |
| `powers/MOCK_TEMPORARY_STRENGTH_LOSS.title` | Mock Temporary Strength Loss | Mock Temporary Strength Loss | Pérdida de fuerza temporal (provisional) | Prueba pérdida temporal de Fuerza | テスト用一時的筋力低下 | 가짜 일시적인 힘 손실 | Perda Temporária de Força (Teste) | Временная потеря силы для… Для слабаков, да | ทดสอบเสียความแข็งแรงชั่วคราว |
| `powers/MOCK_UNHITTABLE_POWER.title` | Mock Unhittable | Mock Unhittable | Inalcanzable (provisional) | Mock Unhittable | テスト用命中回避 | 가짜 적중 당하지 않음 | Mock Unhittable | Mock Unhittable | —（原包缺键） |
| `powers/MONARCHS_GAZE_POWER.title` | 王之凝视 | Monarch's Gaze | Mirada de monarca | Mirada del monarca | 君主の睨み | 군주의 시선 | Olhar do Monarca | Королевский нрав | —（原包缺键） |
| `powers/MONOLOGUE_POWER.title` | 独白 | Monologue | Monólogo | Soliloquio | モノローグ | 독백 | Monólogo | Монолог | —（原包缺键） |
| `powers/NECRO_MASTERY_POWER.title` | 亡灵精通 | Necro Mastery | Nigromaestría | Maestría nigromántica | ネクロマスタリー | 강령의 극의 | Necromaestria | Высшая некромантия | —（原包缺键） |
| `powers/NEMESIS_POWER.title` | 天罚 | Nemesis | Némesis | Némesis | ネメシス | 네메시스 | Nêmesis | Немезис | —（原包缺键） |
| `powers/NEUROSURGE_POWER.title` | 精神过载 | Neurosurge | Sobrecarga neural | Sobrecarga neuronal | ニューロサージ | 정신 폭주 | Sobrecarga Neural | Мозговой штурм | —（原包缺键） |
| `powers/NIGHTMARE_POWER.title` | 夜魇 | Nightmare | Pesadilla | Pesadilla | 悪夢 | 악몽 | Pesadelo | Кошмар | ฝันร้าย |
| `powers/NOSTALGIA_POWER.title` | 怀旧 | Nostalgia | Añoranza | Nostalgia | 郷愁 | 향수 | Nostalgia | Ностальгия | —（原包缺键） |
| `powers/NOXIOUS_FUMES_POWER.title` | 毒雾 | Noxious Fumes | Vapores nocivos | Gases tóxicos | 有毒ガス | 유독 가스 | Gases Tóxicos | Едкие испарения | รมควันพิษ |
| `powers/NO_BLOCK_POWER.title` | 不可格挡 | No Block | Sin bloqueos | Bloqueo anulado | ノーブロック | 방어 불가 | Sem Proteção | Без защиты | ไม่ได้บล็อก |
| `powers/NO_DRAW_POWER.title` | 不可抽牌 | No Draw | No robarás | Robar anulado | ノードロー | 뽑기 불가 | Sem Compra | Без добора | ห้ามจั่วการ์ด |
| `powers/NO_ENERGY_GAIN_POWER.title` | 无法获得能量 | No Energy Gain | Sin energía | Energía agotada | エナジー獲得不可 | 에너지 획득 불가 | Sem Energia Adicional | Предел энергии | ไม่ได้รับพลังงาน |
| `powers/NO_ESCAPE_POWER.title` | 无处可逃 | No Escape | No escaparás | Sin escapatoria | 逃げ場なし | 도망칠 수 없다 | Sem Escapatória | Замкнутый круг | ไร้ทางหนี |
| `powers/OBLIVION_POWER.title` | 湮灭 | Oblivion | Olvido | Olvido | オブリビオン | 망각 | Oblívio | Забвение | —（原包缺键） |
| `powers/ONE_FOR_ALL_POWER.title` | 一心化万 | One for All | Uno para todos | Uno para todos | ワン・フォー・オール | 모두를 위한 하나 | Um por Todos | Один за всех | หนึ่งเดียวสู่ทั้งปวง |
| `powers/ONE_TWO_PUNCH_POWER.title` | 连环拳 | One-Two Punch | Combo uno, dos. | Puñetazo doble | ワン・ツーパンチ | 원투 펀치 | Soco Duplo | Комбо | —（原包缺键） |
| `powers/ORBIT_POWER.title` | 环绕轨道 | Orbit | Órbita | Órbita | オービット | 궤도 | Órbita | Орбита | —（原包缺键） |
| `powers/ORB_GENERATOR.title` | 充能球生成器 | Orb Generator | Generador de orbes | Generador de Orbes | オーブジェネレーター | 구체 생성기 | Gerador de Orbes | Генератор сфер | —（原包缺键） |
| `powers/PAGESTORM_POWER.title` | 书页风暴 | Pagestorm | Tormentágina | Papeleo infernal | 紙の嵐 | 서류 폭풍 | Dilúvio de Páginas | Переплет | —（原包缺键） |
| `powers/PAINFUL_STABS_POWER.title` | 疼痛戳刺 | Painful Stabs | Puñalada dolorosa | Puñaladas dolorosas | 苦痛の一刺し | 고통스러운 찌르기 | Punhaladas Dolorosas | Болезненные порезы | —（原包缺键） |
| `powers/PALE_BLUE_DOT_POWER.title` | 暗淡蓝点 | Pale Blue Dot | Punto azul pálido | Un punto azul pálido | ペイル・ブルー・ドット | 창백한 푸른 점 | Pálido Ponto Azul | Земля в иллюминаторе | จุดสีน้ำเงินซีด |
| `powers/PANACHE_POWER.title` | 神气制胜 | Panache | Garbo | Estilo | 威風堂々 | 위풍당당 | Garbo | Искрометность | วางท่าอย่างเท่ |
| `powers/PAPER_CUTS_POWER.title` | 纸伤难愈 | Paper Cuts | Cortes de papel | Cortes de papel | 紙傷 | 종이 베기 | Cortes de Papel | Бумажные порезы | —（原包缺键） |
| `powers/PARRY_POWER.title` | 招架 | Parry | Desvío | Parada | パリィ | 쳐내기 | Aparar | Парирование | ปัดป้อง |
| `powers/PERSONAL_HIVE_POWER.title` | 人体蜂房 | Personal Hive | Colmena personal | Colmena personal | パーソナルハイヴ | 개인 군락 | Colmeia Pessoal | Личный улей | —（原包缺键） |
| `powers/PHANTOM_BLADES_POWER.title` | 幻影之刃 | Phantom Blades | Cuchillas fantasma | Hojas fantasmales | ファントムブレード | 환영검 | Lâminas Espectrais | Призрачные клинки | —（原包缺键） |
| `powers/PILLAR_OF_CREATION_POWER.title` | 创世之柱 | Pillar of Creation | Pilar de la creación | Pilar de la Creación | 創造の柱 | 창조의 기둥 | Pilar da Criação | Основа мироздания | —（原包缺键） |
| `powers/PLATING_POWER.title` | 覆甲 | Plating | Revestimiento | Blindaje | プレート | 판금 | Blindagem | Панцирь | เกราะโลหะ |
| `powers/PLOW_POWER.title` | 横冲直撞 | Plow | Colapso | Ceremonia | すき込み | 갈아엎기 | Quebra | Крах | —（原包缺键） |
| `powers/POISON_POWER.title` | 中毒 | Poison | Veneno | Veneno | 毒 | 중독 | Veneno | Яд | พิษ |
| `powers/POSSESS_SPEED_POWER.title` | 抢夺速度 | Possess Speed | Eres veloz | Robo de destreza | 敏捷泥棒 | 속도 지배 | Possessão de Velocidade | Краденая ловкость | —（原包缺键） |
| `powers/POSSESS_STRENGTH_POWER.title` | 抢夺力量 | Possess Strength | Eres fuerte | Robo de fuerza | 筋力泥棒 | 힘 지배 | Possessão de Força | Похищенная сила | —（原包缺键） |
| `powers/PREP_TIME_POWER.title` | 准备时间 | Prep Time | Hora de prepararse | Preparativos | ウォームアップ | 준비 시간 | Preparativos | Подготовка | ช่วงเตรียมการ |
| `powers/PYRE_POWER.title` | 薪火之源 | Pyre | Pira funeraria | Pira | かがり火 | 불의 심장 | Pira | Костер | —（原包缺键） |
| `powers/RADIANCE_POWER.title` | 明耀 | Radiance | Resplandor | Resplandor | 輝き | 광휘 | Resplendor | Теплота | —（原包缺键） |
| `powers/RAGE_POWER.title` | 狂怒 | Rage | Furia | Furia | 激怒 | 격노 | Fúria | Ярость | บันดาลโทสะ |
| `powers/RAMPART_POWER.title` | 盾墙 | Rampart | Bastión | Bastión | 城壁 | 방벽 | Muralha | Заслон | —（原包缺键） |
| `powers/RAVENOUS_POWER.title` | 饥饿 | Ravenous | Voracidad | Hambre voraz | 貪欲 | 굶주림 | Voraz | Обжора | —（原包缺键） |
| `powers/READ_THE_BONES_POWER.title` | 识骨知数 | Read the Bones | Leehuesos | Leer los huesos | 骨占い | 뼈 읽기 | Leitura de Ossos | Гадание на костях | —（原包缺键） |
| `powers/REAPER_FORM_POWER.title` | 死神形态 | Reaper Form | Forma segadora | Forma de Segadora | 死神化 | 사신의 형상 | Forma Ceifadora | Облик жнеца | ร่างยมทูต |
| `powers/REATTACH_POWER.title` | 接续 | Reattach | Refragmentación | Reconexión | 再結合 | 재부착 | Reconectar | Восстановление | —（原包缺键） |
| `powers/REBOUND_POWER.title` | 弹回 | Rebound | Rebote | Rebote | リバウンド | 되돌리기 | Rebote | Откат | ดีดกลับ |
| `powers/REFLECTIVE_FORTRESS_POWER.title` | 逆反要塞 | Reflective Fortress | Fortaleza reflectiva | Fortaleza reflectiva | 反射の要塞 | 반사하는 요새 | Fortaleza Refletiva | Отражающий купол | —（原包缺键） |
| `powers/REFLECT_POWER.title` | 倒映 | Reflect | Reflejo | Reflejo | 反射 | 반사 | Refletir | Отражение | —（原包缺键） |
| `powers/REGEN_POWER.title` | 再生 | Regen | Regeneración | Regeneración | 再生 | 재생 | Regeneração | Регенерация | การฟื้นพลัง |
| `powers/RESERVES_POWER.title` | 储备 | Reserves | Reservas | Reservas | リザーブ | 비축물 | Reservas | Запас | —（原包缺键） |
| `powers/RESTORE_DEXTERITY.title` | 敏捷恢复 | Restore Dexterity | Recuperadestrezas | Restaurar destreza | 敏捷回復 | 민첩 회복 | Restaurar Destreza | Восполнение ловкости | —（原包缺键） |
| `powers/RESTORE_FOCUS.title` | 集中恢复 | Restore Focus | Recuperaconcentración | Restaurar concentración | 集中力回復 | 밀집 회복 | Restaurar Foco | Восполнение фокуса | —（原包缺键） |
| `powers/RETAIN_HAND_POWER.title` | 保留手牌 | Retain Hand | Retención de mano | Retener mano | 手札保留 | 손패 보존 | Manter Mão | Постоянство | คงสภาพทั้งมือ |
| `powers/RINGING_POWER.title` | 昏眩 | Ringing | Campanadas | Tañido | 耳鳴り | 공명 | Ressonante | Звон в ушах | หูดับ |
| `powers/RITUAL_POWER.title` | 仪式 | Ritual | Ritual | Ritual | 儀式 | 의식 | Ritual | Ритуал | พิธีกรรม |
| `powers/ROLLING_BOULDER_POWER.title` | 滚石 | Rolling Boulder | Piedra rodante | Canto rodado | 大岩転がし | 굴러가는 바위 | Rocha Rolante | Каменная лавина | หินกลิ้ง |
| `powers/ROYALTIES_POWER.title` | 王国资产 | Royalties | Regalías | Diezmo | ロイヤリティ | 로열티 | Impostos | Дань | ค่าภาคหลวง |
| `powers/RUPTURE_POWER.title` | 撕裂 | Rupture | Ruptura | Ruptura | 血の沸騰 | 파열 | Ruptura | Надрыв | กายาแตกร้าว |
| `powers/SANDPIT_POWER.title` | 沙坑 | Sandpit | Arenas movedizas | Arenas movedizas | 蟻地獄 | 모래 구덩이 | Fosso Arenoso | Зыбучие пески | บ่อทราย |
| `powers/SEEKING_EDGE_POWER.title` | 追踪之刃 | Seeking Edge | Filo buscador | Filo rastreador | 追尾の刃 | 날 세우기 | Lâmina Perseguidora | Жаждущий клинок | —（原包缺键） |
| `powers/SELF_FORMING_CLAY_POWER.title` | 自成型黏土 | Self-Forming Clay | Arcilla automoldeable | Arcilla autoforme | 自己形成粘土 | 자가 형성 점토 | Argila Automoldante | Сам себе скульптор | ดินเหนียวแปรสภาพ |
| `powers/SENTRY_MODE_POWER.title` | 哨卫模式 | Sentry Mode | Modo centinela | Modo centinela | 監視モード | 경계 태세 | Modo Sentinela | Дозор | —（原包缺键） |
| `powers/SERPENT_FORM_POWER.title` | 群蛇形态 | Serpent Form | Forma viperina | Forma ofidia | 大蛇化 | 구렁이의 형상 | Forma Ofídica | Облик змеи | ร่างอสรพิษ |
| `powers/SHADOWMELD_POWER.title` | 融入暗影 | Shadowmeld | Fusión de sombras | Fundirse con las sombras | 影との融合 | 그림자 은신 | Mesclar às Sombras | Бой с тенью | —（原包缺键） |
| `powers/SHADOW_STEP_POWER.title` | 暗影步 | Shadow Step | Paso sombrío | Paso sombrío | シャドウステップ | 그림자 걸음 | Passo Sombrio | Шаг в тень | —（原包缺键） |
| `powers/SHARP_EDGE_POWER.title` | 锋利边缘 | Sharp Edge | Bordes filosos | Afilado perfecto | 鋭利な刃 | 예리한 날 | Ponta Afiada | Острые заточки | —（原包缺键） |
| `powers/SHRIEK_POWER.title` | 尖叫 | Shriek | Grito | Graznido | 悲鳴 | 괴성 | Grunhido | Болезненный визг | —（原包缺键） |
| `powers/SHRINK_POWER.title` | 缩小 | Shrink | Encogimiento | Encoger | 縮小 | 압축 | Encolhimento | Уменьшение | —（原包缺键） |
| `powers/SHROUD_POWER.title` | 厄运之衣 | Shroud | Velo protector | Mortaja | 影の衣 | 수의 | Mortalha | Сумрак | —（原包缺键） |
| `powers/SIC_EM_POWER.title` | 紧追不放 | Sic 'Em | ¡Ataca! | ¡Ataca! | やっちゃえ | 덮쳐! | Avance! | Взять | —（原包缺键） |
| `powers/SIGNAL_BOOST_POWER.title` | 信号增强 | Signal Boost | Amplificaseñales | Potenciador de señal | 信号増幅 | 신호 증폭 | Amplificar Sinal | Усиленный сигнал | —（原包缺键） |
| `powers/SKITTISH_POWER.title` | 胆小 | Skittish | Timidez | Timidez | 臆病 | 겁쟁이 | Arisco | Пугливость | —（原包缺键） |
| `powers/SLEIGHT_OF_FLESH_POWER.title` | 血肉戏法 | Sleight of Flesh | Libra de carne | Perturbación corporal | 呪肉の奇術 | 살점 재주 | Manipulação Corpórea | Податливая плоть | —（原包缺键） |
| `powers/SLIPPERY_POWER.title` | 滑溜 | Slippery | Escurridizo | Escurridizo | スリップ | 미끈거림 | Escorregadio | Изворотливость | —（原包缺键） |
| `powers/SLOTH_POWER.title` | 懒惰 | Sloth | Pereza | Flojera | 怠惰 | 나태 | Preguiça | Лень | ความเกียจคร้าน |
| `powers/SLOW_POWER.title` | 缓慢 | Slow | Lentitud | Lentitud | スロウ | 둔화 | Lentidão | Медлительность | —（原包缺键） |
| `powers/SLUMBER_POWER.title` | 熟睡 | Slumber | Somnolencia | Letargo | まどろみ | 숙면 | Letargia | Дремота | —（原包缺键） |
| `powers/SMOGGY_POWER.title` | 烟雾弥漫 | Smoggy | Contaminado | Polución | スモッグ | 연무 | Poluição | Смог | —（原包缺键） |
| `powers/SMOKESTACK_POWER.title` | 烟囱 | Smokestack | Chimeneas | Chimenea | 煙突 | 배기 장치 | Chaminé | Дымоход | —（原包缺键） |
| `powers/SNEAKY_POWER.title` | 鬼祟 | Sneaky | Furtiva | Sigilo | 隠密 | 비열함 | Furtividade | Скрытность | —（原包缺键） |
| `powers/SOAR_POWER.title` | 翱翔 | Soar | Elevación | Planear | 飛翔 | 날아오르기 | Planar | Крылья | —（原包缺键） |
| `powers/SOULBOUND_POWER.title` | 灵魂绑定 | Soulbound | Ligamiento anímico | Vínculo anímico | ソウルバウンド | 영혼 결속 | Vínculo de Alma | Связь душ | ผูกวิญญาณ |
| `powers/SOUL_WITHER.title` | 灵魂枯萎 | Soul Wither | Marchitaalmas | Marchitar el alma | 魂の枯渇 | 시드는 영혼 | Esmorecer Alma | Увядание души | —（原包缺键） |
| `powers/SOW_POWER.title` | 播种 | Sow | Siembra | Cosecha | 種まき | 씨뿌리기 | Semear | Посев | —（原包缺键） |
| `powers/SPECTRUM_SHIFT_POWER.title` | 光谱偏移 | Spectrum Shift | Escala cromática | Cambio de espectro | スペクトラムシフト | 스펙트럼 이동 | Desvio de Espectro | Спектральный сдвиг | เลื่อนสเปกตรัม |
| `powers/SPEEDSTER_POWER.title` | 速行者 | Speedster | Velocista | Velocista | スピードスター | 스피드스터 | Velocista | Гонка | ผู้ใช้ความเร็ว |
| `powers/SPINNER_POWER.title` | 旋转工艺 | Spinner | Spinner | Spinner | スピナー | 스피너 | Vitro-Fiandeiro | Спиннер | —（原包缺键） |
| `powers/SPIRIT_OF_ASH_POWER.title` | 灰烬之灵 | Spirit of Ash | Espíritu acenizado | Espíritu de ceniza | 灰の精霊 | 잿빛 혼령 | Espírito das Cinzas | Дух пепла | —（原包缺键） |
| `powers/STAMPEDE_POWER.title` | 惊逃 | Stampede | Estampida | Estampida | 暴走 | 쇄도 | Estouro | Натиск | —（原包缺键） |
| `powers/STAR_NEXT_TURN_POWER.title` | 下回合辉星 | Star Next Turn | Próximo turno estrellado | Estrella inminente | 次ターンスター | 다음 턴 별 | Estrela Iminente | Отложенная звезда | ดาวเทิร์นถัดไป |
| `powers/STEAM_ERUPTION_POWER.title` | 蒸汽喷发 | Steam Eruption | Erupción de vapor | Erupción de vapor | 蒸気噴出 | 증기 분출 | Erupção de Vapor | Извержение пара | —（原包缺键） |
| `powers/STOCK_POWER.title` | 库存 | Stock | Almacenamiento | Refuerzos | ストック | 재고 | Reforços | Массовое производство | —（原包缺键） |
| `powers/STORM_POWER.title` | 雷暴 | Storm | Tormenta | Tormenta | ストーム | 폭풍 | Tormenta | Шторм | มรสุม |
| `powers/STRANGLE_POWER.title` | 紧勒 | Strangle | Estrangulación | Estrangular | 絞殺 | 목 조르기 | Estrangular | Асфиксия | —（原包缺键） |
| `powers/STRATAGEM_POWER.title` | 计策 | Stratagem | Estratagema | Estratagema | 計略 | 책략 | Estratagema | Стратагема | —（原包缺键） |
| `powers/STRENGTH_POWER.title` | 力量 | Strength | Fuerza | Fuerza | 筋力 | 힘 | Força | Сила | ความแข็งแรง |
| `powers/SUBROUTINE_POWER.title` | 子程序 | Subroutine | Subrutina | Subrutina | サブルーチン | 서브루틴 | Sub-rotina | Вторичный процесс | —（原包缺键） |
| `powers/SUCK_POWER.title` | 吮吸 | Suck | Absorción | Succión | 吸血 | 흡입 | Sucção | Кровосос | —（原包缺键） |
| `powers/SUMMON_NEXT_TURN_POWER.title` | 下回合召唤 | Summon Next Turn | Invocación de próximo turno | Vinculación inminente | 次ターン召喚 | 다음 턴 소환 | Vinculação Iminente | Отложенный призыв | อัญเชิญเทิร์นถัดไป |
| `powers/SURPRISE_POWER.title` | 意外 | Surprise | Sorpresa | Sorpresa | サプライズ | 서프라이즈 | Surpresa | Сюрприз | เซอร์ไพรส์ |
| `powers/SURROUNDED_POWER.title` | 遭到包围 | Surrounded | Rodeado | Vigila tu espalda | 包囲 | 포위됨 | Sob Cerco | В окружении | ถูกล้อม |
| `powers/SWIPE_POWER.title` | 顺走 | Swipe | Robo | Mangar | 盗む | 슬쩍하기 | Furto | Карманник | —（原包缺键） |
| `powers/SWORD_SAGE_POWER.title` | 剑圣 | Sword Sage | Espadachín sabio | Maestro espadachín | 剣聖 | 검성 | Mestre Espadachim | Мастер меча | —（原包缺键） |
| `powers/TAG_TEAM_POWER.title` | 多人组队 | Tag Team | Ataque en equipo | Maniobra coordinada | タッグチーム | 태그 팀 | Ataque Combinado | Командная работа | —（原包缺键） |
| `powers/TAINTED_POWER.title` | 污染 | Tainted | Corrupción | Impureza | 汚染 | 훼손됨 | Contaminação | Порча | ปนเปื้อน |
| `powers/TANGLED_POWER.title` | 缠结 | Tangled | Enredo | Enredo | もつれ | 뒤얽힘 | Emaranhado | Путы | —（原包缺键） |
| `powers/TANK_POWER.title` | 肉盾 | Tank | Tanque | Tanque | タンク | 탱커 | Tanque | Танк | —（原包缺键） |
| `powers/TAUNT_POWER.title` | 挑衅 | Taunt | Provocación | Provocar | 挑発 | 도발 | Provocar | Насмешка | —（原包缺键） |
| `powers/TENDER_POWER.title` | 柔嫩 | Tender | Doloroso | Sensible | 軟化 | 연약함 | Amaciar | Мягкотелость | —（原包缺键） |
| `powers/TERRITORIAL_POWER.title` | 领地意识 | Territorial | Territorial | Territorial | 縄張り意識 | 영역 동물 | Territorial | Хранитель очага | —（原包缺键） |
| `powers/TEZCATARAS_BLIGHT.title` | 特兹卡塔拉的荒疫 | Tezcataras Blight | Plaga de Tezcatara | Aflicción de Tezcatara | テスカタラの災い | 테즈카타라의 역병 | Corrupção de Tezcatara | Порча Тецкатары | —（原包缺键） |
| `powers/THE_BOMB_POWER.title` | 炸弹 | The Bomb | La bomba | La bomba | 爆弾 | 폭탄 | A Bomba | Бомба | —（原包缺键） |
| `powers/THE_GAMBIT_POWER.title` | 孤注一掷 | The Gambit | Gambito | Jugada arriesgada | 背水の陣 | 포석 | Jogada Arriscada | Игра стоит свеч | เดิมพันชะตา |
| `powers/THE_HUNT_POWER.title` | 狩猎 | The Hunt | La cazadora | La Cacería | 狩猟 | 사냥 | A Caçada | Великая охота | —（原包缺键） |
| `powers/THE_SEALED_THRONE_POWER.title` | 封印王座 | The Sealed Throne | Trono sellado | El Trono Sellado | 封印されし玉座 | 봉인된 왕좌 | O Trono Selado | Запечатанный трон | —（原包缺键） |
| `powers/THIEVERY_POWER.title` | 偷窃 | Thievery | Hurto | Desvalijar | 泥棒 | 도둑질 | Mãos Leves | Воровство | —（原包缺键） |
| `powers/THORNS_POWER.title` | 荆棘 | Thorns | Espinas | Espinas | トゲ | 가시 | Espinhos | Шипы | หนาม |
| `powers/THUNDER_POWER.title` | 雷霆 | Thunder | Trueno | Trueno | サンダー | 벼락 | Trovão | Гром | ฟ้าคะนอง |
| `powers/TOOLS_OF_THE_TRADE_POWER.title` | 必备工具 | Tools of the Trade | Herramientas del oficio | Útiles del oficio | 商売道具 | 작업 도구 | Ferramentas do Ofício | Знаток дела | สรรพอุปกรณ์ |
| `powers/TORIC_TOUGHNESS_POWER.title` | 坚韧之环 | Toric Toughness | Aspereza toroide | Resistencia tórica | 円環の障壁 | 고리형 강인함 | Resistência Tórica | Торическая выдержка | —（原包缺键） |
| `powers/TORN.title` | 破裂 | Torn | Desgarrar | Desgarro | 裂傷 | 찢겨나감 | Desgaste | Рассеянность | —（原包缺键） |
| `powers/TRACKING_POWER.title` | 跟踪 | Tracking | Persecución | Rastrear | 追跡 | 추적 | Rastrear | Следопыт | —（原包缺键） |
| `powers/TRASH_TO_TREASURE_POWER.title` | 化废为宝 | Trash to Treasure | De basura a tesoro | Reutilización | 災い転じて | 고철을 보물로 | Transmutar Sucata | Алмаз среди камней | —（原包缺键） |
| `powers/TYRANNY_POWER.title` | 暴政 | Tyranny | Tiranía | Tiranía | 専制 | 독재 | Tirania | Тирания | —（原包缺键） |
| `powers/UNDERWORLD_POWER.title` | 幽冥之界 | Underworld | Inframundo | Inframundo | 冥界 | 지하세계 | Submundo | Царство мертвых | —（原包缺键） |
| `powers/UNMOVABLE_POWER.title` | 不动 | Unmovable | Inamovible | Inamovible | 盤石 | 요지부동 | Inabalável | Непоколебимость | —（原包缺键） |
| `powers/UNSTABLE.title` | 不稳定 | Unstable | Inestable | Inestable | 不安定 | 불안정 | Instável | Нестабильность | —（原包缺键） |
| `powers/UNSTEADY.title` | 脚下不稳 | Unsteady | Tambaleante | Tambaleo | ふらつき | 휘청거림 | Equilíbrio Precário | Шаткость | —（原包缺键） |
| `powers/VEILPIERCER_POWER.title` | 刺破帷幕 | Veilpiercer | Perforavelos | Perforavelos | ベールピアサー | 장막 관통자 | Perfura-Véu | Разоблачение | —（原包缺键） |
| `powers/VICIOUS_POWER.title` | 凶恶 | Vicious | Despiadado | Despiadado | 残忍 | 포악함 | Brutalidade | Безжалостность | —（原包缺键） |
| `powers/VIGOR_POWER.title` | 活力 | Vigor | Vigor | Vigor | 活力 | 활력 | Vigor | Бодрость | ความกำยำ |
| `powers/VISUAL_ONLY.title` | 仅视觉 | Visual Only | Ref. visual | Solo visual | ビジュアルのみ | 비주얼 온리 | Ref. Visual | Визуал | ภาพเท่านั้น |
| `powers/VITAL_SPARK_POWER.title` | 活力火花 | Vital Spark | Chispa suprema | Chispa vital | 生命の火花 | 생명의 불꽃 | Centelha Vital | Живительная искра | —（原包缺键） |
| `powers/VOID_FORM_POWER.title` | 虚空形态 | Void Form | Forma vacía | Forma del Vacío | 虚無化 | 공허의 형상 | Forma do Vazio | Облик бездны | ร่างสุญญตา |
| `powers/VULNERABLE_POWER.title` | 易伤 | Vulnerable | Vulnerabilidad | Vulnerabilidad | 弱体 | 취약 | Vulnerável | Уязвимость | จุดอ่อน |
| `powers/WASTE_AWAY_POWER.title` | 衰朽 | Waste Away | Atrofiarse | Consumirse | 虚脱 | 쇠퇴 | Esmorecer | Небытие | —（原包缺键） |
| `powers/WEAK_POWER.title` | 虚弱 | Weak | Debilitamiento | Debilidad | 脱力 | 약화 | Fraqueza | Слабость | ความอ่อนแอ |
| `powers/WELL_LAID_PLANS_POWER.title` | 计划妥当 | Well-Laid Plans | Planes bien pensados | Planificar | 用意周到 | 괜찮은 전략 | Planos Bem Feitos | Все по плану | แผนการอย่างดี |
| `powers/WINGED_JUMP_POWER.title` | 翱翔之跃 | Winged Jump | Salto alado | Salto alado | 翼の跳躍 | 비행 점프 | Salto Alado | Крылатый прыжок | —（原包缺键） |
| `powers/WITHERING_PRESENCE_POWER.title` | 凋萎存在 | Withering Presence | Presencia marchitante | Presencia debilitante | 衰微の予兆 | 시들어가는 존재 | Presença Definhadora | Изнуряющее присутствие | —（原包缺键） |
| `powers/WRAITH_FORM_POWER.title` | 幽魂形态 | Wraith Form | Forma espectral | Forma espectral | 死霊化 | 유령의 형상 | Forma Espectral | Облик призрака | ร่างมายา |

## 升级符文引用的原版卡名

| 资源键 | zhs | eng | esp | spa | jpn | kor | ptb | rus | tha |
|---|---|---|---|---|---|---|---|---|---|
| `cards/AUTOMATION.title` | 自动化 | Automation | Automatización | Automatización | オートメーション | 자동화 | Automação | Автоматизация | กลไกอัตโนมัติ |
| `cards/BASH.title` | 痛击 | Bash | Mazazo | Porrazo | 強打 | 강타 | Esmagar | Вмятина | ทุบตี |
| `cards/BATTLE_TRANCE.title` | 战斗专注 | Battle Trance | Trance de batalla | Trance de batalla | バトルトランス | 전투 최면 | Êxtase de Batalha | Боевой транс | หลงใหลการศึก |
| `cards/BLOODLETTING.title` | 放血 | Bloodletting | Baño de sangre | Flebotomía | 瀉血 | 사혈 | Sangria | Кровопускание | กรีดเลือด |
| `cards/BODYGUARD.title` | 护卫 | Bodyguard | Guardaespaldas | Guardaespaldas | ボディガード | 호위 | Guarda-costas | Телохранитель | องครักษ์ |
| `cards/BODY_SLAM.title` | 全身撞击 | Body Slam | Placaje | Embestida | ボディスラム | 몸통 박치기 | Investida | Толчок корпусом | พุ่งเข้าชน |
| `cards/BONE_SHARDS.title` | 碎骨 | Bone Shards | Fragmentos óseos | Metralla ósea | 骨の破片 | 뼛조각 | Fragmentos Ósseos | Костяные осколки | เศษกระดูก |
| `cards/BORROWED_TIME.title` | 预借时间 | Borrowed Time | Tiempo prestado | Tiempo prestado | 借り物の時間 | 연명 | Tempo Emprestado | Сверхурочные | กู้ยืมเวลา |
| `cards/BRAND.title` | 烙印 | Brand | Marca | Marcar | 焼印 | 낙인 | Marcar | Клеймо | —（原包缺键） |
| `cards/BULLET_TIME.title` | 子弹时间 | Bullet Time | Tiempo bala | Tiempo bala | バレットタイム | 불릿 타임 | Tempo Dilatado | Замедление | วินาทีหลบกระสุน |
| `cards/CLAW.title` | 爪击 | Claw | Garras | Tajo | 爪 | 후벼 파기 | Arranhar | Коготь | ข่วน |
| `cards/COMPACT.title` | 压缩 | Compact | Compactar | Comprimir | 圧縮 | 압축 | Comprimir | Утрамбовка | —（原包缺键） |
| `cards/CORROSIVE_WAVE.title` | 腐蚀波 | Corrosive Wave | Deshechos tóxicos | Ola corrosiva | 腐食の波 | 부식성 파도 | Onda Corrosiva | Коррозия | —（原包缺键） |
| `cards/CRASH_LANDING.title` | 迫降 | Crash Landing | Aterrizaje forzoso | Aterrizaje forzoso | 不時着 | 불시착 | Pouso Forçado | Жесткая посадка | —（原包缺键） |
| `cards/CREATIVE_AI.title` | 创造性AI | Creative AI | IA creativa | IA creativa | クリエイティブAI | 창의적인 인공지능 | IA Criativa | Находчивый ИИ | ปัญญาประดิษฐ์สร้างสรรค์ |
| `cards/DECISIONS_DECISIONS.title` | 抉择，抉择 | Decisions, Decisions | Decisiones, decisiones | Difícil elección | 悩ましい選択 | 어려운 결정 | Decisões, Decisões | Сложный выбор | คิดแล้วคิดอีก |
| `cards/DIRGE.title` | 挽歌 | Dirge | Réquiem | Melodía funesta | 葬送歌 | 장송가 | Melodia Fúnebre | Лития | —（原包缺键） |
| `cards/DUALCAST.title` | 双重释放 | Dualcast | Hechizo doble | Descarga doble | デュアルキャスト | 이중 시전 | Dupla Evocação | Сдвоенный разряд | ร่ายซ้ำ |
| `cards/ETERNAL_ARMOR.title` | 永恒铠甲 | Eternal Armor | Armadura eterna | Armadura eterna | 不滅の鎧 | 영원의 갑옷 | Armadura Eterna | Нерушимые латы | —（原包缺键） |
| `cards/EXPOSE.title` | 暴露 | Expose | Exponer | Exponer | 暴露 | 들춰내기 | Expor | Слабое место | —（原包缺键） |
| `cards/FALLING_STAR.title` | 陨星 | Falling Star | Estrella fugaz | Estrella fugaz | 落星 | 별똥별 | Estrela Cadente | Падающая звезда | —（原包缺键） |
| `cards/FEED.title` | 狂宴 | Feed | Saciarse | Alimentarse | 捕食 | 포식 | Consumir | Пожирание | กัดกิน |
| `cards/FLAK_CANNON.title` | 散射炮 | Flak Cannon | Cañon antiaéreo | Artillería antiaérea | フラックキャノン | 대공포 | Artilharia Antiaérea | Зенитная пушка | —（原包缺键） |
| `cards/FLAME_BARRIER.title` | 火焰屏障 | Flame Barrier | Muro flamígero | Barrera de llamas | 炎の障壁 | 화염 장벽 | Barreira Flamejante | Огненный барьер | กำแพงอัคคี |
| `cards/GRAND_FINALE.title` | 华丽收场 | Grand Finale | Gran final | Gran final | グランドフィナーレ | 대단원의 막 | Grande Desfecho | Кульминация | อวสานอลังการ |
| `cards/HANG.title` | 吊杀 | Hang | Cuelgue | Colgar | 絞首 | 매달기 | Enforcar | Повешение | แขวนคอ |
| `cards/HEAVENLY_DRILL.title` | 天际钻头 | Heavenly Drill | Taladro celestial | Perforación celestial | ヘヴンリードリル | 천원돌파 | Perfuração Celestial | Ангельский бур | —（原包缺键） |
| `cards/HIDDEN_GEM.title` | 未掘宝石 | Hidden Gem | Gema oculta | Gema oculta | 隠された宝石 | 숨겨진 보석 | Gema Oculta | Скрытая жемчужина | —（原包缺键） |
| `cards/HOTFIX.title` | 热修复 | Hotfix | Emparchado | Parche rápido | ホットフィックス | 핫픽스 | Ajuste Rápido | Обновление | แก้ไขเร่งด่วน |
| `cards/INFERNO.title` | 狱火 | Inferno | Infierno | Infierno | インフェルノ | 불바다 | Inferno | Кромешный ад | เพลิงโลกันตร์ |
| `cards/JACKPOT.title` | 大奖 | Jackpot | Premio mayor | Premio gordo | ジャックポット | 잭팟 | Sorte Grande | Джекпот | แจ็กพอต |
| `cards/JUGGERNAUT.title` | 势不可当 | Juggernaut | Monstruo imparable | Coloso | ジャガーノート | 절대적인 힘 | Titã Couraçado | Махина | กำลังภายใน |
| `cards/KNOW_THY_PLACE.title` | 何人僭越 | Know Thy Place | Conoce vuestro lugar | Recuerda quién manda | 身の程を知れ | 네 주제를 알라 | Saiba o Seu Lugar | Знай свое место | —（原包缺键） |
| `cards/LOOP.title` | 循环 | Loop | Bucle | Bucle | ループ | 반복 | Ciclo | Цикл | คำสั่งวนซ้ำ |
| `cards/MISERY.title` | 苦难 | Misery | Miseria | Miseria | ミザリー | 비참함 | Sofrimento | Страдания | —（原包缺键） |
| `cards/MOLTEN_FIST.title` | 熔融之拳 | Molten Fist | Puño fundido | Puño magmático | 溶岩の拳 | 녹아내리는 주먹 | Punho de Magma | Раскаленный кулак | —（原包缺键） |
| `cards/NEUROSURGE.title` | 精神过载 | Neurosurge | Sobrecarga neural | Sobrecarga neuronal | ニューロサージ | 정신 폭주 | Sobrecarga Neural | Мозговой штурм | —（原包缺键） |
| `cards/NEUTRALIZE.title` | 中和 | Neutralize | Equlibrio | Neutralizar | 無力化 | 무력화 | Neutralizar | Обезвреживание | ทำให้เป็นกลาง |
| `cards/NIGHTMARE.title` | 夜魇 | Nightmare | Pesadilla | Pesadilla | 悪夢 | 악몽 | Pesadelo | Кошмар | ฝันร้าย |
| `cards/OBLIVION.title` | 湮灭 | Oblivion | Olvido | Olvido | オブリビオン | 망각 | Oblívio | Забвение | —（原包缺键） |
| `cards/PACTS_END.title` | 契约终结 | Pact's End | Pacto cerrado | Fin del pacto | 契約の終わり | 조약의 끝 | Pacto Encerrado | Окончательный пакт | —（原包缺键） |
| `cards/PARTICLE_WALL.title` | 粒子墙 | Particle Wall | Muro de partículas | Muro de partículas | 粒子障壁 | 입자 벽 | Muro de Partículas | Силовое поле | —（原包缺键） |
| `cards/PIERCING_WAIL.title` | 尖啸 | Piercing Wail | Grito perforante | Llanto lacerante | 金切り声 | 귀를 찢는 비명 | Lamento Penetrante | Пронзительный крик | คำครวญกรีดโสต |
| `cards/RAGE.title` | 狂怒 | Rage | Furia | Furia | 激怒 | 격노 | Fúria | Ярость | บันดาลโทสะ |
| `cards/REANIMATE.title` | 死者苏生 | Reanimate | Reanimación | Reanimar | 蘇生 | 소생 | Reanimar | Реанимация | —（原包缺键） |
| `cards/REBOOT.title` | 重启 | Reboot | Reinicio | Reinicio | 再起動 | 다시 시작 | Reiniciar | Перезагрузка | เปิดเครื่องใหม่ |
| `cards/REFLECT.title` | 倒映 | Reflect | Reflejo | Reflejo | 反射 | 반사 | Refletir | Отражение | —（原包缺键） |
| `cards/ROYALTIES.title` | 王国资产 | Royalties | Regalías | Diezmo | ロイヤリティ | 로열티 | Impostos | Дань | ค่าภาคหลวง |
| `cards/SMOKESTACK.title` | 烟囱 | Smokestack | Chimeneas | Chimenea | 煙突 | 배기 장치 | Chaminé | Дымоход | —（原包缺键） |
| `cards/SNAKEBITE.title` | 蛇咬 | Snakebite | Mordida viperina | Mordisco de serpiente | 蛇の噛みつき | 뱀 물기 | Mordida de Serpente | Змеиный укус | —（原包缺键） |
| `cards/SOUL.title` | 灵魂 | Soul | Alma | Alma | ソウル | 영혼 | Alma | Душа | วิญญาณ |
| `cards/STARDUST.title` | 星尘 | Stardust | Polvo estelar | Polvo de estrellas | 星屑 | 우주 먼지 | Poeira Estelar | Звездная пыль | —（原包缺键） |
| `cards/STORM.title` | 雷暴 | Storm | Tormenta | Tormenta | ストーム | 폭풍 | Tormenta | Шторм | มรสุม |
| `cards/SUBROUTINE.title` | 子程序 | Subroutine | Subrutina | Subrutina | サブルーチン | 서브루틴 | Sub-rotina | Вторичный процесс | —（原包缺键） |
| `cards/SURVIVOR.title` | 生存者 | Survivor | Sobreviviente | Superviviente | サバイバー | 생존자 | Sobrevivente | Выживание | ผู้รอดชีวิตฮึดสู้ |
| `cards/UNDEATH.title` | 不死 | Undeath | No-muerto | No-muerte | 不死 | 불사 | Morte-Vida | Жизнь после смерти | —（原包缺键） |
| `cards/UNLEASH.title` | 出击 | Unleash | Liberación | Rienda suelta | 解き放つ | 풀어놓기 | Irromper | Буйство | —（原包缺键） |
| `cards/VENERATE.title` | 崇拜 | Venerate | Veneración | Devoción | 畏敬 | 추앙 | Venerar | Преклонение | —（原包缺键） |
| `cards/VOLTAIC.title` | 电流相生 | Voltaic | Voltaico | Voltaico | 電圧上昇 | 동전기 | Voltaico | Проводник | —（原包缺键） |
| `cards/WHIRLWIND.title` | 旋风斩 | Whirlwind | Remolino | Torbellino | 旋風刃 | 소용돌이 | Redemoinho | Вихрь | พายุลมกรด |
| `cards/WROUGHT_IN_WAR.title` | 战火铸就 | Wrought in War | Forja bélica | Forjado por la guerra | 刃の戦錬 | 전장의 생존자 | Forjado na Guerra | Боевая закалка | —（原包缺键） |
| `cards/ZAP.title` | 电击 | Zap | Electrocución | Calambrazo | ザップ | 파지직 | Fagulha | Шок | ฟ้าแลบ |

## 引用与更新方式

- 官方来源：本机 `SlayTheSpire2.app/Contents/Resources/Slay the Spire 2.pck` 中表头所列资源路径与键，使用 `scan_sts2_rich_text.py` 的 `read_pck_entries` / `read_entry` 导出。
- 对照原版模型所属资源类型，卡牌、能力、附魔、关键词不能因中文相似而混用 title。
- 游戏升级后重新提取相同键，人工裁决缺译/改名；不得把模组翻译回写为“官方”。
- 选择界面 footer 使用 `LocString` 注入 `CardName`；角色后缀来自实际可用性判断，枚举身份和配置 ID 不随显示名变更。
