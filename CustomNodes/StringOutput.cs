using Avalonia.Controls;
using NodeBuilder;

public partial class StringOutputNode : GenericNode<NodeView, NodeData>
{
    public StringOutputNode() : base("String Output", "A string output node")
    {
        var input = AddInputProperty<string>("String", "");
        var stringLabel = new Label();
        input.Data.ValueChanged += (sender, args) =>
        {
            stringLabel.Content = input.Data.Value?.ToString() ?? "null";
        };

        AddControl(stringLabel);
    }
}