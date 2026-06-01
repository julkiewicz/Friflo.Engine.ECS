// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork addition.

using System;
using Friflo.Json.Fliox;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// A non-generic <see cref="ComponentType"/> for plugin component types registered at
/// server startup. Each call to <see cref="NativeAOT.RegisterPluginComponent"/> produces
/// one instance with a unique <see cref="ComponentType.StructIndex"/>.
/// <para>
/// The concrete struct layout is only known in the managed plugin assembly. From the ECS
/// perspective this is an opaque blittable blob of <see cref="Stride"/> bytes.
/// </para>
/// </summary>
internal sealed class PluginComponentType : ComponentType
{
    internal readonly int Stride;

    public override string ToString() => $"PluginComponent[{StructIndex}] stride:{Stride}";

    internal PluginComponentType(int structIndex, int stride)
        : base(
            componentKey:   $"plugin_{structIndex}",    // used as JSON key; serialization is unsupported
            structIndex:    structIndex,
            type:           typeof(PluginComponentMarker),
            indexType:      null,
            indexValueType: null,
            byteSize:       stride,                     // ComponentType.StructSize = stride
            relationType:   null,
            keyType:        null)
    {
        Stride = stride;

        // Override the blittability field that the base constructor sets via reflection.
        // Plugin components are always blittable by contract (enforced at registration time).
        // We use Unsafe.AsRef to write the readonly field from outside the constructor chain.
        System.Runtime.CompilerServices.Unsafe.AsRef(in IsBlittable) = true;
    }

    // CreateHeap is the only method Friflo calls routinely for plugin components.
    // A new PluginStructHeap is created each time an archetype that contains this
    // component type is first materialized.
    internal override StructHeap CreateHeap() => new PluginStructHeap(StructIndex, Stride);

    // Dynamic add/remove is not supported for plugin components. Entities that carry
    // plugin components are always created with the full archetype from the start.
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