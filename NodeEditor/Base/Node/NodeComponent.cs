using System;
using System.Collections.Generic;
using Avalonia.Controls;

using NodeBuilder.Base;
using NodeBuilder.Internal.Interactions;
namespace NodeBuilder;

public interface IGenericNode
{
    public static string Classname { get; } = "Node";
    public Panel Layout { get; }
    public NodeView View { get; }
    public NodeData Data { get; }
    public bool ValidateConnection(INodeProperty a, INodeProperty b);
}

public class GenericNode<TView, TData> : Component<GenericNode<TView, TData>, NodeView, NodeData>, IGenericNode
    where TView : ComponentView<NodeData>
    where TData : ComponentData<NodeData>
{

    public GenericNode(string name, string description, GenericNode<TView, TData>? parent = null) : base(new NodeView(new NodeData(name, description, parent?.Data)))
    {
        Layout.Classes.Add(IGenericNode.Classname);
        parent?.AddChild(this);

        _ = new DragDropInteraction(Layout)
        {
            DraggedAction = gesture =>
            {
                Data.Position += gesture.MouseDelta;
            },
        };
    }

    public virtual bool ValidateConnection(INodeProperty a, INodeProperty b)
    {
        return true;
    }

    protected NodeProperty<T> AddInputProperty<T>(string name, T initialValue = default) where T : IEquatable<T> 
    {
        var property = new NodeProperty<T>(name, NodePropertyType.Input, this, initialValue, (input) => input);
        AddInputProperty(property);
        return property;
    }

    protected INodeProperty AddInputProperty(INodeProperty property)
    {
        Data.AddInputProperty(property.Data);
        View.LeftPropertyDock.Children.Add(property.View.Layout);
        return property;
    }

    protected NodeProperty<T> AddOutputProperty<T>(string name, T initialValue = default, Func<T?, T?>? transformFunction = null) where T : IEquatable<T>
    {
        var property = new NodeProperty<T>(name, NodePropertyType.Output, this, initialValue, transformFunction ?? ((input) => input));
        AddOutputProperty(property);
        return property;
    }

    protected INodeProperty AddOutputProperty(INodeProperty property)
    {
        Data.AddOutputProperty(property.Data);
        View.RightPropertyDock.Children.Add(property.View.Layout);
        return property;
    }

    protected void RemovePropery(INodeProperty property)
    {
        if (property.Data.ConnectionCount > 0)
        {
            property.DisconnectAll();
        }

        if (property.Data.ConnectionType == NodePropertyType.Input)
        {
            Data.RemoveInputProperty(property.Data);
        }
        else
        {
            Data.RemoveOutputProperty(property.Data);
        }

        if (property.View.Layout.Parent is Panel panel)
        {
            panel.Children.Remove(property.View.Layout);
        }
    }

    protected void RemoveInputPropertyAt(int index)
    {
        if (Data.InputCount > 0)
        {
            var prop = INodeProperty.GetComponent(Data.GetInputAt(index));
            if (prop != null)
            {
                RemovePropery(prop);
            }
        }
    }

    protected void GeneratePropertiesFromObject<T>(bool isInput)
    {
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;
            var name = property.Name;
            var propertyInstance = Activator.CreateInstance(typeof(NodeProperty<>).MakeGenericType(propertyType), name, isInput, Data, null);
            if (isInput)
            {
                AddInputProperty((INodeProperty)propertyInstance);
            }
            else
            {
                AddOutputProperty((INodeProperty)propertyInstance);
            }
        }
    }

    protected void AddChild(GenericNode<TView, TData> child)
    {
        if (child == this)
        {
            return;
        }

        Data.AddChild(child.Data);
        child.ClearParent();
        View.MiddleContent.Children.Add(child.View); 
    }

    public void ClearParent()
    {
        if (Layout.Parent is Panel panel)
        {
            panel.Children.Remove(Layout);
        }
    }

    protected void RemoveChild(GenericNode<TView, TData> child)
    {
        child.ClearParent();
        Data.RemoveChild(child.Data);
    }

    protected void AddControl(Control control)
    {
        View.MiddleContent.Children.Add(control);
    }
}