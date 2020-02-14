using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using Containers;
using DynamicData;
using JetBrains.Annotations;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Components.TableFilter {
    public class DragConfig {
        public string SourceId { get; set; }
        public string SourceType { get; set; }
        public Func<DragData, bool> DropAvailability { get; set; } = data => false;
        public string DragValuePath { get; set; }
        public Action Drop { get; set; } = () => { };
        public Action DragSuccess { get; set; } = () => { };
        public Action<DragData> DragStart { get; set; } = d => { };
    }


    public class TableConfig {
        public DragConfig DragConfig { get; set; }
        public ObservableCollection<object> TableItems { get; set; } = new ObservableCollection<object>();

        public ObservableCollection<object> SelectedItems { get; set; } =
            new ObservableCollection<object>();

        [NotNull] public BehaviorSubject<object> SelectedItem { get; set; } = new BehaviorSubject<object>(null);

        [NotNull]
        public Dictionary<string, ListSortDirection> Sorts { get; set; } = new Dictionary<string, ListSortDirection>();

        [CanBeNull] public Func<object, string, bool> Filter { get; set; } = (o, s) => true;

        [CanBeNull] public IEnumerable<GridLength> ColumnWidths { get; set; }

        public bool IsFilterDependsOnlyOnFilterValue { get; set; } = true;
    }

    /// <summary>
    /// Interaction logic for TableFilter.xaml
    /// </summary>
    [ContentProperty("View")]
    public partial class TableFilter : ContentControl {
        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register
            (
                "FilterText",
                typeof(string),
                typeof(TableFilter),
                new FrameworkPropertyMetadata {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    DefaultValue = string.Empty
                }
            );

        public static readonly DependencyProperty ViewProperty =
            DependencyProperty.Register("View", typeof(ViewBase), typeof(TableFilter));

        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register("LineHeight", typeof(int), typeof(TableFilter), new PropertyMetadata(14));

        public static readonly DependencyProperty TableItemContainerStyleProperty =
            DependencyProperty.Register
            (
                "TableItemContainerStyle",
                typeof(Style),
                typeof(TableFilter),
                new PropertyMetadata(defaultValue: null)
            );

        public static readonly DependencyProperty TextBoxHeightProperty =
            DependencyProperty.Register
            (
                "TextBoxHeight",
                typeof(GridLength),
                typeof(TableFilter),
                new PropertyMetadata(new GridLength(40, GridUnitType.Pixel))
            );

        public static readonly DependencyProperty ContentHeightProperty =
            DependencyProperty.Register
            (
                "ContentHeight",
                typeof(GridLength),
                typeof(TableFilter),
                new PropertyMetadata(new GridLength(1, GridUnitType.Star))
            );

        public static readonly DependencyProperty FilterHeightProperty =
            DependencyProperty.Register
            (
                "FilterHeight",
                typeof(GridLength),
                typeof(TableFilter),
                new PropertyMetadata(new GridLength(40, GridUnitType.Pixel))
            );

        public static readonly DependencyProperty TableConfigProperty =
            DependencyProperty.Register
            (
                "TableConfig",
                typeof(TableConfig),
                typeof(TableFilter),
                new FrameworkPropertyMetadata(defaultValue: new TableConfig())
            );

        private Point? _dragStartPoint;
        private readonly List<object> _selectedItems = new List<object>();
        private DragConfig DragConfig => this.TableConfig.DragConfig;

        public string FilterText {
            get => (string) GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }

        public int LineHeight {
            get => (int) GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        public GridLength TextBoxHeight {
            get => (GridLength) GetValue(TextBoxHeightProperty);
            set => SetValue(TextBoxHeightProperty, value);
        }

        public GridLength ContentHeight {
            get => (GridLength) GetValue(ContentHeightProperty);
            set => SetValue(ContentHeightProperty, value);
        }

        public GridLength FilterHeight {
            get => (GridLength) GetValue(FilterHeightProperty);
            set => SetValue(FilterHeightProperty, value);
        }

        public Style TableItemContainerStyle {
            get => (Style) GetValue(TableItemContainerStyleProperty);
            set => SetValue(TableItemContainerStyleProperty, value);
        }

        public object View {
            get => GetValue(ViewProperty);
            set => SetValue(ViewProperty, value);
        }

        public TableConfig TableConfig {
            get => (TableConfig) GetValue(TableConfigProperty);
            set => SetValue(TableConfigProperty, value);
        }

        public TableFilter() {
            InitializeComponent();
        }

        private void UpdateFilter(string text) {
            var collectionView = CollectionViewSource.GetDefaultView(this.TableConfig.TableItems);
            if (!string.IsNullOrWhiteSpace(text) || !this.TableConfig.IsFilterDependsOnlyOnFilterValue) {
                text = text?.ToUpperInvariant() ?? string.Empty;
                collectionView.Filter = o => this.TableConfig.Filter?.Invoke(o, text) ?? true;
            }
            else {
                collectionView.Filter = null;
            }
        }

        private void BuildColumnWidthHelpers([CanBeNull] IEnumerable<GridLength> widths) {
            ColumnWidthHelper.ColumnDefinitions.Clear();
            if (widths == null || !widths.Any()) {
                return;
            }

            var columnDefinitions = widths.Select(length => new ColumnDefinition {Width = length}).ToList();


            ColumnWidthHelper.ColumnDefinitions.Add(columnDefinitions);
            var gridColumns = (LView.View as GridView)?.Columns ?? new GridViewColumnCollection();
            if (gridColumns.Count == 0) {
                return;
            }

            var gridViewColumn = gridColumns[0];
            gridViewColumn.Width = double.NaN;
            BindingOperations.SetBinding(columnDefinitions[0], ColumnDefinition.WidthProperty, new Binding {
                Path = new PropertyPath(nameof(GridViewColumn.ActualWidth)),
                Source = gridViewColumn
            });
            for (var i = 1; i < gridColumns.Count; i++) {
                if (columnDefinitions.Count >= i) {
                    break;
                }

                var columnDefinition = columnDefinitions[i];
                BindingOperations.SetBinding(gridColumns[i], GridViewColumn.WidthProperty, new Binding {
                    Path = new PropertyPath(nameof(ColumnDefinition.ActualWidth)),
                    Source = columnDefinition
                });
            }

            ColumnWidthHelper.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(20)});
        }

        private void SetSelectedItems(IEnumerable items) {
            LView.SelectedItems.Clear();
            foreach (var item in items) {
                LView.SelectedItems.Add(item);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            if (e.Property == FilterTextProperty) {
                UpdateFilter((string) e.NewValue);
            }

            if (e.Property == ViewProperty) {
                LView.View = (ViewBase) e.NewValue;
            }

            if (e.Property == TableItemContainerStyleProperty) {
                if (e.NewValue != null) {
                    var style = LView.ItemContainerStyle;
                    style.BasedOn = (Style) e.NewValue;
                    LView.ItemContainerStyle = style;
                }
            }

            if (e.Property == TableConfigProperty && e.NewValue != null) {
                var config = (TableConfig) e.NewValue;
                SortHelper.AddColumnSorting(LView, config.Sorts);
                SetSelectedItems(config.SelectedItems);
                UpdateFilter("");
                BuildColumnWidthHelpers(config.ColumnWidths);
            }
        }

        private void LView_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.TableConfig.SelectedItems.Clear();
            this.TableConfig.SelectedItems.AddRange(LView.SelectedItems.Cast<object>().ToList());
            this.TableConfig.SelectedItem.OnNext(LView.SelectedItem);
        }

        private void LView_OnDrop(object sender, DragEventArgs e) {
            this.DragConfig?.Drop.Invoke();
        }

        private bool IsDrag(MouseEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || _dragStartPoint == null) {
                return false;
            }

            var point = e.GetPosition(null);
            var diff = _dragStartPoint - point;
            return (Math.Abs(diff.Value.X) > 10 || Math.Abs(diff.Value.Y) > 10);
        }

        private void ListViewItemDragStart(object sender, MouseEventArgs e) {
            if (this.DragConfig == null || sender == null || !IsDrag(e) || e.Source == null) {
                return;
            }

            LView.SelectedItems.Clear();
            _selectedItems.ForEach(item => LView.SelectedItems.Add(item));
            DragData dragData;
            var items = _selectedItems.ToList();
            var dragValuePath = this.DragConfig.DragValuePath;
            if (string.IsNullOrEmpty(dragValuePath) || items.Count == 0) {
                dragData = new DragData(this.DragConfig.SourceType, this.DragConfig.SourceId, items,
                    this.DragConfig.DragSuccess);
            }
            else {
                var property = items[0].GetType().GetProperty(dragValuePath);
                if (property == null) {
                    throw new ArgumentException($"Property {dragValuePath} not found");
                }

                var itemsValues = items.Select(o => property.GetValue(o)).ToList();
                dragData = new DragData(this.DragConfig.SourceType, this.DragConfig.SourceId, itemsValues,
                    this.DragConfig.DragSuccess);
            }

            this.DragConfig.DragStart?.Invoke(dragData);
            DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e) {
            _dragStartPoint = e.GetPosition(null);
            _selectedItems.Clear();

            if (LView.SelectedItems.Count > 1) {
                foreach (var selectedItem in LView.SelectedItems) {
                    _selectedItems.Add(selectedItem);
                }
            }
            else {
                _selectedItems.Add(((ListViewItem) sender).DataContext);
            }
        }
    }
}
