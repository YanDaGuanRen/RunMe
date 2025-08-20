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


## 快速开始
### 1. 无参数启动模式
```bash
RunMe.exe 
```
- 自动按顺序读取以下配置文件：
  - `{程序名}run.txt`（优先级更高）查找程序目录RunMerun.txt文件逐条运行
  - `yanbincfg.ini` 查找config中RunMe的值运行


### 2. list参数模式
```bash
RunMe.exe list [扩展名] [目录路径]
```
示例：
```bash
RunMe.exe list exe C:\Program Files\ 绝对路径
RunMe.exe list bat MakeDown 相对路径
```
- 动态生成可滚动文件列表窗体
- 支持双击运行选中文件
- 列表高度自动调整（最大800px）



## 配置说明

### 配置文件结构解析
1. **文件结构**
   - 配置文件采用标准INI格式，包含`[Settings]`和`[Config]`两个主要节
   - `[Settings]`定义全局设置
   - `[Config]`定义程序快捷方式和具体命令

2. **全局设置说明**
   - `RunParentDirectory`：指定所有相对路径的解析基准目录
   - `ExcludeExeName`：指定需要排除的可执行文件名（不显示在列表中），多个文件名用`|`分隔

3. **路径解析规则**
   - `pf\` → Program Files目录
   - `pf86\` → Program Files (x86)目录
   - `AppData` → 当前用户AppData目录
   - 相对路径解析基准为`RunParentDirectory`配置的目录
   - 支持跨磁盘路径（如`c:\`或`d:\`格式）

4. **命令参数格式**
   - 程序名与配置项匹配：`程序名=显示名1|运行目录或程序1,显示名2|运行目录或程序2,...`
     示例：`RunMe4=runme VS Code|c:\,VS Studio|D:\\`
   - 命令与参数使用竖线`|`分隔
   - 多个命令使用逗号`,`分隔

5. **配置文件读取机制**
   1. 程序启动时调用`CfgInit()`函数初始化配置
   2. 从`[Settings]`节获取`RunParentDirectory`作为路径解析基准
   3. 从`[Config]`节根据**程序名称**（如RunMe4.exe→RunMe4）查找对应的配置项
   4. 解析命令参数格式（命令|参数，逗号分隔多命令）
   5. 执行解析后的命令


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