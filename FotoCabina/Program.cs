using System;
using System.IO;
using System.Windows.Forms;

namespace FotoCabina
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.ThreadException += (s, e) => Registrar(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Registrar(e.ExceptionObject as Exception);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
        static void Registrar(Exception ex)
        {
            try { File.AppendAllText(Path.Combine(Config.CarpetaApp, "error.log"), DateTime.Now + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine); }
            catch { }
        }
    }
}
