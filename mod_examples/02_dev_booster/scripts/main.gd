# ═══════════════════════════════════════════
#  Dev Booster — demonstrates bridge APIs
#  F5: max scores + clear bugs on all projects
#  F6: zero fatigue + max skills on all employees
#  F7: unlock all tech
# ═══════════════════════════════════════════
extends Node

var b = null
var _f5 = false; var _f6 = false; var _f7 = false

func OnLoad(gm, bridge):
	b = bridge
	b.log("Dev Booster loaded — F5/F6/F7")

func _process(delta):
	var b5 = Input.is_key_pressed(KEY_F5)
	var b6 = Input.is_key_pressed(KEY_F6)
	var b7 = Input.is_key_pressed(KEY_F7)

	if b5 and not _f5: _f5 = true; b.max_scores(); b.toast("📊", "All projects maxed")
	elif not b5: _f5 = false

	if b6 and not _f6: _f6 = true; b.zero_fatigue(); b.max_skills(); b.toast("👥", "Employees refreshed")
	elif not b6: _f6 = false

	if b7 and not _f7: _f7 = true; _all_tech()
	elif not b7: _f7 = false

func _all_tech():
	for tid in b.all_tech_ids(): b.unlock_tech(tid)
	b.toast("🔬", "All tech unlocked")
