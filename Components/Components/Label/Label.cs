using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace TeacherAssistant.Label
{
    /// <summary>
    /// Interaction logic for Label.xaml
    /// </summary>
    public class Label : ContentControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(Label), new PropertyMetadata(""));

        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register("LineHeight", typeof(int), typeof(Label), new PropertyMetadata(14));

        public static readonly DependencyProperty LabelWidthProperty =
            DependencyProperty.Register("LabelWidth", typeof(GridLength), typeof(Label),
                new PropertyMetadata(new GridLength(1, GridUnitType.Star)));

        public static readonly DependencyProperty ContentWidthProperty =
            DependencyProperty.Register("ContentWidth", typeof(GridLength), typeof(Label),
                new PropertyMetadata(new GridLength(1, GridUnitType.Star)));


        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public int LineHeight
        {
            get => (int) GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        public GridLength LabelWidth
        {
            get => (GridLength) GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        public GridLength ContentWidth
        {
            get => (GridLength) GetValue(ContentWidthProperty);
            set => SetValue(ContentWidthProperty, value);
        }

        public Label()
        {
            Resources = new ResourceDictionary
            {
                Source = new Uri("/TeacherAssistant.Components;component/Components/Label/LabelTemplate.xaml",
                    UriKind.RelativeOrAbsolute)
            };
            Template = (ControlTemplate) Resources["LabelTemplate"];
        }
    }
}
