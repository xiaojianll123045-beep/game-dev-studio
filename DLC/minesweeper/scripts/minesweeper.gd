extends Node

var gm: Node
var board: Array
var revealed: Array
var flagged: Array
var rows: int = 9
var cols: int = 9
var mine_count: int = 10
var cell_size: int = 32
var offset_x: int = 40
var offset_y: int = 80
var game_over: bool = false
var first_click: bool = true
var flag_mode: bool = false
var timer: float = 0.0
var mines_remaining: int = 10
var started: bool = false
var won: bool = false

var cells: Array = []
var flag_labels: Array = []
var timer_label: Label
var mine_counter: Label
var panel: Panel
var mode_btn: Button

var _pan_x: float = 0.0
var _pan_y: float = 0.0
var _dragging: bool = false
var _drag_start: Vector2 = Vector2()
var _drag_cell_r: int = -1
var _drag_cell_c: int = -1
var _modal_overlay = null
var grid_container: Control
var _init_pw: float = 0
var _init_ph: float = 0
var _init_cs: int = 32
var title_label: Label
var diff_row: HBoxContainer
var close_x_btn: Button
var bottom_bar: HBoxContainer

func OnLoad(_gm, bridge):
	gm = _gm
	StartGame()
	# 全屏遮罩 + IsAnyModalOpen（和游戏内弹窗一致，阻止 3D 场景输入）
	if gm != null:
		var uilayer = gm.get("UiLayer")
		if uilayer != null:
			_modal_overlay = ColorRect.new()
			_modal_overlay.color = Color(0, 0, 0, 0.35)
			_modal_overlay.mouse_filter = Control.MOUSE_FILTER_STOP
			_modal_overlay.set_anchors_and_offsets_preset(15)
			uilayer.add_child(_modal_overlay)
		gm.set("IsAnyModalOpen", true)
	# 用 Timer 代替 _Process，避免暂停影响
	var t = Timer.new()
	t.wait_time = 0.5; t.one_shot = false
	t.timeout.connect(func(): 
		if started and not game_over and not won:
			timer += 0.5
			timer_label.text = "⏱ " + str(int(timer))
	)
	add_child(t)
	t.start()

func StartGame():
	StartNew(9, 9, 10)

