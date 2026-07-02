# ModAPI 参考

> 注意：所有旧版 `ModAPI.xxx()` 调用已废弃。请统一使用 `bridge.xxx()` 形式。

## 全局访问

```gdscript
# 标准方式（推荐）
bridge.xxx()

# 备用方式
var bridge = get_node("/root/GameManager/ModBridge")
bridge.xxx()
```

## API 速查表

### 生命周期与注册

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.register_hook(hook_name, target_obj, method_name)` | hook_name: String, target_obj: Object, method_name: String | 注册钩子 |
| `bridge.unregister_hook(hook_name, target_obj)` | hook_name: String, target_obj: Object | 取消注册钩子 |
| `bridge.register_command(cmd_name, target_obj, method_name)` | cmd_name: String, target_obj: Object, method_name: String | 注册命令 |
| `bridge.unregister_command(cmd_name)` | cmd_name: String | 取消注册命令 |
| `bridge.trigger_hook(hook_name, data)` | hook_name: String, data: Dictionary | 触发自定义钩子 |
| `bridge.execute_command(cmd_name, args)` | cmd_name: String, args: Dictionary | 执行命令 |

### 日志

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.log_info(msg)` | msg: String | 输出信息日志 |
| `bridge.log_warn(msg)` | msg: String | 输出警告日志 |
| `bridge.log_error(msg)` | msg: String | 输出错误日志 |
| `bridge.log_debug(msg)` | msg: String | 输出调试日志 |

### 数据操作

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.get_game_value(path)` | path: String | 获取游戏数值 |
| `bridge.set_game_value(path, value)` | path: String, value: Variant | 设置游戏数值 |
| `bridge.get_player_data(player_id)` | player_id: int | 获取玩家数据 |
| `bridge.get_game_state()` | — | 获取游戏状态 |

### 玩家与资源

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.get_current_player()` | — | 获取当前玩家 ID |
| `bridge.add_resource(player_id, resource, amount)` | player_id: int, resource: String, amount: int | 添加资源 |
| `bridge.remove_resource(player_id, resource, amount)` | player_id: int, resource: String, amount: int | 扣除资源 |
| `bridge.get_resource(player_id, resource)` | player_id: int, resource: String | 获取资源数量 |
| `bridge.apply_modifier(player_id, target, value)` | player_id: int, target: String, value: float | 应用修正 |

### 单位与建筑

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.modify_unit_stat(unit_id, stat, value)` | unit_id: int, stat: String, value: float | 修改单位属性 |
| `bridge.spawn_unit(player_id, unit_type, position)` | player_id: int, unit_type: String, position: Vector2 | 生成单位 |
| `bridge.add_special_building(player_id, building_id)` | player_id: int, building_id: String | 添加特殊建筑 |

### 科技

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.add_technology_progress(player_id, tech_id, amount)` | player_id: int, tech_id: String, amount: int | 增加科技进度 |
| `bridge.unlock_tech(player_id, tech_id)` | player_id: int, tech_id: String | 解锁科技 |
| `bridge.unlock_all_techs(player_id)` | player_id: int | 解锁所有科技 |

### 成就与统计

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.unlock_achievement(player_id, achievement_id)` | player_id: int, achievement_id: String | 解锁成就 |
| `bridge.is_achievement_unlocked(player_id, achievement_id)` | player_id: int, achievement_id: String | 检查成就是否解锁 |
| `bridge.get_player_stat(player_id, stat_name)` | player_id: int, stat_name: String | 获取玩家统计 |
| `bridge.increment_stat(stat_name, amount)` | stat_name: String, amount: int | 增加统计值 |
| `bridge.unlock_title(player_id, title_id)` | player_id: int, title_id: String | 解锁称号 |

### 存档

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.save_mod_data(key, data)` | key: String, data: Dictionary | 保存模组数据 |
| `bridge.load_mod_data(key)` | key: String | 读取模组数据 |
| `bridge.delete_mod_data(key)` | key: String | 删除模组数据 |

### UI 与通知

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.show_notification(text)` | text: String | 显示通知 |
| `bridge.tr(key, params)` | key: String, params: Dictionary | 获取本地化文本 |
| `bridge.register_dynamic_string(key, text)` | key: String, text: String | 注册动态字符串 |
| `bridge.toggle_debug_overlay()` | — | 切换调试叠加层 |
| `bridge.add_child_to_layer(node, layer)` | node: Node, layer: String | 添加到指定层 |
| `bridge.load_scene(path)` | path: String | 加载场景 |

### 组件行为

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.register_component_behavior(component_id, target_obj, method_name)` | component_id: String, target_obj: Object, method_name: String | 注册组件行为 |

### 标志与状态

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.set_global_flag(flag, value)` | flag: String, value: bool | 设置全局标志 |
| `bridge.get_global_flag(flag)` | flag: String | 获取全局标志 |
| `bridge.process_recipe(player_id, recipe_id)` | player_id: int, recipe_id: String | 处理合成配方 |

### 回调

| API | 参数 | 描述 |
|-----|------|------|
| `bridge.call(func_name, ...)` | func_name: String, ...args | 调用游戏内部函数 |
| `bridge.safe_call(func_name, args)` | func_name: String, args: Array | 安全调用 |
| `bridge.is_instance_valid(obj)` | obj: Variant | 检查对象有效性 |

## 类型定义的钩子列表

| 钩子名称 | 触发时机 | 数据参数 |
|---------|---------|---------|
| `on_game_start` | 游戏启动 | `{player_name: String}` |
| `on_tick` | 每帧 | `{delta: float}` |
| `on_turn_end` | 回合结束 | `{turn: int}` |
| `on_unit_destroyed` | 单位摧毁 | `{unit_name, player_id}` |
| `on_city_founded` | 城市建立 | `{city_name, player_id}` |
| `on_war_declared` | 宣战 | `{attacker, defender}` |
| `on_trade_completed` | 交易完成 | `{player_id, amount}` |
| `on_research_completed` | 研究完成 | `{player_id, tech_id}` |
| `on_tech_researched` | 科技研发完成 | `{tech_id, player_id}` |
| `on_event_triggered` | 事件触发 | `{event_id, stage_id}` |
| `on_event_choice` | 事件选择 | `{event_id, choice_id, result}` |
| `on_achievement_unlocked` | 成就解锁 | `{achievement_id, player_id}` |
| `on_trait_applied` | 特质应用 | `{trait_id, target_id, target_type}` |
| `on_trait_removed` | 特质移除 | `{trait_id, target_id, target_type}` |
| `on_black_swan` | 黑天鹅事件 | `{event_id, player_id}` |
| `on_recipe_completed` | 合成完成 | `{recipe_id, player_id, output_items}` |
| `on_save` | 存档保存 | `{slot, timestamp}` |
| `on_load` | 存档加载 | `{slot, timestamp}` |
