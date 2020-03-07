using System;
using System.Collections.Generic;
using System.Text;

namespace Ncp.Timer.HashedTimingWheel
{
    class Wheel
    {
        public int Granularity { get; private set; }
        public int GranularityBit { get; private set; }
        public TimerList[] ArraySolt { get; private set; }
        /// <summary>
        /// current tick's slot index
        /// in AddTimer()， this is the index of another slot which will be run in next tick
        /// </summary>
        public int SoltIndex { get; private set; }
        public int Count { get; set; }

        public Wheel(int granularity)
        {
            GranularityBit = (int)Math.Log(granularity, 2);
            Granularity = granularity;
            ArraySolt = new TimerList[Granularity];
            for (int i = 0; i < Granularity; i++)
            {
                ArraySolt[i] = new TimerList();
                ArraySolt[i].Index = i;
            }
        }

        public bool UpdateTick()
        {
            ++SoltIndex;
            if (SoltIndex < Granularity)
                return false;
            SoltIndex = 0;
            return true;
        }

        public void AddTimer(int soltIdx, Timer timer)
        {
            timer.SetWheel(this);
            ArraySolt[soltIdx].PushBack(timer);
            Count += 1;
        }

        public void RemoveTimer(Timer timer)
        {
            ArraySolt[timer.Index].PopFront(timer);
            timer.SetWheel(null);
            Count -= 1;
        }

        public override string ToString()
        {
            return string.Format("Count = {0}", Count);
        }
        public void Reset()
        {
            SoltIndex = 0;
            Count = 0;
        }
    }
}
