# Mod 系统已知架构限制

## 1. 脚本 Mod 非真热加载

`ModManager.ApplyScriptMod` 创建新 Node 并设置脚本，但卸载时仅 `QueueFree`，不清除静态状态。
`ModAssemblyLoader.UnloadAll()` 调用 `OnUnload()`，但 .NET 程序集一旦加载无法卸载（.NET 固有限制）。

## 2. 安全扫描易绕过

`DangerPatterns` 仅通过正则匹配检测危险代码，可轻松绕过（如 `OS["execute"]`、字符串拼接）。
没有沙箱或权限系统。

## 3. 依赖循环无优雅降级

拓扑排序检测到循环依赖时仅打印错误，继续加载可能导致非确定性行为。
可选依赖（OptionalDependencies）在排序中的处理路径不明确。

## 4. ModMethodOverride 性能开销

每次调用创建 `Dictionary<string, object>` 和闭包，高频路径（月度结算）可能成为瓶颈。
无缓存机制。

## 5. UI 注入能力有限

JSON 声明的 UI 仅支持基础控件，缺少 `size_flags`、`expand` 等布局约束。
硬坐标（x/y/w/h）在不同分辨率下布局异常。

## 6. Mod 间通信无类型安全

`send_message` 使用 `Godot.Collections.Array` + `Variant`，完全动态，编译时不可检查。
跨语言（GDScript ↔ C#）调用时类型转换易出错。

## 7. ModDataDBs 重复代码

`CrisisModDB`、`BlackSwanModDB`、`TraitModDB`、`EventModDB` 的 JSON 解析逻辑高度相似，可用泛型抽象。

## 8. 无 Mod 更新机制

没有版本检查和自动更新能力。
