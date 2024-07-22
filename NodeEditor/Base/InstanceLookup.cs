using System;
using System.Collections.Generic;

public class SerializedInstance
{
    public Guid Guid { get; protected set;}
    public SerializedInstance(Guid? guid = null, bool deserializing = false)
    {
        if (deserializing)
            return;

        Guid = guid ?? Guid.NewGuid();
        InstanceLookup.Lookup[Guid] = this;
    }

    public void ChangeGuid(Guid newGuid)
    {
        InstanceLookup.Lookup.Remove(Guid);
        Guid = newGuid;
        InstanceLookup.Lookup[Guid] = this;
    }

    public void ChangeGuid(string newGuid)
    {
        if (!Guid.TryParse(newGuid, out var guid))
            return;

        ChangeGuid(guid);
    }
}

internal class InstanceLookup
{
    internal static readonly Dictionary<Guid, SerializedInstance> Lookup = [];
}

public interface IInstanceLookup
{
    public Guid Guid { get; }
    public object? Instance { get; }
}

public class InstanceLookup<T> : IInstanceLookup where T : SerializedInstance
{
    private Guid guid;
    private T? instance;
    private bool isInstanceSet = false;
    public T? Instance
    {
        get
        {
            if (isInstanceSet)
                return instance;

            if (guid == Guid.Empty || !InstanceLookup.Lookup.TryGetValue(guid, out var objInstance) || objInstance is not T instanceT)
                return default;

            instance = instanceT;
            isInstanceSet = true;

            return instanceT;
        }
        set
        {
            instance = value;
            guid = value?.Guid ?? Guid.Empty;
            isInstanceSet = true;
        }
    }

    public Guid Guid => guid;
    object? IInstanceLookup.Instance => Instance;



    public InstanceLookup(Guid? guid = null)
    {
        this.guid = guid ?? Guid.Empty;
    }

    public InstanceLookup(string guid)
    {
        if (!Guid.TryParse(guid, out this.guid))
            this.guid = Guid.Empty;
    }

    public static implicit operator string(InstanceLookup<T> instanceLookup) => instanceLookup.guid.ToString();
}