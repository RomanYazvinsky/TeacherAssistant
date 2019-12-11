using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Containers;
using DynamicData;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.Components.TableFilter {
    public class DragConfig {
        public string SourceId { get; set; }
        public string SourceType { get; set; }
        public Func<DragData, bool> DropAvailability { get; set; } = data => false;
        public string DragValuePath { get; set; }
        public Action<DragData> Drop { get; set; } = data => { };
        public Action DragSuccess { get; set; } = () => { };
        public Action DragStart { get; set; } = () => { };
    }


    public class TableConfig {
        public DragConfig DragConfig { get; set; }
        public ObservableRangeCollection<object> TableItems { get; set; } = new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedItems { get; set; } =
            new WpfObservableRangeCollection<object>();

        public BehaviorSubject<object> SelectedItem { get; set; } = new BehaviorSubject<object>(null);
        public Dictionary<string, ListSortDirection> Sorts { get; set; } = new Dictionary<string, ListSortDirection>();
        public Func<object, string, bool> Filter { get; set; } = (o, s) => true;
    }

    /// <summary>
    /// Interaction logic for TableFilter.xaml
    /// </summary>
    [ContentProperty("View")]
    public partial class TableFilter : UserControl {
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

        private IDisposable _dragSubscription;
        private static Point? _dragStartPoint;
        private bool _isMouseDown;
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
            _dragSubscription = Storage.Instance.PublishedDataStore
                .DistinctUntilChanged(containers => containers["DragData"])
                .Select(containers => containers["DragData"].GetData<DragData>())
                .Where(data => data != null)
                .Subscribe(container => {
                    if (this.DragConfig == null) {
                        return;
                    }

                    LView.AllowDrop = !Equals(container.SenderId, this.DragConfig?.SourceId)
                                      && (this.DragConfig?.DropAvailability?.Invoke(container)
                                          ?? false);
                });
            Unloaded += (sender, args) => { _dragSubscription.Dispose(); };
        }

        private void UpdateFilter(string text) {
            var collectionView = CollectionViewSource.GetDefaultView(this.TableConfig.TableItems);
            if (!string.IsNullOrWhiteSpace(text)) {
                collectionView.Filter = o => this.TableConfig.Filter?.Invoke(o, text) ?? true;
            }
            else {
                collectionView.Filter = o => true;
            }
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
            }
        }

        private void LView_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.TableConfig.SelectedItems.Clear();
            this.TableConfig.SelectedItems.AddRange(LView.SelectedItems.Cast<object>().ToList());
            this.TableConfig.SelectedItem.OnNext(LView.SelectedItem);
        }

        private void LView_OnDrop(object sender, DragEventArgs e) {
            this.DragConfig?.Drop.Invoke(StoreManager.Get<DragData>("DragData"));
        }

        private static bool IsDrag(MouseEventArgs e) {
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

            this.DragConfig.DragStart?.Invoke();
            new Storage.DragStart(dragData).Dispatch();
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