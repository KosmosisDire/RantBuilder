using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using Avalonia;
using NodeBuilder.Base;
using NodeBuilder.Internal;
namespace NodeBuilder;

public class NodeData : ComponentData<NodeData>
{
    public static readonly DataProperty<NodeData, string> NameProperty = new (nameof(Name), () => "");
    public static readonly DataProperty<NodeData, string> DescriptionProperty = new (nameof(Description), () => "");
    public static readonly DataProperty<NodeData, Vector> PositionProperty = new (nameof(Position), () => new Vector());
    public static readonly DataProperty<NodeData, Size> SizeProperty = new (nameof(Position), () => new Size());
    public static readonly DataProperty<NodeData, InstanceLookup<NodeData>> ParentProperty = new (nameof(Parent), () => new InstanceLookup<NodeData>());
    public static readonly DataProperty<NodeData, int> InputCountProperty = new (nameof(InputCount), () => 0);
    public static readonly DataProperty<NodeData, int> OutputCountProperty = new (nameof(OutputCount), () => 0);

    public string Name
    {
        get => GetValue(NameProperty) ?? "";
        set => SetValue(NameProperty, value);
    }

    public string Description
    {
        get => GetValue(DescriptionProperty) ?? "";
        set => SetValue(DescriptionProperty, value);
    }

    public Vector Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public double X => Position.X;
    public double Y => Position.Y;

    public Size Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double Width => Size.Width;
    public double Height => Size.Height;

    public NodeData? Parent
    {
        get => GetValue(ParentProperty)?.Instance;
        set => GetValue(ParentProperty)!.Instance = value;
    }

    public int InputCount
    {
        get => GetValue(InputCountProperty);
        set => SetValue(InputCountProperty, value);
    }

    public int OutputCount
    {
        get => GetValue(OutputCountProperty);
        set => SetValue(OutputCountProperty, value);
    }

    public event EventHandler<Vector>? PositionChanged;
    public event EventHandler<Size>? SizeChanged;

    private ObservableCollection<INodePropertyData> InputProperties { get; } = [];
    private ObservableCollection<INodePropertyData> OutputProperties { get; } = [];
    private ObservableCollection<NodeData> Children { get; } = [];

    public NodeData(string name, string description, NodeData? parent = default) : base()
    {
        Name = name;
        Description = description;
        Parent = parent;

        PositionProperty.ValueChanged(this, (sender, args) => 
        {
            PositionChanged?.Invoke(sender, args.NewValue);
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].PositionChanged?.Invoke(sender, Children[i].Position);
            }
        });

        SizeProperty.ValueChanged(this, (sender, args) => 
        {
            SizeChanged?.Invoke(sender, args.NewValue);
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].SizeChanged?.Invoke(sender, Children[i].Size);
            }
        });
    }

    public NodeData(XmlNode node): base(node)
    {
        Name = node.GetString(nameof(Name));
    }

    public void AddChild(NodeData child)
    {
        Children.Add(child);
        child.Parent = this;
    }

    public void RemoveChild(NodeData child)
    {
        Children.Remove(child);
        child.Parent = null;
    }

    public void AddInputProperty(INodePropertyData property)
    {
        InputProperties.Add(property);
        InputCount++;
    }

    public void AddOutputProperty(INodePropertyData property)
    {
        OutputProperties.Add(property);
        OutputCount++;
    }

    public void RemoveInputProperty(INodePropertyData property)
    {
        InputProperties.Remove(property);
        InputCount--;
    }

    public void RemoveOutputProperty(INodePropertyData property)
    {
        OutputProperties.Remove(property);
        OutputCount--;
    }

    public INodePropertyData GetInputAt(int index)
    {
        if (index < 0 || index >= InputCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return InputProperties[index];
    }

    public INodePropertyData GetOutputAt(int index)
    {
        if (index < 0 || index >= OutputCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return OutputProperties[index];
    }


    public void RecalculateSize(double padding)
    {
        var childBounds = new Rect();
        foreach (var child in Children)
        {
            childBounds = childBounds.Union(new Rect(child.X - padding, child.Y - padding, child.Width, child.Height));
        }

        if (Children.Count == 0)
        {
            childBounds = new Rect(X, Y, Width, Height).Deflate(padding * 2);
        }

        childBounds = childBounds.Translate(Position);
        var thisDelta = Position - childBounds.TopLeft;

        Position = new Vector(childBounds.Left, childBounds.Top);

        foreach (var child in Children)
        {
            child.Position += thisDelta;
        }

        Parent?.RecalculateSize(padding);
    }
}
