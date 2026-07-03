# ModBridge API Reference — GDScript Mod Development

Your GDScript mod receives two parameters in `OnLoad(gm, bridge)`:

| Param | Type | Access |
|-------|------|--------|
| `gm` | `GameManager` (Node3D) | Root game manager node |
| `bridge` | `ModBridge` (Node) | **Snake_case API to C# — use this** |

You can also access the bridge directly anytime:
```gdscript
var b = get_node("/root/GameManager/ModBridge")
```

---

## All Bridge Methods

### 🌐 Language
| GDScript | Description |
|----------|-------------|
| `b.get_lang()` | Returns current language code (e.g. `"en"`, `"zh"`) |

### 💰 Money
| GDScript | Description |
|----------|-------------|
| `b.get_money()` | Returns current money |
| `b.set_money(v)` | Set money to exact amount |
| `b.add_money(v)` | Add money |
| `b.spend_money(v, cat="")` | Spend money with optional category tracking |

### ⚡ Inspiration
| GDScript | Description |
|----------|-------------|
| `b.get_inspiration()` | Current inspiration |
| `b.get_max_inspiration()` | Max inspiration |
| `b.add_inspiration(v)` | Add inspiration |
| `b.spend_inspiration(v)` | Spend inspiration |

### 🔬 Technology
| GDScript | Description |
|----------|-------------|
| `b.all_tech_ids()` | Returns Array of all tech IDs |
| `b.is_tech_researched(id)` | Check if tech is unlocked |
| `b.unlock_tech(id)` | Unlock a specific tech |

### 📊 Projects
| GDScript | Description |
|----------|-------------|
| `b.project_count()` | Number of active projects |
| `b.clear_bugs()` | Set BugCount to 0 on all projects |
| `b.max_scores()` | Set all 6 scores to 100 + clear bugs |

### 👥 Employees
| GDScript | Description |
|----------|-------------|
| `b.employee_count()` | Number of employees |
| `b.zero_fatigue()` | Set all employees' Fatigue to 0 |
| `b.max_skills()` | Set all skills to Level 5 |

### 🎮 Fans & Trust
| GDScript | Description |
|----------|-------------|
| `b.add_fans(v)` | Add casual fans |
| `b.add_diehard_fans(v)` | Add diehard fans |
| `b.get_trust()` | Player trust (0-100) |
| `b.set_trust(v)` | Set player trust |

### ⏱ Time & Speed
| GDScript | Description |
|----------|-------------|
| `b.get_month()` | Current game month |
| `b.get_year()` | Current game year |
| `b.set_speed(1-8)` | Set game speed |
| `b.is_paused()` | Is game paused |
| `b.set_paused(v)` | Pause/unpause |

### 📢 UI Notifications
| GDScript | Description |
|----------|-------------|
| `b.toast(title, msg, color=null)` | Toast notification |
| `b.popup(title, msg, color=null)` | Modal popup |

### 🛠 Access Raw Managers
| GDScript | Description |
|----------|-------------|
| `b.get_game_manager()` | GameManager node |
| `b.get_resource_manager()` | ResourceManager node |
| `b.get_dev_manager()` | GameDevManager node |
| `b.get_employee_manager()` | EmployeeManager node |
| `b.get_tech_manager()` | TechManager node |

### 🎮 Custom Minigame
| GDScript | Description |
|----------|-------------|
| `b.register_minigame("name", Callable)` | Register a minigame that appears in the minigame menu |
| `b.unregister_minigame("name")` | Unregister a minigame |
Register in `OnLoad`. The `Callable` is called when the user launches it. Example:
```gdscript
func OnLoad(gm, bridge):
	bridge.register_minigame("My Minigame", self._my_minigame)
func _my_minigame():
	print("Minigame launched!")
	# Create your game UI here
```

### ⌨ Custom Key Binding
| GDScript | Description |
|----------|-------------|
| `b.register_key(KEY_F1, Callable)` | Register a key callback (key = Godot Key enum value) |
| `b.unregister_key(KEY_F1)` | Unregister a key callback |
Register in `OnLoad`. The callback fires before normal game input processing, so it cannot be intercepted by other UI.

### 📋 Logging & Settings
| GDScript | Description |
|----------|-------------|
| `b.log(msg)` | Print to console |
| `b.log_err(msg)` | Print error to console |
| `b.get_setting(mod_id, key, fallback)` | Read persistent setting |
| `b.set_setting(mod_id, key, value)` | Write persistent setting |

---

## Full Working Example

```gdscript
# mods/my_cheat/scripts/main.gd
extends Node

var b = null

func OnLoad(gm, bridge):
	b = bridge
	b.log("My mod loaded!")
	b.add_money(500000)
	b.unlock_tech("3d_v1")
	b.toast("🚀", "My mod is active!")

func _process(delta):
	if Input.is_key_pressed(KEY_F1) and not _f1:
		_f1 = true
		b.max_scores()
		b.zero_fatigue()
		b.toast("✨", "Cheat activated!")
	elif not Input.is_key_pressed(KEY_F1):
		_f1 = false

var _f1 = false
```

## Data Mods (no scripts)

Balance tweaks go in `data/balance.json`. See `mod_examples/03_balance_tweaker/`.

## Notes

- `Input.is_key_just_pressed()` does NOT exist in Godot 4. Use `_process` + `is_key_pressed` + debounce flag.
- C# static classes (`ModAPI`, `TechTreeData`) are NOT accessible from GDScript. Always use `bridge.*`.
- The mod file must be in `res://mods/your_mod_name/` with a `mod.json` manifest.
