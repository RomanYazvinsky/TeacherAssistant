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
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.GroupTable
{
    /// <summary>
    /// Interaction logic for GroupTable.xaml
    /// </summary>
    public partial class GroupTable : UserControl
    {
        private GroupTableModel _model;
        public GroupTable(string id)
        {
            InitializeComponent();
            _model = Injector.GetInstance().Kernel.Get<GroupTableModel>();
            DataContext = _model;
            _model.Init(id);
            SortHelper.AddColumnSorting(Groups);
        }
    }
}
