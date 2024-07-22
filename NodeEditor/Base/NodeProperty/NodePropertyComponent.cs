
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.VisualTree;
using NodeBuilder.Base;
using NodeBuilder.Internal;
using NodeBuilder.Internal.Interactions;
using RantBuilder;

namespace NodeBuilder;

public interface INodeProperty : IComponent<INodePropertyView, INodePropertyData>
{
    public IGenericNode ContainingNode { get; }
    public Dictionary<INodeProperty, NodeSplineView> ConnectionLookup { get; }
    public Point GetConnectionPoint(INodeProperty? relativeTo = null);
    public Vector GetStartDirection(float magnitude = 100);
    public Vector GetEndDirection(float magnitude = 100);
    public void DisconnectAll();
    public static INodeProperty? GetComponent(INodePropertyData data) => null;
}

public static class NodeProperty
{
    private static int GetSimpleHash(string s)
    {
        return s.Select(a => (int)a).Sum();
    }

    public static Color GetColorForType(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                return new Color (255, 116, 173, 192);
            case TypeCode.Int32:
                return new Color (255, 219, 190, 87);
            case TypeCode.Single:
            case TypeCode.Double:
                return new Color (255, 153, 191, 150);
            case TypeCode.String:
                return new Color (255, 217, 145, 87);
            default:
                // generate a unique color for each type based on its name
                return Color.FromArgb(255, (byte)(100 + (GetSimpleHash(type.FullName) % 155)), (byte)(100 + ((GetSimpleHash(type.FullName) >> 8) % 155)), (byte)(100 + ((GetSimpleHash(type.FullName) >> 16) % 155)));
        }
    }
}

