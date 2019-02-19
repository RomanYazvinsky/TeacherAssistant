using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Dao;
using Model.Models;
using Newtonsoft.Json.Linq;
using System.Linq;
using TeacherAssistant.State;
using static System.IO.Directory;

namespace TeacherAssistant.Components
{
    public class PhotoService
    {
        private static PhotoService _instance;
        private PhotoService()
        {
        }

        public static PhotoService GetInstance()
        {
            return _instance ?? (_instance = new PhotoService());
        }

        private static readonly string Directory = Path.Combine(Environment.CurrentDirectory, "photos");

        private static string GetPersonalId(string cardUid)
        {
            HttpWebRequest request =
                (HttpWebRequest)WebRequest.Create("http://api.grsu.by/1.x/app3/getStudentByCard?cardid=" + cardUid);
            try
            {
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream ?? throw new InvalidOperationException(), System.Text.Encoding.UTF8);
                    string s = reader.ReadToEnd().Replace("[", "").Replace("]", "");
                    return JObject.Parse(s)["TN"].Value<string>();
                }
            }
            catch
            {
                return null;
            }
        }

        private static async Task SaveImage(string path, byte[] image)
        {
            using (var sourceStream = File.Open(path, FileMode.OpenOrCreate))
            {
                sourceStream.Seek(0, SeekOrigin.End);
                await sourceStream.WriteAsync(image, 0, image.Length);
            }
        }

        public async Task<string> DownloadPhoto(string id)
        {
            string path = null;
            try
            {
                if (!Exists(Directory))
                {
                    CreateDirectory(Directory);
                }
                path = Path.Combine(Directory, id + ".jpg");
                if (File.Exists(path))
                {
                    return path;
                }

                var personalId = GetPersonalId(id);
                if (personalId == null)
                {
                    return null;
                }
                using (var client = new WebClient())
                {
                    try
                    {
                        var image = await client.DownloadDataTaskAsync(new Uri("https://intra.grsu.by/photos/" + personalId + ".jpg"));
                        await SaveImage(path, image);
                    }
                    catch (Exception e)
                    {
                        return null;
                    }

                }
            }
            catch (Exception e)
            {
            }

            return path;
        }
    }
}