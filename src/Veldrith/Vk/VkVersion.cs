namespace Veldrith.Vk
{
    internal struct VkVersion
    {
        private readonly uint _value;

        public VkVersion(uint major, uint minor, uint patch)
        {
            this._value = (major << 22) | (minor << 12) | patch;
        }

        public uint Major => this._value >> 22;

        public uint Minor => (this._value >> 12) & 0x3ff;

        public uint Patch => (this._value >> 22) & 0xfff;

        public static implicit operator uint(VkVersion version)
        {
            return version._value;
        }
    }
}
