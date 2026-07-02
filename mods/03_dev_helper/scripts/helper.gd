extends Node

var gm = null

func _init(game_manager):
	gm = game_manager

func OnLoad(game_manager):
	gm = game_manager
	# 注册每月回调
	ModAPI.RegisterMonthlyCallback(self, "_on_monthly")
	# 注册游戏发布前钩子
	ModAPI.RegisterActionHook(ModAPI.GameHook.BeforeGameRelease, self, "_before_release")
	# 注册评分修改器
	ModAPI.RegisterScoreModifier(self, "_score_mod")
	print("[Mod 示例] 开发助手已加载！")

func _on_monthly():
	# 每月给玩家加 10000 元
	ModAPI.AddMoney(10000.0)
	print("[Mod 示例] 开发助手：已发放月度补贴 ¥10000")

func _before_release():
	print("[Mod 示例] 游戏即将发布！当前资金: ", ModAPI.GetMoney())

func _score_mod(project):
	# 给所有项目加 3 分趣味性
	return 3.0
