using System;

namespace GameBoyEmu
{
    public class Memory
    {
        public const byte DefaultMemoryByteValue = 0xDD; // an invalid opcode

        // TODO: ensure mapped memory is large enough for boot ROM, which writes to VRAM at 0x9fff, and writes to 0xff26 to "setup audio".
        // TODO: Later on map some high memory accesses directly to VRAM instead of system memory.
        private readonly byte[] memory = new byte[0x10000];

        public byte this[int address]
        {
            get => memory[address];
            set => memory[address] = value;
        }

        public void InjectRom(byte[] bootRomData, byte[] cartRomData)
        {
            for (int i = Math.Max(bootRomData.Length, cartRomData.Length); i < memory.Length; i++)
                memory[i] = DefaultMemoryByteValue;

            Array.Copy(cartRomData, memory, cartRomData.Length);
            Array.Copy(bootRomData, memory, bootRomData.Length);
        }
    }
}