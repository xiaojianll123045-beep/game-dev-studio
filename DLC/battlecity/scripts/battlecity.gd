extends Node

var gm: Node
var panel: Panel
var _modal_overlay = null

const CELL = 24
const COLS = 26
const ROWS = 26
const GRID_W = COLS * CELL
const GRID_H = ROWS * CELL

enum Tile { EMPTY, WALL, BRICK, BASE }

var grid = []
var player = null
var enemies = []
var bullets = []
var explosions = []
var score = 0
var wave = 1
var enemies_per_wave = 3
var enemies_spawned = 0
var enemies_killed = 0
var game_over = false
var won = false
var wave_timer = 0.0
var spawn_timer = 0.0
var spawn_interval = 2.0
var base_alive = true

var cells = []
var player_label: Label
var info_label: Label
var score_label: Label
var wave_label: Label
var enemy_labels: Dictionary = {}
var bullet_labels: Dictionary = {}
var bullet_id_counter = 0

var left = false
var right = false
var up = false
var down = false
var shoot_timer = 0.0
var shoot_cooldown = 0.25
var move_timer = 0.0
var move_interval = 0.12

var dirs = {0: Vector2(0,-1), 1: Vector2(1,0), 2: Vector2(0,1), 3: Vector2(-1,0)}
var dir_chars = {0: "▲", 1: "▶", 2: "▼", 3: "◀"}
var bullet_move_timer = 0.0
var bullet_move_interval = 0.08

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
	# 用 Timer 检测空格射击（_process 的 Input.is_key_pressed 可能被暂停影响）
	var st = Timer.new()
	st.wait_time = 0.05; st.one_shot = false
	st.timeout.connect(func(): if not game_over and not won and Input.is_key_pressed(KEY_SPACE):
		_player_shoot())
	add_child(st)
	st.start()
	start_game()

func start_game():
	grid = []
	enemies = []; bullets = []; explosions = []
	score = 0; wave = 0; game_over = false; won = false; base_alive = true
	player = null
	_build_ui()
	next_wave()

func _build_ui():
	if panel != null: panel.queue_free()
	var pw = GRID_W + 160
	var ph = GRID_H + 60
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
	container.mouse_filter = Control.MOUSE_FILTER_PASS
	container.position = Vector2(20, 10)
	container.size = Vector2(GRID_W, GRID_H)
	panel.add_child(container)

	var ox = GRID_W + 30
	var title = Label.new()
	title.text = "🎮 坦克大战"
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", Color(0.9, 0.9, 0.95))
	title.position = Vector2(ox, 10)
	title.size = Vector2(120, 24)
	panel.add_child(title)

	score_label = Label.new()
	score_label.position = Vector2(ox, 45)
	score_label.size = Vector2(120, 20)
	score_label.add_theme_font_size_override("font_size", 13)
	score_label.add_theme_color_override("font_color", Color(1,1,1))
	panel.add_child(score_label)

	wave_label = Label.new()
	wave_label.position = Vector2(ox, 70)
	wave_label.size = Vector2(120, 20)
	wave_label.add_theme_font_size_override("font_size", 13)
	wave_label.add_theme_color_override("font_color", Color(1,1,1))
	panel.add_child(wave_label)

	info_label = Label.new()
	info_label.text = "WASD/方向键 移动\n空格 射击\n▲▶▼◀ 显示朝向\n消灭所有敌军！"
	info_label.add_theme_font_size_override("font_size", 9)
	info_label.add_theme_color_override("font_color", Color(0.4, 0.4, 0.5))
	info_label.position = Vector2(ox, 100)
	info_label.size = Vector2(120, 100)
	panel.add_child(info_label)

	var close_btn = Button.new()
	close_btn.text = "✕"
	close_btn.flat = true
	close_btn.add_theme_font_size_override("font_size", 14)
	close_btn.add_theme_color_override("font_color", Color(0.8, 0.3, 0.3))
	close_btn.position = Vector2(pw - 30, 6)
	close_btn.size = Vector2(24, 24)
	close_btn.pressed.connect(self._close)
	panel.add_child(close_btn)

	player_label = Label.new()
	player_label.add_theme_font_size_override("font_size", 18)
	player_label.add_theme_color_override("font_color", Color(0.9, 0.8, 0.2))
	panel.add_child(player_label)

