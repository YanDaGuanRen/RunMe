using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using RunMe.Properties;

namespace RunMe
{
    /// <summary>
    /// 主窗体类，用于显示和执行程序列表
    /// </summary>
    public partial class ShowForm : Form
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
        private ListBox listBox1;

        private bool iscomd;

        private bool isprocess;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShowForm));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            Text = "很牛B的一个程序启动器";
            // 注册窗体加载事件处理程序
            Load += new EventHandler(this.ShowForm_Load);
            // 恢复窗体布局逻辑
            ResumeLayout(false);

            RunDict = new Dictionary<string, string>();
            // 初始化exepath为当前应用程序域的基目录
            RunExePath = AppDomain.CurrentDomain.BaseDirectory;
            // 获取当前可执行文件名并移除.exe扩展名
            RunExeName = Path.GetFileName(Application.ExecutablePath).Replace(".exe", "");
            // 获取当前目录的上两级目录路径
            YanBinCfgPath = Path.Combine(RunExePath, "YanBinCfg.ini");
        }
        
        private void NoArgs(string rname = "")
        {
            var runtxt = "";
            if (string.IsNullOrEmpty(rname))
            {
                rname = RunExeName;
            }

            runtxt = Path.Combine(this.RunExePath, rname + "run.txt");

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
                    if (upath.Contains(","))
                    {
                        RunRunme("", upath);
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
                if (se.ToLower() != me.ToLower())
                {
                    File.Delete(se);
                    File.Copy(me, se);
                }
            }
        }
        private void ReplaceAllX()
        {
            var oldlist = Directory.GetFiles(RunExePath, "*.exe", SearchOption.TopDirectoryOnly);
            foreach (var se in oldlist)
            {
                // string programName = Path.GetFileNameWithoutExtension(se);
                var fileInfo = new FileInfo(se);
                if (fileInfo.Name.Replace(fileInfo.Extension, "").ToLower() != RunExeName.ToLower())
                {
                    fileInfo.Delete();
                }
            }

            ;
            var me = Path.Combine(RunExePath, RunExeName + ".exe");
            foreach (var s1 in Config)
            {
                if (s1.Key.ToLower() == "Settings".ToLower())
                {
                    continue;
                }

                foreach (var se in s1.Value)
                {
                    var ddd = se.Key;
                    if (se.Key.ToLower() == RunExeName.ToLower()) continue;
                    var df = Path.Combine(RunExePath, ddd + ".exe");
                    if (!File.Exists(df))
                    {
                        File.Copy(me, df);
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
                // 逐行读取文件内容
                while ((line = reader.ReadLine()) != null)
                {
                    // 线程休眠1秒
                    Thread.Sleep(1000);

                    // 运行当前行指定的程序
                    WinExec(line);
                }
            }
        }
        private void RunRunme(params string[] args)
        {
            if (args.Length < 1) return;
            if (string.IsNullOrEmpty(args[1])) return;
            var arg = Regex.Replace(args[1], "runme ", "", RegexOptions.IgnoreCase);
            var list = arg.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (list.Length < 0) return;
            foreach (var se in list)
            {
                var list2 = se.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var fileNameWithoutExtension = "";
                var path = "";

                if (list2.Length > 1)
                {
                    fileNameWithoutExtension = list2[0];
                    path = ProcessPath(list2[1], RunParentDirectory);
                }
                else
                {
                    fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                    path = ProcessPath(ReadValue("config", se), RunParentDirectory);
                }

                RunDict[fileNameWithoutExtension] = path;
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
            if (this.IsClose)
            {
                // 关闭窗体
                base.Close();
            }
            else
            {
                // 确保ListView在窗体加载时能获取焦点
                listBox1.Focus();

                // 默认选中第一项
                if (listBox1.Items.Count > 0)
                {
                    listBox1.SelectedIndex = 0;
                }
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            // 检测是否按下了回车键，并且有选中项
            if (e.KeyCode == Keys.Enter && listBox1.SelectedIndex != -1)
            {
                // 触发双击事件处理逻辑
                ListBox1_DoubleClick(sender, e);
                e.SuppressKeyPress = true; // 防止系统发出提示音
            }
        }

        /// <summary>
        /// 列表框双击事件处理程序
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ListBox1_DoubleClick(object sender, EventArgs e)
        {
            // 如果路径不为空
            this.WinExec(RunDict[listBox1.SelectedItem.ToString()]);
            // 关闭窗体
            base.Close();
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
            var runname = ReadValue("ExecCmd", RunExeName);

            var proseccname = ReadValue("ExecProcess", RunExeName);

            RunExeName = "g";

            iscomd = !string.IsNullOrEmpty(runname);
            isprocess = !string.IsNullOrEmpty(proseccname);

            if (iscomd || isprocess)
            {
                runname = string.IsNullOrEmpty(runname) ? proseccname : runname;
                int requiredParams = GetFormatParameterCount(runname);
                var runarg = "";

                if (requiredParams > 0)
                {
                    runname = ProcessPlaceholders(runname);
                    var templist = args.Concat(Enumerable.Repeat(" ", requiredParams)).Take(requiredParams);
                    
                    runarg = string.Format(runname, templist.ToArray());
                }
                else
                {
                    runarg = runname + " " + string.Join(" ", args);
                }

                if (iscomd)
                {
                    StartCmdSilently(runarg);
                }
                else
                {
                    var (beforeSpace, afterProcessing) = ProcessString(runarg);
                    StartProcess(beforeSpace, afterProcessing);
                }

                return;
            }

            // 如果没有传入命令行参数
            if (args.Length == 0)
            {
                NoArgs();
                return;
            }
            else if (args[0].ToLower() == "runmeth")
            {
                ReplaceAll();
                return;
            }
            else if (args[0].ToLower() == "runmefth")
            {
                ReplaceAllX();
                return;
            }
            else if (args[0].ToLower() == "runme")
            {
                RunRunme(args);
                return;
            }
            else if (args[0] == "list")
            {
                // 如果参数少于3个
                if (args.Length < 3) return;

                GetFilesList(args[2], "." + args[1]);
                ShowListBox();
            }
            else if (args[0] == "help")
            {
                ShowMessage();
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
            if (!File.Exists(YanBinCfgPath))
            {
                var iniData = new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "Settings",
                        new Dictionary<string, string>
                        {
                            { "RunParentDirectory", RunExePath },
                            { "ExcludeExeName", "RunMe|MeRun" },
                            { "变量名1", "这就就是{env.变量名1}的值" },
                            { "变量名2", "这就就是{env.变量名2}的值" },

                        }
                    },
                    {
                        "ExecCmd",
                        new Dictionary<string, string>
                        {
                            {
                                "Nug1",
                                "dotnet nuget push {0} --api-key {env.变量名1} --source https://api.nuget.org/v3/index.json"
                            }
                        }
                    },
                    {
                        "ExecProcess",
                        new Dictionary<string, string>
                        {
                            {
                                "Nug2",
                                "dotnet nuget push {0} --api-key {1} --source https://api.nuget.org/v3/index.json # 这样的占位符只能在ExecProcess ExecCmd" 
                            }
                        }
                    },
                    {
                        "Config",
                        new Dictionary<string, string>
                        {
                            { "RunMe", "runme 中文测试1|RunMe6,中文测试2|RunMe7" },
                            { "RunMe1", "List exe d:\\" },
                            { "RunMe2", "List exe tools" },
                            { "RunMe3", "runme VS Code|vc,VS Studio|vs" },
                            { "RunMe4", "runme VS Code|c:\\,VS Studio|D:\\" },
                            { "RunMe5", "vs" },
                            { "RunMe6", "http:\\www.bing.com" },
                            { "RunMe7", "help" },
                        }
                    }
                };
                // 创建INI文件
                CreateIniFile(YanBinCfgPath, iniData);
            }
        }
        /// <summary>
        /// 创建一个UTF-16编码、LF换行符的INI文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="iniContent">INI内容</param>
        private void CreateIniFile(string filePath, Dictionary<string, Dictionary<string, string>> iniContent)
        {
            var content = new StringBuilder();

            content.AppendLine(@"#以下全局可用");
            content.AppendLine(@"#{time.格式} - 使用当前时间并按照指定格式格式化");
            content.AppendLine(@"#{env.变量名} - 获取指定的环境变量值 在Settings项中设置" );
            content.AppendLine(@"#{guid.id} - 生成一个新的 GUID");
            content.AppendLine(@"#{random.最小值-最大值} - 生成指定范围内的随机数");
            content.AppendLine(); // 添加空行分隔段落
            content.AppendLine(@"#占位符{0} 只能在ExecCmd与ExecProcess使用");
            content.AppendLine(); // 添加空行分隔段落

            foreach (var section in iniContent)
            {
                if (section.Key.ToLower() == "Config".ToLower())
                {
                    content.AppendLine(); // 添加空行分隔段落
                    content.AppendLine(@"#以下Config可用");
                    content.AppendLine(@"#pf = 从C到G盘的Program Files目录");
                    content.AppendLine(@"#pf86 = 从C到G盘的Program Files (x86)目录)");
                    content.AppendLine(@"#AppData = 用户目录");
                    content.AppendLine(); // 添加空行分隔段落
                }
                content.AppendLine($"[{section.Key}]");


                foreach (var keyValue in section.Value)
                {
                    content.AppendLine($"{keyValue.Key}={keyValue.Value}");
                }

                content.AppendLine(); // 添加空行分隔段落
            }

            // 使用UTF-8 with BOM编码保存文件
            File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);

            // // 将CRLF替换为LF（如果有）
            // var fileContent = File.ReadAllText(filePath, Encoding.UTF8);
            // fileContent = fileContent.Replace("\r\n", "\n");
            // File.WriteAllText(filePath, fileContent, Encoding.UTF8);
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
                                if (!Config.ContainsKey(currentSection))
                                {
                                    currentSectionDict =
                                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    Config[currentSection] = currentSectionDict;
                                }
                                else
                                {
                                    currentSectionDict = Config[currentSection];
                                }
                            }
                            // 处理键值对
                            else if (line.Contains("=") && !string.IsNullOrEmpty(currentSection))
                            {
                                var parts = line.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                                string key = parts[0].Trim();
                                string value = parts[1].Trim();

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
            }
        }

        /// <summary>
        /// 从INI配置文件中读取指定节和键的值
        /// </summary>
        /// <param name="Section">节名称</param>
        /// <param name="Key">键名称</param>
        /// <returns>读取到的值</returns>
        public string ReadValue(string Section, string Key)
        {
            if (Config.TryGetValue(Section, out var ddic))
            {
                if (ddic.TryGetValue(Key, out var sett))
                {
                    return sett;
                }
            }

            return "";
        }

        #endregion

        #region 功能

        /// <summary>
        /// 填充占位符
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string ProcessPlaceholders(string input)
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
                    return ReadValue("Settings", envVar);
                }

                // 处理 GUID 生成
                if (content.Equals("guid.", StringComparison.OrdinalIgnoreCase))
                {
                    return Guid.NewGuid().ToString();
                }

                // 处理随机数 {random.min-max}
                if (content.StartsWith("random.", StringComparison.OrdinalIgnoreCase))
                {
                    var randomParams = content.Substring(7);
                    var parts = randomParams.Split('-');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int min) &&
                        int.TryParse(parts[1], out int max))
                    {
                        var random = new Random();
                        return random.Next(min, max + 1).ToString();
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

            if (upath.Length < 6)
            {
                var t = ReadValue("Config", upath);
                if (!string.IsNullOrEmpty(t))
                {
                    upath = t;
                }
            }

            if (upath.Length > 3 && upath.Substring(1, 2) == ":\\")
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
                    .GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            }
            // 如果路径以"..\"开头
            else if (upath.StartsWith("..\\")) // 处理相对路径
            {
                // 循环处理所有"..\"
                while (upath.StartsWith("..\\"))
                {
                    // 获取父目录的父目录
                    parentDirectory = Directory.GetParent(parentDirectory).FullName;
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
        /// <returns></returns>
        public (string beforeSpace, string afterProcessing) ProcessString(string input,bool findChar=true)
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
                        string exePath = input.Substring(0, exeEndIndex + 4);  // +4包括.exe
                        string arguments = input.Substring(exeEndIndex + 5).TrimStart(); // +5跳过".exe "
                        return (exePath, arguments);
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
        public int GetFormatParameterCount(string format)
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
                return ItemHeight + ItemHeight * RunDict.Count;
            }
            return ItemHeight;
        }

        /// <summary>
        /// 取指定目录指定后缀列表
        /// </summary>
        /// <param name="path"></param>
        /// <param name="Suffix"></param>
        private void GetFilesList(string path, string Suffix)
        {
            var list = ReadValue("Settings", "ExcludeExeName")
                ?.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (list == null || list.Length < 1 || string.IsNullOrEmpty(list[0]))
            {
                list = new[] { "RunMe", "MeRun" };
            }

            // 遍历所有文件
            foreach (string file in Directory.GetFiles(path))
            {
                // 创建文件信息对象
                FileInfo info = new FileInfo(file);
                // 如果文件扩展名匹配且文件名不等于当前主模块名
                var name = Regex.Replace(info.Name, info.Extension, "", RegexOptions.IgnoreCase);
                if (info.Extension.ToLower() == Suffix.ToLower() && !list.Contains(name))
                {
                    RunDict[info.Name.Replace(Suffix, "")] = info.FullName;
                }
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
            // 如果高度仍为初始值（没有找到匹配的文件）
            if (height == ItemHeight)
            {
                // 设置窗体应该关闭
                this.IsClose = true;
                return;
            }

            // 创建一个新的列表框控件
            this.listBox1 = new ListBox();
            try
            {
                // 暂停窗体布局逻辑
                base.SuspendLayout();
                // 设置列表框停靠方式为填充
                this.listBox1.Dock = DockStyle.Fill;
                // 设置列表框字体
                this.listBox1.Font = new Font("微软雅黑", 26.25f);
                // 设置列表项高度
                this.listBox1.ItemHeight = ItemHeight;
                // 设置列表框允许格式化
                this.listBox1.FormattingEnabled = true;
                // 设置列表框位置
                this.listBox1.Location = new Point(0, 0);
                // 设置列表框名称
                this.listBox1.Name = "listBox2";
                // 设置列表框TabIndex属性
                this.listBox1.TabIndex = 0;
                // 注册列表框双击事件处理程序
                this.listBox1.DoubleClick += ListBox1_DoubleClick;

                this.listBox1.KeyDown += listBox1_KeyDown;
                // 将文件名列表添加到列表框中
                this.listBox1.Items.AddRange(RunDict.Keys.ToArray());
                // 设置窗体大小
                base.Size = new Size(FormWidth, height);
                // 将列表框添加到窗体控件集合中
                base.Controls.Add(this.listBox1);
                // 设置窗体起始位置为屏幕中心
                base.StartPosition = FormStartPosition.CenterScreen;
                // 恢复窗体布局逻辑
                base.ResumeLayout(false);
            }
            // 捕获异常
            catch (Exception ex)
            {
                IsClose = true;
                MessageBox.Show($"处理文件列表时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private void ShowMessage()
        {
            // 定义帮助文本
            string str = @"程序执行顺序: 
有参数:
runme.exe list exe c:\ (列表形式显示C盘下所有EXE文件)
runme.exe help (显示程序的帮助程序)
runme.exe XX (运行yanbincfg.ini配置文件中的XX项目)
无参数:
配置文件:配置读取顺序
程序名.run → 
如果程序名为A.exe 
如果程序目录有a.run 会执行a.run文件中每一行的程序间隔1秒
如果程序目录没有a.run 会执行yanbincfg.ini中Config中a键的程序


a.run 为Encoding.UTF8格式
yanbincfg.ini 为 UTF16 LF";
            // 显示帮助信息消息框
            MessageBox.Show("看配置文件", "使用帮助");
        }

        #endregion
        
        #region DllImport
        /// <summary>
        /// 运行指定路径的程序
        /// </summary>
        /// <param name="upath">程序路径</param>
        private void WinExec(string upath)
        {
            var (a, b) = ProcessString(upath,false);
             a = ProcessPath(a, RunParentDirectory);
            

            if (string.IsNullOrEmpty(b))
            {
                StartCmdSilently(a);
            }
            else
            {
                StartProcess(a,b);
            }
        }


        /// <summary>
        /// 启动一个进程并立即返回，不等待其完成
        /// </summary>
        /// <param name="fileName">要启动的程序路径</param>
        /// <param name="arguments">程序参数（可选）</param>
        public void StartProcess(string fileName, string arguments = null)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                if (!string.IsNullOrEmpty(arguments))
                {
                    startInfo.Arguments = ProcessPlaceholders(arguments);
                }

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // 忽略所有异常，确保方法不会因为启动进程失败而中断
                Console.WriteLine($"启动进程时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动CMD命令但不显示黑框
        /// </summary>
        /// <param name="command">要执行的CMD命令</param>
        public void StartCmdSilently(string command,bool show = false)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {ProcessPlaceholders(command)}",
                    UseShellExecute = show,
                    CreateNoWindow = true,
                    WindowStyle =show?ProcessWindowStyle.Normal: ProcessWindowStyle.Hidden
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // 忽略所有异常，确保方法不会因为启动进程失败而中断
                Console.WriteLine($"启动CMD命令时发生错误: {ex.Message}");
            }
        }

        #endregion
        
    }
}