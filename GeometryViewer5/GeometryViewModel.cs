using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GeometryViewer5
{
    public class GeometryViewModel : INotifyPropertyChanged
    {
        private Size _scaling;
        private Point? _cursorWorldPosition;

        public Size Scaling
        {
            get => _scaling;
            set
            {
                _scaling = value;
                OnPropertyChanged();
            }
        }

        public Point? CursorWorldPosition
        {
            get => _cursorWorldPosition;
            set
            {
                _cursorWorldPosition = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LineModel> Lines { get; }
            = new ObservableCollection<LineModel>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}