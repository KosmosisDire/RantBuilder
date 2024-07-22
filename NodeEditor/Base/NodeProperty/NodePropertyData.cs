using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using NodeBuilder.Base;
namespace NodeBuilder;

public enum NodePropertyType
{
    Input,
    Output,
}

public interface INodePropertyData : IComponentData
{
    public static IDataProperty NameProperty { get; }
    public static IDataProperty ValueProperty { get; }
    public static IDataProperty ConnectionTypeProperty { get; }
    public static IDataProperty ContainingNodeProperty { get; }
    public static IDataProperty ConnectionCountProperty { get; }

    public string Name { get; }
    public object? Value { get; set; }
    public NodePropertyType ConnectionType { get; }
    public NodeData ContainingNode { get; }
    public int ConnectionCount { get; }
    public Type DataType { get; }

    public void Propagate();
    public void AddConnection(INodePropertyData other);
    public void RemoveConnection(INodePropertyData other);
}

public class NodePropertyData<TValue> : ComponentData<NodePropertyData<TValue>>, INodePropertyData where TValue : IEquatable<TValue>
{
    public static readonly DataProperty<NodePropertyData<TValue>, string> NameProperty = new(nameof(Name), () => "");
    public static readonly DataProperty<NodePropertyData<TValue>, TValue?> ValueProperty = new(nameof(Value), () => default);
    public static readonly DataProperty<NodePropertyData<TValue>, NodePropertyType> ConnectionTypeProperty = new(nameof(ConnectionType), () => NodePropertyType.Input);
    public static readonly DataProperty<NodePropertyData<TValue>, InstanceLookup<NodeData>> ContainingNodeProperty = new(nameof(ContainingNode), () => new InstanceLookup<NodeData>());
    public static readonly DataProperty<NodePropertyData<TValue>, int> ConnectionCountProperty = new(nameof(ConnectionCount), () => 0);

    public string Name
    {
        get => GetValue(NameProperty) ?? "";
        set => SetValue(NameProperty, value);
    }

