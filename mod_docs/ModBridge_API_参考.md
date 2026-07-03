# ModBridge API 参考 —— GDScript Mod 开发

你的 GDScript Mod 在 `OnLoad(gm, bridge)` 中接收两个参数：

| 参数 | 类型 | 说明 |
|------|------|------|
| `gm` | `GameManager` (Node3D) | 游戏主管理器节点 |
| `bridge` | `ModBridge` (Node) | **蛇形命名的 C# API 封装 —— 用这个** |

也可以在任意时刻通过节点路径获取 bridge：
```gdscript
var b = get_node("/root/GameManager/ModBridge")
```

---

## 全部 Bridge 方法

### 🌐 语言
| GDScript 调用 | 说明 |
|----------|------|
| `b.get_lang()` | 返回当前语言代码 (如 `"zh"`、`"en"`) |

### 💰 资金
| GDScript 调用 | 说明 |
|----------|------|
| `b.get_money()` | 获取当前资金 |
| `b.set_money(v)` | 设置资金为指定值 |
| `b.add_money(v)` | 增加资金 |
| `b.spend_money(v, cat="")` | 消费资金（可带分类） |

### ⚡ 灵感
| GDScript 调用 | 说明 |
|----------|------|
| `b.get_inspiration()` | 当前灵感值 |
| `b.get_max_inspiration()` | 灵感上限 |
| `b.add_inspiration(v)` | 增加灵感 |
| `b.spend_inspiration(v)` | 消耗灵感 |

### 🔬 科技
| GDScript 调用 | 说明 |
|----------|------|
| `b.all_tech_ids()` | 返回所有科技 ID 的 Array |
| `b.is_tech_researched(id)` | 检查是否已解锁 |
| `b.unlock_tech(id)` | 解锁指定科技 |

### 📊 项目
| GDScript 调用 | 说明 |
|----------|------|
| `b.project_count()` | 活跃项目数 |
| `b.clear_bugs()` | 清空所有项目的 Bug |
| `b.max_scores()` | 全部 6 项分数设 100 + 清 Bug |

### 👥 员工
| GDScript 调用 | 说明 |
|----------|------|
| `b.employee_count()` | 员工总数 |
| `b.zero_fatigue()` | 所有员工疲劳归零 |
| `b.max_skills()` | 所有技能升到 5 级 |

### 🎮 粉丝与信任
| GDScript 调用 | 说明 |
|----------|------|
| `b.add_fans(v)` | 增加路人粉 |
| `b.add_diehard_fans(v)` | 增加死忠粉 |
| `b.get_trust()` | 玩家信任度 (0-100) |
| `b.set_trust(v)` | 设置玩家信任度 |

### ⏱ 时间与速度
| GDScript 调用 | 说明 |
|----------|------|
| `b.get_month()` | 当前游戏月份 |
| `b.get_year()` | 当前游戏年份 |
| `b.set_speed(1-8)` | 设置游戏速度 |
| `b.is_paused()` | 是否暂停 |
| `b.set_paused(v)` | 暂停/继续 |

### 📢 界面通知
| GDScript 调用 | 说明 |
|----------|------|
| `b.toast(title, msg, color=null)` | 顶部提示 |
| `b.popup(title, msg, color=null)` | 模态弹窗 |

### 🛠 获取原始管理器
| GDScript 调用 | 说明 |
|----------|------|
| `b.get_game_manager()` | GameManager 节点 |
| `b.get_resource_manager()` | ResourceManager 节点 |
| `b.get_dev_manager()` | GameDevManager 节点 |
| `b.get_employee_manager()` | EmployeeManager 节点 |
| `b.get_tech_manager()` | TechManager 节点 |

### ⌨ 自定义按键
| GDScript 调用 | 说明 |
|----------|------|
| `b.register_key(KEY_F1, Callable)` | 注册按键回调（按键名为 Godot Key 枚举值） |
| `b.unregister_key(KEY_F1)` | 取消注册按键 |
在 `OnLoad` 中注册，按键会在游戏处理之前触发回调，不会被其他 UI 拦截。

### 📋 日志与配置
| GDScript 调用 | 说明 |
|----------|------|
| `b.log(msg)` | 输出日志到控制台 |
| `b.log_err(msg)` | 输出错误日志 |
| `b.get_setting(mod_id, key, fallback)` | 读取持久化设置 |
| `b.set_setting(mod_id, key, value)` | 写入持久化设置 |

---

## 完整工作示例

```gdscript
# mods/my_cheat/scripts/main.gd
extends Node

var b = null

func OnLoad(gm, bridge):
	b = bridge
	b.log("Mod 已加载！")
	b.add_money(500000)
	b.unlock_tech("3d_v1")
	b.toast("🚀", "Mod 已生效！")

func _process(delta):
	if Input.is_key_pressed(KEY_F1) and not _f1:
		_f1 = true
		b.max_scores()
		b.zero_fatigue()
		b.toast("✨", "作弊已激活！")
	elif not Input.is_key_pressed(KEY_F1):
		_f1 = false

var _f1 = false
```

## 数据 Mod（无需脚本）

平衡调整放在 `data/balance.json`。参考 `mod_examples/03_balance_tweaker/`。

## 注意事项

- Godot 4 中没有 `Input.is_key_just_pressed()`。请用 `_process` + `is_key_pressed` + 防抖标志。
- C# 静态类（`ModAPI`、`TechTreeData`）**无法从 GDScript 访问**。始终使用 `bridge.*`。
- Mod 文件必须放在 `res://mods/你的Mod名字/` 下，并包含 `mod.json` 清单文件。
