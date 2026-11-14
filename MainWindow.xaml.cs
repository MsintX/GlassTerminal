using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GlassTerminal
{
    public partial class MainWindow : Window
    {
        private const string InitialDirectory = @"C:\Windows\System32";
        private Process shellProcess;
        private bool isClosing;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TerminalOutput.AppendText(
                "MsintX GlassTerminal\r\n" +
                "版权所有 (C) MsintX 保留所有权利\r\n\r\n");

            StartShell();
            CommandInput.Focus();
        }

        private void StartShell()
        {
            if (!Directory.Exists(InitialDirectory))
            {
                AppendOutput("无法找到初始目录: " + InitialDirectory + "\r\n");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
                Arguments = "/Q /D /K \"prompt GT $P$G\"",
                WorkingDirectory = InitialDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default
            };

            try
            {
                shellProcess = new Process();
                shellProcess.StartInfo = startInfo;
                shellProcess.EnableRaisingEvents = true;
                shellProcess.Exited += ShellProcess_Exited;

                if (!shellProcess.Start())
                {
                    AppendOutput("无法启动 cmd.exe。\r\n");
                    return;
                }

                ReadStreamAsync(shellProcess.StandardOutput);
                ReadStreamAsync(shellProcess.StandardError);
            }
            catch (Exception ex)
            {
                AppendOutput("启动终端失败: " + ex.Message + "\r\n");
                if (shellProcess != null)
                {
                    shellProcess.Dispose();
                    shellProcess = null;
                }
            }
        }

        private async void ReadStreamAsync(StreamReader reader)
        {
            char[] buffer = new char[1024];

            try
            {
                while (!isClosing)
                {
                    int count = await reader.ReadAsync(buffer, 0, buffer.Length);
                    if (count <= 0)
                        break;

                    AppendOutput(new string(buffer, 0, count));
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }
        }

        private void AppendOutput(string text)
        {
            if (isClosing || !IsLoaded)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isClosing)
                    return;

                TerminalOutput.AppendText(text);
                TerminalOutput.ScrollToEnd();
            }));
        }

        private void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            string command = CommandInput.Text;
            CommandInput.Clear();

            if (shellProcess == null || shellProcess.HasExited)
            {
                AppendOutput("终端会话尚未启动。\r\n");
                return;
            }

            TerminalOutput.AppendText(command + "\r\n");
            TerminalOutput.ScrollToEnd();

            try
            {
                shellProcess.StandardInput.WriteLine(command);
                shellProcess.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                AppendOutput("输入失败: " + ex.Message + "\r\n");
            }
        }

        private void ShellProcess_Exited(object sender, EventArgs e)
        {
            AppendOutput("\r\n[终端会话已结束]\r\n");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;

            if (shellProcess == null)
                return;

            try
            {
                if (!shellProcess.HasExited)
                {
                    shellProcess.StandardInput.WriteLine("exit");
                    shellProcess.StandardInput.Flush();

                    if (!shellProcess.WaitForExit(500))
                        shellProcess.Kill();
                }
            }
            catch
            {
                try
                {
                    if (!shellProcess.HasExited)
                        shellProcess.Kill();
                }
                catch
                {
                }
            }
            finally
            {
                shellProcess.Dispose();
                shellProcess = null;
            }
        }

        private void TerminalOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            TerminalOutput.ScrollToEnd();
        }
    }
}
