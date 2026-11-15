using System;
using System.Windows;
using CefSharp;

namespace GlassTerminal
{
    public partial class App : Application
    {
        public App()
        {
            var settings = new CefSettings();
            settings.CachePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "CefCache");

            Cef.Initialize(settings);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Cef.Shutdown();
            base.OnExit(e);
        }
    }
}
