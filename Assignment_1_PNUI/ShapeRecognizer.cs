using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Assignment_1_PNUI
{
    class ShapeRecognizer
    {
        private Shapes currentShape;

        List<Point> square = new List<Point>();
        List<Point> circle = new List<Point>();
        List<Point> triangle = new List<Point>();
        
        public Shapes CurrentShape { get => currentShape; set => currentShape = value; }
        
        public ShapeRecognizer()
        {
            CurrentShape = Shapes.UNKNOWN;
            InitShapeSamples();            
        }

        private void InitShapeSamples()
        {
            CreateCircle();
            CreateSquare();
            CreateTriangle();
        }

        public void RecogniseShape(List<Point> drawing)
        {
            List<Point> normalizedShape = NormaliseDrawing(drawing);
            
            double ut = GetDifference(normalizedShape, triangle);
            double us = GetDifference(normalizedShape, square);
            double uc = GetDifference(normalizedShape, circle);

            if (ut < us && ut < uc)
            {
                CurrentShape = Shapes.TRIANGLE;
            }
            if (us < ut && us < uc)
            {
                CurrentShape = Shapes.RECTANGLE;
            }
            if (uc < ut && uc < us)
            {
                CurrentShape = Shapes.CIRCLE;
            }            
        }

        private double GetDifference(List<Point> drawing, List<Point> sample)
        {
            double sum = 0;
            for (int i = 0; i < drawing.Count; i++)
            {
                double min = 10E+10;
                for (int k = 0; k < sample.Count; k++)
                {
                    double distance =
                        (drawing[i].X - sample[k].X) * (drawing[i].X - sample[k].X) +
                        (drawing[i].Y - sample[k].Y) * (drawing[i].Y - sample[k].Y);
                    if (min > distance)
                        min = distance;
                }
                sum += min;
            }
            return sum / drawing.Count;
        }

        private List<Point> NormaliseDrawing(List<Point> drawing)
        {
            List<Point> normalized = new List<Point>(drawing.Count);
            double maxX = 0;
            double minX = 10E+10;
            double maxY = 0;
            double minY = 10E+10;

            for (int i = 0; i < drawing.Count; i++)
            {
                if (maxX < drawing[i].X)
                    maxX = drawing[i].X;
                if (minX > drawing[i].X)
                    minX = drawing[i].X;
                if (maxY < drawing[i].Y)
                    maxY = drawing[i].Y;
                if (minY > drawing[i].Y)
                    minY = drawing[i].Y;
            }

            for (int i = 0; i < drawing.Count; i++)
            {
                double x = (drawing[i].X - minX) * 100 / (maxX - minX);
                double y = (drawing[i].Y - minY) * 100 / (maxY - minY);
                normalized.Add(new Point(x, y));
            }

            return normalized;
        }

        private void CreateTriangle()
        {
            double side = 100;
            Point pt1 = new Point(50, 0);
            triangle.Add(pt1);
            Point pt2 = new Point(0, 100);
            triangle.Add(pt2);
            Point pt3 = new Point(100, 100);
            triangle.Add(pt3);

            for (int i = 1; i < 10; i++)
            {
                Point pt =
                    new Point(50 + (pt1.X + pt2.X) * i * 10 / side,
                    (pt1.Y + pt2.Y) * i * 10 / side);
                triangle.Add(pt);
                pt = new Point(pt3.X * i * 10 / side, pt2.Y);
                triangle.Add(pt);
                pt =
                    new Point((pt1.X + pt2.X) * i * 10 / side,
                    100 + (pt1.Y - pt2.Y) * i * 10 / side);
                triangle.Add(pt);
            }
        }

        private void CreateCircle()
        {
            double radius = 50;
            for (int i = 0; i < 36; i++)
            {
                Point pt =
                    new Point(50 + radius * Math.Cos(i * 10 * 6.28 / 360),
                    50 + radius * Math.Sin(i * 10 * 6.28 / 360));
                circle.Add(pt);
            }
        }

        private void CreateSquare()
        {
            // Points for square
            Point pt = new Point(0, 0);
            square.Add(pt);
            pt = new Point(100, 100);
            square.Add(pt);
            pt = new Point(0, 100);
            square.Add(pt);
            pt = new Point(100, 0);
            square.Add(pt);
            for (int i = 1; i < 10; i++)
            {
                // Left side
                pt = new Point(0, i * 10);
                square.Add(pt);
                // Right side
                pt = new Point(100, i * 10);
                square.Add(pt);
                // Top side
                pt = new Point(i * 10, 0);
                square.Add(pt);
                // Bottom side
                pt = new Point(i * 10, 100);
                square.Add(pt);
            }
        }

    }


    public enum Shapes
    {
        UNKNOWN,
        CIRCLE,
        TRIANGLE,
        RECTANGLE
    }

}
