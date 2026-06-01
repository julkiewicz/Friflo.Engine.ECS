// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork addition - partial extension to NativeAOT.

// ReSharper disable once CheckNamespace

using System;

namespace Friflo.Engine.ECS;

public sealed partial class NativeAOT
{
    /// <summary>
    /// Registers a plugin component type with the given per-element stride in bytes.
    /// Must be called after all <see cref="RegisterComponent{T}"/> calls and before
    /// <see cref="CreateSchema"/>, typically during plugin assembly loading.
    /// </summary>
    /// <param name="stride">
    /// Size in bytes of one component instance. Must be &gt; 0 and &lt;= 256.
    /// The plugin is responsible for ensuring its struct fits within this stride.
    /// </param>
    /// <returns>
    /// The <c>StructIndex</c> assigned to this component type. Pass this value to
    /// <see cref="EcsPluginQuery"/> functions and to <c>EcsApi.RegisterComponent&lt;T&gt;</c>
    /// on the plugin side.
    /// </returns>
    public int RegisterPluginComponent(int stride)
    {
        if (stride <= 0)
            throw new ArgumentOutOfRangeException(nameof(stride), stride,
                "Plugin component stride must be greater than 0.");
        if (stride > 256)
            throw new ArgumentOutOfRangeException(nameof(stride), stride,
                "Plugin component stride must not exceed 256 bytes. " +
                "Split the component or increase the maximum stride if needed.");

        // 'types' is the List<SchemaType> that NativeAOT accumulates before CreateSchema().
        // The next available StructIndex is types.Count (0-based, same as for typed components).
        var structIndex = schemaTypes.components.Count;
        schemaTypes.components.Add(new PluginComponentType(structIndex, stride));
        return structIndex;
    }
}