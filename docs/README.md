# RunMe 使用手册

RunMe 是一个“单 exe 多身份”的程序启动器，**最大的亮点是改名即用**：把 `RunMe.exe` 改名为你想要的入口名（如 `Vs.exe`），在 `YanBinCfg.ini` 中按这个名字配置好要启动的程序，以后双击 `Vs.exe` 就能启动它。

> 详细运行流程、配置结构与设计说明见 [项目文档.md](项目文档.md)。

## 核心功能

- **改名即用（核心）**：exe 叫什么名字，就执行 `[Config]` 中哪个同名键；一个小 exe 可以复制成任意多个“入口”
- **单条直达 / 多条列表**：配置一个程序 → 双击直接启动；配置多个 `显示名|目标` → 双击弹出列表选择
- **批量维护**：`runmeth` / `runmefth` 统一更新分身、按配置批量生成分身
- **智能路径转换**：支持 `pf\`、`pf86\`、`AppData`、`..\` 等前缀
- **配置异常容错**：配置缺失或非法时跳过，不影响运行

## 安装指南

1. 确保系统已安装 [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472)
2. 将编译生成的 `RunMe.exe` 复制到目标目录（首次运行会自动生成 `YanBinCfg.ini`）
3. （可选）将目录添加到系统 PATH 环境变量，方便命令行调用

## 快速开始：改名即用

### 第 1 步：复制改名

把 `RunMe.exe` 复制一份，改成你想要的入口名，比如 `Vs.exe`、`Firefox.exe`、`Tool.exe`：

```text
Vs.exe        ← 以后双击它启动 Visual Studio
Firefox.exe   ← 以后双击它启动 Firefox
```

### 第 2 步：在 YanBinCfg.ini 中配置同名键

在 `[Config]` 节里为每个分身写同名条目（**exe 叫什么名字，就配哪个键**）：

```ini
[Config]
; 单条：双击 Vs.exe 直接启动
Vs=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe

; 多条：双击 Dev.exe 弹出列表选择（显示名|目标,显示名|目标）
Dev=VS Code|C:\Tools\VSCode\Code.exe,Visual Studio|C:\Program Files\...\devenv.exe

; 路径支持前缀写法
Chrome=pf\Google\Chrome\Application\chrome.exe

; 命令模板：cmd / ps 前缀 + 占位符 + {0} 参数（拖拽文件到分身时填入）
Nug=cmd dotnet nuget push {0} --api-key {env.nugetkey} --source https://api.nuget.org/v3/index.json
```

### 第 3 步：运行分身

双击 `Vs.exe`（或 `Dev.exe`）：

- 配置只有 **1 条** → 直接启动目标程序，不弹窗；
- 配置有 **多条** → 弹出列表窗口（`Enter`/双击启动，`Esc` 取消），列表显示 `显示名`；
- 按住 **`Shift+Enter`** 启动 → 以管理员身份运行。

### 附 1：run.txt 批量启动（可选）

给分身建 `{分身名}run.txt`（如 `Vsrun.txt`），每行一个程序，双击分身按行依次启动（每行间隔 1 秒）：

```text
C:\Tools\7z.exe
pf\Notepad++\notepad++.exe
```

### 附 2：命令行用法（辅助）

日常用不到，需要时可在命令行执行：

- `Vs.exe runme "a|aa.exe,b|bb.exe"`：临时启动/选择，不改配置（单条直接启动，多条弹列表）；
- `Vs.exe list exe C:\Tools`：列出目录中指定后缀的文件供选择（目录可省略，默认 exe 所在目录）；
- `Vs.exe help`：弹出用法帮助；
- `Vs.exe runmeth`：目录中其它 exe 全部被当前 exe 覆盖（统一版本用）；
- `Vs.exe runmefth`：清理其它 exe 后，按配置里的名字批量生成分身副本（已存在的不覆盖）。

## 配置说明

### YanBinCfg.ini 配置文件

```ini
[Settings]
RunParentDirectory=C:\Tools\RunMe\
ExcludeExeName=RunMe|MeRun
; 自定义变量（供 {env.变量名} 引用）
变量名1=值1