func _build_map():
	grid = []
	for r in range(ROWS):
		grid.append([])
		for c in range(COLS):
			grid[r].append(Tile.EMPTY)
	# 砖墙布局
	for r in [3, 4, 7, 8, 11, 12, 15, 16, 19, 20]:
		for c in [2,6,10,14,18,22]:
			if r + 0 < ROWS and c + 0 < COLS: grid[r][c] = Tile.BRICK
			if r + 0 < ROWS and c + 1 < COLS: grid[r][c+1] = Tile.BRICK
			if r + 1 < ROWS and c + 0 < COLS: grid[r+1][c] = Tile.BRICK
			if r + 1 < ROWS and c + 1 < COLS: grid[r+1][c+1] = Tile.BRICK
	# 基地 (底部中央)
	var bc = COLS / 2
	grid[ROWS-1][bc-1] = Tile.BASE; grid[ROWS-1][bc] = Tile.BASE; grid[ROWS-1][bc+1] = Tile.BASE
	grid[ROWS-2][bc-1] = Tile.BASE; grid[ROWS-2][bc] = Tile.BASE; grid[ROWS-2][bc+1] = Tile.BASE
	# 基地保护砖墙
	grid[ROWS-3][bc-1] = Tile.BRICK; grid[ROWS-3][bc] = Tile.BRICK; grid[ROWS-3][bc+1] = Tile.BRICK

func _draw_map():
	for ch in panel.get_children():
		if ch is ColorRect and ch != player_label: ch.queue_free()
	for r in range(ROWS):
		for c in range(COLS):
			var t = grid[r][c]
			if t != Tile.EMPTY:
				var cr = ColorRect.new()
				cr.position = Vector2(20 + c * CELL, 10 + r * CELL)
				cr.size = Vector2(CELL, CELL)
				match t:
					Tile.BRICK: cr.color = Color(0.6, 0.3, 0.1)
					Tile.WALL: cr.color = Color(0.4, 0.4, 0.45)
					Tile.BASE:
						cr.color = Color(0.9, 0.8, 0.2)
						cr.position = Vector2(20 + c * CELL, 10 + r * CELL)
						cr.size = Vector2(CELL, CELL)
				panel.add_child(cr)

func _unhandled_input(ev):
	if game_over or won: return
	if ev is InputEventKey and ev.pressed and not ev.echo:
		match ev.keycode:
			KEY_LEFT, KEY_A: left = true
			KEY_RIGHT, KEY_D: right = true
			KEY_UP, KEY_W: up = true
			KEY_DOWN, KEY_S: down = true
			KEY_SPACE: _player_shoot()
	if ev is InputEventKey and not ev.pressed:
		match ev.keycode:
			KEY_LEFT, KEY_A: left = false
			KEY_RIGHT, KEY_D: right = false
			KEY_UP, KEY_W: up = false
			KEY_DOWN, KEY_S: down = false

func next_wave():
	wave += 1
	enemies_per_wave = 2 + wave * 2
	enemies_spawned = 0
	enemies_killed = 0
	_build_map()
	_draw_map()
	player = {x = COLS/2, y = ROWS-4, dir = 0, hp = 3, invincible_timer = 2.0}
	_draw_player()
	_update_ui()

