using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace TeacherAssistant.ComponentsImpl {
    public class TemplateBuilder {

        public static FrameworkElementFactory BuildElementTemplate(Type type,
            Dictionary<DependencyProperty, Binding> bindings, Dictionary<DependencyProperty, object> values) {
            var frameworkElementFactory = new FrameworkElementFactory(type);
            foreach (var pair in bindings) {
                frameworkElementFactory.SetBinding(pair.Key, pair.Value);
            }

            foreach (var pair in values) {
                frameworkElementFactory.SetValue(pair.Key, pair.Value);
            }

            return frameworkElementFactory;
        }
    }
}