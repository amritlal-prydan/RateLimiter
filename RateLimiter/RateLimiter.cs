using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;

namespace RateLimiter
{
    [Serializable()]
    public class RateLimiter
    {
          public RateLimiter(Int32 requests, TimeSpan interval)
            {
                this.Requests = requests;
                this.Interval = interval;
            }

            public Int32 Requests { get; set; }
            public TimeSpan Interval { get; set; }

            private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            private Dictionary<String, Queue<DateTime>> _internalStorage = new Dictionary<String, Queue<DateTime>>();

            public Boolean Process(String address)
            {
                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (!_internalStorage.ContainsKey(address))
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            _internalStorage.Add(address, new Queue<DateTime>());
                            _internalStorage[address].Enqueue(DateTime.Now);
                            return true;
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    }
                    else
                    {
                        PurgeOldEntries(address);

                        _lock.EnterWriteLock();
                        try
                        {
                            _internalStorage[address].Enqueue(DateTime.Now);
                            return _internalStorage[address].Count <= Requests;
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
            }
       
        private void PurgeOldEntries(String address)
        {
            _lock.EnterWriteLock();
            try
            {
                while (_internalStorage[address].Count > 0 && (DateTime.Now - _internalStorage[address].Peek() > Interval))
                {
                    _internalStorage[address].Dequeue();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
