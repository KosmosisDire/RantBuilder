using Avalonia;
using Avalonia.Controls;

namespace NodeBuilder.Base;

public interface IComponentView<out TData>
{
    public TData Data { get; }
    public Panel Layout { get; }
    public static string Classname { get; }

    public void LayoutAdd(Control element);
    public void LayoutRemove(Control element);
}


public abstract class ComponentView<TData> : IComponentView<TData>
    where TData : ComponentData<TData>
{
    public TData Data { get; protected init; }
    public abstract Panel Layout { get; }

    public void LayoutAdd(Control element) => Layout.Children.Add(element);
    public void LayoutRemove(Control element) => Layout.Children.Remove(element);

    public ComponentView(TData data)
    {
        Data = data;
    }

    public static implicit operator Control(ComponentView<TData> view) => view.Layout;
    public static implicit operator Panel(ComponentView<TData> view) => view.Layout;
    public static implicit operator StyledElement(ComponentView<TData> view) => view.Layout;
}