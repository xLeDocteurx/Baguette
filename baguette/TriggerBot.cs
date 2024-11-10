using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace baguette
{
    public static class TriggerBot
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        public static bool shootLock = false;
        public static int lastTargetIndex = -1;

        public static void shoot(int entityIndex, int reflexTime, int pressedDuration, int delayBetweenClicks)
        {
            if (!shootLock)
            {
                shootLock = true;
                if (entityIndex != lastTargetIndex)
                {
                    Thread.Sleep(reflexTime);
                }
                lastTargetIndex = entityIndex;

                mouse_event(0x0002, 0, 0, 0, 0); // Left down
                Thread.Sleep(pressedDuration);
                mouse_event(0x0004, 0, 0, 0, 0); // Left up
                Thread.Sleep(delayBetweenClicks); // Optional: add a slight delay
                
                shootLock = false;
            }
        }
        public static void shoot(int entityIndex, int reflexTime, int minPressedDuration, int maxPressedDuration, int minDelayBetweenClicks, int maxDelayBetweenClicks)
        {
            if (!shootLock)
            {
                shootLock = true;
                if (entityIndex != lastTargetIndex)
                {
                    Thread.Sleep(reflexTime);
                }
                lastTargetIndex = entityIndex;

                Random random = new Random();
                mouse_event(0x0002, 0, 0, 0, 0); // Left down
                Thread.Sleep(random.Next(minPressedDuration, maxPressedDuration + 1));
                mouse_event(0x0004, 0, 0, 0, 0); // Left up
                Thread.Sleep(random.Next(minDelayBetweenClicks, maxDelayBetweenClicks + 1)); // Optional: add a slight delay
                
                shootLock = false;
            }
        }
    }
}
