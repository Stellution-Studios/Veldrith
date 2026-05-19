namespace Veldrith.Vk
{
    internal struct VkVersion
    {
        private readonly uint _value;

        public VkVersion(uint major, uint minor, uint patch)
        {
            _value = (major << 22) | (minor << 12) | patch;
        }

        public uint Major => _value >> 22;

        public uint Minor => (_value >> 12) & 0x3ff;

        public uint Patch => (_value >> 22) & 0xfff;

        public static implicit operator uint(VkVersion version)
        {
            return version._value;
        }
    }
}
