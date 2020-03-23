using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Ncp.Timer.HashedTimingWheel
{
    public partial class TimerService
    {
        #region private property 
        private const int PerformanceWarnTime = 100000;
        /// <summary>
        /// initial timer count
        /// </summary>
        private const int PooledMaxTimerLimit = 100000;
        private const int TickInterval = 10;
        /// <summary>
        /// wheel's count ,different wheel represent different layer
        /// </summary>
        private const int WheelLayerCount = 5;

        private readonly DateTime utcBegin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>
        /// app's start time
        /// </summary>
        private readonly DateTime appStartTime = DateTime.UtcNow;
        private readonly long baseUtcOffset = System.Convert.ToInt64(TimeZoneInfo.Local.BaseUtcOffset.TotalMilliseconds);
        /// <summary>
        /// 程序启动的UTC时间戳
        /// </summary>
        private long utcBeginMillionseconds;
        private long maxTimerId;
        private Wheel[] wheels;
        private ObjectPool<Timer> timerPool;
        private Dictionary<long, Timer> runningTimers;
        private long runningMillionSeconds;
        private Dictionary<long, TimerStruct> loopTimerDic;
        #endregion
      

        public TimerService(int maxTimerCount = PooledMaxTimerLimit)
        {
            utcBeginMillionseconds = TickToMillisecond((appStartTime - utcBegin).Ticks);
            maxTimerId = 1;
            wheels = new Wheel[WheelLayerCount];
            timerPool = new DefaultObjectPool<Timer>(new TimerPooledObjectPolicy(), PooledMaxTimerLimit);
            
            runningTimers = new Dictionary<long, Timer>();
            runningMillionSeconds = 0;
            loopTimerDic = new Dictionary<long, TimerStruct>();
            //the first wheel has 256 slot; the other four has 64 slot：
            //256 *0.01 + 2.56*64 + (2.56*64*64) + (2.56*64*64*64) + (2.56*64*64*64*64) =
            //( 2.56 + 163.84 + 10485.76+671088.64+42949672.96 )/24 * 60 * 60 = 504 day
            for (var i = 0; i < wheels.Length; ++i)
                wheels[i] = new Wheel(i == 0 ? 256 : 64);
        }
        public static long TickToMillisecond(long tick)
        {
            return tick / TimeSpan.TicksPerMillisecond;
        }
        public long GetRunningMillisecond()
        {
            return runningMillionSeconds;
        }


        /// <summary>
        /// get the UtcMillisecond from game time
        /// </summary>
        /// <returns></returns>
        public long UtcMillisecond()
        {
            return utcBeginMillionseconds + runningMillionSeconds;
        }

        public long GetUtcBeginMillsecond()
        {
            return utcBeginMillionseconds;
        }
        public long UtcMillisecond(DateTime dateTime, DateTimeKind dateTimeKind = DateTimeKind.Utc)
        {
            return (long)(dateTime - utcBegin).TotalMilliseconds;
        }

        public long UtcSecond()
        {
            return UtcMillisecond()/1000;
        }

        public long UtcSecond(DateTime dateTime)
        {
            return (long)(dateTime - utcBegin).TotalSeconds;
        }

        public DateTime UtcDateTime(long millisecond)
        {
            return new DateTime(utcBegin.Ticks + millisecond * 10000);
        }

        public DateTime UtcNow()
        {
            return UtcDateTime(UtcMillisecond());
        }


        /// <summary>
        /// Warning:if the TimerType is inifinte, then the call will be null
        /// </summary>
        /// <param name="id">timer id</param>
        /// <returns></returns>
        public Timer GetTimer(int id)
        {
            Timer timer;
            if (runningTimers.TryGetValue(id, out timer))
                return timer;
            return null;
        }
        public long AddTimer(int startMillisecond, int interval, int triggerCount, object userData, Action<object> callback, long timerId = 0, TimerType timerType = TimerType.Normal)
        {
            if (startMillisecond < 0 || interval < 0 || triggerCount < 0 || callback == null)
                return -1;

            var timer = timerPool.Get();
            if (timer == null)
            {
                return -2;
            }

            timer.Init();

            timer.TimerType = timerType;
            if (triggerCount == int.MaxValue)
            {
                timer.TimerType = TimerType.Infinite;
            }
            timer.Prev = null;
            timer.Next = null;
            timer.StartTick = startMillisecond;
            timer.Interval = interval;
            timer.TriggerCount = triggerCount;
            timer.UserData = userData;
            timer.Callback = callback;
            timer.NextTriggerTick = runningMillionSeconds + startMillisecond;
            timer.Id = timerId == 0 ? GenerateTimerId() : timerId;


            PushWheel(timer);

            runningTimers[timer.Id] = timer;


            return timer.Id;
        }

        public int RemoveTimer(int id)
        {
            loopTimerDic.Remove(id);
            var timer = GetTimer(id);
            if (timer == null)
            {
                return -2;
            }
            return RemoveTimer(timer);
        }



        /// <summary>
        /// timer update
        /// </summary>
        /// <param name="millisecond"> app's running time</param>
        public void UpdateTick(long millisecond)
        {

            if (runningMillionSeconds == 0)
            {
                runningMillionSeconds = millisecond;
                return;
            }

            int wheelSoltIdx = 0;
            bool tick = false;
            int loopCount = 0;

            for (; runningMillionSeconds < millisecond; runningMillionSeconds += TickInterval)
            {
                loopCount += 1;

                var wheel = wheels[0];
                wheelSoltIdx = wheel.SoltIndex;
                tick = wheel.UpdateTick();
                Timer curTimer = null;
                var solt = wheel.ArraySolt[wheelSoltIdx];
                int maxCount = solt.Count;
                int curCount = 0;
                long timerId = -1;
                //CountRunTimers += maxCount;
                while ((curTimer = solt.Peek()) != null && curCount++ < maxCount)
                {
                    timerId = curTimer.Id;
                    try
                    {
                        var performBegin = DateTime.Now.Ticks;
                        curTimer.Callback(curTimer.UserData);
                        var performSpan = DateTime.Now.Ticks - performBegin;
                        if (performSpan > TimerService.PerformanceWarnTime && curTimer.Callback != null)
                        {
                            
                        }
                    }
                    catch (Exception ex)
                    {
                    }

                    if (HasTimer(timerId) == true)
                    {
                        if (--curTimer.TriggerCount > 0)
                        {
                            if (curTimer.TimerType == TimerType.Infinite)
                            {
                                var timerLite = curTimer.GetTimerStruct();
                                RemoveTimer(curTimer);
                                loopTimerDic.Add(timerLite.Id, timerLite);
                            }
                            else
                            {
                                curTimer.NextTriggerTick = runningMillionSeconds + curTimer.Interval;
                                PopWheel(curTimer);
                                PushWheel(curTimer);
                            }
                        }
                        else
                            RemoveTimer(curTimer);
                    }

                    curTimer = null;
                    timerId = -1;
                }

                if (tick == false)
                    continue;

                for (int i = 1; i < WheelLayerCount; i++)
                {
                    wheel = wheels[i];

                    wheelSoltIdx = wheel.SoltIndex;
                    tick = wheel.UpdateTick();
                    solt = wheel.ArraySolt[wheelSoltIdx];
                    maxCount = solt.Count;
                    curCount = 0;
                    while ((curTimer = solt.Peek()) != null && curCount++ < maxCount)
                    {
                        PopWheel(curTimer);
                        PushWheel(curTimer);
                    }

                    if (tick == false)
                        break;

                    curTimer = null;
                }
            }

            foreach (var timerLite in loopTimerDic.Values)
            {
                AddTimer(timerLite.StartTick, timerLite.Interval, timerLite.TriggerCount, timerLite.UserData, timerLite.Callback, timerLite.Id, timerLite.TimerType);
            }

            loopTimerDic.Clear();
        }

        #region private method
        private void PushWheel(Timer timer)
        {
            var offset = (int)(timer.NextTriggerTick - runningMillionSeconds) / TickInterval;
            if (offset <= 0)
                offset = 0;

            for (int i = 0; i < wheels.Length; i++)
            {
                var wheel = wheels[i];
                offset -= 1;
                offset = Math.Max(offset, 0);
                var tempOffset = (offset + wheel.SoltIndex) % wheel.Granularity;
                offset >>= wheel.GranularityBit;
                if (offset > 0)
                    continue;

                wheel.AddTimer(tempOffset, timer);
                break;
            }
        }
        private void PopWheel(Timer timer)
        {
            timer.RemoveFromWheel();
        }

        private long GenerateTimerId()
        {
            long id = maxTimerId;
            maxTimerId++;
            return id;
        }


        private bool HasTimer(long id)
        {
            return runningTimers.ContainsKey(id);
        }
        private int RemoveTimer(Timer timer)
        {
            if (timer == null)
            {
                return -1;
            }
            runningTimers.Remove(timer.Id);

            PopWheel(timer);

            timer.Release();
            timerPool.Return(timer);


            return 0;
        }
        #endregion
    }
}
