// Re-export types from Encina.DistributedLock for backward compatibility
// New code should use Encina.DistributedLock directly

global using IDistributedLockProvider = Encina.DistributedLock.IDistributedLockProvider;
global using LockAcquisitionException = Encina.DistributedLock.LockAcquisitionException;
global using ILockHandle = Encina.DistributedLock.ILockHandle;
global using DistributedLockOptions = Encina.DistributedLock.DistributedLockOptions;
