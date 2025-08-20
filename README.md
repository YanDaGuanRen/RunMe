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