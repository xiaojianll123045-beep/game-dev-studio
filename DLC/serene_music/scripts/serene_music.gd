extends Node

var b = null
var gm = null
var use_serene = false
var serene_tracks = []
var _track_idx = 0
var _rb_original = null
var _rb_serene = null
var _setting_up = false

func OnLoad(game_manager, bridge):
	gm = game_manager; b = bridge
	for i in range(1, 5):
		var path = "res://DLC/serene_music/assets/%d.m4a" % i
		if ResourceLoader.exists(path):
			var s = ResourceLoader.load(path)
			if s != null:
				serene_tracks.append(s)
	if b == null:
		b = Engine.get_singleton("ModBridge")
	if b != null:
		b.register_setting("serene_music", "🎵 音乐风格", self._render_setting)

func _render_setting(root, rowH):
	var label = Label.new()
	label.text = "音乐风格"
	label.add_theme_font_size_override("font_size", 11)
	label.add_theme_color_override("font_color", Color(0.1, 0.14, 0.22))
	root.add_child(label)
	_setting_up = true
	_rb_original = CheckBox.new()
	_rb_original.text = "安静祥和的音乐"
	_rb_original.button_pressed = not use_serene
	_rb_original.add_theme_font_size_override("font_size", 11)
	_rb_original.add_theme_color_override("font_color", Color(0, 0, 0))
	_rb_original.add_theme_color_override("font_hover_color", Color(0.4, 0.4, 0.4))
	_rb_original.add_theme_color_override("font_pressed_color", Color(0, 0, 0))
	_rb_original.add_theme_color_override("font_focus_color", Color(0, 0, 0))
	_rb_original.toggled.connect(self._on_orig_toggled)
	root.add_child(_rb_original)
	_rb_serene = CheckBox.new()
	_rb_serene.text = "安详的音乐"
	_rb_serene.button_pressed = use_serene
	_rb_serene.add_theme_font_size_override("font_size", 11)
	_rb_serene.add_theme_color_override("font_color", Color(0, 0, 0))
	_rb_serene.add_theme_color_override("font_hover_color", Color(0.4, 0.4, 0.4))
	_rb_serene.add_theme_color_override("font_pressed_color", Color(0, 0, 0))
	_rb_serene.add_theme_color_override("font_focus_color", Color(0, 0, 0))
	_rb_serene.toggled.connect(self._on_serene_toggled)
	root.add_child(_rb_serene)
	_setting_up = false

func _on_orig_toggled(on):
	if _setting_up: return
	_setting_up = true
	if on:
		_rb_serene.button_pressed = false
		_on_toggle(false)
	else:
		_rb_serene.button_pressed = true
	_setting_up = false

func _on_serene_toggled(on):
	if _setting_up: return
	_setting_up = true
	if on:
		_rb_original.button_pressed = false
		_on_toggle(true)
	else:
		_rb_original.button_pressed = true
	_setting_up = false

func _on_toggle(checked):
	use_serene = checked
	# 切换所有 BGM（菜单 + 游戏内）
	var root = get_tree() if gm == null else gm.get_tree()
	if root == null: return
	var sm = root.get_first_node_in_group("bgm_player")
	# 查找所有 AudioStreamPlayer（菜单的和游戏内的）
	var players = []
	var mgr = root.get_first_node_in_group("sound_manager")
	if mgr == null and gm != null:
		mgr = gm.get_node("SoundManager")
	if mgr != null:
		var bgm = mgr.get_node("BgmPlayer") if mgr.has_node("BgmPlayer") else null
		if bgm != null: players.append(bgm)
	# 也找菜单音乐
	var mm = root.get_first_node_in_group("menu_music")
	if mm == null:
		var menu = root.get_first_node_in_group("menu_manager")
		if menu != null:
			mm = menu.get_node("MenuMusic") if menu.has_node("MenuMusic") else null
		if mm != null: players.append(mm)
	# 如果找不到就用场景树扫描 AudioStreamPlayer
	if players.size() == 0:
		for p in get_tree().root.get_children():
			_find_bgm(p, players)
	if players.size() == 0 or serene_tracks.size() == 0: return
	for player in players:
		if use_serene:
			if player.finished.is_connected(_play_next_serene):
				player.finished.disconnect(_play_next_serene)
			if not player.finished.is_connected(_play_next_serene):
				player.finished.connect(_play_next_serene)
			_track_idx = 0
			player.stream = serene_tracks[0]
			player.play()
		else:
			if player.finished.is_connected(_play_next_serene):
				player.finished.disconnect(_play_next_serene)

func _find_bgm(node, list):
	for c in node.get_children():
		if c is AudioStreamPlayer and c.name.begins_with("Bgm") or c.name.begins_with("Menu"):
			list.append(c)
		_find_bgm(c, list)

func _play_next_serene():
	if serene_tracks.size() == 0: return
	var players = []
	# 找到当前播放的 player
	for p in get_tree().root.get_children():
		_find_bgm(p, players)
	for player in players:
		if player.playing:
			_track_idx = (_track_idx + 1) % serene_tracks.size()
			player.stream = serene_tracks[_track_idx]
			player.play()
			return

func OnUnload():
	if b != null:
		b.unregister_setting("serene_music")
