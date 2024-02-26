using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaFileMetadataModifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal ModifyOptions Options { get;  }

        public MainWindow()
        {
            Options = new ModifyOptions();
            this.DataContext = Options;
            InitializeComponent();
        }


        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
            {
                return;
            }

            try
            {
                btn.IsEnabled = false;
                var handler = new ModifyHandler(Options)
                {
                    PrintLog = PrintLog
                };
                await Task.Run(handler.Handle);
            }
            catch (Exception ex)
            {
                PrintLog($"[Error] {ex.GetType().Name} {ex.Message}");
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        private void PrintLog(string info)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                InfoTextBox.Text += $"[{DateTime.Now:HHmmss}] " + info + Environment.NewLine;
                InfoTextBox.ScrollToEnd();
            });
        }
    }
}