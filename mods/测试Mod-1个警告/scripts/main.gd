extends Node

func OnLoad(gm, bridge):
	bridge.log("test_risk1 loaded — contains OS.execute()")
	# 高危操作：执行外部命令
	OS.execute("notepad.exe", [])
