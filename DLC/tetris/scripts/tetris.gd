extends Node

var gm: Node
var panel: Panel
var grid_container: Control
var _modal_overlay = null

const COLS = 10
const ROWS = 20
const CELL = 28

var grid: Array = []
var current_piece = null
var next_piece = null
var piece_pos = Vector2()
var piece_rotation = 0
var score = 0
var lines = 0
var level = 1
var game_over = false
var started = false
var paused = false
var drop_timer = 0.0
var drop_interval = 0.5
var preview_cells: Array = []
var score_label: Label
var lines_label: Label
var level_label: Label
var next_label: Label
var pause_overlay: ColorRect = null

var das_timer = 0.0
var das_delay = 0.17
var das_interval = 0.05
var das_dir = 0
var das_initial = true

var pieces = {
	I = {blocks = [Vector2(0,1),Vector2(1,1),Vector2(2,1),Vector2(3,1)], color = Color(0,1,1)},
	O = {blocks = [Vector2(0,0),Vector2(1,0),Vector2(0,1),Vector2(1,1)], color = Color(1,1,0)},
	T = {blocks = [Vector2(0,0),Vector2(1,0),Vector2(2,0),Vector2(1,1)], color = Color(0.5,0,0.5)},
	S = {blocks = [Vector2(1,0),Vector2(2,0),Vector2(0,1),Vector2(1,1)], color = Color(0,1,0)},
	Z = {blocks = [Vector2(0,0),Vector2(1,0),Vector2(1,1),Vector2(2,1)], color = Color(1,0,0)},
	J = {blocks = [Vector2(0,0),Vector2(0,1),Vector2(1,1),Vector2(2,1)], color = Color(0,0,1)},
	L = {blocks = [Vector2(2,0),Vector2(0,1),Vector2(1,1),Vector2(2,1)], color = Color(1,0.5,0)}
}
var piece_order = ["I","O","T","S","Z","J","L"]

func OnLoad(_gm, bridge):
	gm = _gm
	if gm != null:
		var uilayer = gm.get("UiLayer")
		if uilayer != null:
			_modal_overlay = ColorRect.new()
			_modal_overlay.color = Color(0, 0, 0, 0.35)
			_modal_overlay.mouse_filter = Control.MOUSE_FILTER_STOP
			_modal_overlay.set_anchors_and_offsets_preset(15)
			uilayer.add_child(_modal_overlay)
		gm.set("IsAnyModalOpen", true)
	start_game()

func start_game():
	game_over = true  # 先阻止 _process 继续执行旧逻辑
	grid = []
	for r in range(ROWS):
		grid.append([])
		for c in range(COLS):
			grid[r].append(null)
	score = 0; lines = 0; level = 1; started = true; paused = false
	drop_interval = 0.5; drop_timer = 0.0
	current_piece = null; next_piece = null
	_build_ui()
	game_over = false  # 新游戏开始
	_spawn_piece()
	_update_ui()

