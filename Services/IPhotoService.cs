﻿using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TeacherAssistant.Components
{
    public interface IPhotoService
    {
        BitmapImage GetImage(string path);
        Task<string> DownloadPhoto(string cardId);
    }
}