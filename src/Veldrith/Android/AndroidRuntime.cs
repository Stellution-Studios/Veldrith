using System;
using System.Runtime.InteropServices;

namespace Veldrith.Android;

/// <summary>
/// Represents the AndroidRuntime type used by the graphics runtime.
/// </summary>
internal static class AndroidRuntime {

    /// <summary>
    /// Stores the lib name state used by this instance.
    /// </summary>
    private const string _lib_name = "android";

    /// <summary>
    /// Executes the anative window from surface logic for this backend.
    /// </summary>
    /// <param name="jniEnv">The jni env value used by this operation.</param>
    /// <param name="surface">The surface value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_lib_name)]
    public static extern IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr surface);
    
    /// <summary>
    /// Executes the anative window set buffers geometry logic for this backend.
    /// </summary>
    /// <param name="aNativeWindow">The a native window value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_lib_name)]
    public static extern int ANativeWindow_setBuffersGeometry(IntPtr aNativeWindow, int width, int height, int format);
    
    /// <summary>
    /// Executes the anative window release logic for this backend.
    /// </summary>
    /// <param name="aNativeWindow">The a native window value used by this operation.</param>
    [DllImport(_lib_name)]
    public static extern void ANativeWindow_release(IntPtr aNativeWindow);
}
