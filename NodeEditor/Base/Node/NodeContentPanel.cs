using System;
using Avalonia;
using Avalonia.Controls;
using NodeBuilder;

public class NodeContentPanel : Canvas
{
    private Size desiredSize;

    public static readonly StyledProperty<double> PaddingProperty =
        AvaloniaProperty.Register<NodeContentPanel, double>(nameof(Padding), defaultValue: 5.0);

    public double Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    // Override the default Measure method of Panel
    protected override Size MeasureOverride(Size availableSize)
    {
        var panelDesiredSize = new Size();

        if (Children.Count == 0)
        {
            return panelDesiredSize;
        }

        foreach (var child in Children)
        {
            child.Measure(availableSize);

            var right = child.Bounds.Right;
            var bottom = child.Bounds.Bottom;

            if (child.Classes.Contains(IGenericNode.Classname) && child.DataContext is IGenericNode node)
            {
                right = node.Data.X + node.View.GridLayout.DesiredSize.Width;
                bottom = node.Data.Y + node.View.GridLayout.DesiredSize.Height;
            }

            panelDesiredSize = new Size(Math.Max(right + Padding, panelDesiredSize.Width), Math.Max(bottom + Padding, panelDesiredSize.Height));
        }

        desiredSize = panelDesiredSize;
        return panelDesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
        {
            return finalSize;
        }
        
        Measure(desiredSize);
        var top = Padding;
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            if (child.Classes.Contains(IGenericNode.Classname))
            {
                base.ArrangeChild(child, desiredSize);
                continue;
            }

            child.Arrange(new Rect(Padding, top, child.DesiredSize.Width, child.DesiredSize.Height));
            top += (int)child.DesiredSize.Height;
        }
        
        return desiredSize; // Returns the final Arranged size
    }
}