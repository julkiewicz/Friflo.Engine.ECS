// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork addition.

using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// A non-generic <see cref="StructHeap"/> that stores component data as a flat <c>byte[]</c>
/// with a fixed per-element stride. Used for plugin components whose concrete struct type is
/// only known in the managed CoreCLR plugin, not in the AOT-compiled server.
/// <para>
/// All plugin component types are guaranteed blittable (no managed fields). The stride is
/// set at registration time and never changes. Layout within each stride-sized slot is the
/// plugin's responsibility.
/// </para>
/// </summary>
public sealed class PluginStructHeap : StructHeap
{
    // Backing store: stride * capacity bytes, laid out consecutively.
    // Made internal so the AOT server query functions can access it directly.
    internal byte[] components;

    // Single-element stash for indexed-component change tracking.
    // Plugin components never have indices, but the abstract contract requires it.
    private readonly byte[] componentStash;

    public readonly int Stride;

    internal PluginStructHeap(int structIndex, int stride)
        : base(structIndex)
    {
        Stride          = stride;
        components      = new byte[ArchetypeUtils.MinCapacity * stride];
        componentStash  = new byte[stride];
    }

    // -------------------------------------------------------------------------
    // Pointer access - called inside GC.TryStartNoGCRegion on the server so
    // the array will not be moved by the GC during the query window.
    // -------------------------------------------------------------------------

    public override IntPtr ReadyMGetPtrToFirst()
    {
        unsafe
        {
            fixed (byte* ptr = components)
                return (IntPtr)ptr;
        }
    }

    // -------------------------------------------------------------------------
    // Lifecycle / resize - called by Friflo archetype machinery
    // -------------------------------------------------------------------------

    protected override int ComponentsLength => components.Length / Stride;

    internal override void ResizeComponents(int capacity, int count)
    {
        var newComponents = new byte[capacity * Stride];
        Buffer.BlockCopy(components, 0, newComponents, 0, count * Stride);
        components = newComponents;
    }

    internal override void MoveComponent(int from, int to)
    {
        Buffer.BlockCopy(components, from * Stride, components, to * Stride, Stride);
    }

    internal override void CopyComponentTo(int sourcePos, StructHeap targetHeap, int targetPos)
    {
        var target = (PluginStructHeap)targetHeap;
        Buffer.BlockCopy(components, sourcePos * Stride, target.components, targetPos * Stride, Stride);
    }

    internal override void CopyComponent(
        int sourcePos, StructHeap targetHeap, int targetPos,
        in CopyContext context, long updateIndexTypes)
    {
        // Plugin components are always blittable and never carry ECS indices.
        var target = (PluginStructHeap)targetHeap;
        Buffer.BlockCopy(components, sourcePos * Stride, target.components, targetPos * Stride, Stride);
    }

    internal override void SetComponentDefault(int compIndex)
    {
        Array.Clear(components, compIndex * Stride, Stride);
    }

    internal override void SetComponentsDefault(int compIndexStart, int count)
    {
        Array.Clear(components, compIndexStart * Stride, count * Stride);
    }

    // -------------------------------------------------------------------------
    // Index support - plugin components never participate in ECS indexes
    // -------------------------------------------------------------------------

    internal override void StashComponent(int compIndex)
    {
        Buffer.BlockCopy(components, compIndex * Stride, componentStash, 0, Stride);
    }

    internal override void UpdateIndex(Entity entity) { }
    internal override void AddIndex   (Entity entity) { }
    internal override void RemoveIndex(Entity entity) { }

    // -------------------------------------------------------------------------
    // Batch operations - not supported for plugin components
    // -------------------------------------------------------------------------

    internal override void SetBatchComponent(BatchComponent[] batchComponents, int compIndex)
        => throw new NotSupportedException("Batch operations are not supported for plugin components.");

    // -------------------------------------------------------------------------
    // Debug / serialization
    // -------------------------------------------------------------------------

    internal override Type StructType => typeof(PluginComponentMarker);

    public override object GetStashDebug()
    {
        var copy = new byte[Stride];
        Buffer.BlockCopy(componentStash, 0, copy, 0, Stride);
        return copy;
    }

    internal override object GetComponentDebug(int compIndex)
    {
        var copy = new byte[Stride];
        Buffer.BlockCopy(components, compIndex * Stride, copy, 0, Stride);
        return copy;
    }

    internal override Bytes Write(ObjectWriter writer, int compIndex)
        => throw new NotSupportedException("JSON serialization is not supported for plugin components.");

    internal override void Read(ObjectReader reader, int compIndex, JsonValue json)
        => throw new NotSupportedException("JSON deserialization is not supported for plugin components.");

    // -------------------------------------------------------------------------
    // Member access - not supported
    // -------------------------------------------------------------------------

    internal override bool GetComponentMember<TField>(
        int compIndex, MemberPath memberPath, out TField value, out Exception exception)
    {
        exception = new NotSupportedException("Member access is not supported for plugin components.");
        value = default;
        return false;
    }

    internal override bool SetComponentMember<TField>(
        Entity entity, MemberPath memberPath, TField value,
        Delegate onMemberChanged, out Exception exception)
    {
        exception = new NotSupportedException("Member access is not supported for plugin components.");
        return false;
    }
}

/// <summary>
/// Marker type returned by <see cref="PluginStructHeap.StructType"/> for debugging.
/// Not used as an actual ECS component.
/// </summary>
internal sealed class PluginComponentMarker { }