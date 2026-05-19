using System.Text;

namespace Veldrith.MetalBindings;

public static class MTLUtil {

    /// <summary>
    /// Executes GetUtf8String.
    /// </summary>
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