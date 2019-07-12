using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class TextColumn : DataGridTextColumn {

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem) {
            var textBlock = new TextBlock();

            this.Dispatcher?.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action<TextBlock>(x => {
                    x.SetValue(FrameworkElement.StyleProperty, this.ElementStyle);
                    x.SetBinding(TextBlock.TextProperty, this.Binding);
                }),
                textBlock);
            return textBlock;
        }
    }
}