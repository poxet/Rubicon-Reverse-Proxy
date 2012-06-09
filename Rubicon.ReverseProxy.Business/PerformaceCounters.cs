using System;
using System.Diagnostics;

namespace Rubicon.ReverseProxy.Business
{
    class PerformaceCounters
    {
        private static readonly object _syncRoot = new object();

        private bool _counterEnabled;
        private const string CounterCategoryName = "Rubicon Reverse Proxy";
        private const string TelnetClientCount = "Telnet Client Count";
        private const string RedirectRuleCount = "Redirect Rule Count";
        private const string ListenerCount = "Listener Count";
        private const string ConnectionCount = "Connection Count";
        private const string ConnectionSpeedName = "Connections per second";
        private const string RxSpeedName = "Received per second";
        private const string TxSpeedName = "Sent per second";

        private static PerformaceCounters _instance;
        private PerformanceCounter _telnetClientCount;
        private PerformanceCounter _redirectRuleCounter;
        private PerformanceCounter _listenerCounter;
        private PerformanceCounter _connectionCounter;
        private PerformanceCounter _connectionSpeed;
        private PerformanceCounter _rxSpeed;
        private PerformanceCounter _txSpeed;

        public static PerformaceCounters Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new PerformaceCounters();
                        }
                    }
                }
                return _instance;
            }
        }

        private PerformanceCounter TelnetClientCounter { get { return this._telnetClientCount ?? (this._telnetClientCount = new PerformanceCounter(CounterCategoryName, TelnetClientCount, false)); } }
        private PerformanceCounter RedirectRuleCounter { get { return this._redirectRuleCounter ?? (this._redirectRuleCounter = new PerformanceCounter(CounterCategoryName, RedirectRuleCount, false)); } }
        private PerformanceCounter ListenerCounter { get { return this._listenerCounter ?? (this._listenerCounter = new PerformanceCounter(CounterCategoryName, ListenerCount, false)); } }
        private PerformanceCounter ConnectionCounter { get { return this._connectionCounter ?? (this._connectionCounter = new PerformanceCounter(CounterCategoryName, ConnectionCount, false)); } }
        private PerformanceCounter ConnectionSpeed { get { return this._connectionSpeed ?? (this._connectionSpeed = new PerformanceCounter(CounterCategoryName, ConnectionSpeedName, false)); } }
        private PerformanceCounter RxSpeed { get { return this._rxSpeed ?? (this._rxSpeed = new PerformanceCounter(CounterCategoryName, RxSpeedName, false)); } }
        private PerformanceCounter TxSpeed { get { return this._txSpeed ?? (this._txSpeed = new PerformanceCounter(CounterCategoryName, TxSpeedName, false)); } }

        public void Initiate()
        {
            try
            {
                _counterEnabled = true;

                Create();
                Reset();
            }
            catch (Exception)
            {
                _counterEnabled = false;
                throw;
            }
        }

        private void Create()
        {
            try
            {
                if (PerformanceCounterCategory.Exists(CounterCategoryName))
                {
                    return;

                    //Check so that correct counters exists, no more, no less. If not. Delete the counter and recreate it.
                    //TOOD: Implement check


                    //TOOD: Do not delete if the counter exists, then just return from this function.
                    //PerformanceCounterCategory.Delete(CounterCategoryName);
                }

                var counterDatas = new CounterCreationDataCollection
                    {
                        new CounterCreationData
                            {
                                CounterName = TelnetClientCount, 
                                CounterHelp = "Number of telnet clients connected to the server.", 
                                CounterType = PerformanceCounterType.NumberOfItems32
                            },

                        new CounterCreationData
                            {
                                CounterName = RedirectRuleCount, 
                                CounterHelp = "Number of redirect rules.", 
                                CounterType = PerformanceCounterType.NumberOfItems32
                            },                            

                        new CounterCreationData
                            {
                                CounterName = ListenerCount, 
                                CounterHelp = "Number of listeners (ports).", 
                                CounterType = PerformanceCounterType.NumberOfItems32
                            },

                        new CounterCreationData
                            {
                                CounterName = ConnectionCount, 
                                CounterHelp = "Number of connections.", 
                                CounterType = PerformanceCounterType.NumberOfItems32
                            },

                        new CounterCreationData
                            {
                                CounterName = ConnectionSpeedName, 
                                CounterHelp = "Number of connections per second.",
                                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                            },

                        new CounterCreationData
                            {
                                CounterName = RxSpeedName, 
                                CounterHelp = "Data received from the caller (and forwarded to target) each second.",
                                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                            },

                        new CounterCreationData
                            {
                                CounterName = TxSpeedName, 
                                CounterHelp = "Data sent back to the caller each second.",
                                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                            },

                    };

                PerformanceCounterCategory.Create(CounterCategoryName, "Performance counters for Rubicon Reverse Proxy", PerformanceCounterCategoryType.SingleInstance, counterDatas);
            }
            catch (Exception exp)
            {
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
            }
        }

        public void Reset()
        {
            if (!_counterEnabled)
                return;

            TelnetClientCounter.RawValue = 0;
            RedirectRuleCounter.RawValue = 0;
            ListenerCounter.RawValue = 0;
            ConnectionCounter.RawValue = 0;
            ConnectionSpeed.RawValue = 0;
            RxSpeed.RawValue = 0;
            TxSpeed.RawValue = 0;
        }

        public void TelnetClientCounterIncrement()
        {
            if (_counterEnabled) TelnetClientCounter.Increment();
        }

        public void TelnetClientCounterDecrement()
        {
            if (_counterEnabled) TelnetClientCounter.Decrement();
        }

        public void RedirectRuleCounterIncrement()
        {
            if (_counterEnabled) RedirectRuleCounter.Increment();
        }

        public void RedirectRuleCounterDecrement()
        {
            if (_counterEnabled) RedirectRuleCounter.Decrement();
        }

        public void ListenerCounterIncrement()
        {
            if (_counterEnabled) ListenerCounter.Increment();
        }

        public void ListenerCounterDecrement()
        {
            if (_counterEnabled) ListenerCounter.Decrement();
        }

        public void ConnectionCounterIncrement()
        {
            if (_counterEnabled)
            {
                ConnectionCounter.Increment();
                ConnectionSpeed.Increment();
            }
        }

        public void ConnectionCounterDecrement()
        {
            if (_counterEnabled) ConnectionCounter.Decrement();
        }

        public void Rx(int bytesWritten)
        {
            if (_counterEnabled)
                RxSpeed.IncrementBy(bytesWritten);
        }

        public void Tx(int bytesRead)
        {
            if (_counterEnabled) 
                TxSpeed.IncrementBy(bytesRead);
        }
    }
}