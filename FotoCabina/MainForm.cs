using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace FotoCabina
{
    public class MainForm : Form
    {
        enum Estado { Inicio, Capturando, Revision }
        Estado estado = Estado.Inicio;
        VideoCaptureDevice cam;
        Bitmap ultimoFrame;
        readonly object lk = new object();
        int pintando = 0;
        PictureBox vista;
        Label lblMensaje;
        Panel barra;
        Button btnIniciar, btnImprimir, btnRepetir, btnTerminar;
        Button[] actuales = new Button[0];
        Bitmap tira;
        System.Windows.Forms.Timer timerRevision;

        public MainForm()
        {
            Text = "FotoCabina";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.Black;
            KeyPreview = true;
            DoubleBuffered = true;
            vista = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
            lblMensaje = new Label
            {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 60, FontStyle.Bold)
            };
            vista.Controls.Add(lblMensaje);
            barra = new Panel { Dock = DockStyle.Bottom, Height = 120, BackColor = Color.FromArgb(25, 25, 25) };
            btnIniciar = Crear("TOCA PARA COMENZAR", Color.FromArgb(0, 150, 90), async (s, e) => await Sesion());
            btnImprimir = Crear("IMPRIMIR", Color.FromArgb(0, 120, 200), (s, e) => ImprimirTira());
            btnRepetir = Crear("REPETIR", Color.FromArgb(220, 130, 0), async (s, e) => await Sesion());
            btnTerminar = Crear("TERMINAR", Color.FromArgb(120, 120, 120), (s, e) => IrAInicio());
            barra.Controls.AddRange(new Control[] { btnIniciar, btnImprimir, btnRepetir, btnTerminar });
            barra.Resize += (s, e) => Botones(actuales);
            Controls.Add(vista);
            Controls.Add(barra);
            timerRevision = new System.Windows.Forms.Timer();
            timerRevision.Tick += (s, e) => { timerRevision.Stop(); if (estado == Estado.Revision) IrAInicio(); };
            EventHandler iniciarTocando = async (s, e) => { if (estado == Estado.Inicio) await Sesion(); };
            vista.Click += iniciarTocando;
            lblMensaje.Click += iniciarTocando;
            KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.Q) Close(); else if (e.KeyCode == Keys.F1) { e.Handled = true; AbrirMenu(); } };
            Shown += (s, e) => IniciarCamara();
            FormClosing += (s, e) => DetenerCamara();
        }

        void AbrirMenu()
        {
            if (estado == Estado.Capturando) return;
            int camaraAnterior = Config.Camara;
            using (var menu = new MenuConfig())
            {
                if (menu.ShowDialog(this) == DialogResult.OK && Config.Camara != camaraAnterior)
                {
                    DetenerCamara();
                    IniciarCamara();
                }
            }
        }

        Button Crear(string texto, Color color, EventHandler click)
        {
            var b = new Button
            {
                Text = texto, BackColor = color, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 18, FontStyle.Bold), Visible = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += click;
            return b;
        }

        void Botones(params Button[] lista)
        {
            actuales = lista;
            foreach (var b in new[] { btnIniciar, btnImprimir, btnRepetir, btnTerminar }) b.Visible = false;
            int w = 330, gap = 30;
            int total = lista.Length * w + Math.Max(0, lista.Length - 1) * gap;
            int x = Math.Max(0, (barra.Width - total) / 2);
            foreach (var b in lista) { b.SetBounds(x, 15, w, 90); b.Visible = true; x += w + gap; }
        }

        void Mensaje(string texto, float tam)
        {
            lblMensaje.Font = new Font("Segoe UI", tam, FontStyle.Bold);
            lblMensaje.Text = texto;
            lblMensaje.Visible = texto.Length > 0;
        }

        void IniciarCamara()
        {
            try
            {
                var dev = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (dev.Count == 0) { MessageBox.Show("No se encontro ninguna camara conectada.", "FotoCabina"); Close(); return; }
                cam = new VideoCaptureDevice(dev[Math.Min(Config.Camara, dev.Count - 1)].MonikerString);
                var caps = cam.VideoCapabilities;
                if (caps.Length > 0)
                {
                    var mejor = caps.Where(c => c.FrameSize.Width <= 1280)
                        .OrderByDescending(c => c.FrameSize.Width * c.FrameSize.Height)
                        .FirstOrDefault() ?? caps[0];
                    cam.VideoResolution = mejor;
                }
                cam.NewFrame += OnFrame;
                cam.Start();
                IrAInicio();
            }
            catch (Exception ex) { MessageBox.Show("Error al iniciar la camara:\n" + ex.Message, "FotoCabina"); Close(); }
        }

        void DetenerCamara()
        {
            try { if (cam != null && cam.IsRunning) { cam.NewFrame -= OnFrame; cam.SignalToStop(); } } catch { }
            lock (lk) { if (ultimoFrame != null) { ultimoFrame.Dispose(); ultimoFrame = null; } }
        }

        void OnFrame(object sender, NewFrameEventArgs e)
        {
            var copia = (Bitmap)e.Frame.Clone();
            lock (lk) { if (ultimoFrame != null) ultimoFrame.Dispose(); ultimoFrame = copia; }
            if (estado == Estado.Revision) return;
            if (Interlocked.Exchange(ref pintando, 1) == 1) return;
            var disp = (Bitmap)e.Frame.Clone();
            if (Config.Espejo) disp.RotateFlip(RotateFlipType.RotateNoneFlipX);
            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed || estado == Estado.Revision) disp.Dispose();
                    else { var viejo = vista.Image; vista.Image = disp; if (viejo != null) viejo.Dispose(); }
                    Interlocked.Exchange(ref pintando, 0);
                }));
            }
            catch { disp.Dispose(); Interlocked.Exchange(ref pintando, 0); }
        }

        Bitmap Capturar()
        {
            lock (lk) { return ultimoFrame == null ? null : (Bitmap)ultimoFrame.Clone(); }
        }

        void IrAInicio()
        {
            timerRevision.Stop();
            estado = Estado.Inicio;
            if (tira != null) { tira.Dispose(); tira = null; }
            var viejo = vista.Image; vista.Image = null; if (viejo != null) viejo.Dispose();
            Mensaje("", 60);
            Botones(btnIniciar);
        }

        async Task Sesion()
        {
            if (estado == Estado.Capturando) return;
            timerRevision.Stop(); estado = Estado.Capturando; Botones();
            var fotos = new List<Bitmap>();
            try
            {
                int n = Config.NumFotos;
                for (int i = 1; i <= n; i++)
                {
                    Mensaje("Foto " + i + " de " + n, 70);
                    await Task.Delay(1200);
                    for (int c = Config.Cuenta; c >= 1; c--) { Mensaje(c.ToString(), 200); await Task.Delay(1000); }
                    Mensaje("Sonrie!", 120); await Task.Delay(300);
                    var f = Capturar();
                    if (f == null) throw new Exception("La camara no esta entregando imagen.");
                    fotos.Add(f);
                    await Flash();
                }
                Mensaje("Preparando tu tira...", 60);
                var nueva = await Task.Run(() => Composicion.CrearTira(fotos));
                tira = nueva;
                await Task.Run(() => Guardar(fotos, nueva));
            }
            catch (Exception ex) { MessageBox.Show("Ocurrio un error:\n" + ex.Message, "FotoCabina"); IrAInicio(); return; }
            finally { foreach (var f in fotos) f.Dispose(); }
            MostrarRevision();
        }

        async Task Flash()
        {
            Mensaje("", 60); vista.Visible = false; BackColor = Color.White;
            await Task.Delay(150);
            BackColor = Color.Black; vista.Visible = true;
        }

        void MostrarRevision()
        {
            estado = Estado.Revision; Mensaje("", 60);
            var viejo = vista.Image; vista.Image = (Bitmap)tira.Clone(); if (viejo != null) viejo.Dispose();
            if (Config.Imprimir) Botones(btnImprimir, btnRepetir, btnTerminar);
            else Botones(btnRepetir, btnTerminar);
            timerRevision.Interval = Config.SegundosRevision * 1000; timerRevision.Start();
        }

        void Guardar(List<Bitmap> fotos, Bitmap laTira)
        {
            var dir = Path.Combine(Config.CarpetaFotos, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(dir);
            for (int i = 0; i < fotos.Count; i++)
                Composicion.GuardarJpg(fotos[i], Path.Combine(dir, "foto" + (i + 1) + ".jpg"));
            Composicion.GuardarJpg(laTira, Path.Combine(dir, "tira.jpg"));
        }

        void ImprimirTira()
        {
            if (tira == null) return;
            timerRevision.Stop();
            try
            {
                var imagen = (Bitmap)tira.Clone();
                var pd = new PrintDocument();
                if (Config.Impresora.Length > 0) pd.PrinterSettings.PrinterName = Config.Impresora;
                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                pd.OriginAtMargins = false;
                pd.DefaultPageSettings.PaperSize = new PaperSize("Foto", Config.PapelAncho, Config.PapelAlto);
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    var pb = e.PageBounds;
                    if (Config.DuplicarTira)
                    {
                        float w = pb.Width / 2f;
                        g.DrawImage(imagen, 0, 0, w, pb.Height);
                        g.DrawImage(imagen, w, 0, w, pb.Height);
                    }
                    else
                    {
                        float esc = Math.Min(pb.Width / (float)imagen.Width, pb.Height / (float)imagen.Height);
                        float w = imagen.Width * esc, h = imagen.Height * esc;
                        g.DrawImage(imagen, (pb.Width - w) / 2, (pb.Height - h) / 2, w, h);
                    }
                    e.HasMorePages = false;
                };
                pd.EndPrint += (s, e) => imagen.Dispose();
                pd.Print();
                Mensaje("Imprimiendo... gracias!", 60); Botones();
                var t = new System.Windows.Forms.Timer { Interval = 3000 };
                t.Tick += (s, e) => { t.Stop(); t.Dispose(); IrAInicio(); };
                t.Start();
            }
            catch (Exception ex) { MessageBox.Show("No se pudo imprimir:\n" + ex.Message, "FotoCabina"); timerRevision.Start(); }
        }
    }
}