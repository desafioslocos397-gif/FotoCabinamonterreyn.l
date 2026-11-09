using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using AForge.Video.DirectShow;

namespace FotoCabina
{
    public class MenuConfig : Form
    {
        ComboBox camara, impresora;
        NumericUpDown fotos, cuenta, revision, ancho, alto;
        TextBox texto, carpeta;
        CheckBox imprimir, duplicar, espejo;
        public MenuConfig()
        {
            Text = "Configuracion de FotoCabina";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(620, 690);
            BackColor = Color.FromArgb(245, 247, 250);
            var titulo = new Label { Text = "CONFIGURACION", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.FromArgb(30, 55, 80), AutoSize = true, Location = new Point(24, 18) };
            Controls.Add(titulo);
            var panel = new FlowLayoutPanel { Location = new Point(20, 65), Size = new Size(580, 545), AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
            Controls.Add(panel);

            camara = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            var dispositivos = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo d in dispositivos) camara.Items.Add(d.Name);
            if (camara.Items.Count == 0) camara.Items.Add("No se detectaron camaras");
            camara.SelectedIndex = Math.Min(Config.Camara, camara.Items.Count - 1);
            Fila(panel, "Camara", camara);

            fotos = Numero(1, 4, Config.NumFotos); Fila(panel, "Cantidad de fotos por sesion", fotos);
            cuenta = Numero(1, 10, Config.Cuenta); Fila(panel, "Cuenta regresiva (segundos)", cuenta);
            revision = Numero(10, 300, Config.SegundosRevision); Fila(panel, "Tiempo para revisar la tira (segundos)", revision);
            texto = new TextBox { Width = 340, Text = Config.Texto }; Fila(panel, "Texto al pie de la tira", texto);

            imprimir = new CheckBox { Text = "Habilitar impresion", AutoSize = true, Checked = Config.Imprimir }; Fila(panel, "", imprimir);
            impresora = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            impresora.Items.Add("(Impresora predeterminada)");
            foreach (string p in PrinterSettings.InstalledPrinters) impresora.Items.Add(p);
            int pi = impresora.Items.IndexOf(Config.Impresora);
            impresora.SelectedIndex = pi >= 0 ? pi : 0;
            Fila(panel, "Impresora", impresora);
            duplicar = new CheckBox { Text = "Imprimir dos tiras en una hoja", AutoSize = true, Checked = Config.DuplicarTira }; Fila(panel, "", duplicar);
            ancho = Numero(100, 2000, Config.PapelAncho); Fila(panel, "Ancho del papel (centésimas de pulgada)", ancho);
            alto = Numero(100, 3000, Config.PapelAlto); Fila(panel, "Alto del papel (centésimas de pulgada)", alto);
            espejo = new CheckBox { Text = "Mostrar la imagen como espejo", AutoSize = true, Checked = Config.Espejo }; Fila(panel, "", espejo);

            carpeta = new TextBox { Width = 270, Text = Config.CarpetaFotos };
            var elegir = new Button { Text = "Examinar...", Width = 100, Height = 28 };
            elegir.Click += (s, e) => { using (var d = new FolderBrowserDialog { Description = "Selecciona donde guardar las fotografias", SelectedPath = Directory.Exists(carpeta.Text) ? carpeta.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) }) if (d.ShowDialog(this) == DialogResult.OK) carpeta.Text = d.SelectedPath; };
            var filaCarpeta = new FlowLayoutPanel { Width = 550, Height = 42, FlowDirection = FlowDirection.LeftToRight };
            filaCarpeta.Controls.Add(carpeta); filaCarpeta.Controls.Add(elegir);
            Fila(panel, "Carpeta para guardar las fotos", filaCarpeta);

            var guardar = new Button { Text = "GUARDAR CONFIGURACION", Width = 260, Height = 48, BackColor = Color.FromArgb(0, 145, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(180, 625) };
            guardar.Click += Guardar;
            var cancelar = new Button { Text = "CANCELAR", Width = 120, Height = 48, Location = new Point(450, 625) };
            cancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(guardar); Controls.Add(cancelar);
        }
        NumericUpDown Numero(int min, int max, int valor) { return new NumericUpDown { Minimum = min, Maximum = max, Value = Math.Max(min, Math.Min(max, valor)), Width = 340 }; }
        void Fila(FlowLayoutPanel panel, string etiqueta, Control control)
        {
            var fila = new FlowLayoutPanel { Width = 550, Height = 48, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(3, 3, 3, 5) };
            if (!string.IsNullOrEmpty(etiqueta)) fila.Controls.Add(new Label { Text = etiqueta, Width = 540, Height = 18, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            fila.Controls.Add(control); panel.Controls.Add(fila);
        }
        void Guardar(object sender, EventArgs e)
        {
            try
            {
                Config.Guardar("Camara", camara.SelectedIndex.ToString());
                Config.Guardar("NumFotos", fotos.Value.ToString());
                Config.Guardar("Cuenta", cuenta.Value.ToString());
                Config.Guardar("SegundosRevision", revision.Value.ToString());
                Config.Guardar("Texto", texto.Text.Trim());
                Config.Guardar("Imprimir", imprimir.Checked.ToString());
                Config.Guardar("Impresora", impresora.SelectedIndex <= 0 ? "" : impresora.SelectedItem.ToString());
                Config.Guardar("DuplicarTira", duplicar.Checked.ToString());
                Config.Guardar("PapelAncho", ancho.Value.ToString());
                Config.Guardar("PapelAlto", alto.Value.ToString());
                Config.Guardar("Espejo", espejo.Checked.ToString());
                Config.Guardar("CarpetaFotos", carpeta.Text.Trim());
                DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex) { MessageBox.Show("No se pudo guardar la configuracion:\n" + ex.Message, "FotoCabina", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
