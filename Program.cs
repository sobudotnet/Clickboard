using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Clickboard
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Assembly resolve for DLLs in lib folder
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblyName = new AssemblyName(args.Name).Name + ".dll";
                var libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", assemblyName);
                if (File.Exists(libPath))
                    return Assembly.LoadFrom(libPath);
                return null;
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string pinPath = System.IO.Path.Combine(Application.StartupPath, "clickboard.pin");
            Theme defaultTheme = new Theme // fallback theme for PIN entry
            {
                HeaderBarColor = System.Drawing.Color.Black,
                HeaderBarTextColor = System.Drawing.Color.White,
                InputFieldColor = System.Drawing.Color.Black,
                InputFieldTextColor = System.Drawing.Color.White,
                ButtonColor = System.Drawing.Color.Black,
                ButtonTextColor = System.Drawing.Color.White
            };

            if (File.Exists(pinPath))
            {
                using (var pinEntry = new PinEntryForm(pinPath, defaultTheme))
                {
                    if (pinEntry.ShowDialog() != DialogResult.OK)
                    {
                        // Exit if PIN not entered correctly or cancelled
                        return;
                    }
                }
            }
            Application.Run(new mainwindow());
        }
    }
}