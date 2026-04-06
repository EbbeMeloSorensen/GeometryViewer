using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace GeometryViewer3
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
        public Point WorldOrigin
        {
            get => (Point)GetValue(WorldOriginProperty);
            set => SetValue(WorldOriginProperty, value);
        }

        public Size Scaling
        {
            get => (Size)GetValue(ScalingProperty);
            set => SetValue(ScalingProperty, value);
        }

        public static readonly DependencyProperty WorldOriginProperty =
            DependencyProperty.Register(
                nameof(WorldOrigin),
                typeof(Point),
                typeof(GeometryCanvas),
                new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ScalingProperty =
            DependencyProperty.Register(
                nameof(Scaling),
                typeof(Size),
                typeof(GeometryCanvas),
                new FrameworkPropertyMetadata(new Size(1, 1), FrameworkPropertyMetadataOptions.AffectsRender));

        public Rect WorldWindow => ComputeWorldWindow();

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

            var worldWindow = ComputeWorldWindow();

            if (Items == null || worldWindow.IsEmpty)
                return;

            var transform = CreateWorldToViewportTransform(worldWindow, RenderSize);
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

        // Suggested by ChatGpt, but it seems unnecessary
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
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
            m.Scale(scaleX, scaleY);

            return m;
        }

        private Rect ComputeWorldWindow()
        {
            var width = ActualWidth / Scaling.Width;
            var height = ActualHeight / Scaling.Height;

            return new Rect(WorldOrigin.X, WorldOrigin.Y, width, height);
        }
    }
}