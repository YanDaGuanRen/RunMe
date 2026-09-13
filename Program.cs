using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace RunMe
{
    /// <summary>
    /// 程序入口类
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// 程序入口方法
        /// </summary>
        /// <param name="args">命令行参数</param>
        [STAThread]
        public static void Main(string[] args)
        {
            // 启用应用程序的可视化样式
            Application.EnableVisualStyles();
            // 设置应用程序窗体的文本呈现默认值
            Application.SetCompatibleTextRenderingDefault(false);

            // 兜底：UI 线程未处理异常仅记录，不弹 .NET 崩溃对话框
            Application.ThreadException += (sender, e) => Debug.WriteLine("未处理异常: " + e.Exception);

            // 运行ShowForm窗体，并传入命令行参数（构造阶段的意外异常也做兜底提示）
            try
            {
                Application.Run(new ShowForm(args));
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败: " + ex.Message, "RunMe");
            }
        }
    }
}