[Config]
; 键 = 分身 exe 名（不含 .exe），值 = 单条命令 或 runme 显示名|目标,… 列表
; 例如把 exe 改名为 app.exe，这里就配 app=…
app=AppData\Local\Programs\MyApp\app.exe
zip=pf\7-Zip\7zFM.exe
; 管理员权限：命令中带 runadmin 一词即可（与 cmd/ps 顺序任意）
hosts=cmd runadmin notepad C:\Windows\System32\drivers\etc\hosts
```

**`[Settings]` 说明**：`RunParentDirectory` 为相对路径的基准目录；`ExcludeExeName` **只在 `list`（列目录）模式生效**——按 **exe 文件名**（不含扩展名，`|` 分隔）排除，命中的文件不会出现在列表窗口（默认 `RunMe|MeRun`，用于不显示启动器本体；配置定义的列表不受它影响）。

**支持的占位符**（在所有配置值中生效）：`{time.格式}`、`{env.变量名}`、`{guid.id}`、`{random.最小-最大}`；`{0}{1}…` 在带参数运行分身（如拖拽文件到 exe 上）时自动填入。

**特殊执行前缀**（写在命令开头，可用于配置文件值、列表条目目标、run.txt 行）：`cmd 命令` → `cmd.exe /c`；`ps 命令` / `powershell 命令` → 转 Base64 后以 `-EncodedCommand` 执行；`runadmin` → 以**管理员权限**执行（触发 UAC 确认），位置任意可组合：`runadmin cmd xxx`、`cmd runadmin xxx`；`show` → **显示控制台窗口**（默认**不显示**窗口，加 `show` 才显示，如 `show cmd xxx`、`cmd show xxx`；提权时窗口总会显示）。裸命令（如 `dotnet nuget push …`）无需前缀，直接按系统 PATH 查找执行。

**`runme` 列表标记**（值前面加，后跟一个空格）：带 `runme` 的值是**列表**（如 `vs=runme A|aa.exe,B|bb.exe`），单条直接启动、多条弹列表；不带 `runme` 的值整条按**单条命令**执行（不会因含逗号被拆分）。

### run.txt 配置文件

每行包含一个可执行文件路径，支持注释：

```text
# 常用工具列表
C:\Tools\7z.exe
C:\Utils\curl.exe
pf\Notepad++\notepad++.exe
```

## 路径解析规则

1. **前缀解析**（注意不是环境变量语法）：
   - `pf\...` → 依次在 C:~G: 盘的 `Program Files` 下查找
   - `pf86\...` → 同上，查 `Program Files (x86)`
   - `AppData...` → 当前用户 AppData 的上级目录（`C:\Users\用户名`）
2. **相对路径处理**：
   - `..\` / `../` 表示逐级上溯基准目录（`[Settings] RunParentDirectory`）
   - 其余相对路径与 `[Settings] RunParentDirectory` 拼接；**绝对路径（`X:\...`）直接运行、不拼接**
3. **绝对路径与 URL**：
   - `X:\...` 直接使用；`http...` 原样保留

## 界面交互说明

- **列表模式窗口**：
  - `Enter`：启动选中项；`Shift+Enter`：以管理员身份启动
  - 双击条目：启动选中项
  - `Esc`：关闭窗口（不启动任何程序）
  - 鼠标滚轮：循环切换选中项

## 常见问题

### Q：双击分身（如 Vs.exe）没反应？

A：请检查 `YanBinCfg.ini` 的 `[Config]` 里是否有与 exe **同名**的键（如 `Vs=…`，大小写不敏感）；没有对应配置或配置为空时，程序会静默退出。

### Q：路径解析失败？

A：检查前缀写法（`pf\`、`pf86\`、`AppData`、`..\`），不要使用 `%pf%` 之类的环境变量语法。

### Q：如何静默运行？

A：改名模式（配置单条）本身不显示窗口（只有配置多条或 `list` 模式才弹窗），无需额外参数。

## 已知限制

1. 相对路径基准为 `[Settings] RunParentDirectory`，需保证其配置正确；
2. 占位符要求花括号内含 `.`（如 `{time.yyyy}`、`{env.xxx}`）；
3. `cmd`/`ps` 前缀仅在配置文件值（含列表条目目标）与 run.txt 行中生效，直接作为命令行首词（如 `RunMe.exe cmd dir`）无效；
4. 使用 `cmd` 执行重定向时注意 Windows 命令行特性：`echo 文本 5>文件` 中的 `5` 会被解释为句柄重定向（数字紧贴 `>`），导致文件为空；请在 `>` 前保留非数字文本。

## 相关文档

- 详细运行流程、结构说明与缺陷修复记录：[项目文档.md](项目文档.md)
- 端到端回归测试脚本：[run-tests.ps1](run-tests.ps1)（编译后执行：`powershell -NoProfile -ExecutionPolicy Bypass -File docs\run-tests.ps1`）
