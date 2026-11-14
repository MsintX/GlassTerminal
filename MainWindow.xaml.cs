using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GlassTerminal
{
    public partial class MainWindow : Window
    {
        private const string InitialDirectory = @"C:\Windows\System32";
        private Process shellProcess;
        private bool isClosing;

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmIsCompositionEnabled(out bool enabled);

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;
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

                    string chunk = new string(buffer, 0, count);
                    AppendOutput(chunk);
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

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 6)
                return;

            try
            {
                bool compositionEnabled;
                if (DwmIsCompositionEnabled(out compositionEnabled) != 0 || !compositionEnabled)
                    return;

                IntPtr handle = new WindowInteropHelper(this).Handle;
                Margins margins = new Margins
                {
                    Left = -1,
                    Right = -1,
                    Top = -1,
                    Bottom = -1
                };

                DwmExtendFrameIntoClientArea(handle, ref margins);
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        private void TerminalOutput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TerminalOutput.ScrollToEnd();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.ClickCount == 2)
                    ToggleMaximize();
                else
                    DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }
}
