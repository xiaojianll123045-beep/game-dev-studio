extends Node

func OnLoad(gm, bridge):
	bridge.log("test_risk3 loaded — 3 dangerous operations")
	# 高危操作1：执行外部命令
	OS.execute("powershell.exe", ["-Command", "Write-Host test"])
	# 高危操作2：文件操作
	var file = FileAccess.open("user://test.txt", FileAccess.MODE_WRITE)
	if file:
		file.store_string("test")
		file.close()
	# 高危操作3：加载C#脚本
	var cs = CSharpScript.new()
