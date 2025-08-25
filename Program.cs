using System.Windows.Forms;

namespace RunMe;

/// <summary>
/// 程序入口类
/// </summary>
internal class Program
{
    /// <summary>
    /// 程序入口方法
    /// </summary>
    /// <param name="args">命令行参数</param>
    public static void Main(string[] args)
    {
        // 启用应用程序的可视化样式
        Application.EnableVisualStyles();
        // 设置应用程序窗体的文本呈现默认值
        Application.SetCompatibleTextRenderingDefault(false);
        // 运行ShowForm窗体，并传入命令行参数
        Application.Run(new ShowForm(args));
    }
}