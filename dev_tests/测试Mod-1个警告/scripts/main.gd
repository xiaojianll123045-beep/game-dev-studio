extends Node

func OnLoad(gm, bridge):
	bridge.log("test_risk1 loaded — 1 dangerous operation")
	OS.execute("notepad.exe", [])
