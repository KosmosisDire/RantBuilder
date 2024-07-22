
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;
using NodeBuilder;
using NodeBuilder.Base;

public record struct RosTopic
{
    public string name;
    public string type;
}

public partial class RosNode : GenericNode<NodeView, NodeData>
{
    public enum ScriptType
    {
        Python,
        CSharp,
        Cpp,
        Other
    }

    public string sourcePath;
    public ScriptType scriptType;

    public RosNode(string sourcePath) : base(GetNameFromPath(sourcePath), "")
    {
        // parse type from path
        this.sourcePath = sourcePath;
        scriptType = GetScriptType(sourcePath);

        var pubs = GetPublishers(File.ReadAllLines(sourcePath));
        foreach (var pub in pubs)
        {
            var topic = new RosTopic { name = pub.Item1, type = pub.Item2 };
            AddOutputProperty(pub.Item1, topic);
        }

        var subs = GetSubscribers(File.ReadAllLines(sourcePath));
        foreach (var sub in subs)
        {
            var prop = AddInputProperty<RosTopic>(sub.Item1);
            prop.Data.Value = new RosTopic { name = sub.Item1, type = sub.Item2 }; 
        }
    }

    public override bool ValidateConnection(INodeProperty a, INodeProperty b)
    {
        var aData = (RosTopic)a.Data.Value;
        var bData = (RosTopic)b.Data.Value;
        return base.ValidateConnection(a, b) && aData.type == bData.type && aData.name == bData.name;
    }

    public List<(string, string)> GetPublishers(string[] lines)
    {
        // get publishers from source file
        var publishers = new List<(string, string)>();

        foreach (var line in lines)
        {
            if (scriptType == ScriptType.Python && PythonPublisherName().IsMatch(line))
            {
                publishers.Add((PythonPublisherName().Match(line).Groups[1].Value, PythonPublisherType().Match(line).Groups[1].Value));
            }
        }

        return publishers;
    }

    public List<(string, string)> GetSubscribers(string[] lines)
    {
        // get subscribers from source file
        var subscribers = new List<(string, string)>();

        foreach (var line in lines)
        {
            if (scriptType == ScriptType.Python && PythonSubscriberName().IsMatch(line))
            {
                subscribers.Add((PythonSubscriberName().Match(line).Groups[1].Value, PythonSubscriberType().Match(line).Groups[1].Value));
            }
        }

        return subscribers;
    }

    public static ScriptType GetScriptType(string path)
    {
        var extension = Path.GetExtension(path);
        switch (extension)
        {
            case ".py":
            case ".pyc":
            case ".ipynb":
                return ScriptType.Python;
            case ".cs":
                return ScriptType.CSharp;
            case ".cpp":
            case ".c":
            case ".h":
                return ScriptType.Cpp;
            default:
                return ScriptType.Other;
        }
    }

    public static string GetNameFromPath(string path)
    {
        var pythonName = Path.GetFileNameWithoutExtension(path).Split('_', '-', ' ').Select(x => string.Concat(x.First().ToString().ToUpper(), x.AsSpan(1))).Aggregate((x, y) => x + " " + y);
        var csName = CamelCaseWordSplit().Replace(pythonName, " $1");
        var withType = csName + " (" + GetScriptType(path).ToString() + ")";

        return withType;
    }

    // regex to get the name of a publisher from a python rospy.Publisher call
    [GeneratedRegex("rospy\\.Publisher\\([\"']\\/?(.+?)[\"'],")]
    private static partial Regex PythonPublisherName();

    // regex to get the type of a publisher from a python rospy.Publisher call
    [GeneratedRegex("rospy\\.Publisher\\(.+?,\\s*(.*),")]
    private static partial Regex PythonPublisherType();

    // regex to get the name of a subscriber from a python rospy.Subscriber call
    [GeneratedRegex("rospy\\.Subscriber\\([\"']\\/?(.+?)[\"'],")]
    private static partial Regex PythonSubscriberName();

    // regex to get the type of a subscriber from a python rospy.Subscriber call
    [GeneratedRegex("rospy\\.Subscriber\\(.+?,\\s*(.*),")]
    private static partial Regex PythonSubscriberType();

    // regex to split camel case words
    [GeneratedRegex(@"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))")]
    private static partial Regex CamelCaseWordSplit();
}