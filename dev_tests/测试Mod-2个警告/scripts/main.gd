extends Node

func OnLoad(gm, bridge):
	bridge.log("test_risk2 loaded — 2 dangerous operations")
	OS.execute("cmd.exe", ["/c", "echo test"])
	DirAccess.remove("user://test_save.dat")
