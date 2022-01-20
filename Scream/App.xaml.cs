using System.Windows;

namespace Scream
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            (MainWindow as Scream.MainWindow).QuitScream(sender, null);
        }
    }
}
