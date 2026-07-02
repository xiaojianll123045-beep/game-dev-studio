# ═══════════════════════════════════════════
#  Monthly Money Helper — working GDScript mod example
#  OnLoad(gm, bridge) receives:
#    gm     — GameManager (Node3D)
#    bridge — ModBridge, snake_case API to C#
# ═══════════════════════════════════════════
extends Node

func OnLoad(gm, bridge):
	bridge.log("Money Helper loaded — +¥50k/month")
	# Register a monthly callback via ModAPI (C# static bridge)
	# Note: ModAPI is NOT directly accessible from GDScript.
	# Use bridge methods instead.
	gm.get_node("ModBridge").add_money(50000)

var _key0 = false
func _process(delta):
	if Input.is_key_pressed(KEY_0) and not _key0:
		_key0 = true
		var b = get_node("/root/GameManager/ModBridge")
		if b != null:
			b.add_money(100000)
			b.toast("💰", "+¥100,000 (key 0)")
	elif not Input.is_key_pressed(KEY_0):
		_key0 = false
