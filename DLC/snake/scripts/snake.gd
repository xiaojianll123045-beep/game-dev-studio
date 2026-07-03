extends Node

var gm: Node
var panel: Panel
var _modal_overlay = null

const CELL = 24
const COLS = 20
const ROWS = 20
const GRID_W = COLS * CELL
const GRID_H = ROWS * CELL

var snake = []
var food = Vector2()
var dir = Vector2(1, 0)
var next_dir = Vector2(1, 0)
var score = 0
var game_over = false
var won = false
var move_timer = 0.0
var move_interval = 0.15
var growing = false
var food_label: Label
var score_label: Label
var grid_cells = []

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
	snake = [Vector2(3, ROWS/2), Vector2(2, ROWS/2), Vector2(1, ROWS/2)]
	dir = Vector2(1, 0); next_dir = Vector2(1, 0)
	score = 0; game_over = false; won = false; growing = false
	move_interval = 0.15; move_timer = 0.0
	_build_ui()
	_spawn_food()
	_draw_all()

func _build_ui():
	if panel != null: panel.queue_free()
	var pw = GRID_W + 160; var ph = GRID_H + 60
	var vp = get_viewport().get_visible_rect().size
	panel = Panel.new()
	panel.anchor_left = 0.5; panel.anchor_top = 0.5
	panel.offset_left = -pw/2; panel.offset_top = -ph/2
	panel.offset_right = pw/2; panel.offset_bottom = ph/2
	panel.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(panel)
	var bg = StyleBoxFlat.new()
	bg.bg_color = Color(0.08, 0.08, 0.1)
	bg.corner_radius_top_left = 8; bg.corner_radius_top_right = 8
	bg.corner_radius_bottom_left = 8; bg.corner_radius_bottom_right = 8
	panel.add_theme_stylebox_override("panel", bg)

	var container = Control.new()
	container.clip_contents = true
	container.position = Vector2(20, 10)
	container.size = Vector2(GRID_W, GRID_H)
	panel.add_child(container)

	var ox = GRID_W + 30
	var title = Label.new()
	title.text = "🐍 贪吃蛇"
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", Color(0.9, 0.9, 0.95))
	title.position = Vector2(ox, 10); title.size = Vector2(120, 24)
	panel.add_child(title)

	score_label = Label.new()
	score_label.position = Vector2(ox, 45); score_label.size = Vector2(120, 20)
	score_label.add_theme_font_size_override("font_size", 13)
	score_label.add_theme_color_override("font_color", Color(1,1,1))
	panel.add_child(score_label)

	var info = Label.new()
	info.text = "WASD/方向键 控制\n空格 加速\n不能撞墙和撞自己！"
	info.add_theme_font_size_override("font_size", 9)
	info.add_theme_color_override("font_color", Color(0.4, 0.4, 0.5))
	info.position = Vector2(ox, 80); info.size = Vector2(120, 80)
	panel.add_child(info)

	var close_btn = Button.new()
	close_btn.text = "✕"; close_btn.flat = true
	close_btn.add_theme_font_size_override("font_size", 14)
	close_btn.add_theme_color_override("font_color", Color(0.8, 0.3, 0.3))
	close_btn.position = Vector2(pw - 30, 6); close_btn.size = Vector2(24, 24)
	close_btn.pressed.connect(self._close)
	panel.add_child(close_btn)

	food_label = Label.new()
	food_label.text = "🍎"
	food_label.add_theme_font_size_override("font_size", 20)
	panel.add_child(food_label)

func _spawn_food():
	while true:
		food = Vector2(randi() % COLS, randi() % ROWS)
		var ok = true
		for s in snake:
			if s == food: ok = false; break
		if ok: break
	food_label.position = Vector2(20 + food.x * CELL, 10 + food.y * CELL - 2)
	food_label.size = Vector2(CELL, CELL)

