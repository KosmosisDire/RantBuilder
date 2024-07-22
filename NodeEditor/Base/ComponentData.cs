

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NodeBuilder.Internal;

namespace NodeBuilder.Base;

public interface IComponentData
{
    public XmlElement Serialize(XmlElement parent);
}

public abstract class ComponentData<TData> : SerializedInstance, IComponentData
    where TData : ComponentData<TData>
{

    private static void SerializeObject(object? value, XmlElement parent)
    {
        if (value == null)
            return;

        var objectType = value.GetType();
        var objectEl = parent.OwnerDocument.CreateElement("Object");
        parent.AppendChild(objectEl);
        objectEl.SetAttribute("FullType", objectType.FullName);

        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            NamespaceHandling = NamespaceHandling.OmitDuplicates,
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = true,
            NewLineOnAttributes = false
        };
        using var writer = XmlWriter.Create(stream, settings);
        var serializer = new XmlSerializer(objectType);
        serializer.Serialize(writer, value);

        var reader = new StreamReader(stream);
        stream.Position = 0;
        var valueXMLString = reader.ReadToEnd();
        Console.WriteLine(valueXMLString);

        objectEl.InnerXml = valueXMLString;
    }

    private static object? DeserializeObject(XmlElement objectNode)
    {
        var objectType = Type.GetType(objectNode.GetAttribute("FullType"));
        if (objectType == null)
            return null;

        var serializer = new XmlSerializer(objectType);
        using var reader = new StringReader(objectNode.InnerXml);
        return serializer.Deserialize(reader);
    }

    private static void SerializeProperty(object? value, string name, XmlElement parent)
    {
        XmlElement propertyElement;

        if (value is IComponentData componentData)
        {
            propertyElement = componentData.Serialize(parent);
        }
        else if (value is IInstanceLookup instanceLookup)
        {
            propertyElement = parent.OwnerDocument.CreateElement("InstanceLookup");
            propertyElement.SetAttribute("Guid", instanceLookup.Guid.ToString());
        }
        else
        {
            propertyElement = parent.OwnerDocument.CreateElement("Property");
            // use xml serializer to serialize value
            SerializeObject(value, propertyElement);
        }

        propertyElement.SetAttribute("Name", name);

        parent.AppendChild(propertyElement);
    }

    private static object? DeserializeProperty(XmlElement propertyElement)
    {
        if (propertyElement.SelectSingleNode("ComponentData") is XmlElement componentDataElement)
        {
            var type = Type.GetType(componentDataElement.GetAttribute("FullType"));
            if (type == null)
                return null;

            var constructor = type.GetConstructor(new Type[] { typeof(XmlNode) });
            if (constructor == null)
                return null;

            return constructor.Invoke(new object[] { componentDataElement });
        }

        if (propertyElement.SelectSingleNode("InstanceLookup") is XmlElement instanceLookupElement)
        {
            var guid = Guid.Parse(instanceLookupElement.GetAttribute("Guid"));
            return Activator.CreateInstance(typeof(InstanceLookup<>).MakeGenericType(typeof(TData)), guid);
        }

        if (propertyElement.SelectSingleNode("Object") is XmlElement objectElement)
        {
            return DeserializeObject(objectElement);
        }

        return null;
    }


    public XmlElement Serialize(XmlElement parent)
    {
        var element = parent.OwnerDocument.CreateElement("ComponentData");
        parent.SetAttribute("FullType", typeof(TData).FullName);
        parent.AppendChild(element);

        // get all properties using reflection
        var properties = typeof(TData).GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(this);
            if (value is IDataProperty dataProperty)
                SerializeProperty(data[dataProperty], dataProperty.Name, element);
            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    SerializeProperty(item, property.Name, element);
                }
            }
            if (property.Name == "Guid")
            {
                parent.SetAttribute("Guid", value.ToString());
            }
            else
            {
                SerializeProperty(value, property.Name, element);
            }
        }

        return element;
    }

    protected ComponentData(XmlNode deserialize) : base(null, true)
    {
        if (deserialize == null)
            return;

        var type = Type.GetType(deserialize.GetAttribute("FullType"));

        var properties = typeof(TData).GetProperties();
        foreach (var property in properties)
        {
            var propertyNode = deserialize.SelectSingleNode($"Property[@Name='{property.Name}']");
            if (propertyNode is not XmlElement propertyElement)
                continue;

            var value = DeserializeProperty(propertyElement);
            
            // set value using reflection
            property.SetValue(this, value);
        }
    }

    
    public struct ValueChangedArgs(ComponentData<TData> data, IDataProperty property, object? oldValue, object? newValue)
    {
        public ComponentData<TData> Owner { get; } = data;
        public IDataProperty Property { get; } = property;
        public object? OldValue { get; } = oldValue;
        public object? NewValue { get; } = newValue;
    }

    public event EventHandler<ValueChangedArgs>? DataChanged;

    private readonly Dictionary<IDataProperty, object?> data = [];
    protected void SetValue<TDataType>(DataProperty<TData, TDataType> property, TDataType value)
    {
        data.TryGetValue(property, out var old);

        if (Object.Equals(old, value))
            return;

        data[property] = value;
        DataChanged?.Invoke(this, new ValueChangedArgs(this, property, old ?? default, value));
    }

    protected TDataType? GetValue<TDataType>(DataProperty<TData, TDataType> property)
    {
        var result = data.TryGetValue(property, out var value) ? (TDataType?)value : default;

        if (result == null)
        {
            data[property] = property.CreateInitialValue();
            return (TDataType)data[property];
        }

        return result;
    }

    

    protected ComponentData() : base()
    {
    }
}