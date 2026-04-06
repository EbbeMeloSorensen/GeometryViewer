using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeometryViewer2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new GeometryViewModel();

            vm.Lines.Add(new LineModel
            {
                P1 = new Point(0, 0),
                P2 = new Point(100, 50)
            });

            vm.Lines.Add(new LineModel
            {
                P1 = new Point(50, 80),
                P2 = new Point(150, 20)
            });

            DataContext = vm;
        }
    }
}