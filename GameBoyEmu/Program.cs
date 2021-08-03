using System;

namespace GameBoyEmu
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Attempting emulation of GameBoy!");

            var emulator = new Emulator();
            emulator.Run(2);

            Console.WriteLine("Result in BC is: {0}", emulator.Registers.BC);
        }
    }
}