func _process(delta):
	if game_over or won: return
	shoot_timer -= delta
	# 玩家移动（带冷却）
	move_timer -= delta
	if player != null and move_timer <= 0:
		player.invincible_timer -= delta
		var dir = -1
		if up: dir = 0
		elif right: dir = 1
		elif down: dir = 2
		elif left: dir = 3
		if dir >= 0:
			player.dir = dir
			var d = dirs[dir]
			var nx = player.x + d.x
			var ny = player.y + d.y
			if nx >= 0 and nx < COLS and ny >= 0 and ny < ROWS and grid[ny][nx] == Tile.EMPTY:
				# 检查和其他坦克的碰撞
				var blocked = false
				if _tank_at(nx, ny): blocked = true
				if not blocked:
					player.x = nx; player.y = ny
					move_timer = move_interval
			_draw_player()
	# 生成敌人
	spawn_timer -= delta
	if spawn_timer <= 0 and enemies_spawned < enemies_per_wave:
		spawn_timer = spawn_interval
		_spawn_enemy()
	# 敌人 AI
	for e in enemies:
		e.move_timer -= delta
		e.shoot_timer -= delta
		if e.move_timer <= 0:
			e.move_timer = 0.3 + randf() * 0.4
			# 50% 概率改变方向
			if randf() < 0.5:
				e.dir = randi() % 4
			# 移动（只在 move_timer 到期时移动）
			var d = dirs[e.dir]
			var nx = e.x + d.x; var ny = e.y + d.y
			if nx >= 0 and nx < COLS and ny >= 0 and ny < ROWS and grid[ny][nx] == Tile.EMPTY:
				if not _tank_at(nx, ny):
					e.x = nx; e.y = ny
				else:
					e.dir = randi() % 4
			else:
				e.dir = randi() % 4
		# 射击
		if e.shoot_timer <= 0:
			e.shoot_timer = 1.5 + randf() * 1.0
			_fire_bullet(e, false)
		_draw_enemy(e)
	# 子弹更新（带冷却）
	bullet_move_timer -= delta
	var bullet_ready = bullet_move_timer <= 0
	if bullet_ready:
		bullet_move_timer = bullet_move_interval
	var new_bullets = []
	for b in bullets:
		if not bullet_ready: continue
		b.timer -= delta
		if b.timer <= 0: continue
		var d = dirs[b.dir]
		b.x += d.x; b.y += d.y
		# 更新子弹位置
		if bullet_labels.has(b.id):
			bullet_labels[b.id].position = Vector2(20 + b.x * CELL, 10 + b.y * CELL - 4)
		# 边界
		if b.x < 0 or b.x >= COLS or b.y < 0 or b.y >= ROWS:
			if bullet_labels.has(b.id): bullet_labels[b.id].queue_free(); bullet_labels.erase(b.id)
			continue
		# 命中砖墙
		if grid[b.y][b.x] == Tile.BRICK:
			grid[b.y][b.x] = Tile.EMPTY
			_draw_map()
			_add_explosion(b.x, b.y)
			if bullet_labels.has(b.id): bullet_labels[b.id].queue_free(); bullet_labels.erase(b.id)
			continue
		if grid[b.y][b.x] == Tile.BASE:
			base_alive = false; game_over = true
			_add_explosion(b.x, b.y)
			if bullet_labels.has(b.id): bullet_labels[b.id].queue_free(); bullet_labels.erase(b.id)
			_info_msg("💥 基地被摧毁！")
			continue
		# 命中坦克
		if b.is_player_bullet:
			var hit = false
			for e in enemies:
				if abs(e.x - b.x) < 1 and abs(e.y - b.y) < 1:
					if enemy_labels.has(e.id):
						enemy_labels[e.id].queue_free()
						enemy_labels.erase(e.id)
					enemies.erase(e); enemies_killed += 1; score += 100
					_add_explosion(e.x, e.y); hit = true; break
			if hit:
				if bullet_labels.has(b.id): bullet_labels[b.id].queue_free(); bullet_labels.erase(b.id)
				continue
		else:
			if player != null and abs(player.x - b.x) < 1 and abs(player.y - b.y) < 1:
				if player.invincible_timer <= 0:
					player.hp -= 1
					player.invincible_timer = 1.5
					_add_explosion(player.x, player.y)
					if player.hp <= 0:
						player = null; game_over = true
						_info_msg("💥 坦克被摧毁！")
					else:
						_draw_player()
				if bullet_labels.has(b.id): bullet_labels[b.id].queue_free(); bullet_labels.erase(b.id)
				continue
		new_bullets.append(b)
	bullets = new_bullets
	# 清理残留的子弹标签
	var active_ids = []
	for b in bullets: active_ids.append(b.id)
	for bid in bullet_labels.keys():
		if not bid in active_ids:
			bullet_labels[bid].queue_free()
			bullet_labels.erase(bid)
	# 检查胜利
	if enemies_killed >= enemies_per_wave and enemies.size() == 0:
		wave_timer += delta
		if wave_timer > 1.5:
			wave_timer = 0
			next_wave()
	_update_ui()

func _tank_at(x, y):
	if player != null and int(player.x) == int(x) and int(player.y) == int(y): return true
	for e in enemies:
		if int(e.x) == int(x) and int(e.y) == int(y): return true
	return false

