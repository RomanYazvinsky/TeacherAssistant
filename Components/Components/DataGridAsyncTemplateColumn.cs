using System.Windows;
using System.Windows.Controls;

namespace TeacherAssistant.Components {
    public class DataGridAsyncTemplateColumn: DataGridTemplateColumn {
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem) {
            return base.GenerateElement(cell, dataItem);
        }
    }
}