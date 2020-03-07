using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Ncp.Timer.HashedTimingWheel
{
    public class Timer 
    {
        public long Id { set; get; }
        public Action<object> Callback { get; set; }

        internal TimerType TimerType = TimerType.Normal;
        internal Timer Prev;
        internal Timer Next;
        internal int StartTick { set; get; }
        internal int Interval { set; get; }
        internal int TriggerCount { set; get; }
        internal object UserData { get; set; }

        internal long NextTriggerTick { set; get; }

        internal int Index { get; set; }

        private Wheel hostedWheel;

        internal void Init()
        {
            clear();
        }

        internal void Release()
        {
            clear();
        }
        internal void RemoveFromWheel()
        {
            hostedWheel.RemoveTimer(this);
        }
        private void clear()
        {
            Prev = null;
            Next = null;
            Id = 0;
            StartTick = 0;
            Interval = 0;
            TriggerCount = 0;
            UserData = null;
            Callback = null;
            NextTriggerTick = 0;
            hostedWheel = null;
            Index = 0;
        }
        internal void SetWheel(Wheel wheel)
        {
            this.hostedWheel = wheel;
        }
        internal TimerStruct GetTimerStruct()
        {
            var timerLite = new TimerStruct();

            timerLite.TimerType = this.TimerType;
            timerLite.Id = this.Id;
            timerLite.Callback = this.Callback;
            timerLite.Interval = this.Interval;
            timerLite.TriggerCount = this.TriggerCount;
            timerLite.StartTick = this.Interval;
            

            timerLite.UserData = this.UserData;
            
            return timerLite;
        }
    }
}
