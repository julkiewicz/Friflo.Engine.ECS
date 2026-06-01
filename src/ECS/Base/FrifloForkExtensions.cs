// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork additions - partial extensions to existing Friflo types.

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public partial class Archetype
{
    /// <summary>
    /// Returns the <see cref="StructHeap"/> for the given struct index,
    /// or null if this archetype does not contain that component.
    /// Works for both typed AOT heaps (<see cref="StructHeap{T}"/>) and
    /// runtime plugin heaps (<see cref="PluginStructHeap"/>).
    /// </summary>
    public StructHeap? GetHeap(int structIndex)
    {
        if (structIndex >= heapMap.Length) return null;
        return heapMap[structIndex];
    }
}

public partial class EntityStore
{
    /// <summary>
    /// Returns the internal archetype array. Some slots may be null.
    /// Use <see cref="GetArchetypeCount"/> for the valid upper bound.
    /// </summary>
    public Archetype[] GetArchetypes()    => archs;
    public int         GetArchetypeCount() => ArchetypeCount;
}