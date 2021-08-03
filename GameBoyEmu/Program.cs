using System;

namespace GameBoyEmu
{
    static class Program
    {
        static void Main()
        {
            Console.WriteLine("Attempting emulation of GameBoy!");
            Console.WriteLine();

            var emulator = new Emulator();
            emulator.InjectRom(new byte[] { (byte)OpCode.NOP, (byte)OpCode.LD_BC_d16, 0xcd, 0xab });
            emulator.Run(2);

            Console.WriteLine();
            Console.WriteLine("Result in BC is 0x{0:x} and should be 0xabcd", emulator.Registers.BC);
        }
    }
}