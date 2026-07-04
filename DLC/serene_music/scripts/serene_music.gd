extends Node

var b = null
var gm = null
var dlc_folder = null

func OnLoad(game_manager, bridge):
	gm = game_manager; b = bridge
	if b == null:
		b = Engine.get_singleton("ModBridge")
	
	# Use dlc_folder (passed by DlcManager) to find assets
	var base_path = "res://DLC/serene_music"
	if dlc_folder != null and dlc_folder != "":
		base_path = dlc_folder
	
	# Load serene tracks
	var tracks = []
	for i in range(1, 5):
		var loaded = null
		var path = base_path + "/assets/%d.mp3" % i
		if FileAccess.file_exists(path):
			var file = FileAccess.open(path, FileAccess.READ)
			if file != null:
				var s = AudioStreamMP3.new()
				s.data = file.get_buffer(file.get_length())
				file.close()
				loaded = s
		if loaded != null:
			tracks.append(loaded)
	
	# Get or create the persistent manager node (namespaced to avoid conflicts)
	var mgr_name = b.node_name("serene_music", "manager")
	var mgr = b.get_node_or_null(mgr_name)
	if mgr == null:
		mgr = Node.new()
		mgr.name = mgr_name
		mgr.set_script(load("res://DLC/serene_music/scripts/lib/serene_manager.gd"))
		var tree = Engine.get_main_loop()
		if tree != null and tree.root != null:
			tree.root.call_deferred("add_child", mgr)
		# Only register the setting once (on the persistent node)
		mgr._init_manager(b, tracks)
		b.register_setting("serene_music", "🎵 音乐风格", mgr._render_setting)
	
	# Update manager with current game state
	mgr._update_state(gm, tracks)

func OnUnload():
	if b != null:
		b.unregister_setting("serene_music")
		var mgr = b.get_node_or_null("SereneMusic")
		if mgr != null:
			mgr.queue_free()
