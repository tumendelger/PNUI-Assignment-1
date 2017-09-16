using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Assignment_1_PNUI
{
    public interface IMyShapes
    {
        Geometry GetShapeGeometry();
    }

    public class MyShape
    {
        private readonly int MaxSide = 250;
        
        private Point center;

        public int side;

        // 0 - 100
        private int scale;

        public Geometry shapeGeo;

        public Point Center { get => center; set => center = value; }

        public int Side
        {
            get => 
                (scale == 0)? 0: (MaxSide * scale / 100);
            set => side = value;
        }

        public int Scale { get => scale; set => scale = value; }
        
        public MyShape(Point center)
        {
            Center = center;
        }        

        public virtual Geometry GetGeometry() { return shapeGeo; }
    }

    class MyRectangle : MyShape
    {
        public MyRectangle(Point center) : base(center)
        {
        }

        public override Geometry GetGeometry()
        {
            Point start = new Point(Center.X - Side / 2, Center.Y - Side / 2);

            LineSegment[] lineSegments = new LineSegment[]
            {
                new LineSegment(new Point(start.X + Side, start.Y), true),
                new LineSegment(new Point(start.X + Side, start.Y + Side),true),
                new LineSegment(new Point(start.X, start.Y + Side), true)
            };

            PathFigure pathFigure = new PathFigure(start, lineSegments, true);

            PathFigureCollection figures = new PathFigureCollection
            {
                pathFigure
            };

            shapeGeo = new PathGeometry(figures);

            return base.GetGeometry();
        }
    }

    class MyTriangle : MyShape
    {
        public MyTriangle(Point center) : base(center)
        {
        }

        public override Geometry GetGeometry()
        {
            double h = Math.Sqrt(Math.Pow(Side, 2) - Math.Pow(Side / 2, 2));

            Point start = new Point(Center.X, Center.Y - (2 * h / 3));

            LineSegment[] lineSegments = new LineSegment[]
            {
                new LineSegment(new Point(start.X + Side / 2, start.Y + h ), true),
                new LineSegment(new Point(start.X - Side / 2, start.Y + h),true)
            };

            PathFigure pathFigure = new PathFigure(start, lineSegments, true);

            PathFigureCollection figures = new PathFigureCollection
            {
                pathFigure
            };

            shapeGeo = new PathGeometry(figures);

            return base.GetGeometry();
        }
    }

    class MyCircle : MyShape
    {
        public MyCircle(Point center) : base(center)
        {
        }

        public override Geometry GetGeometry()
        {
            // Create circle
            shapeGeo = new EllipseGeometry(Center, Side / 2, Side / 2);

            return base.GetGeometry();
        }
    }
}
