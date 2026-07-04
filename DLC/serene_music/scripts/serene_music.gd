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
	
	# Load serene tracks (WAV with explicit PCM, avoids AudioStreamMP3 decoder issues)
	var tracks = []
	for i in range(1, 5):
		var loaded = null
		var path = base_path + "/assets/%d.wav" % i
		if FileAccess.file_exists(path):
			var file = FileAccess.open(path, FileAccess.READ)
			if file != null:
				var bytes = file.get_buffer(file.get_length())
				file.close()
				# Find "data" chunk to extract raw PCM
				var data_idx = -1
				for j in range(0, min(bytes.size() - 8, 1024)):
					if bytes[j] == 0x64 and bytes[j+1] == 0x61 and bytes[j+2] == 0x74 and bytes[j+3] == 0x61:
						data_idx = j + 8
						break
				if data_idx > 0:
					var s = AudioStreamWAV.new()
					s.format = AudioStreamWAV.FORMAT_16_BITS
					s.mix_rate = 44100
					s.stereo = true
					s.data = bytes.slice(data_idx)
					loaded = s
		if loaded != null:
			tracks.append(loaded)
	
	# Get or create the persistent manager node (namespaced to avoid conflicts)
	var mgr_name = "Mod_serene_music_manager"
	if b != null:
		mgr_name = b.node_name("serene_music", "manager")
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