    public TValue? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, valueTransformation(value));
    }

    object? INodePropertyData.Value
    {
        get => Value;
        set
        {
            if (value is TValue v)
                Value = v;
            else if (value is null)
                Value = default;
            else if (typeof(TValue) == typeof(String))
                Value = (TValue)(object)value.ToString();
            else
                Console.WriteLine($"Failed to set value of type {value.GetType()} to {typeof(TValue)}");
        }
    }

    public event EventHandler<ValueChangedArgs>? ValueChanged;

    public NodePropertyType ConnectionType
    {
        get => GetValue(ConnectionTypeProperty);
        set => SetValue(ConnectionTypeProperty, value);
    }

    public NodeData ContainingNode
    {
        get => GetValue(ContainingNodeProperty).Instance;
        init => GetValue(ContainingNodeProperty).Instance = value;
    }
    
    public int ConnectionCount
    {
        get => GetValue(ConnectionCountProperty);
        protected set => SetValue(ConnectionCountProperty, value);
    }

    public Type DataType => typeof(TValue);

    private readonly ObservableCollection<INodePropertyData> Connections = new();


    public void AddConnection(INodePropertyData other)
    {
        if (!Connections.Contains(other))
        {
            Connections.Add(other);
            other.AddConnection(this);
            ConnectionCount++;
        }
    }

    public void RemoveConnection(INodePropertyData other)
    {
        if (Connections.Remove(other))
        {
            other.RemoveConnection(this);
            ConnectionCount--;
        }
    }

    public ObservableCollection<INodePropertyData> GetConnections() => Connections;

    public void Propagate()
    {
        if (ConnectionType == NodePropertyType.Output)
        {
            foreach (var connection in Connections)
            {
                connection.Value = Value;
            }
        }

        if (ConnectionType == NodePropertyType.Input)
        {
            // propogate all outpuuts of the containing node
            for (int i = 0; i < ContainingNode.OutputCount; i++)
            {
                var output = ContainingNode.GetOutputAt(i);
                output.Value = output.Value;
            }
        }
    }

    private Func<TValue?, TValue?> valueTransformation;
    public void SetPropogationFunction(Func<TValue?, TValue?> func)
    {
        valueTransformation = func;
    }
    
    public NodePropertyData(string name, NodePropertyType type, NodeData parentNode, TValue? initialValue, Func<TValue?, TValue?>? propogationFunction = null)
    {
        Name = name;
        ConnectionType = type;
        ContainingNode = parentNode;

        this.valueTransformation = propogationFunction ?? (v => v);
        ValueProperty.ValueChanged(this, (sender, e) =>
        {
            Propagate();
            ValueChanged?.Invoke(this, new ValueChangedArgs(this, ValueProperty, e.OldValue, e.NewValue));
        });

        ConnectionCountProperty.ValueChanged(this, (sender, e) =>
        {
            Propagate();
        });

        Value = initialValue;
    }

    // public bool IsRecursive()
    // {
    //     var visited = new List<NodePropertyData<TValue>>();
    //     return IsRecursive(visited);
    // }

    // private bool IsRecursive(List<NodePropertyData<TValue>> visited)
    // {
    //     if (visited.Contains(this))
    //         return true;

    //     visited.Add(this);

    //     foreach (var connection in Connections)
    //     {
    //         if (connection.IsRecursive(visited))
    //             return true;
    //     }

    //     return false;
    // }



    // public void SetInput(TValue? value, List<INodePropertyData> visited = null, bool propogate = true)
    // {
    //     if (ConnectionType == NodePropertyType.Output) return;

    //     visited ??= [];

    //     if (visited.Contains(this))
    //         return;
    //     visited.Add(this);

    //     if (EqualityComparer<TValue>.Default.Equals(value, _value))
    //         return;

    //     _value = value;
    //     Console.WriteLine($"Setting input {Name} to {_value}");
    //     Component?.TriggerValueChanged(_value);

    //     if (propogate)
    //     {
    //         PropogateInputs(visited);
    //     }
    // }

    // private void PropogateInputs(List<INodePropertyData> visited)
    // {
    //     var outputs = ParentNode.OutputProperties.ToArray();
    //     foreach (var output in outputs)
    //     {
    //         if (output?.Component?.CalculateOutput != null)
    //         {
    //             var newValue = output.Component.CalculateOutput();
    //             output.UpdateOutputGeneric(newValue, visited);
    //         }
    //     }
    // }

    // public void UpdateOutputGeneric(object? value, List<INodePropertyData> visited = null)
    // {
    //     UpdateOutput((TValue?)value, visited);
    // }

    // internal void UpdateOutput(TValue? value, List<INodePropertyData> visited = null)
    // {
    //     if (ConnectionType == NodePropertyType.Input) return;

    //     visited ??= [];


    //     if (EqualityComparer<TValue>.Default.Equals(value, _value))
    //         return;

    //     _value = value;
    //     Console.WriteLine($"Updating output {Name} to {_value}");
    //     Component?.TriggerValueChanged(_value);

    //     var connections = Connections.ToArray();
    //     List<NodePropertyData<TValue>> connected = new();
    //     foreach (var connection in connections)
    //     {
    //         connection.SetInput(_value, visited, false);
    //         if (!connected.Contains(connection))
    //             connected.Add(connection);
    //     }

    //     // only propogate to connected nodes once
    //     foreach (var c in connected)
    //         c.PropogateInputs(visited);

    //     return;
    // }

    // public NodeData ParentNode { get; init; }
    // public Type DataType => typeof(TValue);
    // private List<NodePropertyData<TValue>> Connections { get; } = [];

    // public void AddConnection(NodePropertyData<TValue> connection)
    // {
    //     if (!Connections.Contains(connection))
    //     {
    //         Connections.Add(connection);
    //         Component.TriggerConnectionAdded(connection);

    //         if (ConnectionType == NodePropertyType.Input)
    //         {
    //             SetInput(connection._value);
    //         }
    //     }

    //     if (!connection.HasConnection(this))
    //         connection.AddConnection(this);
    // }

    // public void RemoveConnection(NodePropertyData<TValue> connection)
    // {
    //     if (Connections.Remove(connection))
    //     {
    //         Component.TriggerConnectionRemoved(connection);

    //         if (ConnectionType == NodePropertyType.Input)
    //         {
    //             SetInput(default);
    //         }
    //     }

    //     if (connection.HasConnection(this))
    //         connection.RemoveConnection(this);
    // }

    // public bool HasConnection(NodePropertyData<TValue> connection)
    // {
    //     return Connections.Contains(connection);
    // }

    // public NodePropertyData<TValue>[] GetConnectionsCopy()
    // {
    //     return [.. Connections];
    // }

    // public int ConnectionCount => Connections.Count;

    // INodePropertyView IComponentData<INodeProperty, INodePropertyView>.View => View;

    // INodeProperty IComponentData<INodeProperty, INodePropertyView>.Component => Component;

    // object? INodePropertyData.Value => Value;

    // public NodePropertyData(string name, NodePropertyType type, NodeData parentNode, TValue? value = default, string? id = null)
    // {
    //     Name = name;
    //     ConnectionType = type;
    //     ParentNode = parentNode;
    //     SetInput(value);
    //     UpdateOutput(value);

    //     if (id == null)
    //     {
    //         Guid = Guid.NewGuid();
    //         UniqueID = HashCode.Combine(Name, ConnectionType, DataType, ParentNode, Connections) + "|" + Guid.ToString();
    //     }
    //     else
    //     {
    //         UniqueID = id;
    //         Guid = Guid.Parse(UniqueID.Split('|')[1]);
    //     }

    //     if (PropertyLookup.ContainsKey(UniqueID))
    //         PropertyLookup[UniqueID] = this;
    //     else
    //         PropertyLookup.Add(UniqueID, this);
    // }

    // public override XmlElement Serialize(XmlDocument document)
    // {
    //     var property = document.CreateElement("Property");
    //     property.SetAttribute(nameof(Name), Name);
    //     property.SetAttribute(nameof(ConnectionType), ConnectionType.ToString());
    //     property.SetAttribute(nameof(UniqueID), UniqueID);
    //     property.SetAttribute(nameof(ParentNode), ParentNode.UniqueID);

    //     var valueEl = document.CreateElement("Value");
    //     property.AppendChild(valueEl);
    //     if (_value != null)
    //     {
    //         valueEl.SetAttribute("Type", DataType.FullName);

    //         using var stream = new MemoryStream();
    //         var settings = new XmlWriterSettings
    //         {
    //             NamespaceHandling = NamespaceHandling.OmitDuplicates,
    //             Indent = true,
    //             IndentChars = "  ",
    //             OmitXmlDeclaration = true,
    //             NewLineOnAttributes = false
    //         };
    //         using var writer = XmlWriter.Create(stream, settings);
    //         var serializer = new XmlSerializer(typeof(TValue));
    //         serializer.Serialize(writer, _value);

    //         var reader = new StreamReader(stream);
    //         stream.Position = 0;
    //         var valueXMLString = reader.ReadToEnd();
    //         Console.WriteLine(valueXMLString);

    //         valueEl.InnerXml = valueXMLString;
    //     }

    //     var connections = document.CreateElement(nameof(Connections));
    //     property.AppendChild(connections);
    //     foreach (var connection in Connections)
    //     {
    //         var connectionEl = document.CreateElement("Connection");
    //         connectionEl.SetAttribute(nameof(connection.UniqueID), connection.UniqueID);
    //         connections.AppendChild(connectionEl);
    //     }

    //     return property;
    // }

    // public static NodePropertyData<TValue> Deserialize(XmlNode property)
    // {
    //     ArgumentNullException.ThrowIfNull(property);

    //     var name = property!.Attributes?[nameof(Name)]?.Value ?? "";
    //     Enum.TryParse(property.Attributes?[nameof(ConnectionType)]?.Value ?? "", out NodePropertyType connectionType);
    //     var uniqueID = property.Attributes?[nameof(UniqueID)]?.Value ?? "";
    //     var parentNodeID = property.Attributes?[nameof(ParentNode)]?.Value ?? "";

    //     if (!NodeData.NodeLookup.TryGetValue(parentNodeID, out var parent))
    //     {
    //         throw new XmlException($"Failed to find parent of node {uniqueID} with ID {parentNodeID}");
    //     }

    //     var valueEl = property.SelectSingleNode("Value");
    //     var type = Type.GetType(valueEl?.Attributes?["Type"]?.Value ?? "System.Object");
    //     var valueXMLString = valueEl?.InnerXml ?? "";

    //     if (type == null)
    //         throw new XmlException($"Failed to find type for property {name}");

    //     if (type.FullName != typeof(TValue).FullName && typeof(TValue) != typeof(object))
    //         throw new XmlException($"Could not load property {name} using type {typeof(TValue).FullName}, because we expected type {type.FullName} instead");

    //     TValue? value = default;
    //     if (valueXMLString.Length > 1)
    //     {
    //         using var stream = new MemoryStream();
    //         using var writer = new StreamWriter(stream);
    //         writer.Write(valueXMLString);
    //         writer.Flush();
    //         stream.Position = 0;

    //         var serializer = new XmlSerializer(typeof(TValue));
    //         value = (TValue?)serializer.Deserialize(stream);
    //     }

    //     var nodeProperty = new NodePropertyData<TValue>(name, connectionType, parent, value, uniqueID);

    //     var connectionIDs = property.SelectNodes(nameof(Connections) + "/Connection");
    //     var connectionList = new List<NodePropertyData<TValue>>();
    //     if (connectionIDs != null)
    //     {
    //         for (var i = 0; i < connectionIDs.Count; i++)
    //         {
    //             var id = connectionIDs[i]?.Attributes?[nameof(UniqueID)]?.Value ?? "";
    //             if (PropertyLookup.TryGetValue(id, out var connection))
    //                 connectionList.Add(connection);
    //             else
    //                 Console.WriteLine($"Failed to find connection with ID {id}");
    //         }
    //     }

    //     nodeProperty.Connections.AddRange(connectionList);

    //     return nodeProperty;
    // }
}

// public class NodePropertyData(string name, NodePropertyType type, NodeData parentNode, object? value = default, string? id = null) : NodePropertyData<object>(name, type, parentNode, value, id)
// {
// }

