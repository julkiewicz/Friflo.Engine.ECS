using System;
using System.Threading;

namespace Friflo.Engine.ECS.ReadyCode;

internal sealed class ReaderWriterLockMgr(ReaderWriterLockSlim rwLock) : IDisposable
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
        rwLock.EnterReadLock();
        enteredLockType = LockTypes.Read;
    }

    internal void EnterWriteLock()
    {
        rwLock.EnterWriteLock();
        enteredLockType = LockTypes.Write;
    }

    internal void EnterUpgradeableReadLock()
    {
        rwLock.EnterUpgradeableReadLock();
        enteredLockType = LockTypes.Upgradeable;
    }

    private void ExitLock()
    {
        switch (enteredLockType)
        {
            case LockTypes.Read:
                rwLock.ExitReadLock();
                enteredLockType = LockTypes.None;
                return;
            case LockTypes.Write:
                rwLock.ExitWriteLock();
                enteredLockType = LockTypes.None;
                return;
            case LockTypes.Upgradeable:
                rwLock.ExitUpgradeableReadLock();
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