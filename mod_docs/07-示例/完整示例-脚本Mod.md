# 完整示例 — 脚本模组

一个完整的脚本模组示例：**"战斗统计与分析器"**

## 目录结构

```
CombatAnalytics/
├── mod.json
├── scripts/
│   ├── main.gd
│   ├── combat_logger.gd
│   └── ui_controller.gd
├── data/
│   └── commands.json
└── locale/
    └── zh-CN.json
```

## mod.json

```json
{
  "id": "combat_analytics",
  "name": "战斗统计与分析器",
  "version": "2.0.0",
  "author": "Analyst",
  "description": "记录战斗数据并生成详细统计报告",
  "type": "script",
  "entry_point": "scripts/main.gd",
  "dependencies": [],
  "compatibility": {
    "game_version": ">=1.0.0"
  }
}
```

## 主入口 (scripts/main.gd)

```gdscript
extends Node

var combat_logger: Node
var ui_controller: Node

func _ready():
    bridge.log_info("战斗统计与分析器 v2.0.0 已加载")

    combat_logger = preload("res://mods/combat_analytics/scripts/combat_logger.gd").new()
    add_child(combat_logger)

    ui_controller = preload("res://mods/combat_analytics/scripts/ui_controller.gd").new()
    add_child(ui_controller)

    bridge.register_hook("on_game_start", self, "_on_game_start")
    bridge.register_command("combat_report", self, "_show_combat_report")

func _on_game_start(data):
    bridge.log_info("游戏开始，初始化战斗分析系统")
    bridge.register_hook("on_unit_destroyed", combat_logger, "_on_unit_destroyed")
    bridge.register_hook("on_turn_end", combat_logger, "_on_turn_end")

func _show_combat_report(args):
    var report = combat_logger.generate_report()
    bridge.log_info("=== 战斗统计报告 ===")
    bridge.log_info("总战斗次数: " + str(report.total_battles))
    bridge.log_info("总击杀: " + str(report.total_kills))
    bridge.log_info("胜率: " + str(report.win_rate * 100) + "%")
    ui_controller.show_report(report)
```

## 战斗记录器 (scripts/combat_logger.gd)

```gdscript
extends Node

var battle_log := []
var total_damage_dealt := 0
var total_damage_taken := 0
var battle_count := 0
var wins := 0
var losses := 0

var _key_last_pressed := {}

func _ready():
    bridge.log_info("战斗记录器已初始化")
    bridge.register_hook("on_tick", self, "_on_tick")

func _on_tick(delta):
    if Input.is_key_pressed(KEY_F5) and not _key_last_pressed.get("f5", false):
        _key_last_pressed["f5"] = true
        _toggle_logging()

    if Input.is_key_pressed(KEY_F6) and not _key_last_pressed.get("f6", false):
        _key_last_pressed["f6"] = true
        _export_log()

    if not Input.is_key_pressed(KEY_F5):
        _key_last_pressed["f5"] = false
    if not Input.is_key_pressed(KEY_F6):
        _key_last_pressed["f6"] = false

var logging_enabled := true

func _toggle_logging():
    logging_enabled = not logging_enabled
    bridge.log_info("战斗记录: " + ("开启" if logging_enabled else "关闭"))

func _on_unit_destroyed(data):
    if not logging_enabled:
        return

    var entry = {
        "time": Time.get_unix_time_from_system(),
        "turn": bridge.get_game_value("game.turn"),
        "unit": data.unit_name,
        "killed_by": data.killed_by if data.has("killed_by") else "unknown",
        "position": data.position if data.has("position") else null
    }

    battle_log.append(entry)
    battle_count += 1

    if data.has("is_friendly") and data.is_friendly:
        losses += 1
        total_damage_taken += data.get("damage", 0)
    else:
        wins += 1
        total_damage_dealt += data.get("damage", 0)

    bridge.log_debug("战斗已记录: " + data.unit_name)

func _on_turn_end(data):
    if battle_log.size() > 0:
        bridge.log_debug("当前回合战斗次数: " + str(battle_log.size()))

func generate_report() -> Dictionary:
    var win_rate = 0.0
    if battle_count > 0:
        win_rate = float(wins) / float(battle_count)

    return {
        "total_battles": battle_count,
        "total_kills": wins,
        "total_losses": losses,
        "win_rate": win_rate,
        "total_damage_dealt": total_damage_dealt,
        "total_damage_taken": total_damage_taken,
        "avg_damage_per_battle": total_damage_dealt / max(battle_count, 1),
        "recent_battles": battle_log.slice(-10)
    }

func _export_log():
    var report = generate_report()
    bridge.save_mod_data("combat_report", report)
    bridge.log_info("战斗报告已导出到存档")

    var text = "战斗统计报告\n"
    text += "================\n"
    text += "总战斗: " + str(report.total_battles) + "\n"
    text += "击杀: " + str(report.total_kills) + "\n"
    text += "阵亡: " + str(report.total_losses) + "\n"
    text += "胜率: " + str(report.win_rate * 100) + "%\n"

    bridge.show_notification("战斗报告已生成！按 F6 重新导出")
```

## UI 控制器 (scripts/ui_controller.gd)