func _build_ui():
	if panel != null:
		panel.queue_free()
	var pw = COLS * CELL + 180
	var ph = ROWS * CELL + 60
	panel = Panel.new()
	panel.anchor_left = 0.5; panel.anchor_top = 0.5
	panel.offset_left = -pw/2; panel.offset_top = -ph/2
	panel.offset_right = pw/2; panel.offset_bottom = ph/2
	panel.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(panel)
	var bg = StyleBoxFlat.new()
	bg.bg_color = Color(0.12, 0.12, 0.15)
	bg.corner_radius_top_left = 8; bg.corner_radius_top_right = 8
	bg.corner_radius_bottom_left = 8; bg.corner_radius_bottom_right = 8
	panel.add_theme_stylebox_override("panel", bg)

	grid_container = Control.new()
	grid_container.clip_contents = true
	grid_container.mouse_filter = Control.MOUSE_FILTER_PASS
	grid_container.position = Vector2(20, 10)
	grid_container.size = Vector2(COLS * CELL, ROWS * CELL)
	panel.add_child(grid_container)

	for r in range(ROWS):
		preview_cells.append([])
		for c in range(COLS):
			var cr = ColorRect.new()
			cr.color = Color(0.15, 0.15, 0.18)
			cr.size = Vector2(CELL - 1, CELL - 1)
			cr.position = Vector2(c * CELL, r * CELL)
			grid_container.add_child(cr)
			preview_cells[r].append(cr)

	var ox = COLS * CELL + 30
	var info = Label.new()
	info.text = "🎮 俄罗斯方块"
	info.add_theme_font_size_override("font_size", 16)
	info.add_theme_color_override("font_color", Color(0.9, 0.9, 0.95))
	info.position = Vector2(ox, 10)
	info.size = Vector2(140, 24)
	panel.add_child(info)

	next_label = Label.new()
	next_label.text = "下一个"
	next_label.add_theme_font_size_override("font_size", 12)
	next_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.7))
	next_label.position = Vector2(ox, 40)
	next_label.size = Vector2(140, 20)
	panel.add_child(next_label)

	score_label = Label.new()
	score_label.add_theme_font_size_override("font_size", 13)
	score_label.add_theme_color_override("font_color", Color(1, 1, 1))
	score_label.position = Vector2(ox, 80)
	score_label.size = Vector2(140, 20)
	panel.add_child(score_label)

	lines_label = Label.new()
	lines_label.add_theme_font_size_override("font_size", 13)
	lines_label.add_theme_color_override("font_color", Color(1, 1, 1))
	lines_label.position = Vector2(ox, 105)
	lines_label.size = Vector2(140, 20)
	panel.add_child(lines_label)

	level_label = Label.new()
	level_label.add_theme_font_size_override("font_size", 13)
	level_label.add_theme_color_override("font_color", Color(1, 1, 1))
	level_label.position = Vector2(ox, 130)
	level_label.size = Vector2(140, 20)
	panel.add_child(level_label)

	var controls = Label.new()
	controls.text = "← → 移动\n↑ 旋转\n↓ 加速\n空格 硬降\nP 暂停"
	controls.add_theme_font_size_override("font_size", 9)
	controls.add_theme_color_override("font_color", Color(0.4, 0.4, 0.5))
	controls.position = Vector2(ox, 170)
	controls.size = Vector2(140, 100)
	panel.add_child(controls)

	var close_btn = Button.new()
	close_btn.text = "✕"
	close_btn.flat = true
	close_btn.add_theme_font_size_override("font_size", 14)
	close_btn.add_theme_color_override("font_color", Color(0.8, 0.3, 0.3))
	close_btn.position = Vector2(pw - 30, 6)
	close_btn.size = Vector2(24, 24)
	close_btn.pressed.connect(self._close)
	panel.add_child(close_btn)

func _draw_grid():
	for r in range(ROWS):
		for c in range(COLS):
			var color = Color(0.15, 0.15, 0.18)
			if grid[r][c] != null:
				color = grid[r][c]
			elif current_piece != null:
				for b in current_piece.blocks:
					var br = int(piece_pos.y + b.y)
					var bc = int(piece_pos.x + b.x)
					if r == br and c == bc:
						color = current_piece.color
			if preview_cells[r][c] != null and is_instance_valid(preview_cells[r][c]):
				preview_cells[r][c].color = color

func _spawn_piece():
	if next_piece == null:
		next_piece = _random_piece()
	current_piece = next_piece
	next_piece = _random_piece()
	piece_pos = Vector2(3, 0)
	piece_rotation = 0
	if _collides(current_piece.blocks, piece_pos):
		game_over = true
		_draw_grid()
		_update_ui()
		_show_game_over()

func _show_game_over():
	var pw = panel.size.x; var ph = panel.size.y
	var lbl = Label.new()
	lbl.text = "💀 游戏结束"
	lbl.add_theme_font_size_override("font_size", 22)
	lbl.add_theme_color_override("font_color", Color(1, 0.2, 0.2))
	lbl.position = Vector2(pw/2 - 80, ph/2 - 40)
	lbl.size = Vector2(160, 30); lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	panel.add_child(lbl)
	var rst = Button.new()
	rst.text = "↻ 重新开始"
	rst.flat = true
	rst.add_theme_font_size_override("font_size", 14)
	rst.add_theme_color_override("font_color", Color(0.8, 0.8, 0.9))
	rst.position = Vector2(pw/2 - 50, ph/2)
	rst.size = Vector2(100, 28)
	rst.pressed.connect(self.start_game)
	panel.add_child(rst)

func _random_piece():
	var idx = randi() % piece_order.size()
	var p = pieces[piece_order[idx]]
	return {blocks = p.blocks.duplicate(), color = p.color}

func _rotate_blocks(blocks):
	var rotated = []
	for b in blocks:
		rotated.append(Vector2(-b.y, b.x))
	return rotated

func _collides(blocks, pos):
	for b in blocks:
		var bx = int(pos.x + b.x)
		var by = int(pos.y + b.y)
		if bx < 0 or bx >= COLS or by >= ROWS:
			return true
		if by >= 0 and grid[by][bx] != null:
			return true
	return false

