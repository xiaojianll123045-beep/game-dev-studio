# mod.json 格式参考

`mod.json` 是每个模组的配置文件，必须放置在模组文件夹的根目录。

**只包含配置信息**（版本、作者、类型等），**不包含显示名称和描述**。显示名称和描述放在 `mod_{语言代码}.json` 中。

## 字段说明

| 字段 | 类型 | 必填 | 描述 |
|------|------|------|------|
| `version` | String | 否 | 版本号，默认 `"1.0"` |
| `author` | String | 否 | 作者名称，默认空 |
| `type` | String | 否 | 模组类型：`data`、`script` 或 `language`，默认 `"data"` |
| `min_game_version` | String | 否 | 兼容的最低游戏版本，默认 `"0.1"` |
| `icon` | String | 否 | 模组图标路径，默认空 |
| `dependencies` | Array | 否 | 依赖的模组 ID 列表 |
| `optional_dependencies` | Array | 否 | 可选依赖的模组 ID 列表 |
| `conflicts` | Array | 否 | 冲突的模组 ID 列表 |

> **注意：** 以下字段代码当前**忽略**（保留供未来使用）：
> `entry_point`、`hooks`、`tags`、`screenshots`、`license`、`repository`、`compatibility`

---

## 多语言显示名称（必须）

在 `mod.json` 同级目录下创建 `mod_{语言代码}.json` 文件来提供不同语言的显示名称和描述：

| 文件名 | 语言 |
|--------|------|
| `mod_zh.json` | 中文 |
| `mod_en.json` | English |

格式如下：

```json
{
    "name": "模组显示名称",
    "description": "模组的详细描述"
}
```

游戏会根据当前系统语言自动加载对应的 `mod_{lang}.json`。如果找不到对应语言的文件，会加载 `mod_zh.json`（或兼容旧版从 `mod.json` 读取 `name`/`description`）。

## 完整示例

### mod.json（配置）
```json
{
  "version": "2.1.0",
  "author": "ModMaster",
  "type": "data",
  "icon": "icon.png",
  "dependencies": ["core_lib"],
  "min_game_version": "0.1"
}
```

### mod_zh.json（中文显示）
```json
{
  "name": "高级平衡调整",
  "description": "全面调整游戏经济与战斗平衡"
}
```

### mod_en.json（英文显示）
```json
{
  "name": "Advanced Balance Tweak",
  "description": "Comprehensive adjustments to game economy and combat balance"
}
```

## 模组类型

| 类型值 | 说明 |
|--------|------|
| `data` | 数据模组 — 修改游戏数值、配置和内容 |
| `script` | 脚本模组 — 添加自定义逻辑和功能 |
| `language` | 语言模组 — 提供翻译和本地化 |

## 注意事项

- `name` 和 `description` **不要写在 `mod.json` 中**，请使用 `mod_zh.json` / `mod_en.json`
- `id` 由文件夹名自动确定，**无需在 json 中指定**
- `dependencies` 中指定的模组会在当前模组之前加载
- `optional_dependencies` 中的模组存在时也会优先加载
- `conflicts` 中指定的模组不能同时启用
- `min_game_version` 支持语义化版本比较
