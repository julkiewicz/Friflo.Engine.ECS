// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork addition.

using System;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// AOT-side StructHeap backed by a TypedComponentHeap&lt;T&gt; living in the embedded CoreCLR runtime.
/// All mutations are dispatched through function pointers into CoreCLR so write barriers always
/// fire on the managed side. The AOT process never writes into the component array directly.
///
/// For blittable T, ReadyMGetPtrToFirst() returns a stable pinned pointer for read-only iteration.
/// For non-blittable T, it returns IntPtr.Zero - callers must dispatch through the delegates instead.
/// </summary>
public sealed class ExternallyManagedHeap : StructHeap
{
    // Opaque GCHandle of the CoreCLR TypedComponentHeap<T>. Passed as context into CopyTo.
    private readonly IntPtr self;

    // Cached raw pointer into the pinned component array. Valid only when IsBlittable is true.
    // Refreshed after every resize.
    private IntPtr ptr;

    // Delegates wrapping the CoreCLR function pointers. Stored as fields so the GC on the AOT
    // side doesn't collect the wrapper objects (the CoreCLR side keeps the underlying stubs alive
    // via PinnedDelegateStore, but the AOT-side delegate wrappers are separate objects).
    private readonly HeapGetPtrDelegate     getPtrToFirst;
    private readonly HeapGetCountDelegate   getLength;
    private readonly HeapResizeDelegate     resize;
    private readonly HeapMoveDelegate       move;
    private readonly HeapCopyToDelegate     copyTo;
    private readonly HeapSetDefaultDelegate setDefault;
    private readonly HeapClearRangeDelegate setRangeDefault;

    public readonly int Stride;
    public readonly bool IsBlittable;

    internal ExternallyManagedHeap(int structIndex, AOTHeapPointers pointers)
        : base(structIndex)
    {
        Stride = pointers.Stride;
        IsBlittable = pointers.IsBlittable == 1;
        self = pointers.Self;

        getPtrToFirst   = Marshal.GetDelegateForFunctionPointer<HeapGetPtrDelegate>    (pointers.GetPtrToFirst);
        getLength       = Marshal.GetDelegateForFunctionPointer<HeapGetCountDelegate>  (pointers.GetLength);
        resize          = Marshal.GetDelegateForFunctionPointer<HeapResizeDelegate>    (pointers.Resize);
        move            = Marshal.GetDelegateForFunctionPointer<HeapMoveDelegate>      (pointers.Move);
        copyTo          = Marshal.GetDelegateForFunctionPointer<HeapCopyToDelegate>    (pointers.CopyTo);
        setDefault      = Marshal.GetDelegateForFunctionPointer<HeapSetDefaultDelegate>(pointers.SetDefault);
        setRangeDefault = Marshal.GetDelegateForFunctionPointer<HeapClearRangeDelegate>(pointers.SetRangeDefault);

        ptr = getPtrToFirst();
    }

    // -------------------------------------------------------------------------
    // ReadyM extension
    // -------------------------------------------------------------------------

    // Blittable T: stable pinned pointer, safe to return directly.
    // Non-blittable T: call through the delegate for a live address.
    //   Valid only within a no-GC region - ScanArchetypes guarantees this.
    public override IntPtr ReadyMGetPtrToFirst()
        => IsBlittable ? ptr : getPtrToFirst();

    // -------------------------------------------------------------------------
    // StructHeap core
    // -------------------------------------------------------------------------

    protected override int ComponentsLength => getLength();

    internal override void ResizeComponents(int capacity, int count)
    {
        resize(capacity, count);
        ptr = getPtrToFirst(); // refresh - managed side may have re-pinned a new array
    }

    internal override void MoveComponent(int from, int to) => move(from, to);

    internal override void CopyComponentTo(int sourcePos, StructHeap targetHeap, int targetPos)
    {
        var target = (ExternallyManagedHeap)targetHeap;
        copyTo(sourcePos, target.self, targetPos);
    }

    internal override void CopyComponent(
        int sourcePos, StructHeap targetHeap, int targetPos,
        in CopyContext context, long updateIndexTypes)
    {
        // Plugin components never carry ECS indices - same path as CopyComponentTo.
        var target = (ExternallyManagedHeap)targetHeap;
        copyTo(sourcePos, target.self, targetPos);
    }

    internal override void SetComponentDefault(int compIndex) => setDefault(compIndex);

    internal override void SetComponentsDefault(int compIndexStart, int count)
        => setRangeDefault(compIndexStart, count);

    // -------------------------------------------------------------------------
    // Index support - plugin components never participate in ECS indexes
    // -------------------------------------------------------------------------

    internal override void StashComponent(int compIndex) 
        => throw new NotSupportedException("Stash is not supported for plugin components.");

    internal override void UpdateIndex(Entity entity) { }
    internal override void AddIndex(Entity entity) { }
    internal override void RemoveIndex(Entity entity) { }

    // -------------------------------------------------------------------------
    // Batch operations - not supported via AOT dispatch path
    // -------------------------------------------------------------------------

    internal override void SetBatchComponent(BatchComponent[] batchComponents, int compIndex)
    {
        // var comp = (PluginBatchComponent)batchComponents[structIndex];
        // actually, this will never happen?
    }

    // -------------------------------------------------------------------------
    // Debug / serialization
    // -------------------------------------------------------------------------

    internal override Type StructType => typeof(PluginComponentMarker);

    public override object GetStashDebug()
        => "(plugin component stash - opaque, inspect via CoreCLR side)";

    internal override object GetComponentDebug(int compIndex)
        => $"(plugin component [{compIndex}] - opaque, inspect via CoreCLR side)";

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
/// Marker type returned by <see cref="ExternallyManagedHeap.StructType"/> for debugging.
/// Not used as an actual ECS component.
/// </summary>
internal class PluginComponentMarker;