using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;

namespace TeacherAssistant.Components.Webcam {
    public partial class Webcam : UserControl {
        public Webcam() {
            InitializeComponent();
        }

        private VideoCaptureDevice _localWebCam;
        private FilterInfoCollection _localWebCamsCollection;

        private void ShowFrame(object _, NewFrameEventArgs eventArgs) {
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
            Dispatcher?.BeginInvoke(() => { Image.Source = bi; }, DispatcherPriority.Normal);
        }

        private void StartRecording(object sender, RoutedEventArgs e) {
            _localWebCamsCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (_localWebCamsCollection.Count == 0) {
                return;
            }
            var localWebCams = _localWebCamsCollection[0];
            _localWebCam = new VideoCaptureDevice(localWebCams.MonikerString);
            _localWebCam.NewFrame += ShowFrame;
            _localWebCam.Start();
        }

        private void OnClose(object sender, RoutedEventArgs e) {
            _localWebCam.Stop();
            _localWebCam.WaitForStop();
        }
    }
}