using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Workbench.Views.Windows
{
    /// <summary>
    /// UserManualWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UserManualWindow
    {
        public UserManualWindow(string filePath=null)
        {
            InitializeComponent();
            DataContext = this;
            InitWebViewAsync(filePath);
        }
        private async Task InitWebViewAsync(string filePath)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            InitWebView2DirAccess(baseDir);
            LoadPdf(filePath);
            //await webView.EnsureCoreWebView2Async(
            //    await CoreWebView2Environment.CreateAsync($"{baseDir}\\WebView2",
            //        options: new CoreWebView2EnvironmentOptions($"-disable-web-security --user-data-dir={baseDir}\\ChromeDevSession")));
            //webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.example", "./dist", CoreWebView2HostResourceAccessKind.Deny);
            //webView.CoreWebView2.Navigate("http://app.example/index.html");
        }
        private async void LoadPdf(string pdfPath)
        {
            await webView.EnsureCoreWebView2Async();
            var uri = new Uri(pdfPath); // 本地：file:/// 开头
            webView.CoreWebView2.Navigate(uri.AbsoluteUri);
            // 示例：跳至第3页可尝试 hash（是否生效取决于内置查看器）
            // PdfView.CoreWebView2.Navigate(uri.AbsoluteUri + "#page=3");
        }
        private void InitWebView2DirAccess(string baseDir)
        {
            try
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                var dataDir = baseDir + $"{assemblyName}.exe.WebView2";
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                    // 获取文件夹的当前安全属性
                    var directoryInfo = new DirectoryInfo(dataDir);
                    DirectorySecurity dirSecurity = directoryInfo.GetAccessControl();
                    SecurityIdentifier usersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                    FileSystemAccessRule writeRule = new FileSystemAccessRule(usersSid, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
                    dirSecurity.AddAccessRule(writeRule);
                    directoryInfo.SetAccessControl(dirSecurity);
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
        }
    }
}
