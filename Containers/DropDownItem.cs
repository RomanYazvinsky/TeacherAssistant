namespace Containers {
    public class DropDownItem<T> {
        public string Label { get; set; }
        public T Value { get; set; }

        public DropDownItem(string label, T value) {
            this.Label = label;
            this.Value = value;
        }
    }
}