using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace GeometryViewer3B
{
    public class GeometryCanvas : FrameworkElement
    {
        // =============================
        // Items (your geometries)
        // =============================
        public IEnumerable Items
        {
            get => (IEnumerable)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(
                nameof(Items),
                typeof(IEnumerable),
                typeof(GeometryCanvas),
                new FrameworkPropertyMetadata(null, OnItemsChanged));

        // =============================
        // WorldWindow (camera)
        // =============================
        public Rect WorldWindow
        {
            get => (Rect)GetValue(WorldWindowProperty);
            set => SetValue(WorldWindowProperty, value);
        }

        public static readonly DependencyProperty WorldWindowProperty =
            DependencyProperty.Register(
                nameof(WorldWindow),
                typeof(Rect),
                typeof(GeometryCanvas),
                new FrameworkPropertyMetadata(Rect.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        // =============================
        // Handle collection changes
        // =============================
        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = (GeometryCanvas)d;

            if (e.OldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= canvas.OnCollectionChanged;

            if (e.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += canvas.OnCollectionChanged;

            canvas.InvalidateVisual();
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        // =============================
        // Rendering
        // =============================
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Debug background
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            if (Items == null || WorldWindow.IsEmpty)
                return;

            var transform = CreateWorldToViewportTransform(WorldWindow, RenderSize);

            var pen = new Pen(Brushes.Black, 1); // always 1 pixel

            foreach (var item in Items)
            {
                if (item is LineModel line)
                {
                    var p1 = transform.Transform(line.P1);
                    var p2 = transform.Transform(line.P2);

                    dc.DrawLine(pen, p1, p2);
                }
            }
        }

        // =============================
        // Transform
        // =============================
        private Matrix CreateWorldToViewportTransform(Rect world, Size viewport)
        {
            var scaleX = viewport.Width / world.Width;
            var scaleY = viewport.Height / world.Height;

            var m = Matrix.Identity;

            m.Translate(-world.X, -world.Y);
            m.Scale(scaleX, -scaleY); // Y-up → Y-down
            m.Translate(0, viewport.Height);

            return m;
        }
    }
}