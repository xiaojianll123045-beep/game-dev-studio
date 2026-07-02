# 完整示例 — 语言模组

一个完整的语言模组示例：**"日语语言包"**

## 目录结构

```
JapaneseLanguagePack/
├── mod.json
└── locale/
    └── ja.json
```

## mod.json

```json
{
  "id": "japanese_language_pack",
  "name": "日本語言語パック",
  "version": "1.0.0",
  "author": "Translator-san",
  "description": "ゲームを日本語に完全翻訳します",
  "type": "language",
  "compatibility": {
    "game_version": ">=1.0.0"
  }
}
```

## 主语言包 (locale/ja.json)

```json
{
  "locale": "ja",
  "strings": {
    "ui.main_menu.title": "ギャラクシーエンパイア",
    "ui.main_menu.new_game": "新しいゲーム",
    "ui.main_menu.load_game": "ゲームをロード",
    "ui.main_menu.settings": "設定",
    "ui.main_menu.quit": "終了",
    "ui.main_menu.mods": "Mod",
    "ui.settings.audio": "オーディオ設定",
    "ui.settings.video": "ビデオ設定",
    "ui.settings.controls": "コントロール設定",
    "ui.settings.language": "言語",
    "ui.loading.tip": "ヒント: 右クリックで選択を解除できます",
    "ui.notification.research_complete": "研究完了: {tech_name}",
    "ui.notification.build_complete": "建設完了: {building_name}",
    "ui.notification.unit_lost": "ユニット喪失: {unit_name}",
    "ui.notification.war_declared": "{attacker} が {defender} に宣戦布告！",
    "ui.notification.trade_complete": "{partner} との取引が完了しました",
    "ui.dialog.confirm": "確認",
    "ui.dialog.cancel": "キャンセル",
    "ui.dialog.yes": "はい",
    "ui.dialog.no": "いいえ",
    "ui.resource.credits": "クレジット",
    "ui.resource.titanium": "チタン",
    "ui.resource.energy": "エネルギー",
    "ui.resource.food": "食料",
    "ui.resource.influence": "影響力",
    "ui.resource.research": "研究力",
    "unit.type.scout": "偵察機",
    "unit.type.warrior": "戦士",
    "unit.type.colonist": "入植者",
    "unit.type.scientist": "科学者",
    "unit.type.engineer": "エンジニア",
    "component.type.weapon": "武器",
    "component.type.armor": "装甲",
    "component.type.shield": "シールド",
    "component.type.engine": "エンジン",
    "component.type.power": "動力源",
    "component.type.special": "特殊",
    "tech.category.engineering": "工学",
    "tech.category.physics": "物理学",
    "tech.category.biology": "生物学",
    "tech.category.military": "軍事",
    "tech.category.economy": "経済",
    "event.type.crisis": "危機",
    "event.type.random": "ランダム",
    "event.type.story": "ストーリー",
    "achievement.category.conquest": "征服",
    "achievement.category.exploration": "探検",
    "achievement.category.economy": "経済",
    "achievement.category.technology": "技術",
    "tooltip.unit.health": "HP: {health}/{max_health}",
    "tooltip.unit.attack": "攻撃力: {attack}",
    "tooltip.unit.defense": "防御力: {defense}",
    "tooltip.unit.speed": "移動速度: {speed}",
    "tooltip.building.output": "生産: {resource} +{amount}/ターン",
    "misc.loading_screen.tip_1": "偵察機を早めに派遣して周辺を探索しましょう",
    "misc.loading_screen.tip_2": "資源の多様化が安定した成長の鍵です",
    "misc.loading_screen.tip_3": "外交関係を構築して有利な貿易協定を結びましょう",
    "misc.loading_screen.tip_4": "軍備を怠ると侵略のリスクが高まります",
    "misc.loading_screen.tip_5": "研究棟を建設して技術開発を加速しましょう"
  }
}
```

## 搭配数据模组使用

如果数据模组添加了自定义内容，可以在语言包中添加对应翻译：

```json
{
  "locale": "ja",
  "strings": {
    "component.quantum_core.name": "量子コア",
    "component.quantum_core.desc": "量子計算で全システムを最適化",
    "component.plasma_cannon_mk2.name": "プラズマキャノン Mk2",
    "component.plasma_cannon_mk2.desc": "改良型プラズマ兵器、高い射撃速度と貫通力を持つ",
    "tech.quantum_computing.name": "量子コンピューティング",
    "tech.quantum_computing.desc": "量子力学の原理を利用して次世代コンピュータを開発",
    "tech.plasma_weapons.name": "プラズマ兵器",
    "tech.plasma_weapons.desc": "高エネルギープラズマ兵器システムを開発",
    "event.asteroid_strike.name": "隕石衝突",
    "event.asteroid_strike.desc": "巨大な隕石が惑星に向かって接近中！",
    "achievement.galactic_conqueror.name": "銀河の征服者",
    "achievement.galactic_conqueror.desc": "全ての敵対文明を征服",
    "trait.industrial_giant.name": "産業の巨人",
    "trait.industrial_giant.desc": "産業生産が大幅に向上するが、文化発展がやや低下する"
  }
}
```

## 切换语言

在游戏设置的"语言"选项中选择"日本語"即可应用此语言包。

所有界面文本、提示信息、通知等将显示为日文。如果某个文本在语言包中未定义，将回退显示原文键值或英语文本。
