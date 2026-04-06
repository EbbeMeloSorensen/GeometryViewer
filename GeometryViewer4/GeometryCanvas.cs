using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GeometryViewer4
{
    public class GeometryCanvas : FrameworkElement
    {
        private bool _isPanning;
        private Point _panStartMouse;
        private Point _panStartWorldOrigin;

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

        public Point? CursorWorldPosition
        {
            get => (Point)GetValue(CursorWorldPositionProperty);
            set => SetValue(CursorWorldPositionProperty, value);
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

        public static readonly DependencyProperty CursorWorldPositionProperty =
            DependencyProperty.Register(
                nameof(CursorWorldPosition),
                typeof(Point?),
                typeof(GeometryCanvas),
                new FrameworkPropertyMetadata(null));

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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Mouse.OverrideCursor = Cursors.Hand;
                _isPanning = true;
                _panStartMouse = e.GetPosition(this);
                _panStartWorldOrigin = WorldOrigin;

                CaptureMouse();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var mousePos = e.GetPosition(this);

            var worldWindow = ComputeWorldWindow();
            var transform = CreateWorldToViewportTransform(worldWindow, RenderSize);
            var inverse = transform;
            inverse.Invert();
            var isActive = IsMouseOver || _isPanning;

            if (isActive)
            {
                inverse.Invert();

                CursorWorldPosition = inverse.Transform(mousePos);
            }
            else
            {
                CursorWorldPosition = null;
            }

            if (_isPanning)
            {
                var currentMouse = mousePos;

                var deltaPixels = currentMouse - _panStartMouse;

                var scaleX = Scaling.Width;
                var scaleY = Scaling.Height;

                // Convert pixel delta → world delta
                var deltaWorldX = deltaPixels.X / scaleX;
                var deltaWorldY = deltaPixels.Y / scaleY;

                WorldOrigin = new Point(
                    _panStartWorldOrigin.X - deltaWorldX,
                    _panStartWorldOrigin.Y - deltaWorldY);
            }
            else
            {
                var worldPos = inverse.Transform(mousePos);
                CursorWorldPosition = worldPos;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isPanning)
            {
                Mouse.OverrideCursor = Cursors.Arrow;
                _isPanning = false;
                ReleaseMouseCapture();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (!_isPanning)
            {
                CursorWorldPosition = null;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (ActualWidth == 0 || ActualHeight == 0)
                return;

            var mousePos = e.GetPosition(this);

            var worldWindow = ComputeWorldWindow();
            var transform = CreateWorldToViewportTransform(worldWindow, RenderSize);

            var inverse = transform;
            inverse.Invert();

            // 1. World point under cursor BEFORE zoom
            var worldBefore = inverse.Transform(mousePos);

            // 2. Determine zoom factor
            var zoomFactor = e.Delta > 0 ? 0.8 : 1.2;

            var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            var alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;

            var scaleX = Scaling.Width;
            var scaleY = Scaling.Height;

            if (ctrl)
            {
                // X only
                scaleX *= zoomFactor;
            }
            else if (alt)
            {
                // Y only
                scaleY *= zoomFactor;
            }
            else
            {
                // uniform
                scaleX *= zoomFactor;
                scaleY *= zoomFactor;
            }

            var newScaling = new Size(scaleX, scaleY);

            // 3. Compute new origin so cursor stays fixed
            // Screen → world relation:
            // world = origin + pixel * unitsPerPixel

            var newOriginX = worldBefore.X - mousePos.X / newScaling.Width;
            var newOriginY = worldBefore.Y - mousePos.Y / newScaling.Height;

            // 4. Apply
            Scaling = newScaling;
            WorldOrigin = new Point(newOriginX, newOriginY);
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