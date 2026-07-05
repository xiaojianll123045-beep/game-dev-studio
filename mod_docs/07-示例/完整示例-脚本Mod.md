# 完整示例 — 开发辅助工具

一个完整的脚本 Mod 示例：**"开发辅助工具" (Dev Helper Tool)**

## 目录结构

```
DevHelper/
├── mod.json                 # Mod 清单（英文默认）
├── mod_zh.json              # 中文名称/描述
├── scripts/
│   ├── main.gd              # 主入口（OnLoad / OnUnload）
│   └── panel.gd             # 统计面板（由 main.gd 实例化）
└── locale/
    └── zh-CN.json           # 多语言翻译
```

## mod.json

```json
{
  "id": "dev_helper",
  "version": "1.0.0",
  "author": "Helper",
  "type": "script",
  "dependencies": [],
  "compatibility": {
    "game_version": ">=0.1"
  }
}
```

## mod_zh.json

```json
{
  "name": "开发辅助工具",
  "description": "提供开发快捷键、统计面板和自动修复模式"
}
```

> `name` 和 `description` 优先从 `mod_{lang}.json`（如 `mod_zh.json`、`mod_en.json`）读取，找不到时回退到 `mod.json`。推荐用此方式实现多语言。

## 主入口 (scripts/main.gd)

```gdscript
extends Node

var b: Variant = null
var panel: Control = null
var auto_fix: bool = false
var project_count: int = 0
var _key_cooldown: Dictionary = {}

func OnLoad(gm: Node, bridge) -> void:
	# bridge 参数已弃用，通过单例访问
	b = Engine.get_singleton("ModBridge")
	if b == null:
		push_error("ModBridge 不可用，加载失败")
		return

	b.log("开发辅助工具 v1.0.0 已加载")

	# 注册快捷键（F1 / F2 由系统捕获，不影响游戏内其他按键）
	b.register_key(KEY_F1, func():
		b.add_money(50000)
		b.toast("💰", "资金 +50,000")
	)
	b.register_key(KEY_F2, func():
		b.log("灵感 +20（演示 API 调用）")
		b.toast("💡", "灵感 +20", Color.YELLOW)
	)

	# 注册设置开关 — 在 Mod 设置页面生成一个开关
	b.register_setting("auto_fix_mode", "自动修复模式", func(val: bool):
		auto_fix = val
		b.log("自动修复: " + ("开启" if auto_fix else "关闭"))
		b.toast("🛠️", "自动修复 " + ("已开启" if auto_fix else "已关闭"))
	)

	# 注册 ModCommAPI 端点，其他 Mod 可通过 bridge.send_message 调用
	b.register_endpoint("dev_helper", "ping", func(args: Dictionary) -> Dictionary:
		return {"pong": true, "project_count": project_count}
	)

	# 解锁成就
	b.unlock_achievement("mod_dev_helper_loaded")

	# 读取持久化存档数据
	var data = b.load_mod_data("dev_helper_save")
	if data:
		project_count = data.get("project_count", 0)
		auto_fix = data.get("auto_fix", false)
		b.log("已恢复存档数据，累计项目数: " + str(project_count))

func _process(delta: float) -> void:
	# F3/F4 使用 _process + 去抖标志处理（非全局注册键）
	if Input.is_key_pressed(KEY_F3) and not _key_cooldown.get("f3", false):
		_key_cooldown["f3"] = true
		_toggle_panel()
	if Input.is_key_pressed(KEY_F4) and not _key_cooldown.get("f4", false):
		_key_cooldown["f4"] = true
		_save_project()

	# 释放去抖
	if not Input.is_key_pressed(KEY_F3):
		_key_cooldown["f3"] = false
	if not Input.is_key_pressed(KEY_F4):
		_key_cooldown["f4"] = false

func _toggle_panel() -> void:
	if panel and is_instance_valid(panel):
		panel.visible = not panel.visible
		b.toast("📊", "统计面板 " + ("已显示" if panel.visible else "已隐藏"))
	else:
		panel = preload("panel.gd").new()
		panel._init_panel(self)
		get_node("/root/GameManager/UiLayer").add_child(panel)

func _save_project() -> void:
	project_count += 1
	b.log("项目已记录，累计: " + str(project_count))
	b.toast("💾", "项目已记录（共 " + str(project_count) + " 个）")

	# 通过 ModCommAPI 广播消息
	b.broadcast_message("project_saved", {
		"count": project_count,
		"mod": "dev_helper"
	})

	# 持久化保存
	b.save_mod_data("dev_helper_save", {
		"project_count": project_count,
		"auto_fix": auto_fix
	})

func OnUnload() -> void:
	b.log("开发辅助工具 正在卸载")
	b.unregister_key(KEY_F1)
	b.unregister_key(KEY_F2)
	b.unregister_endpoint("dev_helper", "ping")
	if panel and is_instance_valid(panel):
		panel.queue_free()
		panel = null
```

