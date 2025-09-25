using System;
using System.Threading;

namespace Friflo.Engine.ECS.ReadyCode;

internal static class EcsRwLock
{
    internal static volatile ReaderWriterLockSlim Instance = new(LockRecursionPolicy.SupportsRecursion);

    internal sealed class ReaderWriterLockMgr(ReaderWriterLockSlim readerWriterLock) : IDisposable
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
            readerWriterLock.EnterReadLock();
            enteredLockType = LockTypes.Read;
        }

        internal void EnterWriteLock()
        {
            readerWriterLock.EnterWriteLock();
            enteredLockType = LockTypes.Write;
        }

        internal void EnterUpgradeableReadLock()
        {
            readerWriterLock.EnterUpgradeableReadLock();
            enteredLockType = LockTypes.Upgradeable;
        }

        private void ExitLock()
        {
            switch (enteredLockType)
            {
                case LockTypes.Read:
                    readerWriterLock.ExitReadLock();
                    enteredLockType = LockTypes.None;
                    return;
                case LockTypes.Write:
                    readerWriterLock.ExitWriteLock();
                    enteredLockType = LockTypes.None;
                    return;
                case LockTypes.Upgradeable:
                    readerWriterLock.ExitUpgradeableReadLock();
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

    internal static ReaderWriterLockMgr GetWriteLock() => new(Instance);
}