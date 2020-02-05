using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Containers.Annotations;
using TeacherAssistant.ComponentsImpl;

namespace Containers
{
    public class ButtonConfig : INotifyPropertyChanged
    {
        private ICommand _command;
        private Visibility _visibility = Visibility.Visible;
        private bool _isEnabled = true;
        private string _text;
        private Image _icon;
        private string _tooltip;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Tooltip {
            get => _tooltip;
            set {
                if (value == _tooltip) return;
                _tooltip = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (value == _text) return;
                _text = value;
                OnPropertyChanged(nameof(this.Text));
            }
        }

        public ICommand Command
        {
            get => _command;
            set
            {
                if (Equals(value, _command)) return;
                _command = value;
                OnPropertyChanged(nameof(this.Command));
            }
        }

        public Visibility Visibility
        {
            get => _visibility;
            set
            {
                if (value == _visibility) return;
                _visibility = value;
                OnPropertyChanged(nameof(this.Visibility));
            }
        }

        public bool IsVisible
        {
            get => this.Visibility == Visibility.Visible;
            set => this.Visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value == _isEnabled) return;
                _isEnabled = value;
                OnPropertyChanged(nameof(this.IsEnabled));
            }
        }

        public Image Icon {
            get => _icon;
            set {
                if (Equals(value, _icon)) return;
                _icon = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
