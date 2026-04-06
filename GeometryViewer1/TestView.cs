using System.Windows;
using System.Windows.Media;

namespace GeometryViewer1
{
    public class TestView : FrameworkElement
    {
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Draw a simple diagonal line across the control
            var pen = new Pen(Brushes.Black, 1);

            dc.DrawLine(
                pen,
                new Point(10, 10),
                new Point(200, 100));
        }
    }
}
