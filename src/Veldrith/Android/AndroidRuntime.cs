using System;
using System.Runtime.InteropServices;

namespace Veldrith.Android
{
    /// <summary>
    ///     Function imports from the Android runtime library (android.so).
    /// </summary>
    internal static class AndroidRuntime
    {
        private const string lib_name = "android.so";

        [DllImport(lib_name)]
        public static extern IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr surface);

        [DllImport(lib_name)]
        public static extern int ANativeWindow_setBuffersGeometry(IntPtr aNativeWindow, int width, int height, int format);

        [DllImport(lib_name)]
        public static extern void ANativeWindow_release(IntPtr aNativeWindow);
    }
}
