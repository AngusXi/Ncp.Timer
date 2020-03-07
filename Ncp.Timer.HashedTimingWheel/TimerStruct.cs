using System;
using System.Collections.Generic;
using System.Text;

namespace Ncp.Timer.HashedTimingWheel
{
    class TimerStruct
    {
        public TimerType TimerType { get; set; }
        public long Id { get; set; }
        public int StartTick { set; get; }
        public int Interval { set; get; }
        public int TriggerCount { set; get; }
        public object UserData { get; set; }
        public Action<object> Callback { get; set; }
    }
}
