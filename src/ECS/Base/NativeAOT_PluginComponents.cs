// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork addition - partial extension to NativeAOT.

// ReSharper disable once CheckNamespace

using System;

namespace Friflo.Engine.ECS;

public sealed partial class NativeAOT
{
    public static bool SchemaCreated => Instance?.entitySchema != null;

    public int RegisterPluginComponent(int stride)
    {
        if (stride <= 0)
            throw new ArgumentOutOfRangeException(nameof(stride), stride,
                "Plugin component stride must be greater than 0.");
        if (stride > 256)
            throw new ArgumentOutOfRangeException(nameof(stride), stride,
                "Plugin component stride must not exceed 256 bytes. " +
                "Split the component or increase the maximum stride if needed.");

        var structIndex = schemaTypes.components.Count + 1;
        schemaTypes.components.Add(new PluginComponentType(structIndex, stride));

        return structIndex;
    }
}