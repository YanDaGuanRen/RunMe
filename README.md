# RunMe 使用手册

## 项目概述
RunMe 是一个 Windows 窗体程序，用于根据启动参数执行文件操作和程序运行。通过智能路径解析和配置驱动的自动化操作，为用户提供灵活高效的程序执行方式。

### 核心功能
- **无参数启动**：自动读取配置文件执行任务
- **参数化操作**：支持list参数动态生成可执行文件列表
- **智能路径转换**：处理相对路径、系统目录快捷方式
- **后台执行**：独立线程运行保持界面响应
- **错误处理**：配置异常静默处理机制

## 安装指南
1. 确保系统已安装 [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472)
2. 将编译生成的 `RunMe.exe` 及相关文件复制到目标目录
3. （可选）将目录添加到系统PATH环境变量

## 快速开始
### 1. 无参数启动模式
```bash
RunMe.exe
```
- 自动按顺序读取以下配置文件：
  - `{程序名}run.txt`（优先级更高）
  - `yanbincfg.ini`
- 逐行执行文件路径或程序命令

### 2. list参数模式
```bash
RunMe.exe list [扩展名] [目录路径]
```
示例：
```bash
RunMe.exe list exe C:\Program Files\
RunMe.exe list bat
```
- 动态生成可滚动文件列表窗体
- 支持双击运行选中文件
- 列表高度自动调整（最大800px）

### 3. 直接路径运行
```bash
RunMe.exe "C:\path\to\file.exe" [参数]
```
示例：
```bash
RunMe.exe pf\Notepad++\notepad++.exe"
RunMe.exe "C:\Tools\script.bat" "arg1 arg2"
```

## 配置说明
### YanBinCfg.ini 配置文件
```ini
[Settings]
# 要执行的文件路径列表
Path1=C:\Program Files\App1\app.exe
Path2=%AppData%\config.xml
Path3=..\relative\path\to\file.bat

# 启动参数（可选）
Args1=--silent
Args2=-config default
```

### run.txt 配置文件
每行包含一个可执行文件路径，支持注释：
```
# 常用工具列表
C:\Tools\7z.exe
C:\Utils\curl.exe
pf\Notepad++\notepad++.exe
```

## 路径解析规则
1. **系统变量解析**：
   - `%pf%` → Program Files
   - `%pf86%` → Program Files (x86)
   - `%AppData%` → 当前用户AppData
2. **相对路径处理**：
   - `..\` 表示上层目录
   - 优先解析相对于程序所在目录
3. **跨磁盘路径**：
   - 自动处理 `C:\path` 或 `D:\another\path` 格式

## 界面交互说明
- **列表模式窗口**：
  - 双击条目：立即执行对应程序
  - 右键菜单：
    - "Run in Background"：后台运行
    - "Show Properties"：查看文件属性
    - "Copy Path"：复制文件路径到剪贴板

## 常见问题
### Q：程序启动后无响应？
A：检查配置文件是否存在或路径是否正确，确保.NET Framework已正确安装

### Q：路径解析失败？
A：避免使用特殊字符（除Unicode路径外），检查系统变量格式是否正确

### Q：如何后台静默运行？
A：使用参数模式执行或在列表界面选择"Run in Background"

## 已知限制
1. 不支持非Unicode特殊字符路径
2. 配置文件不存在时将静默退出
3. 相对路径解析可能存在优先级问题

## 技术支持
如有问题请参考：
- 项目文档：https://github.com/yourname/yourproject/wiki
- 提交Issue：https://github.com/yourname/yourproject/issues
# RunMe 应用程序

## 项目概述
Windows窗体程序，用于根据启动参数执行文件操作和程序运行。

## 主要功能
### 无参数启动模式
1. 自动检测同级目录下的配置文件：
   - 优先读取`{程序名}run.txt`逐行执行
   - 支持INI格式配置文件解析
2. 路径智能转换：
   - 自动处理多层相对路径（`../../target.exe`）
   - 支持系统目录快捷方式：
     * `pf\\` → Program Files
     * `pf86\\` → Program Files (x86)
     * `AppData` → 应用程序数据目录

### list增强功能
- 实时文件系统监控：
  ```csharp
  string[] files = Directory.GetFiles(this.path);
  // 自动刷新文件列表
  ```

### list参数模式
```cmd
runme.exe list [扩展名] [目录路径]
```
- 动态生成可滚动列表窗体
- 支持过滤指定扩展名的文件
- 双击可执行文件项直接运行

### 路径处理特性
- 自动转换相对路径为绝对路径
- 支持跨磁盘搜索（C: 到 G:）
- 智能处理多余反斜杠

### 配置文件示例
`yanbincfg.ini`:
```ini
[Config]
wx=AppData\\Local\\WeChat\\wx.exe
chrome=pf\\Google\\Chrome\\chrome.exe
```

## 参数使用说明
```cmd
# 显示C盘所有EXE文件
runme.exe list exe c:\

# 显示帮助信息
runme.exe help
```

## 配置文件说明
1. `{程序名}run.txt`：每行包含要执行的程序路径
2. `yanbincfg.ini`：
```ini
[Config]
wx=AppData\Local\MyApp\app.exe
```

## 高级功能
### 后台执行机制
- 使用独立线程执行外部程序
- 主界面保持响应

### 错误处理
- 配置文件不存在时静默退出
- 路径解析失败时保留原始路径

## 运行示例
```powershell
# 后台批量执行run.txt配置
Start-Process runme.exe -WindowStyle Hidden

# 开发环境调试
runme.exe list exe bin\\Debug
```

## 注意事项
- 需要.NET Framework 4.7.2运行环境
- 列表窗体支持动态高度调整（最大800px）
- 路径解析顺序：
  1. 绝对路径直接使用
  2. 网络路径保留原样
  3. 特殊前缀路径转换
  4. 相对路径计算
- 支持扩展名类型：exe/bat/msi等可执行格式