using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLUtil class.
/// </summary>
public static class MTLUtil {

    /// <summary>
    /// Performs the GetUtf8String operation.
    /// </summary>
    /// <param name="stringStart">The value of stringStart.</param>
    /// <returns>The result of the GetUtf8String operation.</returns>
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