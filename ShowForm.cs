using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace RunMe
{
    /// <summary>
    /// 主窗体类，用于显示和执行程序列表
    /// </summary>
    public class ShowForm : Form
    {
        #region Init

        /// <summary>
        /// 定义窗体宽度常量，值为402像素
        /// </summary>
        private const int FormWidth = 0x192;

        /// <summary>
        /// 定义列表项高度常量，值为48像素
        /// </summary>
        private const int ItemHeight = 0x30;

        /// <summary>
        /// 定义窗体最大高度常量，值为800像素
        /// </summary>
        private const int MaxFormHeight = 0x320;

        /// <summary>
        /// 存储可执行文件路径的私有字段
        /// </summary>
        private string RunExePath { get; set; }

        private string RunExeName { get; set; }

        private string RunParentDirectory { get; set; }

        private Dictionary<string, string> RunDict { get; set; }
        private string YanBinCfgPath { get; set; }

        private Dictionary<string, Dictionary<string, string>> Config { get; set; }

        /// <summary>
        /// 标识窗体是否应该关闭的布尔字段
        /// </summary>
        private bool IsClose { get; set; }

        /// <summary>
        /// 列表框控件的私有字段
        /// </summary>
        private ListBox _listBox1;

        /// <summary>
        /// 占位符 {random.min-max} 使用的共享随机数生成器
        /// </summary>
        private static readonly Random RandomPicker = new Random();

        /// <summary>
        /// 命令行参数：用于填充配置模板中的 {0}{1}… 占位符（无此场景时为 null）
        /// </summary>
        private string[] _extraArgs;

        #endregion

        #region 窗体方法

        /// <summary>
        /// 初始化窗体
        /// </summary>
        private void FormInit()
        {
            IsClose = true;
            // 暂停窗体布局逻辑
            SuspendLayout();
            // 设置窗体大小
            Size = new Size(FormWidth, ItemHeight);
            // 设置窗体名称
            Name = "ShowForm";
            Text = $@"很牛B的一个程序启动器";
            // 注册窗体加载事件处理程序
            Load += ShowForm_Load;
            // 恢复窗体布局逻辑
            ResumeLayout(false);

            RunDict = new Dictionary<string, string>();
            // 初始化exepath为当前应用程序域的基目录
            RunExePath = AppDomain.CurrentDomain.BaseDirectory;
            // 获取当前可执行文件名并移除扩展名
            RunExeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            // 获取当前目录的上两级目录路径
            YanBinCfgPath = Path.Combine(RunExePath, "YanBinCfg.ini");
        }

        private void NoArgs(string rname = "")
        {
            if (string.IsNullOrEmpty(rname))
            {
                rname = RunExeName;
            }

            var runtxt = Path.Combine(RunExePath, rname + "run.txt");

            if (File.Exists(runtxt))
            {
                RunFileContent(runtxt);
            }
            // 如果存在yanbincfg.ini配置文件
            else
            {
                // 从配置文件中读取与当前程序名对应的值
                string upath = ReadValue("Config", rname);

                if (!string.IsNullOrEmpty(upath))
                {
                    // 列表标记：值以 "runme " 开头即为列表（显示名|目标,…），否则整条按单条命令执行
                    if (upath.StartsWith("runme ", StringComparison.OrdinalIgnoreCase))
                    {
                        RunRunme(upath.Substring(6).TrimStart());
                    }
                    else
                    {
                        WinExec(upath);
                    }
                }
            }
        }

        private void ReplaceAll()
        {
            var filelist = Directory.GetFiles(RunExePath, "*.exe", SearchOption.TopDirectoryOnly);
            var me = Path.Combine(RunExePath, RunExeName + ".exe");
            foreach (var se in filelist)
            {
                if (!se.Equals(me, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(se);
                        File.Copy(me, se);
                    }
                    catch (Exception ex)
                    {
                        // 文件被占用等情况：跳过该文件，不中断
                        Debug.WriteLine("替换失败: " + se + " - " + ex.Message);
                    }
                }
            }
        }

        private void ReplaceAllX()
        {
            var oldlist = Directory.GetFiles(RunExePath, "*.exe", SearchOption.TopDirectoryOnly);
            foreach (var se in oldlist)
            {
                var fileInfo = new FileInfo(se);
                if (!Path.GetFileNameWithoutExtension(fileInfo.Name).Equals(RunExeName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (Exception ex)
                    {
                        // 文件被占用等情况：跳过该文件，不中断
                        Debug.WriteLine("删除失败: " + se + " - " + ex.Message);
                    }
                }
            }

            var me = Path.Combine(RunExePath, RunExeName + ".exe");
            foreach (var s1 in Config)
            {
                if (s1.Key.Equals("Settings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var se in s1.Value)
                {
                    var ddd = se.Key;
                    if (se.Key.Equals(RunExeName, StringComparison.OrdinalIgnoreCase)) continue;
                    var df = Path.Combine(RunExePath, ddd + ".exe");
                    if (!File.Exists(df))
                    {
                        try
                        {
                            File.Copy(me, df);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("创建失败: " + df + " - " + ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 运行文件内容的方法
        /// </summary>
        /// <param name="runFilePath"></param>
        private void RunFileContent(string runFilePath)
        {
            // 使用UTF8编码读取运行文件
            using (StreamReader reader = new StreamReader(runFilePath, Encoding.UTF8))
            {
                string line;
                var first = true;
                // 逐行读取文件内容
                while ((line = reader.ReadLine()) != null)
                {
                    if (!first)
                    {
                        // 行与行之间间隔 1 秒（首行立即执行）
                        Thread.Sleep(1000);
                    }

                    first = false;

                    // 运行当前行指定的程序
                    WinExec(line);
                }
            }
        }
        private void CmdExec(string command, bool runas = false, bool show = false)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    UseShellExecute = runas,
                    CreateNoWindow = !show, // 默认不显示窗口，加 show 标记才显示（提权总会显示）
                    WorkingDirectory = RunExePath
                };
                if (runas) startInfo.Verb = "runas"; // 请求提升权限

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("CMD 启动失败：" + ex.Message);
            }
        }
        private void PowerShellExec(string command, bool runas = false, bool show = false)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            // PowerShell 的 -EncodedCommand 要求使用 UTF-16LE 编码
            var encodedCommand = Convert.ToBase64String(
                Encoding.Unicode.GetBytes(command)
            );

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoLogo -NoProfile -EncodedCommand {encodedCommand}",
                    UseShellExecute = runas,
                    CreateNoWindow = !show, // 默认不显示窗口，加 show 标记才显示（提权总会显示）
                    WorkingDirectory = RunExePath
                };
                if (runas) startInfo.Verb = "runas"; // 请求提升权限

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("PowerShell 启动失败：" + ex.Message);
            }
        }

        private void RunRunme(string args)
        {
            if (string.IsNullOrEmpty(args)) return;

            // 兼容 "runme 名称|目标,…" 写法（剥离列表标记）
            if (args.StartsWith("runme ", StringComparison.OrdinalIgnoreCase))
            {
                args = args.Substring(6).TrimStart();
            }

            foreach (var se in args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var list2 = se.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (list2.Length > 1)
                {
                    RunDict[list2[0]] = ProcessPath(list2[1], RunParentDirectory);
                }
            }

            if (RunDict.Count == 1)
            {
                WinExec(RunDict.First().Value);
            }
            else
            {
                ShowListBox();
            }
        }

        #endregion

        #region 窗体事件

        /// <summary>
        /// 窗体加载事件处理程序
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ShowForm_Load(object sender, EventArgs e)
        {
            // 如果设置了关闭标志
            if (IsClose)
            {
                // 关闭窗体
                Close();
            }
            else
            {
                // 确保ListView在窗体加载时能获取焦点
                _listBox1.Focus();

                // 默认选中第一项
                if (_listBox1.Items.Count > 0)
                {
                    _listBox1.SelectedIndex = 0;
                }
            }
        }

        private void ListBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_listBox1.Items.Count <= 0) return;

            var currentIndex = _listBox1.SelectedIndex;
            if (currentIndex < 0)
            {
                // 没有选中项时先选中第一项
                _listBox1.SelectedIndex = 0;
                return;
            }

            if (e.Delta > 0)
            {
                // 向上滚动
                if (currentIndex <= 0)
                {
                    _listBox1.SelectedIndex = _listBox1.Items.Count - 1;
                }
                else
                {
                    _listBox1.SelectedIndex = currentIndex - 1;
                }
            }
            else if (e.Delta < 0)
            {
                if (currentIndex >= _listBox1.Items.Count - 1)
                {
                    _listBox1.SelectedIndex = 0;
                }
                else
                {
                    _listBox1.SelectedIndex = currentIndex + 1;
                }
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _listBox1.SelectedIndex != -1)
            {
                e.SuppressKeyPress = true; // 防止系统发出提示音
                WinExec(RunDict[_listBox1.SelectedItem.ToString()], (e.Modifiers & Keys.Shift) == Keys.Shift);
                Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true; // 防止系统发出提示音
                Close();
            }
        }

        /// <summary>
        /// 列表框双击事件处理程序
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ListBox1_DoubleClick(object sender, EventArgs e)
        {
            if (_listBox1?.SelectedItem == null)
            {
                return;
            }

            WinExec(RunDict[_listBox1.SelectedItem.ToString()]);
            Close();
        }

        #endregion

        /// <summary>
        /// 构造函数，根据传入的参数初始化窗体
        /// </summary>
        /// <param name="args">命令行参数数组</param>
        public ShowForm(string[] args)
        {
            FormInit();
            CfgInit();
            InitializeConfigCache();
            RunParentDirectory = ReadValue("Settings", "RunParentDirectory");
            if (string.IsNullOrEmpty(RunParentDirectory) ||
                string.IsNullOrEmpty(RunExeName) ||
                string.IsNullOrEmpty(RunExePath)
               ) return;

            // 如果没有传入命令行参数
            if (args.Length == 0)
            {
                NoArgs();
            }
            else if (args[0]?.StartsWith("runmeth", StringComparison.OrdinalIgnoreCase) == true)
            {
                ReplaceAll();
            }
            else if (args[0]?.StartsWith("runmefth", StringComparison.OrdinalIgnoreCase) == true)
            {
                ReplaceAllX();
            }
            else if (args[0]?.Equals("runme", StringComparison.OrdinalIgnoreCase) == true ||
                     args[0]?.StartsWith("runme ", StringComparison.OrdinalIgnoreCase) == true)
            {

                var cmdArgs = args.Skip(1).ToList();
                if (args[0].Length > 5)
                {
                    // 兼容 "runme xxx" 引号整串写法
                    var inline = args[0].Substring(6).Trim();
                    if (inline.Length > 0)
                    {
                        cmdArgs.Insert(0, inline);
                    }
                }

                if (cmdArgs.Count == 0) return;
                RunRunme(string.Join(" ", cmdArgs));
            }
            else if (args[0]?.Equals("list", StringComparison.OrdinalIgnoreCase) == true ||
                     args[0]?.StartsWith("list ", StringComparison.OrdinalIgnoreCase) == true)
            {
                var listArgs = new List<string>();
                if (args[0].Length > 4)
                {
                    listArgs.AddRange(args[0].Substring(5).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                }
                listArgs.AddRange(args.Skip(1));

                // 至少要指定扩展名；目录可省略，默认取程序所在目录
                if (listArgs.Count < 1) return;
                var ext = listArgs[0].TrimStart('.');
                var dir = listArgs.Count > 1 ? listArgs[1] : RunExePath;
                GetFilesList(dir, "." + ext);
                ShowListBox();
            }
            else if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage();
            }
            else if (!string.IsNullOrEmpty(ReadValue("Config", RunExeName)))
            {
                // 分身带参数运行（如拖拽文件到 exe 上）：执行自身配置，参数供 {0} 填充或追加到命令尾部
                _extraArgs = args;
                NoArgs();
            }
            else
            {
                NoArgs(args[0]);
            }
        }

        #region 配置相关

        /// <summary>
        /// 初始化INI文件
        /// </summary>
        private void CfgInit()
        {
            if (File.Exists(YanBinCfgPath))
            {
                return;
            }

            // 生成带注释的默认配置：每个功能点一条示例，注释写在配置上方
            var lines = new List<string>
            {
                "# ================= RunMe 使用说明 ==================",
                "# 改名即用：把 RunMe.exe 改名为入口名（如 Vs.exe），在 [Config] 中配置同名键，",
                "# 双击分身（如 Vs.exe）即可启动对应程序；一个小 exe 可复制成任意多个“入口”。",
                "#",
                "# 占位符（所有配置值中可用）：",
                "#   {time.格式}         当前时间，如 {time.yyyyMMdd}",
                "#   {env.变量名}        引用 [Settings] 中的自定义变量",
                "#   {guid.id}           生成新的 GUID",
                "#   {random.最小-最大}  生成区间内随机整数（支持负数）",
                "#   {0} {1} …           命令行参数；带参数运行分身（如拖拽文件到 exe 上）时自动填入",
                "#",
                "# 执行方式（写在命令开头；runadmin 与 cmd/ps 顺序任意，[Config] 值与列表条目均可用）：",
                "#   cmd 命令            用 CMD 执行（可用重定向、管道等）",
                "#   ps 命令             用 PowerShell 执行",
                "#   runadmin            管理员权限（例：runadmin cmd xxx、cmd runadmin xxx、ps runadmin xxx）",
                "#   show                显示窗口执行（默认不显示窗口；可与 cmd/ps 任意组合，例：show cmd xxx、cmd show xxx）",
                "#   都不加              直接启动；裸命令（dotnet、git、notepad…）按系统 PATH 查找",
                "",
                "[Settings]",
                "# 相对路径的基准目录（一般保持为程序所在目录）",
                $"RunParentDirectory={RunExePath}",
                "# list 模式排除名单（| 分隔、不含扩展名；仅 list 生效，[Config] 列表不受影响）",
                "ExcludeExeName=RunMe|MeRun",
                "# 自定义变量：供 {env.变量名} 引用",
                "apiKey=你的密钥",
                "",
                "[Config]",
                "# 键 = 分身 exe 名（不含 .exe）；值 = 启动目标（路径 / 命令 / 显示名|目标,… 列表）",
                "",
                "# ① 绝对路径：双击 Pg.exe 直接启动",
                "Pg=C:\\Windows\\System32\\notepad.exe",
                "",
                "# ② 相对路径：基于 [Settings] RunParentDirectory 拼接",
                "Rel=Tools\\SomeTool\\tool.exe",
                "",
                "# ③ 路径：绝对路径直接运行；相对路径前缀 pf\\ = C~G 盘 Program Files、pf86\\ = Program Files (x86)、",
                "#    AppData = 用户目录、..\\ = 上一级目录，其余相对路径以 [Settings] RunParentDirectory 为基础拼接",
                "Firefox=pf\\Mozilla Firefox\\firefox.exe",
                "",
                "# ④ 网址与目录：直接打开",
                "Bing=https://www.bing.com",
                "Docs=Docs\\手册",
                "",
                "# ⑤ 裸命令：按系统 PATH 查找（无需加 cmd）",
                "IPConfig=ipconfig",
                "",
                "# ⑥ cmd 前缀：需要 CMD 特性（重定向、管道、start 等）时使用",
                "WinCalc=cmd start \"\" shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
                "",
                "# ⑦ ps 前缀：用 PowerShell 执行",
                "HelloPS=ps echo hello > \"$HOME\\hello.txt\"",
                "",
                "# ⑧ 多条目列表：值以 runme 开头 + 显示名|目标,…（单条直接启动，多条弹列表）",
                "#    列表窗口：Enter 启动 / Shift+Enter 管理员启动 / Esc 关闭",
                "Dev=runme 7-Zip|pf\\7-Zip\\7zFM.exe,Notepad++|pf\\Notepad++\\notepad++.exe",
                "Tools=runme 记事本|notepad,计算器|cmd start calc",
                "",
                "# ⑨ 占位符：{time.*} {env.*} {guid.*} {random.*} 在执行时自动替换",
                "Tmp=cmd echo {time.yyyyMMdd}-{random.1-99} > \"%TEMP%\\{guid.id}.txt\"",
                "",
                "# ⑩ 参数：带参数运行分身（如拖拽文件到 exe 上）时 {0} 自动填入",
                "Deploy=dotnet publish {0} -c Release",
                "",
                "# ⑪ 批量启动：新建 {分身名}run.txt（如 Pgrun.txt），每行一个程序，双击分身按行依次启动",
                "",
                "# ⑫ 管理员权限：命令中带 runadmin 一词即管理员（与 cmd / ps 顺序任意），执行时弹 UAC 确认",
                "Hosts=cmd runadmin notepad C:\\Windows\\System32\\drivers\\etc\\hosts",
                "",
                "# ================= 命令行命令（如 Vs.exe 后跟） ==================",
                "#   help                    显示帮助",
                "#   list 扩展名 [目录]      列出目录中指定后缀的文件供选择（目录缺省为本目录）",
                "#   runme 显示名|目标,…    临时列表（单条直接启动，多条弹列表）",
                "#   runmeth                 目录中其它 exe 全部替换为当前 exe",
                "#   runmefth                按 [Config] 的键批量生成分身（已存在不覆盖）",
                ""
            };

            // 创建默认配置文件
            CreateIniFile(YanBinCfgPath, lines);
        }

        /// <summary>
        /// 创建默认的 YanBinCfg.ini（UTF-8 with BOM）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="lines">文件内容行</param>
        private void CreateIniFile(string filePath, IEnumerable<string> lines)
        {
            try
            {
                File.WriteAllText(filePath, string.Join(Environment.NewLine, lines), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("创建默认配置失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 初始化配置缓存
        /// </summary>
        private void InitializeConfigCache()
        {
            Config = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(YanBinCfgPath))
                {
                    string currentSection = "";
                    Dictionary<string, string> currentSectionDict = null;

                    using (var reader = new StreamReader(YanBinCfgPath, Encoding.UTF8))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                                continue;

                            // 检查是否是节标题，如 [SectionName]
                            if (line.StartsWith("[") && line.EndsWith("]"))
                            {
                                currentSection = line.Substring(1, line.Length - 2);

                                // 创建新的节字典
                                if (!Config.TryGetValue(currentSection, out var value))
                                {
                                    currentSectionDict =
                                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    Config[currentSection] = currentSectionDict;
                                }
                                else
                                {
                                    currentSectionDict = value;
                                }
                            }
                            // 处理键值对（允许空值：用 IndexOf 切分，避免空段异常中断整份解析）
                            else if (line.IndexOf('=') > 0 && !string.IsNullOrEmpty(currentSection))
                            {
                                var eqIndex = line.IndexOf('=');
                                string key = line.Substring(0, eqIndex).Trim();
                                string value = line.Substring(eqIndex + 1).Trim();

                                // 确保当前节字典存在
                                if (currentSectionDict == null)
                                {
                                    currentSectionDict =
                                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    Config[currentSection] = currentSectionDict;
                                }

                                currentSectionDict[key] = value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("配置解析失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 从INI配置文件中读取指定节和键的值
        /// </summary>
        /// <param name="section">节名称</param>
        /// <param name="key">键名称</param>
        /// <returns>读取到的值</returns>
        private string ReadValue(string section, string key)
        {
            if (Config.TryGetValue(section, out var ddic))
            {
                if (ddic.TryGetValue(key, out var sett))
                {
                    return sett;
                }
            }
            return null;
        }

        #endregion

        #region 功能

        /// <summary>
        /// 填充占位符
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string ProcessPlaceholders(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 使用正则表达式查找所有形如 {XXXXX.YYYY} 的占位符（必须包含一个点）
            var regex = new Regex(@"\{([^}]+\.[^}]+)\}");
            return regex.Replace(input, match =>
            {
                var content = match.Groups[1].Value;

                // 处理 time. 开头的占位符
                if (content.StartsWith("time.", StringComparison.OrdinalIgnoreCase))
                {
                    var format = content.Substring(5);
                    try
                    {
                        return DateTime.Now.ToString(format);
                    }
                    catch
                    {
                        // 格式错误时返回原占位符
                        return match.Value;
                    }
                }

                // 处理环境变量
                if (content.StartsWith("env.", StringComparison.OrdinalIgnoreCase))
                {
                    var envVar = content.Substring(4);
                    return ReadValue("Settings", envVar) ?? "";
                }

                // 处理 GUID 生成（{guid.后缀}，后缀忽略）
                if (content.StartsWith("guid.", StringComparison.OrdinalIgnoreCase))
                {
                    return Guid.NewGuid().ToString();
                }

                // 处理随机数 {random.min-max}（支持负数；最小大于最大时自动交换）
                if (content.StartsWith("random.", StringComparison.OrdinalIgnoreCase))
                {
                    var randomMatch = Regex.Match(content.Substring(7), @"^(-?\d+)-(-?\d+)$");
                    if (randomMatch.Success)
                    {
                        var min = int.Parse(randomMatch.Groups[1].Value);
                        var max = int.Parse(randomMatch.Groups[2].Value);
                        if (min > max)
                        {
                            var tmp = min;
                            min = max;
                            max = tmp;
                        }

                        lock (RandomPicker)
                        {
                            return RandomPicker.Next(min, max + 1).ToString();
                        }
                    }
                }

                // 如果没有匹配的处理方式，返回原占位符
                return match.Value;
            });
        }

        /// <summary>
        /// 处理路径字符串的方法
        /// </summary>
        /// <param name="upath">原始路径</param>
        /// <param name="parentDirectory">父目录路径</param>
        /// <returns>处理后的完整路径</returns>
        private string ProcessPath(string upath, string parentDirectory)
        {
            // 如果路径为空或null，直接返回
            if (string.IsNullOrEmpty(upath))
            {
                return upath;
            }


            var t = ReadValue("Config", upath);
            if (!string.IsNullOrEmpty(t))
            {
                upath = t;
            }


            if (upath.Length > 3 && upath.Substring(1, 2) == ":\\")
            {
                return upath;
            }

            // cmd / ps / powershell / runadmin / show 标记开头的命令不做路径解析
            if (upath.StartsWith("cmd ", StringComparison.OrdinalIgnoreCase) ||
                upath.StartsWith("ps ", StringComparison.OrdinalIgnoreCase) ||
                upath.StartsWith("powershell ", StringComparison.OrdinalIgnoreCase) ||
                upath.StartsWith("runadmin ", StringComparison.OrdinalIgnoreCase) ||
                upath.StartsWith("show ", StringComparison.OrdinalIgnoreCase))
            {
                return upath;
            }

            // 无路径分隔符且无扩展名的裸命令词（如 dotnet、git、notepad）交给系统按 PATH 查找，不做路径拼接
            if (upath.IndexOfAny(new[] { '\\', '/', ':' }) < 0 &&
                string.IsNullOrEmpty(Path.GetExtension(upath)))
            {
                return upath;
            }

            if (upath.StartsWith("http"))
            {
                return upath;
            }

            // 如果路径以反斜杠开头，移除第一个字符
            if (upath[0] == '\\')
            {
                upath = upath.Substring(1);
            }

            // 如果路径以"AppData"开头
            if (upath.StartsWith("AppData"))
            {
                // 移除"AppData"部分
                upath = upath.Substring(7);
                // 获取应用程序数据目录的父目录
                parentDirectory = Directory
                    .GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
                    ?.FullName;
            }
            // 如果路径以"..\"开头
            else if (upath.StartsWith("..\\")) // 处理相对路径
            {
                // 循环处理所有"..\"
                while (upath.StartsWith("..\\"))
                {
                    // 获取父目录的父目录
                    parentDirectory = Directory.GetParent(parentDirectory)?.FullName;
                    // 移除路径中的"..\"
                    upath = upath.Substring(3);
                }
            }
            // 如果路径以"../"开头
            else if (upath.StartsWith("../"))
            {
                // 循环处理所有"../"
                while (upath.StartsWith("../"))
                {
                    // 获取父目录的父目录
                    parentDirectory = Directory.GetParent(parentDirectory).FullName;
                    // 移除路径中的"../"
                    upath = upath.Substring(3);
                }
            }
            // 如果路径以"pf\\"开头（Program Files的缩写）
            else if (upath.StartsWith("pf\\"))
            {
                // 移除"pf\\"部分
                upath = upath.Substring(3);
                // 遍历C到G盘符
                for (char c = 'C'; c <= 'G'; c++)
                {
                    // 构造Program Files目录路径
                    parentDirectory = c + ":\\Program Files\\";
                    // 如果文件存在于该路径下
                    if (File.Exists(Path.Combine(parentDirectory, upath)))
                    {
                        // 构造完整路径并跳出循环
                        upath = Path.Combine(parentDirectory, upath);
                        break;
                    }
                }
            }
            // 如果路径以"pf86\\"开头（Program Files (x86)的缩写）
            else if (upath.StartsWith("pf86\\"))
            {
                // 移除"pf86\\"部分
                upath = upath.Substring(5);
                // 遍历C到G盘符
                for (char c = 'C'; c <= 'G'; c++)
                {
                    // 构造Program Files (x86)目录路径
                    parentDirectory = c + ":\\Program Files (x86)\\";
                    // 如果文件存在于该路径下
                    if (File.Exists(Path.Combine(parentDirectory, upath)))
                    {
                        // 构造完整路径并跳出循环
                        upath = Path.Combine(parentDirectory, upath);
                        break;
                    }
                }
            }

            // 如果路径的第2、3个字符不是":\"且不以"http://"开头
            if (upath.Length > 3 && upath.Substring(1, 2) != ":\\")
            {
                // 循环移除路径开头的反斜杠
                while (upath[0] == '\\')
                {
                    upath = upath.Substring(1);
                }

                // 组合父目录和路径
                upath = Path.Combine(parentDirectory, upath);
            }

            // 返回处理后的路径
            return upath;
        }

        /// <summary>
        /// 分离出程序与参数
        /// </summary>
        /// <param name="input"></param>
        /// <param name="findChar"></param>
        /// <returns></returns>
        private (string beforeSpace, string afterProcessing) ProcessString(string input, bool findChar = true)
        {
            // 处理空输入
            if (string.IsNullOrEmpty(input))
                return (input, string.Empty);

            // 如果第一个字符是双引号，寻找与之匹配的双引号
            if (input[0] == '"')
            {
                for (int i = 1; i < input.Length; i++)
                {
                    if (input[i] == '"')
                    {
                        // 找到匹配的双引号
                        string beforeSpace = input.Substring(1, i - 1); // 去掉首尾的双引号
                        string afterProcessing = input.Substring(i + 1).TrimStart(); // 取后面的内容并去掉前导空格
                        return (beforeSpace, afterProcessing);
                    }
                }

                // 如果没有找到匹配的双引号，返回整个字符串（去掉第一个双引号）
                return (input.Substring(1), string.Empty);
            }
            else
            {
                if (findChar)
                {
                    // 如果第一个字符不是双引号，寻找第一个空格
                    for (int i = 0; i < input.Length; i++)
                    {
                        if (input[i] == ' ')
                        {
                            string beforeSpace = input.Substring(0, i);
                            string afterProcessing = input.Substring(i + 1).TrimStart(); // 去掉前导空格
                            return (beforeSpace, afterProcessing);
                        }
                    }
                }
                else
                {
                    // 查找".exe "来分离程序路径和参数
                    int exeEndIndex = input.IndexOf(".exe ", StringComparison.OrdinalIgnoreCase);
                    if (exeEndIndex >= 0)
                    {
                        // 找到".exe "，分离路径和参数
                        string exePath = input.Substring(0, exeEndIndex + 4); // +4包括.exe
                        string arguments = input.Substring(exeEndIndex + 5).TrimStart(); // +5跳过".exe "
                        return (exePath, arguments);
                    }

                    // 兜底：首个词是不带路径分隔符的裸命令词（如 dotnet、git）时，按首个空格分离
                    int spaceIndex = input.IndexOf(' ');
                    if (spaceIndex > 0)
                    {
                        var head = input.Substring(0, spaceIndex);
                        if (head.IndexOfAny(new[] { '\\', '/', ':' }) < 0)
                        {
                            return (head, input.Substring(spaceIndex + 1).TrimStart());
                        }
                    }
                }

                // 如果没有找到空格
                return (input, string.Empty);
            }
        }

        /// <summary>
        /// 取占位符数
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private int GetFormatParameterCount(string format)
        {
            if (string.IsNullOrEmpty(format))
                return 0;

            var regex = new Regex(@"\{(\d+)\}");
            var matches = regex.Matches(format);

            if (matches.Count == 0)
                return 0;

            int maxIndex = 0;
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int index) && index > maxIndex)
                {
                    maxIndex = index;
                }
            }

            return maxIndex + 1;
        }

        /// <summary>
        /// 取窗口中列表高度
        /// </summary>
        /// <returns></returns>
        private int GetListheight()
        {
            if (RunDict.Count > 0)
            {
                var height = ItemHeight + ItemHeight * RunDict.Count;
                return height > MaxFormHeight ? MaxFormHeight : height;
            }

            return ItemHeight;
        }

        /// <summary>
        /// 取指定目录指定后缀列表
        /// </summary>
        /// <param name="path"></param>
        /// <param name="suffix"></param>
        private void GetFilesList(string path, string suffix)
        {
            // 目录不存在时给出提示并返回，避免未处理异常导致程序崩溃
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                MessageBox.Show($@"目录不存在: {path}");
                return;
            }

            var list = ReadValue("Settings", "ExcludeExeName")
                ?.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (list == null || list.Length < 1 || string.IsNullOrEmpty(list[0]))
            {
                list = new[] { "RunMe", "MeRun" };
            }

            try
            {
                // 遍历所有文件
                foreach (string file in Directory.GetFiles(path))
                {
                    // 创建文件信息对象
                    FileInfo info = new FileInfo(file);
                    // 如果文件扩展名匹配且文件名不在排除名单中（忽略大小写）
                    var name = Path.GetFileNameWithoutExtension(info.Name);
                    if (info.Extension.Equals(suffix, StringComparison.OrdinalIgnoreCase) &&
                        !list.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        RunDict[name] = info.FullName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"读取目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示窗口程序列表
        /// </summary>
        private void ShowListBox()
        {
            if (RunDict.Count < 1)
            {
                return;
            }

            IsClose = false;
            var height = GetListheight();

            // 创建一个新的列表框控件
            _listBox1 = new ListBox();
            try
            {
                // 暂停窗体布局逻辑
                SuspendLayout();
                // 设置列表框停靠方式为填充
                _listBox1.Dock = DockStyle.Fill;
                // 设置列表框字体
                _listBox1.Font = new Font("微软雅黑", 26.25f);
                // 设置列表项高度
                _listBox1.ItemHeight = ItemHeight;
                // 设置列表框允许格式化
                _listBox1.FormattingEnabled = true;
                // 设置列表框位置
                _listBox1.Location = new Point(0, 0);
                // 设置列表框名称
                _listBox1.Name = "listBox2";
                // 设置列表框TabIndex属性
                _listBox1.TabIndex = 0;
                // 注册列表框双击事件处理程序
                _listBox1.DoubleClick += ListBox1_DoubleClick;
                _listBox1.MouseWheel += ListBox1_MouseWheel;
                _listBox1.KeyDown += listBox1_KeyDown;
                // 将文件名列表添加到列表框中
                _listBox1.Items.AddRange(RunDict.Keys.ToArray());
                // 设置窗体大小
                Size = new Size(FormWidth, height);
                // 将列表框添加到窗体控件集合中
                Controls.Add(_listBox1);
                // 设置窗体起始位置为屏幕中心
                StartPosition = FormStartPosition.CenterScreen;
                // 恢复窗体布局逻辑
                ResumeLayout(false);
            }
            // 捕获异常
            catch (Exception ex)
            {
                IsClose = true;
                MessageBox.Show($@"处理文件列表时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private void ShowMessage()
        {
            // 显示帮助信息消息框
            var help = string.Join(Environment.NewLine, new[]
            {
                "改名即用：把本 exe 改名为入口名（如 Vs.exe），在 YanBinCfg.ini 的 [Config] 中配置同名键。",
                "",
                "  Vs=程序或路径                       双击直接启动",
                "  Dev=runme 名称1|目标1,名称2|目标2   双击弹出列表选择（runme 是列表标记）",
                "",
                "值支持的写法（配置文件、列表条目目标、run.txt 行通用）：",
                "  · cmd / ps / powershell 命令         用 CMD 或 PowerShell 执行",
                "  · runadmin 目标                      管理员权限（与 cmd / ps 顺序任意：cmd runadmin xxx、ps runadmin xxx）",
                "  · show 目标                          显示窗口运行（默认不显示，例：show cmd xxx、cmd show xxx；提权时总显示）",
                "  · 裸命令                             dotnet、git、notepad 等按系统 PATH 直接执行",
                "  · 网址 / 目录 / 文件                 https://…、目录、文档交给系统默认方式打开",
                "  · 占位符                             {time.格式} {env.变量名} {guid.id} {random.最小-最大}",
                "  · {0}{1}…                            带参数运行分身（如拖拽文件到 exe 上）时自动填入",
                "  · 路径                               绝对路径直接运行；相对路径支持 pf\\、pf86\\、AppData、..\\ 前缀，其余按基准目录拼接",
                "",
                "列表窗口：Enter 启动 / Shift+Enter 管理员启动 / 双击启动 / 滚轮切换 / Esc 关闭",
                "",
                "批量启动：新建 {分身名}run.txt（如 Vsrun.txt），每行一个目标，双击分身按行依次启动",
                "",
                "配置：[Settings] RunParentDirectory=相对路径基准目录；ExcludeExeName=list 模式的排除名单",
                "",
                "其他命令（命令行传递）：",
                "  runme 显示名|目标,…    临时列表（单条直接启动，多条弹列表）",
                "  list 扩展名 [目录]     列出目录中指定后缀的文件供选择（目录缺省为本目录）",
                "  runmeth                目录中其它 exe 全部替换为当前 exe",
                "  runmefth               按 [Config] 的键批量生成分身（已存在不覆盖）",
                "  help                   显示本帮助",
                "",
                "注：值以 runme 开头即为列表（显示名|目标,…），不带则整条按单条命令执行；详细说明见 YanBinCfg.ini 注释与 README.md"
            });
            MessageBox.Show(help, @"使用帮助");
        }

        #endregion

        #region 进程启动

        /// <summary>
        /// 运行指定路径的程序
        /// </summary>
        /// <param name="upath">程序路径</param>
        private void WinExec(string upath, bool runas = false)
        {
            if (string.IsNullOrWhiteSpace(upath))
            {
                return;
            }

            // 全局占位符替换（{time.*}/{env.*}/{guid.*}/{random.*}），cmd / ps 命令同样生效
            upath = ProcessPlaceholders(upath);

            // {0}{1}… 参数填充（带参数运行分身或模板时）；值不含占位符时把参数追加到命令尾部
            var requiredParams = GetFormatParameterCount(upath);
            if (requiredParams > 0)
            {
                upath = FillFormatArgs(upath, requiredParams);
            }
            else if (_extraArgs != null && _extraArgs.Length > 0)
            {
                upath += " " + string.Join(" ", _extraArgs);
            }

            // 执行标记解析：从命令开头逐个取词，runadmin（管理员）/ show（显示窗口）与 cmd / ps / powershell 顺序任意
            // 默认不显示窗口；例：runadmin cmd xxx、cmd show xxx、show ps xxx、ps show xxx
            var body = upath.TrimStart();
            var shell = "";
            var show = false;

            while (true)
            {
                var sp = body.IndexOf(' ');
                var word = sp < 0 ? body : body.Substring(0, sp);

                if (word.Equals("runadmin", StringComparison.OrdinalIgnoreCase))
                {
                    runas = true;
                }
                else if (word.Equals("show", StringComparison.OrdinalIgnoreCase))
                {
                    show = true;
                }
                else if (word.Equals("cmd", StringComparison.OrdinalIgnoreCase))
                {
                    shell = "cmd";
                }
                else if (word.Equals("ps", StringComparison.OrdinalIgnoreCase) ||
                         word.Equals("powershell", StringComparison.OrdinalIgnoreCase))
                {
                    shell = "ps";
                }
                else
                {
                    break; // 首个非标记词即是命令体开始
                }

                if (sp < 0)
                {
                    body = ""; // 只有标记词，没有命令体
                    break;
                }

                body = body.Substring(sp + 1).TrimStart();
            }

            if (body.Length == 0)
            {
                return; // 无有效命令：不执行
            }

            if (shell == "cmd")
            {
                CmdExec(body, runas, show);
                return;
            }

            if (shell == "ps")
            {
                PowerShellExec(body, runas, show);
                return;
            }

            var (a, b) = ProcessString(body, false);
            StartProcess(ProcessPath(a, RunParentDirectory), b, runas, show);
        }

        /// <summary>
        /// 将命令行参数填入 {0}{1}… 占位符（不足补空格，多余忽略；格式非法时原样返回）
        /// </summary>
        private string FillFormatArgs(string format, int requiredParams)
        {
            try
            {
                var source = _extraArgs ?? new string[0];
                var values = source.Concat(Enumerable.Repeat(" ", requiredParams))
                    .Take(requiredParams)
                    .ToArray();
                return string.Format(format, values);
            }
            catch
            {
                return format;
            }
        }


        /// <summary>
        /// 启动一个进程并立即返回，不等待其完成
        /// </summary>
        /// <param name="fileName">要启动的程序路径</param>
        /// <param name="arguments">程序参数（可选）</param>
        private void StartProcess(string fileName, string arguments = null, bool runas = false, bool show = false)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                // 默认不显示窗口（无 shell 方式启动）；加 show 或提权时用 shell 方式启动（控制台窗口可见）
                startInfo.UseShellExecute = show || runas;
                startInfo.CreateNoWindow = !show;
                if (!string.IsNullOrEmpty(arguments))
                {
                    startInfo.FileName = fileName;
                    startInfo.Arguments = arguments;
                    if (runas) startInfo.Verb = "runas"; // 请求提升权限
                }
                else
                {
                    if (runas)
                    {
                        startInfo.Verb = "runas"; // 请求提升权限
                        startInfo.FileName = fileName;
                    }
                    else if (fileName.IndexOfAny(new[] { '\\', '/', ':' }) < 0 &&
                             string.IsNullOrEmpty(Path.GetExtension(fileName)))
                    {
                        // 裸命令（如 notepad）：直接启动，交给系统按 PATH 查找
                        startInfo.FileName = fileName;
                    }
                    else
                    {
                        startInfo.FileName = "explorer.exe";
                        startInfo.Arguments = fileName;
                    }
                }
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // 忽略所有异常，确保方法不会因为启动进程失败而中断
                Console.WriteLine($@"启动进程时发生错误: {ex.Message}");
            }
        }


        #endregion
    }
}