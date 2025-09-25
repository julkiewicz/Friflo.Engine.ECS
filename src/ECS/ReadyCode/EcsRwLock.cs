using System;
using System.Threading;

namespace Friflo.Engine.ECS.ReadyCode;

internal static class EcsRwLock
{
    internal sealed class ReaderWriterLockMgr : IDisposable
    {
        private readonly ReaderWriterLockSlim readerWriterLock = null;

        private enum LockTypes
        {
            None,
            Read,
            Write,
            Upgradeable
        }

        private LockTypes enteredLockType = LockTypes.None;

        public ReaderWriterLockMgr(ReaderWriterLockSlim readerWriterLock)
        {
            this.readerWriterLock = readerWriterLock;
        }

        public void EnterReadLock()
        {
            readerWriterLock.EnterReadLock();
            enteredLockType = LockTypes.Read;
        }

        public void EnterWriteLock()
        {
            readerWriterLock.EnterWriteLock();
            enteredLockType = LockTypes.Write;
        }

        public void EnterUpgradeableReadLock()
        {
            readerWriterLock.EnterUpgradeableReadLock();
            enteredLockType = LockTypes.Upgradeable;
        }

        public bool ExitLock()
        {
            switch (enteredLockType)
            {
                case LockTypes.Read:
                    readerWriterLock.ExitReadLock();
                    enteredLockType = LockTypes.None;
                    return true;
                case LockTypes.Write:
                    readerWriterLock.ExitWriteLock();
                    enteredLockType = LockTypes.None;
                    return true;
                case LockTypes.Upgradeable:
                    readerWriterLock.ExitUpgradeableReadLock();
                    enteredLockType = LockTypes.None;
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
            ExitLock();
        }

        ~ReaderWriterLockMgr()
        {
            ExitLock();
        }
    }

    internal static readonly ReaderWriterLockSlim Instance = new(LockRecursionPolicy.NoRecursion);

    public static ReaderWriterLockMgr GetWriteLock() => new(Instance);
}