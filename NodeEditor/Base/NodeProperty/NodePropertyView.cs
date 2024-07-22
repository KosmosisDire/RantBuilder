using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using NodeBuilder.Base;
using NodeBuilder.Internal.Interactions;
namespace NodeBuilder;

public interface INodePropertyView : IComponentView<INodePropertyData>
{
    public Panel ConnectionPoint { get; }
    public Border GrabShape { get; }
    public Label Label { get; }
}


public class NodePropertyView<TValue> : ComponentView<NodePropertyData<TValue>>, INodePropertyView where TValue : IEquatable<TValue>
{
    public override Grid Layout { get; } = new();
    public Label Label { get; } = new();
    public Panel ConnectionPoint { get; } = new();
    public Border GrabShape { get; } = new();

    INodePropertyData IComponentView<INodePropertyData>.Data => Data;

    public NodePropertyView(NodePropertyData<TValue> data) : base(data)
    {
        Layout.RowDefinitions = [new RowDefinition()];
        Layout.ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(5, GridUnitType.Pixel), new ColumnDefinition(GridLength.Auto)];
        Layout.Margin = new Thickness(10, 0, 10, 0);

        var grabSize = 25;
        GrabShape.Width = grabSize * 3;
        GrabShape.Height = grabSize;
        GrabShape.CornerRadius = new CornerRadius(grabSize / 2);
        GrabShape.Background = Brushes.Transparent;
        GrabShape.Margin = new Thickness(-grabSize);
        GrabShape.DataContext = this;
        GrabShape.Classes.Add("NodePropertyGrab");
        GrabShape.ZIndex = 10;

        ToolTip.SetTip(Layout, Data.DataType.Name);
        
        ConnectionPoint.Width = 12;
        ConnectionPoint.Height = 12;
        ConnectionPoint.Classes.Add("NodePropertyPoint");
        ConnectionPoint.Children.Add(new Border()
        {
            CornerRadius = new CornerRadius(6),
            BorderBrush = new SolidColorBrush(NodeProperty.GetColorForType(Data.DataType))
        });

        
        Label.Content = Data.Name;
        Label.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        Label.FontWeight = FontWeight.Light;
        Label.Margin = new Thickness(0, 0, 0, 0);
        Label.FontSize = 12;

        LayoutAdd(GrabShape);
        LayoutAdd(ConnectionPoint);
        LayoutAdd(Label);

        if (Data.ConnectionType == NodePropertyType.Input)
        {
            Grid.SetColumn(GrabShape, 0);
            Grid.SetColumn(ConnectionPoint, 0);
            Grid.SetColumn(Label, 2);
            Layout.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        }
        else
        {
            Grid.SetColumn(GrabShape, 2);
            Grid.SetColumn(ConnectionPoint, 2);
            Grid.SetColumn(Label, 0);
            Layout.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
        }

        HoverClassname.AddHover(GrabShape, Layout);
    }
}