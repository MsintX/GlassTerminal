using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CefSharp;

namespace GlassTerminal
{
    public partial class MainWindow : Window
    {
        private const string InitialDirectory = @"C:\Windows\System32";
        private const int WmDwmCompositionChanged = 0x031E;

        private HwndSource windowSource;
        private WinPtySession terminalSession;
        private readonly TerminalBridge terminalBridge;
        private bool isClosing;
        private bool terminalPageLoaded;

        public MainWindow()
        {
            InitializeComponent();

            terminalBridge = new TerminalBridge(this);
            TerminalBrowser.JavascriptObjectRepository.Register(
                "bridge",
                terminalBridge,
                true,
                BindingOptions.DefaultBinder);
            TerminalBrowser.FrameLoadEnd += TerminalBrowser_FrameLoadEnd;
            Loaded += MainWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            windowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (windowSource != null)
                windowSource.AddHook(WindowMessageHook);

            ApplyAeroGlass();
        }

        private IntPtr WindowMessageHook(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (message == WmDwmCompositionChanged)
                ApplyAeroGlass();

            return IntPtr.Zero;
        }

        private void ApplyAeroGlass()
        {
            try
            {
                Margins margins = new Margins
                {
                    Left = -1,
                    Right = -1,
                    Top = -1,
                    Bottom = -1
                };

                int result = DwmExtendFrameIntoClientArea(
                    new WindowInteropHelper(this).Handle,
                    ref margins);

                if (result == 0)
                {
                    if (windowSource != null && windowSource.CompositionTarget != null)
                        windowSource.CompositionTarget.BackgroundColor = Colors.Transparent;

                    WindowRoot.Background = Brushes.Transparent;
                    return;
                }
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }

            if (windowSource != null && windowSource.CompositionTarget != null)
                windowSource.CompositionTarget.BackgroundColor = Color.FromRgb(0x0B, 0x17, 0x24);

            WindowRoot.Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x17, 0x24));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string pagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Terminal",
                "index.html");

            if (!File.Exists(pagePath))
            {
                MessageBox.Show(this,
                    "找不到本地终端页面：" + pagePath + Environment.NewLine +
                    "请先在项目目录运行 npm install 和 npm run prepare-terminal。",
                    "GlassTerminal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            TerminalBrowser.Address = new Uri(pagePath).AbsoluteUri;
        }

        private void TerminalBrowser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (!e.Frame.IsMain || isClosing)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isClosing || terminalPageLoaded)
                    return;

                terminalPageLoaded = true;
                StartShell();
            }));
        }

        private void StartShell()
        {
            if (!Directory.Exists(InitialDirectory))
            {
                WriteTerminalText("无法找到初始目录: " + InitialDirectory + "\r\n");
                return;
            }

            try
            {
                WriteTerminalText(
                    "MsintX GlassTerminal\r\n" +
                    "版权所有 (C) MsintX 保留所有权利\r\n\r\n");

                string commandProcessor = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "cmd.exe");

                terminalSession = new WinPtySession(
                    commandProcessor,
                    "/Q /D /K \"prompt GT $P$G\"",
                    InitialDirectory,
                    100,
                    30);

                terminalSession.OutputReceived += TerminalSession_OutputReceived;
                terminalSession.Faulted += TerminalSession_Faulted;
                terminalSession.StartReading();
            }
            catch (Exception ex)
            {
                WriteTerminalText("启动终端失败: " + ex.Message + "\r\n");
            }
        }

        private void TerminalSession_OutputReceived(byte[] data)
        {
            string base64 = Convert.ToBase64String(data);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!isClosing && TerminalBrowser.IsBrowserInitialized)
                    TerminalBrowser.ExecuteScriptAsync(
                        "window.writeTerminalBase64('" + base64 + "');");
            }));
        }

        private void TerminalSession_Faulted(Exception exception)
        {
            WriteTerminalText("\r\n[终端后端错误] " + exception.Message + "\r\n");
        }

        private void WriteTerminalText(string text)
        {
            if (isClosing)
                return;

            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!isClosing && TerminalBrowser.IsBrowserInitialized)
                    TerminalBrowser.ExecuteScriptAsync(
                        "window.writeTerminalBase64('" + base64 + "');");
            }));
        }

        private void SendInput(string data)
        {
            if (isClosing || terminalSession == null)
                return;

            try
            {
                terminalSession.WriteText(data);
            }
            catch (Exception ex)
            {
                WriteTerminalText("\r\n[输入失败] " + ex.Message + "\r\n");
            }
        }

        private void ResizeTerminal(int columns, int rows)
        {
            if (isClosing || terminalSession == null)
                return;

            try
            {
                terminalSession.Resize(columns, rows);
            }
            catch (Exception)
            {
                // A resize can race with shell shutdown. The next session owns
                // a fresh PTY, so a late resize is intentionally ignored.
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;

            if (terminalSession != null)
            {
                terminalSession.Dispose();
                terminalSession = null;
            }

            if (windowSource != null)
            {
                windowSource.RemoveHook(WindowMessageHook);
                windowSource = null;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd,
            ref Margins margins);

        public sealed class TerminalBridge
        {
            private readonly MainWindow owner;

            public TerminalBridge(MainWindow owner)
            {
                this.owner = owner;
            }

            public void SendInput(string data)
            {
                owner.Dispatcher.BeginInvoke(new Action(() => owner.SendInput(data)));
            }

            public void Resize(int columns, int rows)
            {
                owner.Dispatcher.BeginInvoke(
                    new Action(() => owner.ResizeTerminal(columns, rows)));
            }

            public void Ready()
            {
                // The page is already loaded before this callback can be invoked.
            }
        }
    }
}
