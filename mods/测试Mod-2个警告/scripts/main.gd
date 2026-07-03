extends Node

func OnLoad(gm, bridge):
	bridge.log("test_risk2 loaded")
	# 高危操作1：执行外部命令
	OS.execute("cmd.exe", ["/c", "echo test"])
	# 高危操作2：删除文件
	DirAccess.remove("user://test_save.dat")
