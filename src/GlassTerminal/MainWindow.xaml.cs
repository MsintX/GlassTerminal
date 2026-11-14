using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GlassTerminal
{
    public partial class MainWindow : Window
    {
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
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 6)
                return;

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