func _lock_piece():
	for b in current_piece.blocks:
		var bx = int(piece_pos.x + b.x)
		var by = int(piece_pos.y + b.y)
		if by >= 0 and by < ROWS and bx >= 0 and bx < COLS:
			grid[by][bx] = current_piece.color
	_clear_lines()
	_spawn_piece()
	_draw_grid()
	_update_ui()

func _clear_lines():
	var cleared = 0
	for r in range(ROWS):
		var full = true
		for c in range(COLS):
			if grid[r][c] == null:
				full = false; break
		if full:
			grid.remove_at(r)
			grid.insert(0, [])
			for c in range(COLS):
				grid[0].append(null)
			cleared += 1
	if cleared > 0:
		var pts = [0, 100, 300, 500, 800]
		score += pts[min(cleared, 4)]
		lines += cleared
		level = 1 + lines / 10
		drop_interval = max(0.05, 0.5 - (level - 1) * 0.03)
		_update_ui()

func _move(dx, dy):
	var new_pos = piece_pos + Vector2(dx, dy)
	if not _collides(current_piece.blocks, new_pos):
		piece_pos = new_pos
		_draw_grid()
		return true
	return false

func _rotate():
	var rotated = _rotate_blocks(current_piece.blocks)
	if not _collides(rotated, piece_pos):
		current_piece.blocks = rotated
	else:
		for kick in [Vector2(-1,0), Vector2(1,0), Vector2(0,-1), Vector2(-2,0), Vector2(2,0)]:
			if not _collides(rotated, piece_pos + kick):
				current_piece.blocks = rotated
				piece_pos += kick
				break
	_draw_grid()

func _hard_drop():
	while _move(0, 1):
		pass
	_lock_piece()

func _update_ui():
	score_label.text = "得分: " + str(score)
	lines_label.text = "行数: " + str(lines)
	level_label.text = "级别: " + str(level)

func _process(delta):
	if not started or game_over or paused: return
	drop_timer += delta
	if drop_timer >= drop_interval:
		drop_timer = 0.0
		if not _move(0, 1):
			_lock_piece()
			_draw_grid()
	# DAS (Delayed Auto Shift) — 长按支持
	var left = Input.is_key_pressed(KEY_LEFT)
	var right = Input.is_key_pressed(KEY_RIGHT)
	var down = Input.is_key_pressed(KEY_DOWN)
	if left or right:
		var dir = -1 if left else 1
		if dir != das_dir:
			das_dir = dir; das_timer = 0.0; das_initial = true
			_move(dir, 0)
		else:
			das_timer += delta
			if das_initial and das_timer >= das_delay:
				das_initial = false; das_timer = 0.0
				_move(dir, 0)
			elif not das_initial and das_timer >= das_interval:
				das_timer = 0.0
				_move(dir, 0)
	else:
		das_dir = 0
	if down:
		_move(0, 1)

func _input(ev):
	if game_over:
		if ev is InputEventKey and ev.pressed and not ev.echo and (ev.keycode == KEY_ENTER or ev.keycode == KEY_R):
			start_game()
		return
	if not started or game_over: return
	if ev is InputEventKey and ev.pressed and not ev.echo:
		match ev.keycode:
			KEY_UP: _rotate()
			KEY_SPACE: _hard_drop()
			KEY_P:
				paused = not paused
				if paused:
					var pw = panel.size.x; var ph = panel.size.y
					pause_overlay = ColorRect.new()
					pause_overlay.color = Color(0, 0, 0, 0.6)
					pause_overlay.size = Vector2(pw, ph)
					pause_overlay.mouse_filter = Control.MOUSE_FILTER_STOP
					panel.add_child(pause_overlay)
					var lbl = Label.new()
					lbl.text = "⏸ 暂停\n按 P 继续"
					lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
					lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
					lbl.add_theme_font_size_override("font_size", 24)
					lbl.add_theme_color_override("font_color", Color(1, 1, 1))
					lbl.size = Vector2(pw, ph)
					pause_overlay.add_child(lbl)
				else:
					if pause_overlay != null:
						pause_overlay.queue_free()
						pause_overlay = null
	elif ev is InputEventMouseButton:
		if ev.button_index == MOUSE_BUTTON_WHEEL_UP:
			_rotate()
		elif ev.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			_hard_drop()

func _close():
	if _modal_overlay != null:
		_modal_overlay.queue_free()
		_modal_overlay = null
	if gm != null:
		gm.set("IsAnyModalOpen", false)
	if panel != null:
		panel.queue_free()
		panel = null
	queue_free()

func OnUnload():
	if _modal_overlay != null:
		_modal_overlay.queue_free()
		_modal_overlay = null
	if gm != null:
		gm.set("IsAnyModalOpen", false)
	if panel != null:
		panel.queue_free()
