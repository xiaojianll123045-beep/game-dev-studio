extends Node

var b = null
var use_serene = false
var serene_tracks = []
var _track_idx = 0
var _lock_timer = null
var _original_stream = null
var _menu_stream = null
var _gm = null
var _serene_rounds = 0
var _ach_unlocked = false

func _init_manager(bridge, tracks):
	b = bridge
	serene_tracks = tracks
	_serene_rounds = 0
	var saved = b.get_setting("serene_music", "use_serene", "false")
	use_serene = (saved == "true")
	if use_serene and serene_tracks.size() > 0:
		call_deferred("_try_immediate_override")

func _try_immediate_override():
	if _gm != null:
		_start_serene()
	else:
		# At the menu - try to find MenuMusic and override it
		_override_menu_music()

func _update_state(gm, tracks):
	if gm != _gm:
		_original_stream = null  # Reset when game context changes
		_menu_stream = null
	_gm = gm
	if tracks.size() > 0 and serene_tracks.size() == 0:
		serene_tracks = tracks
	if _gm != null and use_serene and serene_tracks.size() > 0:
		call_deferred("_start_serene")

func _render_setting(root, rowH):
	var hb = HBoxContainer.new()
	hb.add_theme_constant_override("separation", 4)
	hb.custom_minimum_size = Vector2(0, rowH)
	var label = Label.new()
	label.text = "音乐风格"
	label.add_theme_font_size_override("font_size", 10)
	label.add_theme_color_override("font_color", Color(0.1, 0.14, 0.22))
	hb.add_child(label)
	var group = ButtonGroup.new()
	var rb_orig = CheckBox.new()
	rb_orig.text = "安静祥和"
	rb_orig.button_group = group
	rb_orig.add_theme_font_size_override("font_size", 10)
	rb_orig.add_theme_color_override("font_color", Color(0, 0, 0))
	rb_orig.add_theme_color_override("font_hover_color", Color(0.4, 0.4, 0.4))
	rb_orig.add_theme_color_override("font_hover_pressed_color", Color(0.4, 0.4, 0.4))
	rb_orig.add_theme_color_override("font_pressed_color", Color(0, 0, 0))
	rb_orig.toggled.connect(_on_toggled.bind(false))
	hb.add_child(rb_orig)
	var rb_serene = CheckBox.new()
	rb_serene.text = "安详"
	rb_serene.button_group = group
	rb_serene.add_theme_font_size_override("font_size", 10)
	rb_serene.add_theme_color_override("font_color", Color(0, 0, 0))
	rb_serene.add_theme_color_override("font_hover_color", Color(0.4, 0.4, 0.4))
	rb_serene.add_theme_color_override("font_hover_pressed_color", Color(0.4, 0.4, 0.4))
	rb_serene.add_theme_color_override("font_pressed_color", Color(0, 0, 0))
	rb_serene.toggled.connect(_on_toggled.bind(true))
	hb.add_child(rb_serene)
	var spacer = Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hb.add_child(spacer)
	root.add_child(hb)
	rb_orig.button_pressed = not use_serene
	rb_serene.button_pressed = use_serene

func _on_toggled(on, is_serene):
	if not on: return
	use_serene = is_serene
	if b != null:
		b.set_setting("serene_music", "use_serene", "true" if is_serene else "false")
	if is_serene:
		_start_serene()
		if _gm == null:
			_override_menu_music()
	else:
		_stop_serene()

func _start_serene():
	if serene_tracks.size() == 0: return
	if _lock_timer == null:
		_lock_timer = Timer.new()
		_lock_timer.wait_time = 0.5
		_lock_timer.one_shot = false
		_lock_timer.timeout.connect(_force_serene)
		add_child(_lock_timer)
	if is_inside_tree():
		_lock_timer.start()
	_force_serene()

func _stop_serene():
	if _lock_timer != null:
		_lock_timer.stop()
	_lock_timer = null
	_serene_rounds = 0  # Reset counter on disable
	# Restore menu music if at menu
	if _gm == null:
		if _menu_stream != null:
			var tree = Engine.get_main_loop()
			if tree != null:
				var mm = tree.root.find_child("MenuMusic", true, false)
				if mm != null:
					mm.stream = _menu_stream
					if not mm.playing: mm.play()
			_menu_stream = null
		return
	# In-game: restore original BGM
	var sm = _gm.get_node("SoundManager") if _gm else null
	if sm != null:
		var bgm = sm.get_node("BgmPlayer") if sm else null
		if bgm != null and _original_stream != null:
			bgm.stream = _original_stream
		_original_stream = null
		sm.PlayGameBgm()

func _force_serene():
	if not use_serene or _gm == null or serene_tracks.size() == 0: return
	var sm = _gm.get_node("SoundManager") if _gm else null
	if sm == null: return
	var bgm = sm.get_node("BgmPlayer") if sm else null
	if bgm == null: return
	# Detect interruption: BGM was changed or stopped by game
	var interrupted = false
	if bgm.stream != serene_tracks[_track_idx]:
		# Stream was replaced (e.g. by C# _SwapBgm after track finish)
		interrupted = true
	if _original_stream == null:
		# Save original on first call only (before any override)
		if bgm.stream != null:
			_original_stream = bgm.stream
		elif serene_tracks.size() > 0:
			# No original stream yet, set our own
			pass
	# Connect to finished signal for track cycling and round counting (once)
	if not bgm.finished.is_connected(_on_serene_track_finished):
		bgm.finished.connect(_on_serene_track_finished)
	var menu = sm.get_node("MenuPlayer") if sm else null
	if menu != null:
		menu.process_mode = Node.PROCESS_MODE_ALWAYS
		menu.stream = serene_tracks[0]
	bgm.process_mode = Node.PROCESS_MODE_ALWAYS
	bgm.stream = serene_tracks[_track_idx]
	if not bgm.playing:
		# BGM was stopped (e.g. by PlayMenuBgm) → interruption
		interrupted = true
		bgm.play()
	if interrupted:
		_serene_rounds = 0

func _on_serene_track_finished():
	# Track finished naturally → cycle to next and count
	if not use_serene: return
	_track_idx = (_track_idx + 1) % serene_tracks.size()
	_serene_rounds += 1
	# Sync stream immediately (Timer will maintain it)
	var sm = _gm.get_node("SoundManager") if _gm else null
	var bgm = sm.get_node("BgmPlayer") if sm else null
	if bgm != null:
		bgm.stream = serene_tracks[_track_idx]
		if not bgm.playing:
			bgm.play()
	# Check achievement
	if not _ach_unlocked and _serene_rounds >= 5:
		_ach_unlocked = true
		if b != null:
			b.unlock_achievement("easter_send_off")

func _override_menu_music():
	var tree = Engine.get_main_loop()
	if tree == null: return
	var mm = tree.root.find_child("MenuMusic", true, false)
	if mm == null or serene_tracks.size() == 0: return
	if _menu_stream == null:
		_menu_stream = mm.stream
	mm.stream = serene_tracks[0]
	if not mm.playing: mm.play()
