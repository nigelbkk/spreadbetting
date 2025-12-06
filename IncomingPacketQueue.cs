using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SpreadTrader
{
    public class IncomingPacketQueue : IDisposable
    {
        BlockingCollection<String> _packetQ = new BlockingCollection<String>();

        public IncomingPacketQueue(int workerCount)
        {
            for (int i = 0; i < workerCount; i++)
            {
                Task.Factory.StartNew(Consume);
            }
        }

        public void EnqueueSweep(String incoming)
        {
            _packetQ.Add(incoming);
        }

        private void Consume()
        {
            foreach (var sweep in _packetQ.GetConsumingEnumerable())
            {
                //do stuff
                Debug.WriteLine("Thread {0} picking up a pending notification with {0}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        public int QueueCount
        {
            get
            {
                return _packetQ.Count;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            _packetQ.CompleteAdding();
        }

        #endregion
    }
}
