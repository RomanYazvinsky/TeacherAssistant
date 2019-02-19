using System.Collections.Generic;
using System.ComponentModel;

namespace TeacherAssistant.Components.Table
{
    public class ListViewComparer<T> : Comparer<T>
    {
        public List<SortDescription> SortOrder { get; set; }

        public ListViewComparer(List<SortDescription> sortOrder)
        {
            SortOrder = sortOrder;
        }

        public override int Compare(T x, T y)
        {
            throw new System.NotImplementedException();
        }
    }
    public class ColumnConfig
    {
        public string PropertyPath { get; set; }
        public string StringFormat { get; set; }
        public double DefaultWidth { get; set; }
        public bool SortEnabled { get; set; } = true;
    }
    public class TableConfig<T>
    {
        public Dictionary<string, ColumnConfig> ColumnConfigs { get; set; }
        public ListViewComparer<T> DefaultSortColumn { get; set; }
        public bool SelectOnClick { get; set; }
        public bool SelectOnDoubleClick { get; set; }
        public bool SelectOnEnter { get; set; }
        public bool EnableMultiSelect { get; set; }
    }
}