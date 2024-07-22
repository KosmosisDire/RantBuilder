using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using NodeBuilder.Base;
using NodeBuilder.Internal;
using NodeBuilder.Internal.Interactions;

namespace NodeBuilder;

public class NodeView : ComponentView<NodeData>
{
    public override Canvas Layout { get; } = new();
    public Grid GridLayout { get; } = new();
    private Label NameLabel { get; } = new();
    public Grid PropertyPanelContainer { get; } = new();
    public StackPanel LeftPropertyDock { get; } = new();
    public NodeContentPanel MiddleContent { get; } = new();
    public StackPanel RightPropertyDock { get; } = new();

    public double NodePadding { get; set; } = 15;

    public NodeView(NodeData data) : base(data)
    {
        SetupElements();
    }

    public void GridAdd(Control control, int row = 0, int column = 0)
    {
        GridLayout.Children.Add(control);
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
    }

    private void SetupElements()
    {
        Layout.Width = 5;
        Layout.Height = 5;

        GridLayout.RowDefinitions = [new(GridLength.Auto), new(5, GridUnitType.Pixel), new(1, GridUnitType.Star)];
        GridLayout.ColumnDefinitions = [new(GridLength.Auto)];
        LayoutAdd(GridLayout);

        // create node background
        Border background = new() { Classes = { "NodeBackground" } };
        GridAdd(background);
        Grid.SetColumnSpan(background, GridLayout.ColumnDefinitions.Count);
        Grid.SetRowSpan(background, GridLayout.RowDefinitions.Count);

        // create node heading label
        NameLabel.Content = Data.Name;
        NameLabel.Foreground = new SolidColorBrush(new Color(255, 255, 255, 255));
        NameLabel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        NameLabel.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        NameLabel.FontWeight = FontWeight.Light;
        NameLabel.FontSize = 12;
        NameLabel.Margin = new Thickness(8, 3, 0, 3);
        GridAdd(NameLabel);

        // create heading divider
        var headingDivider = new Panel(){ Classes = { "Divider" } };
        GridAdd(headingDivider, 1);

        // create property panel container
        PropertyPanelContainer.RowDefinitions = [new(GridLength.Star)];
        PropertyPanelContainer.ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)];
        GridAdd(PropertyPanelContainer, 2);

        // create left dock panel
        LeftPropertyDock.MinWidth = 10;
        LeftPropertyDock.Background = new SolidColorBrush(new Color(50, 0, 0, 0));
        LeftPropertyDock.Spacing = NodePadding / 2;
        LeftPropertyDock.Classes.Add("PropertyDock");
        PropertyPanelContainer.Children.Add(LeftPropertyDock);
        Grid.SetColumn(LeftPropertyDock, 0);

        // create right dock panel
        RightPropertyDock.MinWidth = 10;
        RightPropertyDock.Background = new SolidColorBrush(new Color(100, 0, 0, 0));
        RightPropertyDock.Spacing = NodePadding / 2;
        RightPropertyDock.Classes.Add("PropertyDock");
        PropertyPanelContainer.Children.Add(RightPropertyDock);
        Grid.SetColumn(RightPropertyDock, 2);

        // create node outline
        var outline = new Border() { Classes = { "NodeOutline" } };
        GridAdd(outline);
        Grid.SetColumnSpan(outline, GridLayout.ColumnDefinitions.Count);
        Grid.SetRowSpan(outline, GridLayout.RowDefinitions.Count);

        // create middle content panel
        MiddleContent.Background = new SolidColorBrush(new Color(0, 0, 0, 0));
        MiddleContent.ZIndex = 10;
        PropertyPanelContainer.Children.Add(MiddleContent);
        Grid.SetColumn(MiddleContent, 1);

        // set hover class
        HoverClassname.AddHover(Layout, GridLayout);

        TaskUtils.RunAfter(RecalculateSize, 10);

        NodeData.PositionProperty.ValueChanged(Data, (sender, args) =>
        {
            var pos = args.NewValue;
            Canvas.SetLeft(Layout, pos.X);
            Canvas.SetTop(Layout, pos.Y);
        });
    }

    void RecalculateSize()
    {
        MiddleContent.Measure(Size.Infinity);
        Data.RecalculateSize(MiddleContent.Padding);
    }
}
