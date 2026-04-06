using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GeometryViewer4
{
    public class GeometryCanvas : FrameworkElement
    {
        private const double _zoomBase = 1.2;
        private const int _minZoomLevel = -20;
        private const int _maxZoomLevel = 20;

        private int _zoomLevelX;
        private int _zoomLevelY;

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

            DrawGrid(dc);
            DrawAxes(dc);
            DrawAxisTicks(dc);
            DrawGridLabels(dc);

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

            if (_isPanning)
            {
                // Convert pixel delta to world delta
                var worldWindow = ComputeWorldWindow();
                var transform = CreateWorldToViewportTransform(worldWindow, RenderSize);
                var inverse = transform;

                inverse.Invert();

                var worldStart = inverse.Transform(_panStartMouse);
                var worldCurrent = inverse.Transform(mousePos);
                var deltaWorld = worldStart - worldCurrent;

                WorldOrigin = new Point(
                    _panStartWorldOrigin.X + deltaWorld.X,
                    _panStartWorldOrigin.Y + deltaWorld.Y);
            }
            else
            {
                if (IsMouseOver)
                {
                    var scaleX = Scaling.Width;
                    var scaleY = Scaling.Height;
                    var worldX = WorldOrigin.X + mousePos.X / scaleX;
                    var worldY = WorldOrigin.Y + mousePos.Y / scaleY;
                    CursorWorldPosition = new Point(worldX, worldY);
                }
                else
                {
                    CursorWorldPosition = null;
                }
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
            //var zoomFactor = e.Delta > 0 ? 1.2 : 0.8;
            //var zoomFactor = Math.Pow(0.999, -e.Delta);

            var steps = e.Delta / 120; // standard wheel notch

            var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            var alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;

            if (ctrl)
            {
                // X only
                _zoomLevelX += steps;
            }
            else if (alt)
            {
                // Y only
                _zoomLevelY += steps;
            }
            else
            {
                // uniform
                _zoomLevelX += steps;
                _zoomLevelY += steps;
            }

            if (_zoomLevelX > _maxZoomLevel)
            {
                _zoomLevelX = _maxZoomLevel;
            }
            else if (_zoomLevelX < _minZoomLevel)
            {
                _zoomLevelX = _minZoomLevel;
            }

            if (_zoomLevelY > _maxZoomLevel)
            {
                _zoomLevelY = _maxZoomLevel;
            }
            else if (_zoomLevelY < _minZoomLevel)
            {
                _zoomLevelY = _minZoomLevel;
            }

            var newScaling = new Size(
                Math.Pow(_zoomBase, _zoomLevelX),
                Math.Pow(_zoomBase, _zoomLevelY));

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

        private void DrawGrid(DrawingContext dc)
        {
            if (ActualWidth == 0 || ActualHeight == 0)
                return;

            var scaleX = Scaling.Width;
            var scaleY = Scaling.Height;

            var stepX = GetNiceStep(scaleX);
            var stepY = GetNiceStep(scaleY);

            var world = ComputeWorldWindow();

            var pen = new Pen(Brushes.LightGray, 1);

            // Vertical lines
            for (var x = Math.Floor(world.X / stepX) * stepX; x < world.Right; x += stepX)
            {
                var screenX = (x - WorldOrigin.X) * scaleX;

                dc.DrawLine(
                    pen,
                    new Point(screenX, 0),
                    new Point(screenX, ActualHeight));
            }

            // Horizontal lines
            for (var y = Math.Floor(world.Y / stepY) * stepY; y < world.Bottom; y += stepY)
            {
                var screenY = (y - WorldOrigin.Y) * scaleY;

                dc.DrawLine(
                    pen,
                    new Point(0, screenY),
                    new Point(ActualWidth, screenY));
            }
        }

        private double GetNiceStep(double scaling)
        {
            var targetPixels = 100; // desired spacing

            var rawStep = targetPixels / scaling;

            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
            var normalized = rawStep / magnitude;

            double nice;

            if (normalized < 1.5) nice = 1;
            else if (normalized < 3) nice = 2;
            else if (normalized < 7) nice = 5;
            else nice = 10;

            return nice * magnitude;
        }

        private double WorldToScreenX(double worldX)
        {
            return (worldX - WorldOrigin.X) * Scaling.Width;
        }

        private double WorldToScreenY(double worldY)
        {
            return (worldY - WorldOrigin.Y) * Scaling.Height;
        }

        private void DrawAxes(DrawingContext dc)
        {
            var pen = new Pen(Brushes.Black, 2);

            var world = ComputeWorldWindow();

            // Y-axis (x = 0)
            if (world.Left <= 0 && world.Right >= 0)
            {
                double screenX = WorldToScreenX(0);

                dc.DrawLine(
                    pen,
                    new Point(screenX, 0),
                    new Point(screenX, ActualHeight));
            }

            // X-axis (y = 0)
            if (world.Top <= 0 && world.Bottom >= 0)
            {
                double screenY = WorldToScreenY(0);

                dc.DrawLine(
                    pen,
                    new Point(0, screenY),
                    new Point(ActualWidth, screenY));
            }
        }

        private void DrawAxisTicks(DrawingContext dc)
        {
            double scaleX = Scaling.Width;
            double scaleY = Scaling.Height;

            double stepX = GetNiceStep(scaleX);
            double stepY = GetNiceStep(scaleY);

            var world = ComputeWorldWindow();

            var pen = new Pen(Brushes.Black, 1);

            double tickSize = 5;

            // X-axis ticks (along y = 0)
            if (world.Top <= 0 && world.Bottom >= 0)
            {
                double y = WorldToScreenY(0);

                for (double x = Math.Floor(world.X / stepX) * stepX; x < world.Right; x += stepX)
                {
                    double sx = WorldToScreenX(x);

                    dc.DrawLine(
                        pen,
                        new Point(sx, y - tickSize),
                        new Point(sx, y + tickSize));
                }
            }

            // Y-axis ticks (along x = 0)
            if (world.Left <= 0 && world.Right >= 0)
            {
                double x = WorldToScreenX(0);

                for (double y = Math.Floor(world.Y / stepY) * stepY; y < world.Bottom; y += stepY)
                {
                    double sy = WorldToScreenY(y);

                    dc.DrawLine(
                        pen,
                        new Point(x - tickSize, sy),
                        new Point(x + tickSize, sy));
                }
            }
        }

        private void DrawGridLabels(DrawingContext dc)
        {
            var scaleX = Scaling.Width;
            var scaleY = Scaling.Height;

            var stepX = GetNiceStep(scaleX);
            var stepY = GetNiceStep(scaleY);

            var world = ComputeWorldWindow();

            var typeface = new Typeface("Segoe UI");
            double fontSize = 10;

            double margin = 4;

            // -------------------------
            // X labels (bottom)
            // -------------------------
            var yScreen = ActualHeight - margin;

            for (var x = Math.Floor(world.X / stepX) * stepX; x < world.Right; x += stepX)
            {
                var sx = WorldToScreenX(x);

                // Skip if outside screen (safety)
                if (sx < 0 || sx > ActualWidth)
                    continue;

                var text = new FormattedText(
                    x.ToString("G"),
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.Black,
                    1.0);

                // Center text under grid line
                dc.DrawText(
                    text,
                    new Point(sx - text.Width / 2, yScreen - text.Height));
            }

            // -------------------------
            // Y labels (left side)
            // -------------------------
            var xScreen = margin;

            for (var y = Math.Floor(world.Y / stepY) * stepY; y < world.Bottom; y += stepY)
            {
                var sy = WorldToScreenY(y);

                if (sy < 0 || sy > ActualHeight)
                    continue;

                var text = new FormattedText(
                    y.ToString("G"),
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.Black,
                    1.0);

                dc.DrawText(
                    text,
                    new Point(xScreen, sy - text.Height / 2));
            }
        }
    }
}