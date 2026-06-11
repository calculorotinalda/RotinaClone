using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RotinaClone.App.Components
{
    public partial class DiskChart : UserControl
    {
        private readonly Queue<double> _readHistory = new Queue<double>();
        private readonly Queue<double> _writeHistory = new Queue<double>();
        private const int MaxPoints = 35;
        private double _maxSpeed = 100.0; // Default max scale is 100 MB/s

        public DiskChart()
        {
            InitializeComponent();
            SizeChanged += (s, e) => DrawChart();
            
            // Seed chart with empty data points for start
            for (int i = 0; i < MaxPoints; i++)
            {
                _readHistory.Enqueue(0);
                _writeHistory.Enqueue(0);
            }
            DrawChart();
        }

        public void AddDataPoint(double readSpeedMB, double writeSpeedMB)
        {
            _readHistory.Dequeue();
            _readHistory.Enqueue(readSpeedMB);

            _writeHistory.Dequeue();
            _writeHistory.Enqueue(writeSpeedMB);

            // Dynamically scale maximum speed
            double peak = System.Math.Max(_readHistory.Max(), _writeHistory.Max());
            _maxSpeed = System.Math.Max(100.0, peak * 1.15); // Add 15% headroom

            DrawChart();
        }

        private void DrawChart()
        {
            double width = ActualWidth;
            double height = ActualHeight;

            if (width <= 0 || height <= 0) return;

            DrawGrid(width, height);

            var readCoords = CalculateCoordinates(_readHistory.ToList(), width, height);
            var writeCoords = CalculateCoordinates(_writeHistory.ToList(), width, height);

            UpdatePathGeometry(ReadLinePath, ReadAreaPath, readCoords, height);
            UpdatePathGeometry(WriteLinePath, WriteAreaPath, writeCoords, height);
        }

        private void DrawGrid(double width, double height)
        {
            ChartGridCanvas.Children.Clear();

            // Draw 4 horizontal grid lines
            int linesCount = 4;
            for (int i = 1; i <= linesCount; i++)
            {
                double y = (height / (linesCount + 1)) * i;
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                    StrokeThickness = 1
                };
                ChartGridCanvas.Children.Add(line);

                // Add text indicator for speed
                double speedVal = _maxSpeed - ((_maxSpeed / (linesCount + 1)) * i);
                var text = new TextBlock
                {
                    Text = $"{speedVal:F0} MB/s",
                    Foreground = new SolidColorBrush(Color.FromArgb(80, 160, 178, 198)),
                    FontSize = 8,
                    Margin = new Thickness(width - 45, y - 12, 0, 0)
                };
                ChartGridCanvas.Children.Add(text);
            }
        }

        private List<Point> CalculateCoordinates(List<double> values, double width, double height)
        {
            var points = new List<Point>();
            double xStep = width / (MaxPoints - 1);

            for (int i = 0; i < values.Count; i++)
            {
                double x = i * xStep;
                // In WPF, 0,0 is top-left, so we subtract scaled height from bottom
                double y = height - (values[i] / _maxSpeed * (height * 0.8)); // Limit to 80% height for padding
                points.Add(new Point(x, y));
            }

            return points;
        }

        private void UpdatePathGeometry(Path linePath, Path areaPath, List<Point> points, double height)
        {
            if (points == null || points.Count == 0) return;

            // Generate Line Geometry
            var lineGeom = new PathGeometry();
            var lineFig = new PathFigure { StartPoint = points[0] };
            
            for (int i = 1; i < points.Count; i++)
            {
                lineFig.Segments.Add(new LineSegment(points[i], true));
            }
            lineGeom.Figures.Add(lineFig);
            linePath.Data = lineGeom;

            // Generate Area Geometry (closes the shape at the bottom of the canvas)
            var areaGeom = new PathGeometry();
            var areaFig = new PathFigure { StartPoint = points[0] };

            for (int i = 1; i < points.Count; i++)
            {
                areaFig.Segments.Add(new LineSegment(points[i], true));
            }
            
            // Connect to bottom right
            areaFig.Segments.Add(new LineSegment(new Point(points[points.Count - 1].X, height), false));
            // Connect to bottom left
            areaFig.Segments.Add(new LineSegment(new Point(points[0].X, height), false));
            areaFig.IsClosed = true;

            areaGeom.Figures.Add(areaFig);
            areaPath.Data = areaGeom;
        }
    }
}
