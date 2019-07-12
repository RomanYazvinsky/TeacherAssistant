using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class NameColumn : DataGridTemplateColumn {
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem) {
            var contentPresenter = new ContentPresenter();
            this.Dispatcher?.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action<ContentPresenter>(x => {
                    BindingOperations.SetBinding(x, ContentPresenter.ContentProperty,
                        new Binding());
                    x.ContentTemplate = this.CellTemplate;
                    x.ContentTemplateSelector = this.CellTemplateSelector;
                }),
                contentPresenter);
            return contentPresenter;
        }
    }
}