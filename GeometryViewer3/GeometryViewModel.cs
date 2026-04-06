using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GeometryViewer3
{
    public class GeometryViewModel : INotifyPropertyChanged
    {
        public Point WorldOrigin { get; set; }   // top-left
        public Size Scaling { get; set; }

        public ObservableCollection<LineModel> Lines { get; }
            = new ObservableCollection<LineModel>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}