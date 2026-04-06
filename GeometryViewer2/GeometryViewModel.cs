using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GeometryViewer2;

public class GeometryViewModel : INotifyPropertyChanged
{
    private Rect _worldWindow = new Rect(0, 0, 200, 100);

    public ObservableCollection<LineModel> Lines { get; }
        = new ObservableCollection<LineModel>();

    public Rect WorldWindow
    {
        get => _worldWindow;
        set { _worldWindow = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}