```gdscript
extends Node

var _last_pressed := {}

func _ready():
    bridge.register_hook("on_tick", self, "_on_tick")
    bridge.register_command("toggle_analytics_ui", self, "_toggle_ui")

func _on_tick(delta):
    if Input.is_key_pressed(KEY_F7) and not _last_pressed.get("f7", false):
        _last_pressed["f7"] = true
        _toggle_ui()
    elif not Input.is_key_pressed(KEY_F7):
        _last_pressed["f7"] = false

var ui_visible := false

func _toggle_ui(args = null):
    ui_visible = not ui_visible
    bridge.log_info("分析界面: " + ("显示" if ui_visible else "隐藏"))
    bridge.toggle_debug_overlay()

func show_report(report: Dictionary):
    bridge.show_notification("战斗统计 — 胜率: " + str(report.win_rate * 100) + "%")
    bridge.log_info("最近战斗: " + str(report.recent_battles.size()) + " 场")
```

## 语言包 (locale/zh-CN.json)

```json
{
  "locale": "zh-CN",
  "strings": {
    "combat_analytics.mod_name": "战斗统计与分析器",
    "combat_analytics.report_title": "=== 战斗统计报告 ===",
    "combat_analytics.total_battles": "总战斗次数",
    "combat_analytics.total_kills": "总击杀",
    "combat_analytics.win_rate": "胜率",
    "combat_analytics.logging_enabled": "战斗记录已开启",
    "combat_analytics.logging_disabled": "战斗记录已关闭"
  }
}
```

## 键盘快捷键

| 快捷键 | 功能 |
|--------|------|
| F5 | 切换战斗记录开关 |
| F6 | 导出战斗报告 |
| F7 | 切换分析界面 |

所有按键处理均使用 `Input.is_key_pressed()` + 去抖标志模式实现。

---

## 注册自定义小游戏

使用 `bridge.register_minigame()` 让你的 Mod 出现在游戏的小游戏菜单中。

### mod.json
```json
{
  "version": "1.0",
  "author": "示例",
  "type": "script"
}
```

### mod_zh.json
```json
{
  "name": "猜数字",
  "description": "一个简单的猜数字小游戏"
}
```

### scripts/main.gd
```gdscript
extends Node

var b = null
var panel = null

func OnLoad(gm, bridge):
	b = bridge
	b.register_minigame("🔢 猜数字", self._launch)

func _launch():
	if panel != null: return
	var vp = get_viewport().get_visible_rect().size
	panel = Panel.new()
	panel.anchor_left = 0.3; panel.anchor_top = 0.2
	panel.anchor_right = 0.7; panel.anchor_bottom = 0.8
	panel.mouse_filter = Control.MOUSE_FILTER_STOP
	var bg = StyleBoxFlat.new()
	bg.bg_color = Color(0.1, 0.1, 0.12)
	bg.corner_radius_top_left = 8; bg.corner_radius_top_right = 8
	bg.corner_radius_bottom_left = 8; bg.corner_radius_bottom_right = 8
	panel.add_theme_stylebox_override("panel", bg)
	get_node("/root/GameManager").UiLayer.add_child(panel)

	var lbl = Label.new()
	lbl.text = "🔢 猜数字 (1-100)"
	lbl.add_theme_font_size_override("font_size", 20)
	lbl.add_theme_color_override("font_color", Color(0.9, 0.9, 0.95))
	lbl.anchor_left = 0.1; lbl.anchor_top = 0.1
	lbl.anchor_right = 0.9; lbl.anchor_bottom = 0.25
	panel.add_child(lbl)

	var target = randi() % 100 + 1
	var input = LineEdit.new()
	input.anchor_left = 0.1; input.anchor_top = 0.3
	input.anchor_right = 0.9; input.anchor_bottom = 0.4
	panel.add_child(input)

	var feedback = Label.new()
	feedback.add_theme_font_size_override("font_size", 16)
	feedback.add_theme_color_override("font_color", Color(1, 1, 1))
	feedback.anchor_left = 0.1; feedback.anchor_top = 0.45
	feedback.anchor_right = 0.9; feedback.anchor_bottom = 0.55
	panel.add_child(feedback)

	var guess_btn = Button.new()
	guess_btn.text = "猜！"
	guess_btn.anchor_left = 0.1; guess_btn.anchor_top = 0.6
	guess_btn.anchor_right = 0.5; guess_btn.anchor_bottom = 0.7
	guess_btn.pressed.connect(func():
		var val = int(input.text)
		if val == target:
			feedback.text = "🎉 猜对了！"
			guess_btn.disabled = true
		elif val < target:
			feedback.text = "太小了！"
		else:
			feedback.text = "太大了！"
	)
	panel.add_child(guess_btn)

	var close_btn = Button.new()
	close_btn.text = "✕ 关闭"
	close_btn.anchor_left = 0.6; close_btn.anchor_top = 0.6
	close_btn.anchor_right = 0.9; close_btn.anchor_bottom = 0.7
	close_btn.pressed.connect(func(): panel.queue_free(); panel = null)
	panel.add_child(close_btn)

func OnUnload():
	b.unregister_minigame("🔢 猜数字")
	if panel != null: panel.queue_free()
```

---

## 注册自定义按键

使用 `bridge.register_key()` 拦截指定按键，在游戏处理之前触发。

### scripts/main.gd
```gdscript
extends Node

var b = null

func OnLoad(gm, bridge):
	b = bridge
	# 按 F10 给钱，F11 全科技
	b.register_key(KEY_F10, func(): b.add_money(1000000); b.toast("💰", "+¥1,000,000"))
	b.register_key(KEY_F11, func():
		for tid in b.all_tech_ids():
			b.unlock_tech(tid)
		b.toast("🔬", "所有科技已解锁")
	)

func OnUnload():
	b.unregister_key(KEY_F10)
	b.unregister_key(KEY_F11)
```
