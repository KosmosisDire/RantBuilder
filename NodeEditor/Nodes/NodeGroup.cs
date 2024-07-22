using NodeBuilder.Base;
namespace NodeBuilder;

public class NodeGroup : GenericNode<NodeView, NodeData>
{
    public NodeGroup(string name) : base(name, "", null)
    {
        Layout.Classes.Add("NodeGroup");
    }
}