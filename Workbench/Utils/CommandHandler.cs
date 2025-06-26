using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Utils
{
    public class CommandHandler
    {
        private Process process;
        public async Task<string> ExecuteCommandAysnc(List<string> commands)
        {
            var cmd = string.Join(" & ", commands);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {cmd}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (process = new Process())
            {
                process.StartInfo = startInfo;

                process.Start();

                // 异步读取输出
                var readOutputTask = Task.Run(() =>
                {
                    return process.StandardOutput.ReadToEnd();
                });

                // 异步等待进程结束
                var waitTask = Task.Run(() =>
                {
                    process.WaitForExit();
                });

                // 等待任务完成
                await Task.WhenAll(readOutputTask, waitTask);

                string output = await readOutputTask;

                return output;
            }
        }

        public void KillProcessOnPort(int port)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c netstat -ano | findStr :" + port)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process { StartInfo = processInfo };
            process.Start();

            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                if (line.Trim().StartsWith("TCP") || line.Trim().StartsWith("UDP"))
                {
                    var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var pid = parts[parts.Length - 1]; // 使用传统方法获取最后一个元素

                    // 确认这行包含了 PID 信息
                    if (!string.IsNullOrEmpty(pid) && int.TryParse(pid, out int validPid))
                    {
                        KillProcessByPid(validPid);
                    }
                }
            }
        }

        private static void KillProcessByPid(int pid)
        {
            var processKill = new ProcessStartInfo("cmd.exe", "/c taskkill /F /PID " + pid)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process { StartInfo = processKill };
            Task.Run(() =>
            {
                process.Start();
                process.WaitForExit();
            });
        }

        public void CloseCommandProcess()
        {
            KillProcessOnPort(1880);
        }
    }
}
