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
        private bool isAppendingOutput;
        private int inputStart;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AppendOutput("MsintX GlassTerminal\r\n" +
                         "版权所有 (C) MsintX 保留所有权利\r\n\r\n");
            StartShell();
            TerminalOutput.Focus();
            TerminalOutput.CaretIndex = TerminalOutput.Text.Length;
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

                isAppendingOutput = true;
                TerminalOutput.AppendText(text);
                inputStart = TerminalOutput.Text.Length;
                TerminalOutput.CaretIndex = inputStart;
                TerminalOutput.ScrollToEnd();
                isAppendingOutput = false;
            }));
        }

        private void TerminalOutput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int selectionStart = TerminalOutput.SelectionStart;
            int selectionEnd = selectionStart + TerminalOutput.SelectionLength;
            int caret = TerminalOutput.CaretIndex;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                string command = TerminalOutput.Text.Substring(inputStart);
                TerminalOutput.AppendText(Environment.NewLine);
                inputStart = TerminalOutput.Text.Length;
                TerminalOutput.CaretIndex = inputStart;
                TerminalOutput.ScrollToEnd();

                if (shellProcess == null || shellProcess.HasExited)
                {
                    AppendOutput("终端会话尚未启动。" + Environment.NewLine);
                    return;
                }

                try
                {
                    shellProcess.StandardInput.WriteLine(command);
                    shellProcess.StandardInput.Flush();
                }
                catch (Exception ex)
                {
                    AppendOutput("输入失败: " + ex.Message + Environment.NewLine);
                }
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                e.Key == Key.A)
            {
                e.Handled = true;
                TerminalOutput.Select(inputStart, TerminalOutput.Text.Length - inputStart);
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                e.Key == Key.V)
            {
                e.Handled = true;
                if (selectionStart >= inputStart)
                {
                    string paste = Clipboard.GetText();
                    if (!String.IsNullOrEmpty(paste))
                        TerminalOutput.SelectedText = paste;
                }
                return;
            }

            if (e.Key == Key.Back)
            {
                if (selectionStart < inputStart || (TerminalOutput.SelectionLength == 0 && caret <= inputStart))
                    e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete && selectionStart < inputStart)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left && TerminalOutput.SelectionLength == 0 && caret <= inputStart)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Home)
            {
                e.Handled = true;
                TerminalOutput.CaretIndex = inputStart;
                return;
            }

            if (selectionStart < inputStart &&
                (e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.X))
            {
                e.Handled = true;
            }
        }

        private void TerminalOutput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (TerminalOutput.SelectionStart < inputStart)
                e.Handled = true;
        }

        private void TerminalOutput_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TerminalOutput.CaretIndex < inputStart)
                e.Handled = true;
        }

        private void TerminalOutput_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (TerminalOutput.CaretIndex < inputStart)
                TerminalOutput.CaretIndex = TerminalOutput.Text.Length;
        }

        private void ShellProcess_Exited(object sender, EventArgs e)
        {
            AppendOutput(Environment.NewLine + "[终端会话已结束]" + Environment.NewLine);
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
            if (!isAppendingOutput && TerminalOutput.CaretIndex < inputStart)
                TerminalOutput.CaretIndex = TerminalOutput.Text.Length;

            TerminalOutput.ScrollToEnd();
        }
    }
}
