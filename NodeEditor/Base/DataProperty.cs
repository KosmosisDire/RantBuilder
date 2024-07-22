
using System;
namespace NodeBuilder.Base;

public interface IDataProperty
{
    public string Name { get; }
    public Type OwnerType { get; }
    public Type DataType { get; }
}

public class DataProperty<TOwner, TData>(string name, Func<TData> createInitialValue) : IDataProperty where TOwner : ComponentData<TOwner>
{
    public string Name { get; private set; } = name;
    public Func<TData> CreateInitialValue { get; } = createInitialValue;
    public Type OwnerType { get; } = typeof(TOwner);
    public Type DataType { get; } = typeof(TData);

    public struct ValueChangedArgs
    {
        public ComponentData<TOwner> Owner { get; }
        public TData? OldValue { get; }
        public TData? NewValue { get; }

        public ValueChangedArgs(ComponentData<TOwner> data, TData? oldValue, TData? newValue)
        {
            Owner = data;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public void ValueChanged(ComponentData<TOwner> changedOn, EventHandler<ValueChangedArgs> handler)
    {
        changedOn.DataChanged += (sender, data) =>
        {
            if (data.Property != this)
                return;

            Console.WriteLine($"DataProperty: {Name} changed from {data.OldValue} to {data.NewValue}");

            var old = data.OldValue;
            var @new = data.NewValue;

            if (old == null && @new == null)
                return;

            if (old == null)
            {
                old = CreateInitialValue();
            }

            var args = new ValueChangedArgs(changedOn, (TData?)old, (TData?)data.NewValue);
            handler(sender, args);
        };
    }
}