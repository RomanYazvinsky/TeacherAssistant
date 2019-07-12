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
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Forms.StreamForm
{
    /// <summary>
    /// Interaction logic for StreamForm.xaml
    /// </summary>
    public partial class StreamForm : View<StreamFormModel>
    {
        public StreamForm(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
        }
    }
}
