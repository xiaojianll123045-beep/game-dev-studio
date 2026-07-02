extends Node

var gm = null
var b = null   # bridge
var panel = null
var ui = null
var _f3_down = false

func OnLoad(game_manager, bridge):
	gm = game_manager; b = bridge
	print("[CheatMenu] loaded")

func _process(delta):
	var f3 = Input.is_key_pressed(KEY_F3)
	if f3 and not _f3_down:
		_f3_down = true
		if panel == null or not is_instance_valid(panel): _open()
		else: _close()
	elif not f3: _f3_down = false

func _open():
	var vp = get_viewport().get_visible_rect().size
	panel = Panel.new()
	panel.anchor_right = 1.0; panel.anchor_bottom = 1.0
	panel.mouse_filter = Control.MOUSE_FILTER_STOP
	var bg = StyleBoxFlat.new()
	bg.bg_color = Color(0.05, 0.05, 0.08, 0.92)
	bg.corner_radius_top_left = 8; bg.corner_radius_top_right = 8
	panel.add_theme_stylebox_override("panel", bg)
	gm.UiLayer.add_child(panel)
	var sc = ScrollContainer.new()
	sc.anchor_right = 1.0; sc.anchor_bottom = 1.0
	panel.add_child(sc)
	ui = VBoxContainer.new()
	ui.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	ui.add_theme_constant_override("separation", 4)
	sc.add_child(ui)

	_t("✧ CHEAT  [F3]")
	_s(); _h("💰 MONEY")
	_b("+¥1,000,000", "_m1", Color(0.8, 0.9, 0.3))
	_s(); _h("⚡ INSPIRATION")
	_b("Fill", "_i1", Color(0.6, 0.8, 1.0))
	_s(); _h("📊 PROJECTS")
	_b("Fix Bugs", "_clr_bugs", Color(0.3, 0.9, 0.6))
	_b("Max Scores", "_max_scores", Color(0.3, 0.9, 0.6))
	_s(); _h("🔬 TECH")
	_b("Unlock All", "_all_tech", Color(0.9, 0.5, 0.3))
	_s(); _h("👥 EMPLOYEES")
	_b("Zero Fatigue", "_zf", Color(0.7, 0.6, 1.0))
	_b("Max Skills", "_ms", Color(0.7, 0.6, 1.0))
	_s()
	var c = Button.new(); c.text = "✕ CLOSE"
	c.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	c.custom_minimum_size = Vector2(160, 32)
	c.add_theme_font_size_override("font_size", 13)
	c.focus_mode = Control.FOCUS_NONE
	c.pressed.connect(_close); ui.add_child(c)
	ui.add_child(Control.new())

func _close():
	if panel != null and is_instance_valid(panel): panel.queue_free()
	panel = null; ui = null

func _t(t):
	var l = Label.new(); l.text = t; l.add_theme_font_size_override("font_size", 20)
	l.add_theme_color_override("font_color", Color(1, 0.85, 0.3))
	l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	l.custom_minimum_size = Vector2(0, 36); ui.add_child(l)
func _h(t):
	var l = Label.new(); l.text = t; l.add_theme_font_size_override("font_size", 14)
	l.add_theme_color_override("font_color", Color(0.6, 0.8, 1.0, 0.9))
	l.custom_minimum_size = Vector2(0, 24); ui.add_child(l)
func _s():
	var x = ColorRect.new(); x.color = Color(0.4, 0.5, 0.6, 0.15)
	x.custom_minimum_size = Vector2(0, 1); ui.add_child(x)
func _b(t, m, c):
	var x = Button.new(); x.text = t; x.custom_minimum_size = Vector2(200, 28)
	x.add_theme_font_size_override("font_size", 12)
	x.add_theme_color_override("font_color", c)
	x.focus_mode = Control.FOCUS_NONE
	x.pressed.connect(Callable(self, m)); ui.add_child(x)

func _m1(): b.add_money(1000000)
func _i1(): b.add_inspiration(999)
func _clr_bugs(): b.clear_bugs()
func _max_scores(): b.max_scores()
func _all_tech():
	for tid in b.all_tech_ids(): b.unlock_tech(tid)
func _zf(): b.zero_fatigue()
func _ms(): b.max_skills()