func StartNew(nr: int, nc: int, nm: int):
	rows = nr; cols = nc; mine_count = nm
	game_over = false; first_click = true; started = false; won = false
	timer = 0.0; mines_remaining = nm
	flag_mode = false
	board = []
	revealed = []
	flagged = []
	cells = []

	cell_size = clamp(600 / max(rows, cols), 20, 48)
	var pw = cols * cell_size + 80
	var ph = rows * cell_size + 130

	if panel != null:
		for ch in panel.get_children():
			panel.remove_child(ch)
			ch.queue_free()
		panel.queue_free()

	var vp = get_viewport().get_visible_rect().size
	panel = Panel.new()
	panel.anchor_left = 0.5; panel.anchor_top = 0.5
	panel.offset_left = -pw/2; panel.offset_top = -ph/2
	panel.offset_right = pw/2; panel.offset_bottom = ph/2
	add_child(panel)

	panel.mouse_filter = Control.MOUSE_FILTER_STOP
	var bg = StyleBoxFlat.new()
	bg.bg_color = Color(0.85, 0.85, 0.87)
	bg.corner_radius_top_left = 8; bg.corner_radius_top_right = 8
	bg.corner_radius_bottom_left = 8; bg.corner_radius_bottom_right = 8
	panel.add_theme_stylebox_override("panel", bg)

	title_label = Label.new()
	title_label.text = "💣 扫雷"
	title_label.add_theme_font_size_override("font_size", 22)
	title_label.add_theme_color_override("font_color", Color(0.12, 0.14, 0.18))
	title_label.position = Vector2(pw/2 - 80, 8)
	title_label.size = Vector2(160, 30)
	title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	panel.add_child(title_label)

	mine_counter = Label.new()
	mine_counter.text = "💣 " + str(mines_remaining)
	mine_counter.add_theme_font_size_override("font_size", 16)
	mine_counter.add_theme_color_override("font_color", Color(0.8, 0.15, 0.15))
	mine_counter.position = Vector2(10, 45)
	mine_counter.size = Vector2(100, 24)
	panel.add_child(mine_counter)

	timer_label = Label.new()
	timer_label.text = "⏱ 0"
	timer_label.add_theme_font_size_override("font_size", 16)
	timer_label.add_theme_color_override("font_color", Color(0.15, 0.15, 0.8))
	timer_label.position = Vector2(pw - 110, 45)
	timer_label.size = Vector2(100, 24)
	timer_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	panel.add_child(timer_label)

	mode_btn = Button.new()
	mode_btn.text = "⛏ 挖掘"
	mode_btn.flat = true
	mode_btn.add_theme_font_size_override("font_size", 12)
	mode_btn.add_theme_color_override("font_color", Color(0.2, 0.3, 0.6))
	mode_btn.position = Vector2(pw/2 - 40, 72)
	mode_btn.size = Vector2(80, 24)
	mode_btn.pressed.connect(self._ToggleMode)
	panel.add_child(mode_btn)

	diff_row = HBoxContainer.new()
	diff_row.position = Vector2(pw/2 - 140, 44)
	diff_row.size = Vector2(280, 24)

	for diff in [["初级 9×9", 9, 9, 10], ["中级 16×16", 16, 16, 40], ["高级 30×16", 30, 16, 99]]:
		var btn = Button.new()
		btn.text = diff[0]
		btn.flat = true; btn.add_theme_font_size_override("font_size", 10)
		btn.add_theme_color_override("font_color", Color(0.4, 0.4, 0.4))
		var dr = diff[1]; var dc = diff[2]; var dm = diff[3]
		btn.pressed.connect(self._Reset.bind(dr, dc, dm))
		diff_row.add_child(btn)
	panel.add_child(diff_row)

	close_x_btn = Button.new()
	close_x_btn.text = "✕"
	close_x_btn.flat = true
	close_x_btn.add_theme_font_size_override("font_size", 14)
	close_x_btn.add_theme_color_override("font_color", Color(0.8, 0.3, 0.3))
	close_x_btn.position = Vector2(pw - 30, 6)
	close_x_btn.size = Vector2(24, 24)
	close_x_btn.pressed.connect(self._Close)
	panel.add_child(close_x_btn)

	# 雷区容器（裁剪超出部分，保持固定尺寸）
	_init_pw = pw; _init_ph = ph; _init_cs = cell_size
	grid_container = Control.new()
	grid_container.clip_contents = true
	grid_container.mouse_filter = Control.MOUSE_FILTER_PASS
	grid_container.position = Vector2(offset_x, offset_y)
	grid_container.size = Vector2(cols * _init_cs, rows * _init_cs + 38)
	panel.add_child(grid_container)

	for r in range(rows):
		cells.append([])
		for c in range(cols):
			var cr = ColorRect.new()
			cr.color = Color(0.7, 0.75, 0.8)
			cr.size = Vector2(cell_size - 2, cell_size - 2)
			cr.position = Vector2(c * cell_size, r * cell_size)
			var cap_r = r; var cap_c = c
			var gui = Control.new()
			gui.mouse_filter = Control.MOUSE_FILTER_STOP
			gui.gui_input.connect(func(ev): _OnCellInput(ev, cap_r, cap_c))
			gui.size = cr.size
			cr.add_child(gui)
			grid_container.add_child(cr)
			cells[r].append(cr)

	bottom_bar = HBoxContainer.new()
	bottom_bar.position = Vector2(0, rows * cell_size + 6)
	bottom_bar.size = Vector2(cols * cell_size, 32)
	var close_txt = Button.new()
	close_txt.text = "✕ 关闭"
	close_txt.flat = true
	close_txt.add_theme_font_size_override("font_size", 12)
	close_txt.add_theme_color_override("font_color", Color(0.6, 0.2, 0.2))
	close_txt.pressed.connect(self._Close)
	bottom_bar.add_child(close_txt)
	var spacer = Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	bottom_bar.add_child(spacer)
	grid_container.add_child(bottom_bar)

	# 重置平移 & 面板输入
	_pan_x = 0; _pan_y = 0; _dragging = false
	panel.gui_input.connect(_OnPanelInput)

func _ToggleMode():
	flag_mode = not flag_mode
	mode_btn.text = "🚩 标记" if flag_mode else "⛏ 挖掘"
	mode_btn.add_theme_color_override("font_color", Color(0.8, 0.5, 0.1) if flag_mode else Color(0.2, 0.3, 0.6))

func _Reset(r: int, c: int, m: int):
	StartNew(r, c, m)

func _OnPanelInput(ev: InputEvent):
	if game_over or won: return
	if ev is InputEventMouseButton and ev.pressed:
		if ev.button_index == MOUSE_BUTTON_WHEEL_UP:
			_ZoomIn(); get_viewport().set_input_as_handled()
		elif ev.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			_ZoomOut(); get_viewport().set_input_as_handled()
		elif ev.button_index == MOUSE_BUTTON_RIGHT:
			_dragging = true; _drag_start = get_viewport().get_mouse_position()
			_drag_cell_r = -1
			get_viewport().set_input_as_handled()
	elif ev is InputEventMouseButton and not ev.pressed:
		if ev.button_index == MOUSE_BUTTON_RIGHT:
			_dragging = false
	elif ev is InputEventMouseMotion and _dragging:
		var mp = get_viewport().get_mouse_position()
		_pan_x += mp.x - _drag_start.x; _pan_y += mp.y - _drag_start.y
		_drag_start = mp; _RefreshLayout()

