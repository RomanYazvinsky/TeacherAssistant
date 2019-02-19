using System.Windows.Controls;

namespace TeacherAssistant.Components.Photo
{
    /// <summary>
    /// Interaction logic for Photo.xaml
    /// </summary>
    public partial class Photo : UserControl
    {
        public Photo(string id)
        {
            InitializeComponent();
            DataContext = new PhotoModel(id);
        }
    }
}
