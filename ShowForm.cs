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

        /// <summary>
        /// 标识窗体是否应该关闭的布尔字段
        /// </summary>
        private bool IsClose { get; set; }

        /// <summary>
        /// 列表框控件的私有字段
        /// </summary>
        private ListBox listBox1;

        #endregion

        /// <summary>
        /// 构造函数，根据传入的参数初始化窗体
        /// </summary>
        /// <param name="args">命令行参数数组</param>
        public ShowForm(string[] args)
        {
            FormInit();
            CfgInit();
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
            else if (args[0].ToLower() == "runme")
            {
                RunRunme(args);
            }
            else if (args[0].ToLower() == "list")
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
                    path = SetupPath(list2[1], RunParentDirectory);
                }
                else
                {
                    fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                    path = SetupPath(ReadValue("config", se), RunParentDirectory);
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

        private int GetListheight()
        {
            if (RunDict.Count > 0)
            {
                return ItemHeight + ItemHeight * RunDict.Count;
            }

            return ItemHeight;
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

        private void GetFilesList(string path, string Suffix)
        {
            // 遍历所有文件
            foreach (string file in Directory.GetFiles(path))
            {
                // 创建文件信息对象
                FileInfo info = new FileInfo(file);
                // 如果文件扩展名匹配且文件名不等于当前主模块名
                if (info.Extension.ToLower() == Suffix.ToLower() &&
                    info.Name != Process.GetCurrentProcess().MainModule.ModuleName)
                {
                    RunDict[info.Name.Replace(Suffix, "")] = info.FullName;
                }
            }
        }


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
            MessageBox.Show(str, "使用帮助");
        }

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
                            { "ExcludeExeName", "RunMe.exe|MeRun.Exe" }
                        }
                    },
                    {
                        "Config",
                        new Dictionary<string, string>
                        {
                            { "RunMe", "http:\\www.bing.com" },
                            { "RunMe1", "List exe d:\\" },
                            { "RunMe2", "List exe tools" },
                            { "RunMe3", "runme VS Code|vc,VS Studio|vs" },
                            { "RunMe4", "runme VS Code|c:\\,VS Studio|D:\\" },
                            { "RunMe5", "vs" },
                            { "RunMe6", "http:\\www.bing.com" },
                            { "RunMe7", "help" }
                        }
                    }
                };
                // 创建INI文件
                CreateIniFile(YanBinCfgPath, iniData);
            }
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


        private void FormInit()
        {
            IsClose = true;
            // 暂停窗体布局逻辑
            SuspendLayout();
            // 设置窗体大小
            Size = new Size(FormWidth, ItemHeight);
            // 设置窗体名称
            Name = "ShowForm";

            Text = "程序启动器";
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
            
            RunParentDirectory = ReadValue("Settings", "RunParentDirectory");

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

        /// <summary>
        /// 处理路径字符串的方法
        /// </summary>
        /// <param name="upath">原始路径</param>
        /// <param name="parentDirectory">父目录路径</param>
        /// <returns>处理后的完整路径</returns>
        private string SetupPath(string upath, string parentDirectory)
        {
            // 如果路径为空或null，直接返回
            if (string.IsNullOrEmpty(upath))
            {
                return upath;
            }

            if (upath.Substring(1, 2) == ":\\")
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
            if (upath.Substring(1, 2) != ":\\")
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

        #region DllImport

        /// <summary>
        /// 创建一个UTF-16编码、LF换行符的INI文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="iniContent">INI内容</param>
        private void CreateIniFile(string filePath, Dictionary<string, Dictionary<string, string>> iniContent)
        {
            var content = new StringBuilder();

            foreach (var section in iniContent)
            {
                content.AppendLine($"[{section.Key}]");
                foreach (var keyValue in section.Value)
                {
                    content.AppendLine($"{keyValue.Key}={keyValue.Value}");
                }

                content.AppendLine(); // 添加空行分隔段落
            }

            // 使用UTF-16编码和LF换行符保存文件
            File.WriteAllText(filePath, content.ToString(), Encoding.Unicode);

            // 将CRLF替换为LF（如果有）
            var fileContent = File.ReadAllText(filePath, Encoding.Unicode);
            fileContent = fileContent.Replace("\r\n", "\n");
            File.WriteAllText(filePath, fileContent, Encoding.Unicode);
        }

        /// <summary>
        /// 从INI配置文件中读取指定节和键的值
        /// </summary>
        /// <param name="pathstr">配置文件路径</param>
        /// <param name="Section">节名称</param>
        /// <param name="Key">键名称</param>
        /// <returns>读取到的值</returns>
        public string ReadValue(string Section, string Key)
        {
            try
            {
                // 创建StringBuilder对象用于接收返回值
                StringBuilder retVal = new StringBuilder(0x3_2000);
                // 调用Windows API读取配置值
                int num = GetPrivateProfileString(Section, Key, "", retVal, 0x3_2000, YanBinCfgPath);
                // 返回读取到的字符串
                return retVal.ToString();
            }
            catch
            {
                // 发生异常时返回空字符串
                return "";
            }
        }

        /// <summary>
        /// 运行指定路径的程序
        /// </summary>
        /// <param name="upath">程序路径</param>
        private void WinExec(string upath)
        {
            var path = SetupPath(upath, RunParentDirectory);
            // 调用Windows API执行程序
            WinExec("explorer.exe " + path, 5);
        }

        /// <summary>
        /// 调用Windows API执行程序
        /// </summary>
        /// <param name="exeName">要执行的程序名和参数</param>
        /// <param name="operType">操作类型</param>
        /// <returns>执行结果</returns>
        [DllImport("kernel32.dll")]
        public static extern int WinExec(string exeName, int operType);

        /// <summary>
        /// 调用Windows API读取INI配置文件
        /// </summary>
        /// <param name="section">节名称</param>
        /// <param name="key">键名称</param>
        /// <param name="def">默认值</param>
        /// <param name="retVal">返回值</param>
        /// <param name="size">缓冲区大小</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>复制到缓冲区的字符数</returns>
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal,
            int size, string filePath);

        #endregion
    }
}