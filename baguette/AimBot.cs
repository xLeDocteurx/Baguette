using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace baguette
{
    public static class AimBot
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public static void followHead(Vector2 target)
        {
            GetCursorPos(out POINT currentPos);

            int deltaX = (int)(target.X - currentPos.X);
            int deltaY = (int)(target.Y - currentPos.Y);

            mouse_event(0x0001, deltaX, deltaY, 0, 0);
        }
    }
}
