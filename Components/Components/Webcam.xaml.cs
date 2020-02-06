using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;

namespace TeacherAssistant {
    public partial class Webcam : UserControl {
        public Webcam() {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Unloaded += (sender, args) => {
                LocalWebCam.Stop();
                LocalWebCam.WaitForStop();
            };
        }

        VideoCaptureDevice LocalWebCam;
        public FilterInfoCollection LoaclWebCamsCollection;

        void Cam_NewFrame(NewFrameEventArgs eventArgs) {
            BitmapImage bi;
            using (var bitmap = (Bitmap) eventArgs.Frame.Clone()) {
                bi = new BitmapImage();
                bi.BeginInit();
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Jpeg);
                ms.Seek(0, SeekOrigin.Begin);
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
            }

            bi.Freeze();
            Dispatcher.BeginInvoke(new ThreadStart(delegate { Image.Source = bi; }), DispatcherPriority.Background);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            LoaclWebCamsCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            LocalWebCam = new VideoCaptureDevice(LoaclWebCamsCollection[0].MonikerString);
            Observable.FromEventPattern<NewFrameEventHandler, NewFrameEventArgs>(
                    h => LocalWebCam.NewFrame += h,
                    h => LocalWebCam.NewFrame -= h
                )
                .Select(pattern => pattern.EventArgs)
                .Subscribe(Cam_NewFrame);

            LocalWebCam.Start();
        }
    }
}