using System;
using System.Threading;

namespace Friflo.Engine.ECS.ReadyCode;

internal static class EcsRwLock
{
    internal static volatile ReaderWriterLockSlim Instance = new(LockRecursionPolicy.SupportsRecursion);

    internal sealed class ReaderWriterLockMgr : IDisposable
    {
        private enum LockTypes
        {
            None,
            Read,
            Write,
            Upgradeable
        }

        private LockTypes enteredLockType = LockTypes.None;

        internal void EnterReadLock()
        {
            Instance.EnterReadLock();
            enteredLockType = LockTypes.Read;
        }

        internal void EnterWriteLock()
        {
            Instance.EnterWriteLock();
            enteredLockType = LockTypes.Write;
        }

        internal void EnterUpgradeableReadLock()
        {
            Instance.EnterUpgradeableReadLock();
            enteredLockType = LockTypes.Upgradeable;
        }

        private void ExitLock()
        {
            switch (enteredLockType)
            {
                case LockTypes.Read:
                    Instance.ExitReadLock();
                    enteredLockType = LockTypes.None;
                    return;
                case LockTypes.Write:
                    Instance.ExitWriteLock();
                    enteredLockType = LockTypes.None;
                    return;
                case LockTypes.Upgradeable:
                    Instance.ExitUpgradeableReadLock();
                    enteredLockType = LockTypes.None;
                    return;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            ExitLock();
        }

        ~ReaderWriterLockMgr()
        {
            ExitLock();
        }
    }

    internal static ReaderWriterLockMgr GetWriteLock() => new();
}