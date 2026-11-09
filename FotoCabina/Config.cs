using System;
using System.Configuration;
using System.IO;

namespace FotoCabina
{
    static class Config
    {
        static Configuration Usuario
        {
            get { return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal); }
        }
        static string S(string k, string d)
        {
            try { var v = Usuario.AppSettings.Settings[k]; if (v != null && !string.IsNullOrWhiteSpace(v.Value)) return v.Value.Trim(); } catch { }
            var original = ConfigurationManager.AppSettings[k];
            return string.IsNullOrWhiteSpace(original) ? d : original.Trim();
        }
        static int I(string k, int d) { int r; return int.TryParse(S(k, ""), out r) ? r : d; }
        static bool B(string k, bool d) { bool r; return bool.TryParse(S(k, ""), out r) ? r : d; }
        public static void Guardar(string clave, string valor)
        {
            var cfg = Usuario;
            if (cfg.AppSettings.Settings[clave] == null) cfg.AppSettings.Settings.Add(clave, valor);
            else cfg.AppSettings.Settings[clave].Value = valor;
            cfg.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        public static string CarpetaApp { get { return AppDomain.CurrentDomain.BaseDirectory; } }
        public static int NumFotos { get { return Math.Max(1, Math.Min(4, I("NumFotos", 4))); } }
        public static int Cuenta { get { return Math.Max(1, I("Cuenta", 3)); } }
        public static int Camara { get { return Math.Max(0, I("Camara", 0)); } }
        public static string Texto { get { return S("Texto", "Fotocabina"); } }
        public static bool Imprimir { get { return B("Imprimir", true); } }
        public static string Impresora { get { return S("Impresora", ""); } }
        public static bool DuplicarTira { get { return B("DuplicarTira", true); } }
        public static int PapelAncho { get { return I("PapelAncho", 400); } }
        public static int PapelAlto { get { return I("PapelAlto", 600); } }
        public static bool Espejo { get { return B("Espejo", true); } }
        public static int SegundosRevision { get { return Math.Max(10, I("SegundosRevision", 45)); } }
        public static string CarpetaFotos { get { var c = S("CarpetaFotos", ""); if (c.Length == 0) c = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FotoCabina"); return c; } }
    }
}
