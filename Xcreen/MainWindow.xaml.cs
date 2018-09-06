using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.IO;

namespace Xcreen {

    public partial class MainWindow : Window {

        private static Screen screen;
        private static string tempFilename = System.Windows.Forms.Application.StartupPath + "\\last_temp.png";
        private static Bitmap actualImage;
        private static NotifyIcon icon;
        private static KeyboardHook hook;
        private System.Windows.Point start;
        private System.Windows.Forms.MenuItem[] ctxItems = new System.Windows.Forms.MenuItem[] {
            new System.Windows.Forms.MenuItem("Sair", new EventHandler(ExitApp))
        };

        public MainWindow() {
            InitializeComponent();
            // Minimize to SystemTray on startup
            icon = new NotifyIcon();
            icon.Icon = new Icon("App.ico");
            icon.ContextMenu = new System.Windows.Forms.ContextMenu(ctxItems);
            icon.Text = "Xscreen screenshot tool";
            icon.Visible = true;
            Hide();
            // Register EventHandler for Prt Sc Key
            // Check https://docs.microsoft.com/en-us/windows/desktop/inputdev/virtual-key-codes for the key codes
            hook = new KeyboardHook(this, 0x2C);
            hook.Triggered += PrintScreen;
        }

        private void PrintScreen() {
            screen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);

            var top = screen.Bounds.Top;
            var left = screen.Bounds.Left;
            var width = screen.Bounds.Width;
            var height = screen.Bounds.Height;

            Top = top;
            Left = left;
            Width = width;
            Height = height;

            // delete temp image if it still exists
            if (File.Exists(tempFilename))
                File.Delete(tempFilename);

            // Screenshot the whole screen & save it
            actualImage = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(actualImage);
            g.CopyFromScreen(left, top, 0, 0, actualImage.Size);
            actualImage.Save(tempFilename);

            // create the temp image
            BitmapImage bmpImg = new BitmapImage();
            Stream str = File.OpenRead(tempFilename);
            bmpImg.BeginInit();
            bmpImg.CacheOption = BitmapCacheOption.OnLoad;
            bmpImg.StreamSource = str;
            bmpImg.EndInit();

            // Dispose some trash
            str.Close();
            str.Dispose();
            g.Dispose();

            // render the screenshot
            image.Source = bmpImg;
            image.Opacity = 0.35;
            Show();
            Activate();
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e) {
            // reset the toolbar
            toolbar.Visibility = Visibility.Hidden;
            // reset the rectangle
            rect.Width = 0;
            rect.Height = 0;

            start = e.GetPosition(canvas);

            Canvas.SetLeft(rect, start.X);
            Canvas.SetRight(rect, start.Y);

            rect.Visibility = Visibility.Visible;
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            var pos = e.GetPosition(canvas);
        
            var x = Math.Min(pos.X, start.X);
            var y = Math.Min(pos.Y, start.Y);

            var w = Math.Max(pos.X, start.X) - x;
            var h = Math.Max(pos.Y, start.Y) - y;

            if (w < 10 || h < 10)
                return;

            if (toolbar.Visibility == Visibility.Hidden)
                toolbar.Visibility = Visibility.Visible;

            rect.Width = w;
            rect.Height = h;

            var toolbarX = (x + w) - toolbar.Width;
            var toolbarY = (y + h + toolbar.Height) > screen.Bounds.Height ? screen.Bounds.Height - toolbar.Height : y + h;

            Canvas.SetLeft(toolbar, toolbarX);
            Canvas.SetTop(toolbar, toolbarY);

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
        }

        private void saveAs_Click(object sender, RoutedEventArgs e) {
            Bitmap bmp = GetImageArea();

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "PNG Image|*.png|JPEG Image|*.jpg";
            save.Title = "Salva a tua captura de ecrã";
            save.ShowDialog();

            if (save.FileName != "") {
                FileStream fs = (FileStream) save.OpenFile();
                switch (save.FilterIndex) {
                    case 1:
                        bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    case 2:
                        bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                }
            }

            bmp.Dispose();
            save.Dispose();

            ExitPrintScreen();
        }

        private void copyImage_Click(object sender, RoutedEventArgs e) {
            Bitmap bmp = GetImageArea();
            System.Windows.Forms.Clipboard.SetImage(bmp);

            bmp.Dispose();
            ExitPrintScreen();
        }

        private Bitmap GetImageArea() {
            // create the crop area
            double x = Canvas.GetLeft(rect), y = Canvas.GetTop(rect);
            double w = rect.Width, h = rect.Height;
            System.Drawing.Rectangle crop = new System.Drawing.Rectangle((int) x, (int) y, (int) w, (int) h);

            // create the final image
            Bitmap final = new Bitmap((int) rect.Width, (int) rect.Height);
            Graphics g = Graphics.FromImage(final);
            g.DrawImage(actualImage, new System.Drawing.Rectangle(0, 0, (int) w, (int) h), crop, GraphicsUnit.Pixel);

            // collect the trash
            g.Dispose();

            return final;
        }

        private void ExitPrintScreen() {
            rect.Width = 0;
            rect.Height = 0;
            toolbar.Visibility = Visibility.Hidden;
            rect.Visibility = Visibility.Hidden;
            actualImage.Dispose();
            actualImage = null;
            image.Source = null;

            if (File.Exists(tempFilename))
                File.Delete(tempFilename);

            Close();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Escape)
                ExitPrintScreen();
        }

        protected override void OnClosing(CancelEventArgs e) {
            // Prevents the app from being closed unintentionally
            e.Cancel = true;
            GC.Collect();
            Hide();
        }

        private static void ExitApp(object sender, EventArgs e) {
            icon.Visible = false;
            icon.Dispose();
            hook.Dispose();

            if (File.Exists(tempFilename))
                File.Delete(tempFilename);

            Environment.Exit(0);
        }

    }

}
