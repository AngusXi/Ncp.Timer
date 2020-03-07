using System;
using System.Collections.Generic;
using System.Text;

namespace Ncp.Timer.HashedTimingWheel
{
    class TimerList
    {
        public int Index { get; set; }
        public int Count { get; private set; }

        private Timer head;

        //could not use pushfront(), because we should keep the sequence
        public void PushBack(Timer timer)
        {
            if (head == null)
            {
                head = timer;
                timer.Prev = timer.Next = timer;
            }
            else
            {
                var node = head.Next;

                timer.Next = node;
                timer.Prev = head;

                node.Prev = timer;
                head.Next = timer;
                head = timer;
            }
            Count += 1;
            timer.Index = Index;
        }
        //get the element inserted firstly and delete from list
        public Timer PopFront(Timer timer = null)
        {
            if (timer == null)
            {
                if (head == null) return null;
                timer = head.Next;
            }


            if (timer == null || (timer.Prev == null && timer.Next == null) || timer.Index != Index)
                return null;

            Count -= 1;
            timer.Index = -1;

            if (head == timer)
            {
                if (head.Prev == head)
                {
                    head = null;
                    timer.Prev = null;
                    timer.Next = null;
                    return timer;
                }
                head = head.Next;
            }

            timer.Prev.Next = timer.Next;
            timer.Next.Prev = timer.Prev;

            timer.Prev = null;
            timer.Next = null;

            return timer;
        }

        //仅获取最先添加的timer
        public Timer Peek()
        {
            if (head == null) return null;
            var timer = head.Next;
            return timer;
        }
        public override string ToString()
        {
            return string.Format("Count = {0}", Count);
        }
    }
}
