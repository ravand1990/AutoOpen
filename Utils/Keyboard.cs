using System.Runtime.InteropServices;

namespace AutoOpen.Utils
{
    internal class Keyboard
    {
        public enum KeyboardEvents
        {
            KEY_DOWN = 0x0001,
            KEY_UP = 0x0002
        }


        public static bool hold = false;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public static void HoldKey(byte key)
        {
            keybd_event(key, 0, (int)KeyboardEvents.KEY_DOWN, 0);
        }

        public static void ReleaseKey(byte key)
        {
            keybd_event(key, 0, (int)KeyboardEvents.KEY_DOWN | (int)KeyboardEvents.KEY_UP, 0);
        }

        public static void PressKey(byte key)
        {
            HoldKey(key);
            ReleaseKey(key);
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int nVirtKey);
        const byte KEY_UP = 0x1;

        public static bool IsKeyPressed(int key)
        {
            return GetAsyncKeyState(key) == -32767;
        }
    }
}