## 统计面板 (scripts/panel.gd)

```gdscript
extends Control

var mod_main: Node = null

func _init_panel(parent_mod: Node) -> void:
	mod_main = parent_mod
	anchor_left = 0.7
	anchor_top = 0.1
	anchor_right = 0.98
	anchor_bottom = 0.5
	mouse_filter = MOUSE_FILTER_STOP

	var bg = StyleBoxFlat.new()
	bg.bg_color = Color(0.08, 0.08, 0.1, 0.9)
	bg.corner_radius_top_left = 6
	bg.corner_radius_top_right = 6
	bg.corner_radius_bottom_left = 6
	bg.corner_radius_bottom_right = 6
	add_theme_stylebox_override("panel", bg)

	var title = Label.new()
	title.text = "📊 Dev Helper Stats"
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", Color(0.9, 0.9, 0.95))
	title.anchor_left = 0.05
	title.anchor_top = 0.05
	title.anchor_right = 0.95
	title.anchor_bottom = 0.2
	add_child(title)

	var stats = Label.new()
	stats.name = "StatsLabel"
	stats.add_theme_font_size_override("font_size", 13)
	stats.add_theme_color_override("font_color", Color(0.7, 0.8, 1.0))
	stats.anchor_left = 0.05
	stats.anchor_top = 0.25
	stats.anchor_right = 0.95
	stats.anchor_bottom = 0.7
	add_child(stats)

	var close_btn = Button.new()
	close_btn.text = "✕"
	close_btn.anchor_left = 0.85
	close_btn.anchor_top = 0.05
	close_btn.anchor_right = 0.95
	close_btn.anchor_bottom = 0.2
	close_btn.pressed.connect(func(): visible = false)
	add_child(close_btn)

func _process(delta: float) -> void:
	var label = get_node_or_null("StatsLabel")
	if not label:
		return
	var gm = get_node("/root/GameManager")
	label.text = (
		"资金: ¥" + _fmt(gm.money if gm.has("money") else 0) + "\n" +
		"项目数: " + str(mod_main.project_count if mod_main else 0) + "\n" +
		"自动修复: " + ("✔" if mod_main and mod_main.auto_fix else "✘")
	)

func _fmt(v: float) -> String:
	if v >= 1000000:
		return str(v / 1000000.0).pad_decimals(1) + "M"
	if v >= 1000:
		return str(v / 1000.0).pad_decimals(1) + "K"
	return str(v)
```

## 语言包 (locale/zh-CN.json)

```json
{
  "locale": "zh-CN",
  "strings": {
    "dev_helper.mod_name": "开发辅助工具",
    "dev_helper.mod_desc": "提供开发快捷键、统计面板和自动修复模式",
    "dev_helper.auto_fix_label": "自动修复模式",
    "dev_helper.stats_title": "📊 Dev Helper 统计",
    "dev_helper.money_label": "资金",
    "dev_helper.project_label": "项目数",
    "dev_helper.auto_fix_on": "✔ 已开启",
    "dev_helper.auto_fix_off": "✘ 已关闭"
  }
}
```

## 功能一览

| 快捷键 | 功能                 | 实现方式                     |
|--------|----------------------|------------------------------|
| F1     | 资金 +50,000         | `bridge.register_key()`      |
| F2     | 灵感 +20             | `bridge.register_key()`      |
| F3     | 切换统计面板         | `_process()` + 去抖标志      |
| F4     | 记录项目（计数+1）   | `_process()` + 去抖标志      |

## 使用的 API

| API | 用途 |
|------|------|
| `b.log(msg)` | 日志输出 |
| `b.add_money(v)` | 增加资金 |
| `b.toast(title, msg, color?)` | 弹出提示 |
| `b.register_key(key, callable)` | 注册快捷键 |
| `b.unregister_key(key)` | 卸载快捷键 |
| `b.register_setting(id, label, callable)` | 设置开关 |
| `b.register_endpoint(id, endpoint, callable)` | ModCommAPI 端点 |
| `b.broadcast_message(endpoint, args)` | 广播消息 |
| `b.unlock_achievement(id)` | 解锁成就 |
| `b.load_mod_data(key)` / `b.save_mod_data(key, val)` | 存档数据持久化 |

## 加载机制说明

- 所有 `scripts/` 目录下的 `.gd` 文件都会被加载到场景树中（`entry_point` 字段被游戏忽略）。
- 只有实现了 `OnLoad(gm, bridge)` 的脚本才会收到初始化调用。
- `bridge` 参数已弃用，始终使用 `Engine.get_singleton("ModBridge")` 获取桥接对象。
- `OnUnload()` 在 Mod 禁用时调用，用于释放资源。
- `name` / `description` 优先从 `mod_{lang}.json` 读取，找不到时回退到 `mod.json`。
- 面板脚本 (`panel.gd`) 不自启动（没有 `OnLoad`），由 `main.gd` 通过 `preload()` 实例化。
