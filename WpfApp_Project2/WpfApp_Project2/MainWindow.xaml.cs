using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QuadrupoleFieldSimulator
{
    public partial class MainWindow : Window
    {
        private readonly Charge[] charges = new Charge[]
        {
            new Charge(1.0, new Point(-100, -100)),  // +q
            new Charge(-1.0, new Point(100, -100)),   // -q
            new Charge(1.0, new Point(100, 100)),     // +q
            new Charge(-1.0, new Point(-100, 100))    // -q
        };

        private const double k = 9e9; // Электрическая постоянная
        private const int maxSteps = 1000;

        public MainWindow()
        {
            InitializeComponent();
            RedrawField();
        }

        private void RedrawField(object sender = null, RoutedEventArgs e = null)
        {
            FieldCanvas.Children.Clear();
            DrawCharges();
            DrawFieldLines();
            DrawEquipotentialLines(10, 50, 5);
        }

        private void DrawCharges()
        {
            foreach (var charge in charges)
            {
                var ellipse = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = charge.Value > 0 ? Brushes.Red : Brushes.Blue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(ellipse, charge.Position.X + FieldCanvas.ActualWidth / 2 - 5);
                Canvas.SetTop(ellipse, charge.Position.Y + FieldCanvas.ActualHeight / 2 - 5);

                FieldCanvas.Children.Add(ellipse);

                // Подпись заряда
                var label = new TextBlock
                {
                    Text = charge.Value > 0 ? "+q" : "-q",
                    FontSize = 10,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(label, charge.Position.X + FieldCanvas.ActualWidth / 2 + 10);
                Canvas.SetTop(label, charge.Position.Y + FieldCanvas.ActualHeight / 2 - 5);
                FieldCanvas.Children.Add(label);
            }
        }

        private void DrawFieldLines()
        {
            int stepSize = (int)StepSlider.Value;

            foreach (var charge in charges)
            {
                if (charge.Value > 0)
                {
                    for (int angle = 0; angle < 360; angle += 15)
                    {
                        DrawFieldLine(charge.Position, angle, stepSize);
                    }
                }
            }
        }

        private void DrawFieldLine(Point startPoint, double startAngle, int stepSize)
        {
            Point current = startPoint;
            double angle = startAngle * Math.PI / 180;
            Point direction = new Point(Math.Cos(angle), Math.Sin(angle));

            var path = new Path
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Point(current.X + FieldCanvas.ActualWidth / 2, current.Y + FieldCanvas.ActualHeight / 2)
            };
            geometry.Figures.Add(figure);

            for (int i = 0; i < maxSteps; i++)
            {
                Point field = CalculateField(current);
                double fieldMagnitude = Math.Sqrt(field.X * field.X + field.Y * field.Y);

                if (fieldMagnitude < 0.1) break;

                field.X /= fieldMagnitude;
                field.Y /= fieldMagnitude;

                Point next = new Point(
                    current.X + stepSize * field.X,
                    current.Y + stepSize * field.Y
                );

                // Проверка на приближение к заряду
                foreach (var charge in charges)
                {
                    if (Distance(next, charge.Position) < 10)
                    {
                        figure.Segments.Add(new LineSegment(
                            new Point(charge.Position.X + FieldCanvas.ActualWidth / 2,
                                     charge.Position.Y + FieldCanvas.ActualHeight / 2),
                            true));
                        path.Data = geometry;
                        FieldCanvas.Children.Add(path);
                        return;
                    }
                }

                figure.Segments.Add(new LineSegment(
                    new Point(next.X + FieldCanvas.ActualWidth / 2,
                             next.Y + FieldCanvas.ActualHeight / 2),
                    true));
                current = next;
            }

            path.Data = geometry;
            FieldCanvas.Children.Add(path);
        }

        private void DrawEquipotentialLines(double minPotential, double maxPotential, int linesCount)
        {
            double step = (maxPotential - minPotential) / linesCount;

            for (double potential = minPotential; potential <= maxPotential; potential += step)
            {
                for (int x = -200; x <= 200; x += 10)
                {
                    for (int y = -200; y <= 200; y += 10)
                    {
                        Point point = new Point(x, y);
                        double currentPotential = CalculatePotential(point);

                        if (Math.Abs(currentPotential - potential) < 5)
                        {
                            var dot = new Ellipse
                            {
                                Width = 2,
                                Height = 2,
                                Fill = Brushes.Blue
                            };

                            Canvas.SetLeft(dot, x + FieldCanvas.ActualWidth / 2 - 1);
                            Canvas.SetTop(dot, y + FieldCanvas.ActualHeight / 2 - 1);
                            FieldCanvas.Children.Add(dot);
                        }
                    }
                }
            }
        }

        private Point CalculateField(Point point)
        {
            Point field = new Point(0, 0);

            foreach (var charge in charges)
            {
                double dx = point.X - charge.Position.X;
                double dy = point.Y - charge.Position.Y;
                double rSquared = dx * dx + dy * dy;
                double r = Math.Sqrt(rSquared);

                if (r < 0.1) continue;

                double force = k * charge.Value / rSquared;
                field.X += force * dx / r;
                field.Y += force * dy / r;
            }

            return field;
        }

        private double CalculatePotential(Point point)
        {
            double potential = 0;

            foreach (var charge in charges)
            {
                double dx = point.X - charge.Position.X;
                double dy = point.Y - charge.Position.Y;
                double r = Math.Sqrt(dx * dx + dy * dy);

                if (r < 0.1) continue;

                potential += k * charge.Value / r;
            }

            return potential;
        }

        private double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public class Charge
    {
        public double Value { get; set; }
        public Point Position { get; set; }

        public Charge(double value, Point position)
        {
            Value = value;
            Position = position;
        }
    }
}