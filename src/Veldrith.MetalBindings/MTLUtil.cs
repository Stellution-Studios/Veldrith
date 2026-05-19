using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Provides Objective-C interop bindings for MTLUtil.
/// </summary>
public static class MTLUtil {

    /// <summary>
    /// Gets the utf8 string value.
    /// </summary>
    /// <param name="stringStart">The string start value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static unsafe string GetUtf8String(byte* stringStart) {
        int characters = 0;
        while (stringStart[characters] != 0) {
            characters++;
        }

        return Encoding.UTF8.GetString(stringStart, characters);
    }

    public static T AllocInit<T>(string typeName) where T : struct {
        ObjCClass cls = new(typeName);
        return cls.AllocInit<T>();
    }
}