using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings
{
    public struct NSError
    {
        public readonly IntPtr NativePtr;
        public string domain => string_objc_msgSend(NativePtr, sel_domain);
        public string localizedDescription => string_objc_msgSend(NativePtr, sel_localizedDescription);

        private static readonly Selector sel_domain = "domain";
        private static readonly Selector sel_localizedDescription = "localizedDescription";
    }
}
