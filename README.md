# RunMe

「单 exe 多身份」的 Windows 程序启动器 —— **改名即用**：把 `RunMe.exe` 复制改名为入口名（如 `Vs.exe`），在 `YanBinCfg.ini` 的 `[Config]` 中配置同名键，双击分身即可启动对应程序。

## 亮点

- **改名即用**：一个小 exe 复制成任意多个「入口」
- **单条直达 / 多条列表**：配置一个程序 → 双击直接启动；配置多个 `显示名|目标` → 双击弹出列表选择
- **执行标记**（写在命令开头、顺序任意）：`cmd 命令` / `ps 命令` 换外壳执行；`runadmin` 管理员权限；`show` 显示窗口（默认静默不弹窗）
- **占位符**：`{time.格式}` `{env.变量名}` `{guid.id}` `{random.最小-最大}`、`{0}`（拖拽文件到分身时自动填入）
- **路径前缀**：`pf\`、`pf86\`、`AppData`、`..\`；绝对路径直接运行
- **批量维护**：`runmeth` 把目录内所有分身更新为当前版本；`runmefth` 按 `[Config]` 键批量生成分身

## 文档 & 测试

- [使用手册](docs/README.md)（安装、配置、命令行用法）
- [项目文档](docs/项目文档.md)（运行流程、配置参考、设计说明与修复记录）
- [端到端回归测试](docs/run-tests.ps1)（29 项）

```powershell
# 构建（Release）
msbuild RunMe.sln /p:Configuration=Release

# 回归测试（需先构建）
powershell -NoProfile -ExecutionPolicy Bypass -File docs\run-tests.ps1
```

## 环境

- Windows / .NET Framework 4.7.2