public class NodeProperty<TInput> : Component<NodeProperty<TInput>, NodePropertyView<TInput>, NodePropertyData<TInput>>, INodeProperty 
    where TInput : IEquatable<TInput>
{
    private static NodeSplineView tempSpline = new();
    public static NodeSplineView ResetTempSpline(NodeProperty<TInput> newProperty) 
    { 
        var parent = tempSpline.Path.Parent;
        if (parent is Panel control)
        {
            control.Children.Remove(tempSpline.Path);
        }
        tempSpline.DisconnectPath();
        newProperty.ContainingNode.View.Layout.Children.Add(tempSpline.Path);
        tempSpline.SetColor(NodeProperty.GetColorForType(typeof(TInput)));
        return tempSpline;
    }
    
    public IGenericNode ContainingNode { get; }
    public Dictionary<INodeProperty, NodeSplineView> ConnectionLookup { get;} = [];

    // INodeProperty implementation
    INodePropertyData IComponent<INodePropertyView, INodePropertyData>.Data => Data;
    INodePropertyView IComponent<INodePropertyView, INodePropertyData>.View => View;
    public static INodeProperty? GetComponent(INodePropertyData data) => Component<NodeProperty<TInput>, NodePropertyView<TInput>, NodePropertyData<TInput>>.GetComponent(data);

    public static bool HasImplicitConversion(Type baseType, Type targetType)
    {
        return baseType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == targetType)
            .Any(mi => {
                ParameterInfo? pi = mi?.GetParameters()?.FirstOrDefault();
                return pi != null && pi.ParameterType == baseType;
            });
    }

    private bool ValidateConnection(INodeProperty? target)
    {
        if (target == null)
            return false;

        var thisType = Data.DataType;
        var targetType = target.Data.DataType;
        var canConvert = thisType.IsAssignableTo(targetType) || HasImplicitConversion(thisType, targetType) || (thisType.GetInterface(nameof(IConvertible)) != null && targetType.GetInterface(nameof(IConvertible)) != null);
        Console.WriteLine($"Can convert {thisType} to {targetType}: {canConvert}");

        var connectionExists = ConnectionLookup.ContainsKey(target);

        return canConvert && !connectionExists && target.Data.ConnectionType != Data.ConnectionType && target.Data.ContainingNode != Data.ContainingNode && ContainingNode.ValidateConnection(this, target) && target.ContainingNode.ValidateConnection(target, this);
    }

    public NodeProperty(string name, NodePropertyType type, IGenericNode containingNode, TInput? initialValue, Func<TInput?, TInput?> transformationFunction) : base(new NodePropertyView<TInput>(new NodePropertyData<TInput>(name, type, containingNode.Data, initialValue, transformationFunction)))
    {
        ContainingNode = containingNode;
        View.GrabShape.DataContext = this;

        _ = new DragDropInteraction(View.GrabShape)
        {
            DragStartedAction = g => ResetTempSpline(this),

            DraggedAction = gesture =>
            {
                if (Data.ConnectionType == NodePropertyType.Input && Data.ConnectionCount > 0)
                {
                    DisconnectAll();
                }


                var target = GetConnectorAt(gesture.MousePosition);
                if (!ValidateConnection(target))
                {
                    var mouseTarget = gesture.DraggedControl.TranslatePoint(gesture.RelativeMousePosition, ContainingNode.View.Layout) ?? gesture.RelativeMousePosition;
                    var distance = (mouseTarget - GetConnectionPoint()).ToVector2().Length() / 3;
                    tempSpline.UpdatePath(GetConnectionPoint(), mouseTarget, GetStartDirection(distance), GetEndDirection(distance));
                }
                else
                {
                    tempSpline.ConnectPathOnce(this, target);
                }
            },

            DragEndedAction = gesture =>
            {
                var target = GetConnectorAt(gesture.MousePosition);
                ResetTempSpline(this);
                Connect(target);
            }
        }; 

        // make connections flash when the value changes
        Data.ValueChanged += (sender, args) =>
        {
            foreach (var connection in ConnectionLookup.Values)
            {
                connection.Flash();
            }
        };
    }

    public Point GetConnectionPoint(INodeProperty? relativeTo = null)
    {
        if (relativeTo == null)
            relativeTo = this;

        var relativePosition = new Point(View.ConnectionPoint.Width / 2, View.ConnectionPoint.Height / 2);
        return View.ConnectionPoint.TranslatePoint(relativePosition, relativeTo.ContainingNode.View.Layout) ?? relativePosition;
    }

    public Vector GetStartDirection(float magnitude = 100)
    {
        return new Vector(magnitude * (Data.ConnectionType == NodePropertyType.Input ? -1 : 1), 0);
    }

    public Vector GetEndDirection(float magnitude = 100)
    {
        return -GetStartDirection(magnitude);
    }

    public void Connect(INodeProperty? target)
    {
        if (!ValidateConnection(target))
        {
            Console.WriteLine("Connection not valid");
            return;
        }

        var connection = new NodeSplineView();
        connection.SetColor(NodeProperty.GetColorForType(typeof(TInput)));
        connection.ConnectPath(this, target);
        ConnectionLookup.Add(target, connection);
        target.ConnectionLookup.Add(this, connection);
        Data.AddConnection(target.Data);
        ContainingNode.View.Layout.Children.Add(connection.Path);
    }

    public void Disconnect(INodeProperty? target)
    {
        if (target == null)
            return;

        if (ConnectionLookup.TryGetValue(target, out var connection))
        {
            connection.DisconnectPath();
            ConnectionLookup.Remove(target);
            target.ConnectionLookup.Remove(this);
            Data.RemoveConnection(target.Data);
            ContainingNode.View.Layout.Children.Remove(connection.Path);
        }
    }

    public void DisconnectAll()
    {
        var connectionsCopy = ConnectionLookup.Keys.ToList();
        foreach (var connection in connectionsCopy)
        {
            Disconnect(connection);
        }
    }

    static INodeProperty? GetConnectorAt(Point position)
    {
        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow as MainWindow : null;
        return mainWindow?.GetVisualsAt(position).FirstOrDefault(x =>  x.Classes.Contains("NodePropertyGrab") && x.DataContext is INodeProperty)?.DataContext as INodeProperty;
    }
}