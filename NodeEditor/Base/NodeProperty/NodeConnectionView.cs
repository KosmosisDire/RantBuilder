using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;

using NodeBuilder.Base;
using NodeBuilder.Internal;
namespace NodeBuilder;

public class NodeSplineView
{
    public Path Path { get; } = new();
    INodeProperty start;
    INodeProperty end;

    public NodeSplineView()
    {
        Path.Bind(Path.StrokeProperty, Application.Current.Resources.GetResourceObservable("SplineColor"));
    }

    static NodeSplineView()
    {
        Application.Current.Styles.Add(new Style(x => x.OfType<Path>())
        {
            Setters = 
            {
                new Setter(Path.StrokeThicknessProperty, 3.5),
                new Setter(Path.StrokeLineCapProperty, PenLineCap.Round),
            }
        });
        Application.Current.Styles.Add(new Style(x => x.Class("NodeConnection" + ":pointerover"))
        {
            Setters = {new Setter(Path.StrokeThicknessProperty, 5.5)}
        });
    }

    public void UpdatePath(Point start, Point end, Vector startDirection, Vector endDirection)
    {
        var pathGeometry = new PathGeometry()
        { 
            Figures = new PathFigures() 
        };

        var pathFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments = new PathSegments(),
        };

        pathFigure.Segments.Add(new BezierSegment
        {
            Point1 = start + startDirection,
            Point2 = end + endDirection,
            Point3 = end,
        });

        pathGeometry.Figures.Add(pathFigure);
        Path.Data = pathGeometry;
    }

    void PropChangedCallback()
    {
        var distance = (end.GetConnectionPoint(start) - start.GetConnectionPoint(start)).ToVector2().Length() / 3;
        UpdatePath(start.GetConnectionPoint(start), end.GetConnectionPoint(start), start.GetStartDirection(distance), end.GetStartDirection(distance));
    }

    void PosCallback(object? sender, Vector pos)
    {
        PropChangedCallback();
    }

    void SizeCallback(object? sender, Size size)
    {
        PropChangedCallback();
    }

    public void ConnectPathOnce(INodeProperty start, INodeProperty end)
    {
        this.start = start;
        this.end = end;
        PropChangedCallback();
    }

    public void ConnectPath(INodeProperty start, INodeProperty end)
    {
        this.start = start;
        this.end = end;
        start.Data.ContainingNode.PositionChanged += PosCallback;
        start.Data.ContainingNode.SizeChanged += SizeCallback;
        end.Data.ContainingNode.PositionChanged += PosCallback;
        end.Data.ContainingNode.SizeChanged += SizeCallback;
        PropChangedCallback();
    }

    public void DisconnectPath()
    {
        Path.Data = null;

        if (start == null || end == null)
            return;

        if (start.Data.ContainingNode == null || end.Data.ContainingNode == null)
            return;


        start.Data.ContainingNode.PositionChanged -= PosCallback;
        start.Data.ContainingNode.SizeChanged -= SizeCallback;
        end.Data.ContainingNode.PositionChanged -= PosCallback;
        end.Data.ContainingNode.SizeChanged -= SizeCallback;

        start = null;
        end = null;
    }

    public void SetColor(Color color)
    {
        Path.Stroke = new SolidColorBrush(color);
    }

    public void SetColor(Brush brush)
    {
        Path.Stroke = brush;
    }

    public async void Flash()
    {
        SetColor(Colors.White);
        await Task.Delay(100);
        SetColor(NodeProperty.GetColorForType(start.Data.DataType));
    }
}