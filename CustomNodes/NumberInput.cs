using Avalonia.Controls;
using NodeBuilder;

public partial class NumberInputNode : GenericNode<NodeView, NodeData>
{
    public NumberInputNode() : base("Number Input", "A number input node")
    {
        var output = AddOutputProperty<float>("Output", 0);
        var textBox = new TextBox();
        textBox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        textBox.Width = 100;
        textBox.TextChanged += (sender, args) =>
        {
            if (float.TryParse(textBox.Text, out var value))
            {
                output.Data.Value = value;
            }
            else
            {
                output.Data.Value = 0;
            }
        };

        AddControl(textBox);
    }
}