func _unhandled_input(ev: InputEvent):
	if ev is InputEventMouseButton and (ev.button_index == MOUSE_BUTTON_RIGHT or ev.button_index == MOUSE_BUTTON_WHEEL_UP or ev.button_index == MOUSE_BUTTON_WHEEL_DOWN):
		if panel != null and panel.visible:
			get_viewport().set_input_as_handled()

func _ZoomIn():
	cell_size = min(cell_size + 4, 64)
	_RefreshLayout()

func _ZoomOut():
	cell_size = max(cell_size - 4, 16)
	_RefreshLayout()

func _RefreshLayout():
	if _init_pw <= 0: return
	# 面板和 UI 固定不动
	panel.offset_left = -_init_pw/2; panel.offset_top = -_init_ph/2
	panel.offset_right = _init_pw/2; panel.offset_bottom = _init_ph/2
	# 裁剪容器固定初始尺寸，超出部分自动隐藏
	grid_container.size = Vector2(cols * _init_cs, rows * _init_cs + 38)
	for r in range(rows):
		for c in range(cols):
			var cr = cells[r][c]
			var sz = cell_size - 2
			cr.size = Vector2(sz, sz)
			cr.position = Vector2(c * cell_size + _pan_x, r * cell_size + _pan_y)
			for ch in cr.get_children():
				if ch is Control:
					ch.size = cr.size
				if ch is Label:
					ch.size = cr.size
					ch.add_theme_font_size_override("font_size", clamp(cell_size - 12, 8, 22))
	if bottom_bar != null:
		bottom_bar.position = Vector2(_pan_x, rows * cell_size + _pan_y + 6)
		bottom_bar.size = Vector2(cols * cell_size, 32)

func _Close():
	if _modal_overlay != null:
		_modal_overlay.queue_free()
		_modal_overlay = null
	if gm != null:
		gm.set("IsAnyModalOpen", false)
	if panel != null:
		panel.queue_free()
		panel = null
	queue_free()

	

func _OnCellInput(ev: InputEvent, r: int, c: int):
	if game_over or won: return
	if ev is InputEventMouseButton:
		if not ev.pressed and ev.button_index == MOUSE_BUTTON_RIGHT:
			if _drag_cell_r >= 0 and not _dragging:
				_ToggleFlag(_drag_cell_r, _drag_cell_c)
			_dragging = false; _drag_cell_r = -1; return
		if ev.pressed:
			if ev.button_index == MOUSE_BUTTON_WHEEL_UP:
				_ZoomIn(); get_viewport().set_input_as_handled(); return
			elif ev.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				_ZoomOut(); get_viewport().set_input_as_handled(); return
			if ev.button_index == MOUSE_BUTTON_LEFT:
				if flag_mode:
					_ToggleFlag(r, c)
				else:
					_RevealCell(r, c)
			elif ev.button_index == MOUSE_BUTTON_RIGHT:
				_drag_cell_r = r; _drag_cell_c = c; _dragging = false
				_drag_start = get_viewport().get_mouse_position()
				get_viewport().set_input_as_handled()
	elif ev is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		if not _dragging:
			_dragging = true
		var mp = get_viewport().get_mouse_position()
		_pan_x += mp.x - _drag_start.x; _pan_y += mp.y - _drag_start.y
		_drag_start = mp; _RefreshLayout()

func _ToggleFlag(r: int, c: int):
	if r < 0 or r >= rows or c < 0 or c >= cols: return
	if first_click:
		_GenerateBoard(r, c)
		first_click = false
		started = true
	if revealed[r][c]: return
	flagged[r][c] = not flagged[r][c]
	_UpdateCellVisual(r, c)
	mines_remaining += -1 if flagged[r][c] else 1
	mine_counter.text = "💣 " + str(mines_remaining)

func _RevealCell(r: int, c: int):
	if r < 0 or r >= rows or c < 0 or c >= cols: return

	if first_click:
		_GenerateBoard(r, c)
		first_click = false
		started = true

	if revealed[r][c] or flagged[r][c]: return

	revealed[r][c] = true

	if board[r][c] == -1:
		_GameOver()
		return

	_UpdateCellVisual(r, c)

	if board[r][c] == 0:
		for dr in [-1, 0, 1]:
			for dc in [-1, 0, 1]:
				if dr == 0 and dc == 0: continue
				_RevealCell(r + dr, c + dc)

	_CheckWin()

