using NodeBuilder;

public partial class AddNode : GenericNode<NodeView, NodeData>
{
    private void AddInput()
    {
        var input = AddInputProperty<float>($"Input {Data.InputCount + 1}");
        NodePropertyData<float>.ConnectionCountProperty.ValueChanged(input.Data, (sender, args) =>
        {
            if (args.NewValue == 1)
            {
                AddInput();
            }

            if (args.NewValue == 0 && Data.InputCount > 1)
            {
                Data.RemoveInputProperty(input.Data);
                View.LeftPropertyDock.Children.Remove(input.View.Layout);
            }
        });
    }

    public AddNode() : base("Addition", "Adds numbers")
    {
        var output = AddOutputProperty<float>("Result", 0, (input) =>
        {
            var result = 0f;
            for (int i = 0; i < Data.InputCount; i++)
            {
                var val = Data.GetInputAt(i).Value;
                if (val == null)
                {
                    continue;
                }
                result += (float)val;
            }
            return result;
        });



        AddInput();
    }
}