using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
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

namespace Workbench.Views
{
    /// <summary>
    /// NodeRedView.xaml 的交互逻辑
    /// </summary>
    public partial class NodeRedView : UserControl
    {
        private const string NodeRedUrl = "http://localhost:1880/admin";
        private readonly HttpClient _httpClient = new HttpClient(); // 创建一个HttpClient来发送请求

        public NodeRedView()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            // 设定间隔重试时间
            TimeSpan retryInterval = TimeSpan.FromMilliseconds(1000);

            // 检查服务器是否已经启动，如果500毫秒内无响应则认为服务器未启动
            //while (!await IsServerAvailable(NodeRedUrl))
            //{
            //    // 等待指定的重试间隔时间后继续执行
            //    await Task.Delay(retryInterval);
            //}

            // 确认服务器可用后设置WebView2的Source
            //webView2.Source = new Uri(NodeRedUrl);
            Dispatcher.Invoke(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(3));
                webView2.Source = new Uri(NodeRedUrl);
            });
        }

        // 检查服务器是否已经启动的方法
        private async Task<bool> IsServerAvailable(string url)
        {
            try
            {
                // 尝试获取响应
                HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                // 如果有异常，表明服务器还未就绪
                return false;
            }
        }
    }
}