func _GenerateBoard(safe_r: int, safe_c: int):
	board = []
	revealed = []
	flagged = []
	for r in range(rows):
		board.append([])
		revealed.append([])
		flagged.append([])
		for c in range(cols):
			board[r].append(0)
			revealed[r].append(false)
			flagged[r].append(false)

	var rng = RandomNumberGenerator.new()
	var placed = 0
	while placed < mine_count:
		var rr = rng.randi_range(0, rows - 1)
		var rc = rng.randi_range(0, cols - 1)
		if board[rr][rc] == -1: continue
		if abs(rr - safe_r) <= 1 and abs(rc - safe_c) <= 1: continue
		board[rr][rc] = -1
		placed += 1

	for r in range(rows):
		for c in range(cols):
			if board[r][c] == -1: continue
			var cnt = 0
			for dr in [-1, 0, 1]:
				for dc in [-1, 0, 1]:
					var nr = r + dr; var nc = c + dc
					if nr >= 0 and nr < rows and nc >= 0 and nc < cols and board[nr][nc] == -1:
						cnt += 1
			board[r][c] = cnt

func _UpdateCellVisual(r: int, c: int):
	var cr = cells[r][c]
	if revealed[r][c]:
		if board[r][c] == -1:
			cr.color = Color(0.9, 0.2, 0.2)
		elif board[r][c] == 0:
			cr.color = Color(0.85, 0.88, 0.9)
		else:
			cr.color = Color(0.8, 0.82, 0.85)
	elif flagged[r][c]:
		cr.color = Color(0.9, 0.5, 0.1)
	else:
		cr.color = Color(0.7, 0.75, 0.8)

	# 移除旧 label
	for ch in cr.get_children():
		if ch is Label:
			cr.remove_child(ch); ch.queue_free()

	if revealed[r][c] and board[r][c] >= 0:
		var lbl = Label.new()
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		lbl.size = cr.size
		lbl.add_theme_font_size_override("font_size", clamp(cell_size - 12, 8, 22))
		cr.add_child(lbl)
		if board[r][c] > 0:
			var nums = [Color(0.2, 0.4, 0.9), Color(0.2, 0.7, 0.2), Color(0.9, 0.2, 0.2),
				Color(0.2, 0.2, 0.8), Color(0.7, 0.1, 0.1), Color(0.2, 0.6, 0.6),
				Color(0.3, 0.3, 0.3), Color(0.5, 0.5, 0.5)]
			lbl.text = str(board[r][c])
			lbl.add_theme_color_override("font_color", nums[board[r][c] - 1])
	elif revealed[r][c] and board[r][c] == -1:
		var lbl = Label.new()
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		lbl.size = cr.size
		lbl.add_theme_font_size_override("font_size", clamp(cell_size - 12, 8, 22))
		lbl.text = "💣"
		lbl.add_theme_color_override("font_color", Color(0, 0, 0))
		cr.add_child(lbl)

func _GameOver():
	game_over = true
	for r in range(rows):
		for c in range(cols):
			if board[r][c] == -1:
				revealed[r][c] = true
				_UpdateCellVisual(r, c)
	var msg = Label.new()
	msg.text = "💥 踩雷了！点难度重开"
	msg.add_theme_font_size_override("font_size", 16)
	msg.add_theme_color_override("font_color", Color(0.9, 0.2, 0.2))
	msg.position = Vector2(_pan_x, rows * cell_size + _pan_y + 10)
	msg.size = Vector2(cols * cell_size, 30)
	msg.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	grid_container.add_child(msg)

func _CheckWin():
	var revealed_count = 0
	for r in range(rows):
		for c in range(cols):
			if revealed[r][c]: revealed_count += 1
	if revealed_count == rows * cols - mine_count:
		won = true
		var msg = Label.new()
		msg.text = "🎉 你赢了！耗时 " + str(int(timer)) + " 秒"
		msg.add_theme_font_size_override("font_size", 16)
		msg.add_theme_color_override("font_color", Color(0.2, 0.7, 0.3))
		msg.position = Vector2(_pan_x, rows * cell_size + _pan_y + 10)
		msg.size = Vector2(cols * cell_size, 30)
		msg.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		grid_container.add_child(msg)
		for r in range(rows):
			for c in range(cols):
				if flagged[r][c] and board[r][c] != -1:
					_ToggleFlag(r, c)

func OnUnload():
	if _modal_overlay != null:
		_modal_overlay.queue_free()
		_modal_overlay = null
	if gm != null:
		gm.set("IsAnyModalOpen", false)
	if panel != null:
		panel.queue_free()
