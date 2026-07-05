# ModBridge API Reference — GDScript Mod Development

Your GDScript mod receives two parameters in `OnLoad(gm, bridge)`:

| Param | Type | Access |
|-------|------|--------|
| `gm` | `GameManager` (Node3D) | Root game manager node (null at menu) |
| `bridge` | `ModBridge` (Node) | **Deprecated**, always null. Use the global singleton `ModBridge` instead |

Recommended way to access the bridge:
```gdscript
var b = Engine.get_singleton("ModBridge")
```

You can also reach it by node path (in-game only, null at menu):
```gdscript
var b = get_node("/root/GameManager/ModBridge")
```

**Note:** The `bridge` parameter in `OnLoad` is always null. Use the `ModBridge` singleton instead.

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
var b = Engine.get_singleton("ModBridge")
func OnLoad(gm, bridge):
	b.register_minigame("My Minigame", self._my_minigame)
func _my_minigame():
	print("Minigame launched!")
	# Create your game UI here
```

### 🏆 Achievements
| GDScript | Description |
|----------|-------------|
| `b.unlock_achievement(achievement_id)` | Unlock an achievement by ID (must be registered in AchievementManager) |

### ⚙ Custom Settings
| GDScript | Description |
|----------|-------------|
| `b.register_setting("id", "label", Callable)` | Add a custom option in the Settings panel (below audio settings) |
| `b.unregister_setting("id")` | Remove custom setting |
`Callable` receives two params: `(VBoxContainer root, float rowH)`. Example:
```gdscript
var b = Engine.get_singleton("ModBridge")
func OnLoad(gm, bridge):
	b.register_setting("my_setting", "My Setting", self._render)
func _render(root, rowH):
	var hb = HBoxContainer.new()
	hb.add_theme_constant_override("separation", 8)
	hb.custom_minimum_size = Vector2(0, rowH)
	var label = Label.new()
	label.text = "Toggle"
	label.add_theme_font_size_override("font_size", 11)
	label.custom_minimum_size = Vector2(130, rowH)
	hb.add_child(label)
	var cb = CheckBox.new()
	cb.text = "Enable"
	cb.toggled.connect(func(on): print("Toggle:", on))
	hb.add_child(cb)
	var spacer = Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hb.add_child(spacer)
	root.add_child(hb)
```

### ⌨ Custom Key Binding
| GDScript | Description |
|----------|-------------|
| `b.register_key(KEY_F1, Callable)` | Register a key callback (key = Godot Key enum value) |
| `b.unregister_key(KEY_F1)` | Unregister a key callback |
Register in `OnLoad`. The callback fires before normal game input processing, so it cannot be intercepted by other UI.

### 📻 Audio Loading (bypass import system)
| GDScript | Description |
|----------|-------------|
| `b.load_mp3("res://path.mp3")` | Create AudioStream directly from MP3 file (no import needed) |

### 📛 Node Namespacing (avoid name conflicts)
| GDScript | Description |
|----------|-------------|
| `b.node_name("mod_id", "suffix")` | Returns `Mod_modId_suffix`, guaranteed unique across mods |

### 📋 Logging & Settings
| GDScript | Description |
|----------|-------------|
| `b.log(msg)` | Print to console |
| `b.log_err(msg)` | Print error to console |
| `b.get_setting(mod_id, key, fallback)` | Read persistent setting |
| `b.set_setting(mod_id, key, value)` | Write persistent setting |

### 🔗 Mod Communication
| GDScript | Description |
|----------|-------------|
| `b.register_endpoint(mod_id, endpoint, Callable)` | Register a communication endpoint for other mods to call |
| `b.send_message(target_mod_id, endpoint, args)` | Send a message to a specific mod, returns result |
| `b.broadcast_message(endpoint, args)` | Broadcast to all mods with that endpoint, returns dict |
| `b.has_endpoint(mod_id, endpoint)` | Check if a mod has registered an endpoint |
| `b.get_mods_with_endpoint(endpoint)` | List all mod IDs that registered an endpoint |

Example:
```gdscript
# Mod A — register endpoint
b.register_endpoint("mod_a", "get_data", func(args):
    return {"value": 42}
)
# Mod B — call endpoint
var result = b.send_message("mod_a", "get_data", [])
b.log("Got: " + str(result))
```

---

## Full Working Example

```gdscript
# mods/my_cheat/scripts/main.gd
extends Node

var b = null

func OnLoad(gm, bridge):
	b = Engine.get_singleton("ModBridge")
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
- **Debug logging**: Press F9 in-game to view the Mod log. The log is automatically saved to `user://mod_log.txt` — packaged game users can share this file with mod authors for debugging.
- **Console**: Press `~` or F12 to open the ModConsole. Common commands:

  | Command | Description |
  |---------|-------------|
  | `help [cmd]` | Show help |
  | `mod_log` | View full mod log |
  | `save_log` | Save log to user://mod_log.txt |
  | `status` | View game state |
  | `list_mods` | List all loaded mods |
  | `projects` | View all projects |
  | `money [amount]` | Get/set money |
  | `inspiration [val]` | Get/set inspiration |
  | `unlock_tech <ID> [all]` | Unlock tech |
  | `add_fans <count>` | Add fans |
  | `god` | Toggle god mode |
  | `set_speed <1-8>` | Set game speed |
  | `save [slot]` | Save game |
  | `load <slot>` | Load save |
  | `reset_mods` | Reset all mods |
  | `clear` / `cls` | Clear console |
