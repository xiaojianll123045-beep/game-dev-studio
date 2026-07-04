extends Node

var gm = null
var b = null
var use_serene = false
var serene_stream = null
var original_streams = []
var _sound_mgr = null

func OnLoad(game_manager, bridge):
	gm = game_manager; b = bridge
	# 加载哀乐音乐
	var path = "res://DLC/serene_music/assets/funeral_music.mp3"
	if ResourceLoader.exists(path):
		var s = ResourceLoader.load(path)
		if s != null:
			serene_stream = s
			print("[SereneMusic] loaded funeral music")
	else:
		print("[SereneMusic] no funeral music found at ", path)
	# 注册设置项
	b.register_setting("serene_music", "🎵 音乐风格", self._render_setting)

func _render_setting(root, rowH):
	# root 是 VBoxContainer，rowH 是行高
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
	# 找 SoundManager
	if _sound_mgr == null:
		_sound_mgr = gm.get_node("SoundManager")
	if _sound_mgr == null:
		return
	# 根据模式切换 BGM
	if use_serene and serene_stream != null:
		# 切换到哀乐
		_swap_bgm(serene_stream)
	else:
		# 切换回原版
		_restore_bgm()
	# 更新复选框文字
	for ch in get_tree().root.get_children():
		_update_checkboxes()

func _update_checkboxes():
	# 通过场景树找到设置面板的复选框更新文字
	pass  # 复选框更新较复杂，留给用户反馈

func _swap_bgm(new_stream):
	# 保存原版流并切换
	var player = _sound_mgr.get_node("BgmPlayer") if _sound_mgr else null
	if player == null:
		return
	if not player.playing:
		return
	player.stream = new_stream
	player.play()

func _restore_bgm():
	# 重载 SoundManager 重新播放原版
	if _sound_mgr != null and _sound_mgr.has_method("PlayGameBgm"):
		_sound_mgr.PlayGameBgm()

func OnUnload():
	b.unregister_setting("serene_music")
