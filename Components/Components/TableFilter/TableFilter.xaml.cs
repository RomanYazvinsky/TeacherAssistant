using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
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

        public static readonly DependencyProperty SortsProperty =
            DependencyProperty.Register
            (
                "Sorts",
                typeof(Dictionary<string, ListSortDirection>),
                typeof(TableFilter)
            );

        public static readonly DependencyProperty TableItemsProperty =
            DependencyProperty.Register
            (
                "TableItems",
                typeof(ObservableRangeCollection<object>),
                typeof(TableFilter),
                new FrameworkPropertyMetadata() {
                    DefaultValue = new WpfObservableRangeCollection<object>()
                }
            );

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register
            (
                "SelectedItems",
                typeof(ObservableRangeCollection<object>),
                typeof(TableFilter),
                new PropertyMetadata(new WpfObservableRangeCollection<object>())
            );

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register
            (
                "SelectedItem",
                typeof(object),
                typeof(TableFilter),
                new FrameworkPropertyMetadata {
                    BindsTwoWayByDefault = true,
                    DefaultValue = null
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

        public static readonly DependencyProperty FilterFunctionProperty =
            DependencyProperty.Register
            (
                "FilterFunction",
                typeof(Func<object, string, bool>),
                typeof(TableFilter),
                new PropertyMetadata(defaultValue: null)
            );

        public static readonly DependencyProperty DropAvailabilityProperty =
            DependencyProperty.Register
            (
                "DropAvailability",
                typeof(Func<DragData, bool>),
                typeof(TableFilter),
                new PropertyMetadata(defaultValue: new Func<DragData, bool>(o => false))
            );

        public static readonly DependencyProperty DragValuePathProperty =
            DependencyProperty.Register
            (
                "DragValuePath",
                typeof(string),
                typeof(TableFilter),
                new PropertyMetadata(defaultValue: null)
            );

        public static readonly DependencyProperty DropProperty =
            DependencyProperty.Register
            (
                "Drop",
                typeof(Action<DragData>),
                typeof(TableFilter),
                new PropertyMetadata(defaultValue: null)
            );

        public static readonly DependencyProperty DragSuccessProperty =
            DependencyProperty.Register
            (
                "DragSuccess",
                typeof(Action),
                typeof(TableFilter),
                new PropertyMetadata(defaultValue: null)
            );

        public static readonly DependencyProperty DragStartProperty =
            DependencyProperty.Register
            (
                "DragStart",
                typeof(Action),
                typeof(TableFilter),
                new FrameworkPropertyMetadata(defaultValue: null,
                    (o, args) => { (o as TableFilter)._DragStart = (Action) args.NewValue; }) {
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            );

        private IDisposable _dragSubscription;
        private static Point? _dragStartPoint;
        private bool _isMouseDown;
        private readonly List<object> _selectedItems = new List<object>();

        public string FilterText {
            get => (string) GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }

        public Dictionary<string, ListSortDirection> Sorts {
            get => (Dictionary<string, ListSortDirection>) GetValue(SortsProperty);
            set => SetValue(SortsProperty, value);
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

        public ObservableCollection<object> TableItems {
            get => (ObservableCollection<object>) GetValue(TableItemsProperty);
            set => SetValue(TableItemsProperty, value);
        }

        public ObservableRangeCollection<object> SelectedItems {
            get => (ObservableRangeCollection<object>) GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public object View {
            get => GetValue(ViewProperty);
            set => SetValue(ViewProperty, value);
        }

        public object SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public Func<object, string, bool> FilterFunction {
            get => (Func<object, string, bool>) GetValue(FilterFunctionProperty);
            set => SetValue(FilterFunctionProperty, value);
        }

        public Func<DragData, bool> DropAvailability {
            get => (Func<DragData, bool>) GetValue(DropAvailabilityProperty);
            set => SetValue(DropAvailabilityProperty, value);
        }

        public string DragValuePath {
            get => (string) GetValue(DragValuePathProperty);
            set => SetValue(DragValuePathProperty, value);
        }

        public Action<DragData> Drop {
            get => (Action<DragData>) GetValue(DropProperty);
            set => SetValue(DropProperty, value);
        }

        public Action DragSuccess {
            get => (Action) GetValue(DragSuccessProperty);
            set => SetValue(DragSuccessProperty, value);
        }

        public Action DragStart {
            get => (Action) GetValue(DragStartProperty);
            set => SetValue(DragStartProperty, value);
        }

        private Action _DragStart { get; set; }

        public TableFilter() {
            InitializeComponent();
            _dragSubscription = Storage.Instance.PublishedDataStore
                .DistinctUntilChanged(containers => containers["DragData"])
                .Select(containers => containers["DragData"].GetData<DragData>())
                .Where(data => data != null)
                .Subscribe(container => {
                    LView.AllowDrop = !Equals(container.Sender, LView)
                                      && (this.DropAvailability?.Invoke(container)
                                          ?? false);
                });
            Unloaded += (sender, args) => { _dragSubscription.Dispose(); };
        }

        private void UpdateFilter(string text) {
            var collectionView = CollectionViewSource.GetDefaultView(this.TableItems);
            if (!string.IsNullOrWhiteSpace(text)) {
                collectionView.Filter = o => this.FilterFunction?.Invoke(o, text) ?? true;
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

            if (e.Property == SortsProperty) {
                SortHelper.AddColumnSorting(LView, (Dictionary<string, ListSortDirection>) e.NewValue);
            }

            if (e.Property == SelectedItemsProperty) {
                SetSelectedItems((IEnumerable) (e.NewValue ?? new ObservableCollection<object>()));
            }

            if (e.Property == TableItemContainerStyleProperty) {
                if (e.NewValue != null) {
                    var style = LView.ItemContainerStyle;
                    style.BasedOn = (Style) e.NewValue;
                    LView.ItemContainerStyle = style;
                }
            }
        }

        private void LView_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.SelectedItems.Clear();
            this.SelectedItems.AddRange(LView.SelectedItems.Cast<object>().ToList());
            this.SelectedItem = LView.SelectedItem;
        }

        private void LView_OnDrop(object sender, DragEventArgs e) {
            this.Drop?.Invoke(StoreManager.Get<DragData>("DragData"));
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
            if (sender == null) {
                return;
            }

            if (!IsDrag(e)) {
                return;
            }

            if (e.Source == null) {
                return;
            }
            LView.SelectedItems.Clear();
            _selectedItems.ForEach(item => LView.SelectedItems.Add(item));
            DragData dragData;
            var items = _selectedItems.ToList();
            var dragValuePath = this.DragValuePath;
            if (string.IsNullOrEmpty(dragValuePath) || items.Count == 0) {
                dragData = new DragData(this, items, this.DragSuccess);
            }
            else {
                var property = items[0].GetType().GetProperty(dragValuePath);
                if (property == null) {
                    throw new ArgumentException($"Property {dragValuePath} not found");
                }

                var itemsValues = items.Select(o => property.GetValue(o)).ToList();
                dragData = new DragData(this, itemsValues, this.DragSuccess);
            }

            this._DragStart?.Invoke();
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

        public static T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement {
            while (true) {
                var parent = VisualTreeHelper.GetParent(child);
                if (parent != null && !(parent is T)) {
                    child = (FrameworkElement) parent;
                    continue;
                }

                return (T) parent;
            }
        }
    }
}