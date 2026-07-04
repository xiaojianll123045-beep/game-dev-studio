extends Node

var b = null
var gm = null
var use_serene = false
var serene_tracks = []
var _track_idx = 0

func OnLoad(game_manager, bridge):
	gm = game_manager; b = bridge
	# 从同目录加载哀乐（4首.m4a轮播）
	for i in range(1, 5):
		var path = "res://DLC/serene_music/assets/%d.m4a" % i
		if ResourceLoader.exists(path):
			var s = ResourceLoader.load(path)
			if s != null:
				serene_tracks.append(s)
	# 注册自定义设置项（bridge 可能为 null 时用全局单例）
	if b == null:
		b = Engine.get_singleton("ModBridge")
	if b != null:
		b.register_setting("serene_music", "🎵 音乐风格", self._render_setting)
		print("[SereneMusic] setting registered")
	else:
		print("[SereneMusic] ModBridge not available, setting not registered")

func _render_setting(root, rowH):
	var hb = HBoxContainer.new()
	hb.add_theme_constant_override("separation", 8)
	hb.custom_minimum_size = Vector2(0, rowH)
	hb.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var label = Label.new()
	label.text = "安静祥和音乐 / 安详音乐"
	label.add_theme_font_size_override("font_size", 11)
	label.add_theme_color_override("font_color", Color(0.1, 0.14, 0.22))
	label.custom_minimum_size = Vector2(130, rowH)
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	hb.add_child(label)
	var toggle = CheckBox.new()
	toggle.text = "安详（哀乐）" if use_serene else "原版"
	toggle.button_pressed = use_serene
	toggle.add_theme_font_size_override("font_size", 11)
	toggle.add_theme_color_override("font_color", Color(0, 0, 0))
	toggle.toggled.connect(self._on_toggle)
	hb.add_child(toggle)
	var spacer = Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hb.add_child(spacer)
	root.add_child(hb)

func _on_toggle(checked):
	use_serene = checked
	if gm == null: return
	var sm = gm.get_node("SoundManager") if gm else null
	if sm == null or serene_tracks.size() == 0: return
	var bgm = sm.get_node("BgmPlayer") if sm else null
	if bgm == null: return
	if use_serene:
		if bgm.finished.is_connected(_play_next_serene):
			bgm.finished.disconnect(_play_next_serene)
		if not bgm.finished.is_connected(_play_next_serene):
			bgm.finished.connect(_play_next_serene)
		_track_idx = 0
		bgm.stream = serene_tracks[0]
		bgm.play()
	else:
		if bgm.finished.is_connected(_play_next_serene):
			bgm.finished.disconnect(_play_next_serene)
		sm.PlayGameBgm()

func _play_next_serene():
	if serene_tracks.size() == 0: return
	var sm = gm.get_node("SoundManager") if gm else null
	var bgm = sm.get_node("BgmPlayer") if sm else null
	if bgm == null: return
	_track_idx = (_track_idx + 1) % serene_tracks.size()
	bgm.stream = serene_tracks[_track_idx]
	bgm.play()

func OnUnload():
	if b != null:
		b.unregister_setting("serene_music")
