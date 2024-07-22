using System.Collections.Generic;
using Avalonia.Controls;

namespace NodeBuilder.Base;

public interface IComponent<out TView, out TData>
{
    public Panel Layout { get; } 
    public TData Data { get; }
    public TView View { get; }
}

public abstract class Component<TComponent, TView, TData> : IComponent<TView, TData>
    where TComponent : Component<TComponent, TView, TData>
    where TView : ComponentView<TData>
    where TData : ComponentData<TData>
{
    public static Dictionary<TData, TComponent> DataToComponent { get; } = [];

    public TData Data { get; protected init; }
    public TView View { get; protected init; }

    public static string Classname { get; } = typeof(TComponent).Name;

    public Panel Layout => View.Layout;

    protected Component(TView view)
    {
        Data = view.Data;
        View = view;
        View.Layout.Classes.Set(Classname, true);
        DataToComponent.Add(Data, (TComponent)this);
    }

    public static TComponent? GetComponent(TData data) => DataToComponent.TryGetValue(data, out var component) ? component : null;
    public static TComponent? GetComponent(IComponentData data) => DataToComponent.TryGetValue((TData)data, out var component) ? component : null;

    public static implicit operator Panel(Component<TComponent, TView, TData> component) => component.Layout;
    public static implicit operator Control(Component<TComponent, TView, TData> component) => component.Layout;
}