using System;
using System.Windows.Media.Imaging;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Components.Photo
{
    public class PhotoModel : AbstractModel
    {
        private BitmapImage _studentPhoto;
        public BitmapImage StudentPhoto
        {
            get => _studentPhoto;
            set
            {
                _studentPhoto = value;
                OnPropertyChanged(nameof(StudentPhoto));
            }
        }

        public PhotoModel(string id) : base(id)
        {
            S<string>("PhotoPath", path =>
            {
                if (path == null)
                {
                    StudentPhoto = null;
                    return;
                }
                var uriSource = new Uri(path);
                StudentPhoto = new BitmapImage(uriSource);
            });
        }
    }
}