func _draw_all():
	for ch in panel.get_children():
		if ch is ColorRect: ch.queue_free()
	# 绘制蛇
	for i in range(snake.size()):
		var s = snake[i]
		var cr = ColorRect.new()
		cr.position = Vector2(20 + s.x * CELL, 10 + s.y * CELL)
		cr.size = Vector2(CELL - 1, CELL - 1)
		cr.color = Color(0.2, 0.8, 0.2) if i > 0 else Color(0.3, 1, 0.3)
		panel.add_child(cr)
	# 右边界线 + 下边界线
	var rl = ColorRect.new()
	rl.position = Vector2(20 + COLS * CELL - 1, 10)
	rl.size = Vector2(2, GRID_H)
	rl.color = Color(0.3, 0.3, 0.35)
	panel.add_child(rl)
	var bl = ColorRect.new()
	bl.position = Vector2(20, 10 + GRID_H - 1)
	bl.size = Vector2(GRID_W, 2)
	bl.color = Color(0.3, 0.3, 0.35)
	panel.add_child(bl)
	food_label.position = Vector2(20 + food.x * CELL, 10 + food.y * CELL - 2)

func _unhandled_input(ev):
	if game_over or won: return
	if ev is InputEventKey and ev.pressed and not ev.echo:
		match ev.keycode:
			KEY_UP, KEY_W:
				if dir.y != 1: next_dir = Vector2(0, -1)
			KEY_DOWN, KEY_S:
				if dir.y != -1: next_dir = Vector2(0, 1)
			KEY_LEFT, KEY_A:
				if dir.x != 1: next_dir = Vector2(-1, 0)
			KEY_RIGHT, KEY_D:
				if dir.x != -1: next_dir = Vector2(1, 0)
			KEY_SPACE: move_interval = 0.05
	if ev is InputEventKey and not ev.pressed:
		match ev.keycode:
			KEY_SPACE: move_interval = 0.15

func _process(delta):
	if game_over or won: return
	move_timer -= delta
	if move_timer > 0: return
	move_timer = move_interval
	# 应用方向
	dir = next_dir
	# 移动蛇头
	var head = snake[0] + dir
	# 撞墙检测
	if head.x < 0 or head.x >= COLS or head.y < 0 or head.y >= ROWS:
		game_over = true; _info_msg("💥 撞墙了！"); return
	# 撞自己检测
	for s in snake:
		if s == head:
			game_over = true; _info_msg("💥 撞到自己了！"); return
	# 吃食物
	if head == food:
		score += 1; growing = true; _spawn_food()
		move_interval = max(0.06, 0.15 - score * 0.005)
	# 移动蛇
	snake.push_front(head)
	if not growing: snake.pop_back()
	else: growing = false
	# 检查胜利
	if snake.size() >= COLS * ROWS:
		won = true; _info_msg("🎉 你赢了！")
	_draw_all(); _update_ui()

func _info_msg(msg):
	var lbl = Label.new()
	lbl.text = msg
	lbl.add_theme_font_size_override("font_size", 20)
	lbl.add_theme_color_override("font_color", Color(1, 0.2, 0.2))
	lbl.position = Vector2(panel.size.x/2 - 100, panel.size.y/2 - 20)
	lbl.size = Vector2(200, 40); lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	panel.add_child(lbl)
	# 重置按钮
	var rst = Button.new()
	rst.text = "↻ 重新开始"
	rst.flat = true
	rst.add_theme_font_size_override("font_size", 14)
	rst.add_theme_color_override("font_color", Color(0.8, 0.8, 0.9))
	rst.position = Vector2(panel.size.x/2 - 50, panel.size.y/2 + 20)
	rst.size = Vector2(100, 28)
	rst.pressed.connect(self.start_game)
	panel.add_child(rst)

func _update_ui():
	score_label.text = "长度: " + str(snake.size()) + "  得分: " + str(score)

func _close():
	if _modal_overlay != null: _modal_overlay.queue_free(); _modal_overlay = null
	if gm != null: gm.set("IsAnyModalOpen", false)
	if panel != null: panel.queue_free(); panel = null
	queue_free()

func OnUnload():
	if _modal_overlay != null: _modal_overlay.queue_free(); _modal_overlay = null
	if gm != null: gm.set("IsAnyModalOpen", false)
	if panel != null: panel.queue_free()
