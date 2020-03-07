using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Ncp.Timer.HashedTimingWheel
{
    class TimerPooledObjectPolicy : IPooledObjectPolicy<Timer>
    {
        public Timer Create()
        {
            return new Timer();
        }

        public bool Return(Timer obj)
        {
            obj.Release();
            return true;
        }
    }
}
