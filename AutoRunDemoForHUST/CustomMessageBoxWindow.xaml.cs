using System.Windows;

namespace AutoRunDemoForHUST
{
    /// <summary>
    /// CustomMessageBoxWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CustomMessageBoxWindow : Window
    {
        public CustomMessageBoxWindow(string message)
        {
            InitializeComponent();
            TxtMessage.Text = message;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



    }
}
