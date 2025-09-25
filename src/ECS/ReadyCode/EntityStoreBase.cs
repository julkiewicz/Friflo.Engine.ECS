using System.Threading;
using Friflo.Engine.ECS.ReadyCode;

namespace Friflo.Engine.ECS;

public partial class EntityStoreBase
{
    internal readonly ReaderWriterLockSlim RwLock = new(LockRecursionPolicy.SupportsRecursion);
    
    internal ReaderWriterLockMgr GetScopedLock() => new(RwLock);
}