# mod.json 格式参考

`mod.json` 是每个模组的配置文件，必须放置在模组文件夹的根目录。

## 字段说明

### 基础字段

| 字段 | 类型 | 必填 | 描述 |
|------|------|------|------|
| `id` | String | 是 | 模组唯一标识符，使用小写字母和下划线 |
| `name` | String | 是 | 模组显示名称 |
| `version` | String | 是 | 版本号，遵循 semver 格式 |
| `author` | String | 是 | 作者名称 |
| `description` | String | 否 | 模组描述 |
| `type` | String | 是 | 模组类型：`data`、`script` 或 `language` |

### 脚本模组字段

| 字段 | 类型 | 必填 | 描述 |
|------|------|------|------|
| `entry_point` | String | script 类型必填 | 入口脚本文件路径 |
| `dependencies` | Array | 否 | 依赖的模组 ID 列表 |
| `hooks` | Object | 否 | 注册的钩子列表 |

### 兼容性字段

| 字段 | 类型 | 描述 |
|------|------|------|
| `compatibility.game_version` | String | 兼容的游戏版本范围 |
| `compatibility.platform` | Array | 支持的平台 |

### 元数据字段

| 字段 | 类型 | 描述 |
|------|------|------|
| `icon` | String | 模组图标路径 |
| `screenshots` | Array | 截图路径列表 |
| `tags` | Array | 标签列表 |
| `license` | String | 开源协议 |
| `repository` | String | 源码仓库地址 |

## 完整示例

```json
{
  "id": "advanced_balance_mod",
  "name": "高级平衡调整",
  "version": "2.1.0",
  "author": "ModMaster",
  "description": "全面调整游戏经济与战斗平衡",
  "type": "data",
  "icon": "icon.png",
  "entry_point": "scripts/main.gd",
  "dependencies": [
    "core_lib"
  ],
  "compatibility": {
    "game_version": ">=1.0.0 <2.0.0",
    "platform": ["windows", "linux"]
  },
  "hooks": {
    "on_game_start": "scripts/main.gd"
  },
  "tags": ["balance", "economy", "combat"],
  "license": "MIT",
  "repository": "https://github.com/example/my_mod"
}
```

## 模组类型

| 类型值 | 说明 |
|--------|------|
| `data` | 数据模组 — 修改游戏数值、配置和内容 |
| `script` | 脚本模组 — 添加自定义逻辑和功能 |
| `language` | 语言模组 — 提供翻译和本地化 |

## 注意事项

- `id` 一旦发布不应更改，否则会导致存档兼容性问题
- `dependencies` 中指定的模组会在当前模组之前加载
- `compatibility.game_version` 支持语义化版本范围，如 `>=1.0.0`、`>=1.0.0 <2.0.0`、`~1.2.0`、`^1.0.0`
