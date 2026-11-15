using System;
using System.IO;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace GlassTerminal
{
    public partial class App : Application
    {
        public App()
        {
            CefRuntime.SubscribeAnyCpuAssemblyResolver();

            var settings = new CefSettings
            {
                BackgroundColor = 0x00000000,
                CachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MsintX",
                    "GlassTerminal",
                    "CefCache")
            };

            Cef.Initialize(settings);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Cef.Shutdown();
            base.OnExit(e);
        }
    }
}
