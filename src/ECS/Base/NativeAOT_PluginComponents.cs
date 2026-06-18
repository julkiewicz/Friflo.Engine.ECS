// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.
// Friflo.Engine.ECS fork addition - partial extension to NativeAOT.

// ReSharper disable once CheckNamespace

namespace Friflo.Engine.ECS;

public sealed partial class NativeAOT
{
    public static bool SchemaCreated => Instance?.entitySchema != null;

    public int RegisterModComponent(ModComponentRegistration pointers)
    {
        var structIndex = schemaTypes.components.Count + 1;
        schemaTypes.components.Add(new ModComponentType(structIndex, pointers));

        return structIndex;
    }
}