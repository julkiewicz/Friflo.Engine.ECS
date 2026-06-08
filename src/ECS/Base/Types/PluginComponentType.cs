// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork addition.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork addition.

[StructLayout(LayoutKind.Sequential)]
public struct PluginComponentRegistration
{
    /// <summary><c>Unsafe.SizeOf&lt;T&gt;()</c> - informs the AOT side of component stride.</summary>
    public int Stride;

    /// <summary>1 if T is blittable and raw pointer iteration is available, 0 otherwise.</summary>
    public byte IsBlittable;
    
    public IntPtr AllocHeap;
}

/// <summary>
/// Delegate matching the AllocHeap function pointer in PluginComponentRegistration.
/// </summary>
public delegate void AllocHeapDelegate(int capacity, IntPtr outAOTHeapPointers);


internal sealed class PluginComponentType : ComponentType
{
    public override string ToString() => $"PluginComponent[{StructIndex}] stride:{registration.Stride}";

    private readonly PluginComponentRegistration registration;

    // Wrapped and stored as a field so the AOT-side GC doesn't collect the delegate wrapper
    // between archetype materializations.
    private readonly AllocHeapDelegate allocHeap;

    internal PluginComponentType(int structIndex, PluginComponentRegistration registration)
        : base(
            componentKey:   $"plugin_{structIndex}",
            structIndex:    structIndex,
            type:           typeof(PluginComponentMarker),
            indexType:      null,
            indexValueType: null,
            byteSize:       registration.Stride,
            relationType:   null,
            keyType:        null)
    {
        this.registration = registration;
        allocHeap    = Marshal.GetDelegateForFunctionPointer<AllocHeapDelegate>(registration.AllocHeap);

        Unsafe.AsRef(in IsBlittable) = registration.IsBlittable != 0;
    }

    // Called by the ECS each time an archetype that contains this component type is first
    // materialized. Calls back into CoreCLR to allocate a fresh TypedComponentHeap<T>.
    internal override unsafe StructHeap CreateHeap()
    {
        // Stack-allocate the output struct. The call is synchronous, so the stack frame
        // is alive for the duration of AllocHeapImpl writing into it.
        AOTHeapPointers pointers;
        allocHeap(ArchetypeUtils.MinCapacity, (IntPtr)(&pointers));
        return new ExternallyManagedHeap(StructIndex, pointers);
    }

    internal override bool RemoveEntityComponent(Entity entity)
        => throw new NotSupportedException(
            $"Cannot dynamically remove plugin component [{StructIndex}] from an entity. " +
            "Plugin component membership is fixed at entity creation.");

    internal override bool AddEntityComponent(Entity entity)
        => throw new NotSupportedException(
            $"Cannot dynamically add plugin component [{StructIndex}] to an entity. " +
            "Plugin component membership is fixed at entity creation.");

    internal override bool AddEntityComponentValue(Entity entity, object value)
        => throw new NotSupportedException(
            "Cannot add plugin component by value. " +
            "Plugin component membership is fixed at entity creation.");
}