func _spawn_enemy():
	var spawns = [Vector2(1,0), Vector2(COLS-2,0), Vector2(COLS/2,0)]
	var s = spawns[randi() % spawns.size()]
	if _tank_at(s.x, s.y): return
	var id = enemies_spawned
	var e = {x = s.x, y = s.y, dir = 2, hp = 1, move_timer = 0.5, shoot_timer = 2.0, id = id}
	enemies.append(e)
	enemies_spawned += 1
	# 创建敌人视觉元素
	var lbl = Label.new()
	lbl.text = "🔻"
	lbl.position = Vector2(20 + e.x * CELL, 10 + e.y * CELL - 4)
	lbl.size = Vector2(CELL, CELL)
	lbl.add_theme_font_size_override("font_size", 20)
	panel.add_child(lbl)
	enemy_labels[id] = lbl

func _player_shoot():
	if player == null or shoot_timer > 0: return
	shoot_timer = shoot_cooldown
	_fire_bullet(player, true)

func _fire_bullet(src, is_player):
	var b = {x = float(src.x), y = float(src.y), dir = src.dir, timer = 2.0, is_player_bullet = is_player, id = bullet_id_counter}
	bullet_id_counter += 1
	var d = dirs[src.dir]
	b.x += d.x; b.y += d.y
	if b.x >= 0 and b.x < COLS and b.y >= 0 and b.y < ROWS:
		bullets.append(b)
		# 子弹视觉
		var lbl = Label.new()
		lbl.text = "●"
		lbl.position = Vector2(20 + b.x * CELL, 10 + b.y * CELL - 4)
		lbl.size = Vector2(CELL, CELL)
		lbl.add_theme_font_size_override("font_size", 14)
		lbl.add_theme_color_override("font_color", Color(1,1,0) if is_player else Color(1,0,0))
		panel.add_child(lbl)
		bullet_labels[b.id] = lbl

func _add_explosion(x, y):
	var e = {x = x, y = y, timer = 0.3, label = null}
	var lbl = Label.new()
	lbl.text = "💥"
	lbl.position = Vector2(20 + x * CELL, 10 + y * CELL)
	lbl.size = Vector2(CELL, CELL)
	lbl.add_theme_font_size_override("font_size", 18)
	panel.add_child(lbl)
	e.label = lbl
	explosions.append(e)

func _draw_player():
	if player == null: return
	var color = Color(0.9, 0.8, 0.2)
	if player.invincible_timer > 0:
		color = Color(1, 1, 1) if int(player.invincible_timer * 4) % 2 == 0 else color
	player_label.text = dir_chars.get(player.dir, "▲") + "❤" + str(player.hp)
	player_label.position = Vector2(20 + player.x * CELL - 4, 10 + player.y * CELL - 4)
	player_label.size = Vector2(CELL + 8, CELL)
	player_label.add_theme_color_override("font_color", color)

func _draw_enemy(e):
	var lbl = enemy_labels.get(e.id)
	if lbl != null:
		lbl.position = Vector2(20 + e.x * CELL, 10 + e.y * CELL - 4)

func _info_msg(msg):
	var lbl = Label.new()
	lbl.text = msg
	lbl.add_theme_font_size_override("font_size", 20)
	lbl.add_theme_color_override("font_color", Color(1, 0.2, 0.2))
	lbl.position = Vector2(panel.size.x/2 - 100, panel.size.y/2 - 20)
	lbl.size = Vector2(200, 40)
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	panel.add_child(lbl)

func _update_ui():
	score_label.text = "得分: " + str(score)
	wave_label.text = "波次: " + str(wave)
	if player != null:
		score_label.text = "得分: " + str(score) + "  ❤x" + str(player.hp)

func _close():
	if _modal_overlay != null: _modal_overlay.queue_free(); _modal_overlay = null
	if gm != null: gm.set("IsAnyModalOpen", false)
	if panel != null: panel.queue_free(); panel = null
	queue_free()

func OnUnload():
	if _modal_overlay != null: _modal_overlay.queue_free(); _modal_overlay = null
	if gm != null: gm.set("IsAnyModalOpen", false)
	if panel != null: panel.queue_free()
