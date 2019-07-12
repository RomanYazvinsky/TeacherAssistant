using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace TeacherAssistant.ComponentsImpl {
    public class SortHelper : Adorner {
        public static void AddColumnSorting(ListView listView, Dictionary<string, ListSortDirection> sorts) {
            GridViewColumnHeader listViewSortCol = null;
            SortHelper listViewSortHelper = null;

            listView.Items.SortDescriptions.Clear();
            foreach (var listSortDirection in sorts) {
                listView.Items.SortDescriptions.Add
                    (new SortDescription(listSortDirection.Key, listSortDirection.Value));
            }

            void Down(object sender, MouseButtonEventArgs args) {
                if (!(sender is GridViewColumnHeader column))
                    return;
                var sortBy = column.Tag.ToString();
                if (listViewSortCol != null) {
                    AdornerLayer.GetAdornerLayer(listViewSortCol)?.Remove(listViewSortHelper);
                    listView.Items.SortDescriptions.Clear();
                }

                var newDir = ListSortDirection.Ascending;
                if (listViewSortCol == column && listViewSortHelper.Direction == newDir)
                    newDir = ListSortDirection.Descending;

                listViewSortCol = column;
                listViewSortHelper = new SortHelper(listViewSortCol, newDir);
                AdornerLayer.GetAdornerLayer(listViewSortCol)?.Add(listViewSortHelper);

                listView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
                foreach (var keyValuePair in sorts.Where(pair => !pair.Key.Equals(sortBy))) {
                    listView.Items.SortDescriptions.Add(new SortDescription(keyValuePair.Key, keyValuePair.Value));
                }
            }

            foreach (var gridViewColumn in ((GridView) listView.View).Columns) {
                var gridViewColumnHeader = (GridViewColumnHeader) gridViewColumn.Header;
                if (gridViewColumnHeader != null) {
                    gridViewColumnHeader.MouseDown += Down;
                }
            }
        }

        private static Geometry ascGeometry =
            Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

        private static Geometry descGeometry =
            Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

        public ListSortDirection Direction { get; private set; }

        public SortHelper(UIElement element, ListSortDirection direction)
            : base(element) {
            Direction = direction;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
                return;

            TranslateTransform transform = new TranslateTransform
            (
                AdornedElement.RenderSize.Width - 15,
                (AdornedElement.RenderSize.Height - 5) / 2
            );
            drawingContext.PushTransform(transform);

            Geometry geometry = ascGeometry;
            if (this.Direction == ListSortDirection.Descending)
                geometry = descGeometry;
            drawingContext.DrawGeometry(Brushes.Black, null, geometry);

            drawingContext.Pop();
        }
    }
}