using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Workbench.SerialAsistant.Events;
using Workbench.SerialAsistant.Utils;
using Workbench.SerialAsistant.ViewModels;

namespace Workbench.SerialAsistant.Views
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        public MainView(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            InitializeComponent();
            Listener();
        }

        private void Listener()
        {
            _eventAggregator.GetEvent<SendDataEvent>().Subscribe((bytes) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var viewModel = DataContext as MainViewModel;
                    viewModel.SendCount += bytes.Count();
                    if (viewModel.ShowSend)
                    {
                        ShowMessageInRichTextBox(bytes, "发送");
                    }
                });
            });

            _eventAggregator.GetEvent<ReceiveDataEvent>().Subscribe((bytes) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ShowMessageInRichTextBox(bytes, "接收");
                    var viewModel = DataContext as MainViewModel;
                    viewModel.ReceiveCount += bytes.Count();
                });

            });
        }

        private void ShowMessageInRichTextBox(byte[] bytes, string prefix)
        {
            var viewModel = DataContext as MainViewModel;
            var messageType = viewModel.ReceiveMessageType.FirstOrDefault(t => t.IsSelected)?.Name;
            var message = string.Empty;
            if (messageType == Constants.Hex)
                message = StringHelper.BytesToHexStrWithSpace(bytes);
            else
                message = Encoding.ASCII.GetString(bytes);

            var timeText = $"【{DateTime.Now.ToString("HH:mm:ss.fff")}】";

            // 也可以添加格式化文本
            Paragraph paragraph = new Paragraph();
            var run = new Run($"【{prefix}】{timeText}：{message}");
            if (!viewModel.ShowTime)
                run = new Run($"【{prefix}】：{message}");
            paragraph.Inlines.Add(run);
            paragraph.Margin = new Thickness(0, 0, 0, 2);
            richTextBox.Document.Blocks.Add(paragraph);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            richTextBox.Document.Blocks.Clear();
        }
    }
}
