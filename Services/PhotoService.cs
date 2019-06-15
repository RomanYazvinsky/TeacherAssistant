using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Windows.Media.Imaging;
using static System.IO.Directory;

namespace TeacherAssistant.Components
{
    public class PhotoService : IPhotoService
    {
        private const int CacheCapacity = 60;
        private readonly Dictionary<string, BitmapImage> _cache = new Dictionary<string, BitmapImage>(CacheCapacity);
        private bool _exist = false;

        public string Directory = Path.Combine(Environment.CurrentDirectory, "photos");

        private async Task<string> GetPersonalId(string cardUid)
        {
            var request =
                (HttpWebRequest) WebRequest.Create("http://api.grsu.by/1.x/app3/getStudentByCard?cardid=" + cardUid);
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            try
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    var reader = new StreamReader(responseStream ?? throw new InvalidOperationException(),
                        System.Text.Encoding.UTF8);
                    var s = reader.ReadToEnd().Replace("[", "").Replace("]", "");
                    return JObject.Parse(s)["TN"].Value<string>();
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveImage(string path, byte[] image)
        {
            using (var sourceStream = File.Open(path, FileMode.OpenOrCreate))
            {
                sourceStream.Seek(0, SeekOrigin.End);
                await sourceStream.WriteAsync(image, 0, image.Length).ConfigureAwait(false);
            }
        }

        public async Task<BitmapImage> GetImage(string path)
        {
            if (path == null) return null;
            BitmapImage bitmapImage = null;
            if (_cache.ContainsKey(path))
            {
                return _cache[path];
            }

            await Task.Run(() =>
            {
                using (var stream = File.OpenRead(path))
                {
                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                }

                bitmapImage.Freeze();
            }).ConfigureAwait(false);
            if (_cache.Count == CacheCapacity)
            {
                _cache.Remove(_cache.Keys.First());
            }

            _cache.Add(path, bitmapImage);
            return bitmapImage;
        }

        public async Task<string> DownloadPhoto(string cardId)
        {
            string path = null;
            try
            {
                if (!_exist || !Exists(Directory))
                {
                    _exist = CreateDirectory(Directory).Exists;
                }

                path = Path.Combine(Directory, cardId + ".jpg");
                if (File.Exists(path))
                {
                    return path;
                }

                var personalId = await GetPersonalId(cardId).ConfigureAwait(false);
                if (personalId == null)
                {
                    return null;
                }

                using (var client = new WebClient())
                {
                    try
                    {
                        var image = await client.DownloadDataTaskAsync(
                            new Uri("https://intra.grsu.by/photos/" + personalId + ".jpg")).ConfigureAwait(false);
                        await SaveImage(path, image).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return path;
        }
    }
}