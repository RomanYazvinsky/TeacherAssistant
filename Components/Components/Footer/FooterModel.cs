using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Footer
{
    public class FooterModel : AbstractModel
    {
        private static readonly string LocalizationKey = "element.footer";

        [Reactive] public List<StatusBarItem> StatusItems { get; set; }

        public FooterModel(string id) : base(id)
        {
        }

        protected override string GetLocalizationKey()
        {
            return LocalizationKey;
        }

        public override Task Init()
        {
            SelectCollection<StatusBarItem>("StatusItems").Subscribe(items =>
            {
                if (items == null) return;
                this.StatusItems = new List<StatusBarItem>(items);
            });
            return Task.CompletedTask;
        }
    }
}