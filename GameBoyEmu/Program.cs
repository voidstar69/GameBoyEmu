using System;
using System.IO;

namespace GameBoyEmu
{
    static class Program
    {
        static void Main()
        {
            Console.WriteLine("Attempting emulation of GameBoy!");
            Console.WriteLine();

            //var emulator = new Emulator();
            //emulator.InjectRom(new byte[] { (byte)OpCode.NOP, (byte)OpCode.LD_BC_d16, 0xcd, 0xab });
            //emulator.Run(2);
            //Console.WriteLine();
            //Console.WriteLine("Result in BC is 0x{0:x} and should be 0xabcd", emulator.Registers.BC);

            //var emulator = new Emulator();
            //emulator.InjectRom(new byte[] { (byte)OpCode.LD_A_HLmem });
            //emulator.Run(1);

            Emulator emulator = new Emulator();

            RunBootRom_UntilLogoCheck(emulator);
            //RunBootRomAndCartRom(emulator);

            Console.WriteLine();
            Console.WriteLine("Background Tile data (256 tiles, 16 bytes each):");
            byte[] tileData = emulator.Memory.GetBackgroundAndWindowTileData();
            for (int tile = 0; tile < 256; tile++)
            {
                Console.WriteLine("Tile {0}:", tile);
                for (int i = 0; i < 16; i++)
                {
                    Console.Write(tileData[tile * 16 + i]);
                    Console.Write(',');
                }
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("Tile map (32 x 32):");
            byte[,] tileMap = emulator.Memory.GetTileMap2D();
            Print2DArrayToConsole(tileMap);

            Console.WriteLine();
            Console.WriteLine("Tile #1 decoded colours (8 x 8):");
            byte[,] tileColours = emulator.Memory.DecodeTileDataToColours(tileData, tileId: 1);
            Print2DArrayToConsole(tileColours);

            Console.WriteLine();
            Console.WriteLine("Tile #25 decoded colours (8 x 8):");
            tileColours = emulator.Memory.DecodeTileDataToColours(tileData, tileId: 25);
            Print2DArrayToConsole(tileColours);
        }

        private static void Print2DArrayToConsole(byte[,] data)
        {
            for (int row = 0; row < data.GetLength(0); row++)
            {
                for (int col = 0; col < data.GetLength(1); col++)
                {
                    Console.Write(data[col, row]);
                    Console.Write(',');
                }
                Console.WriteLine();
            }
        }

        private static void RunBootRom_UntilLogoCheck(Emulator emulator)
        {
            var romData = File.ReadAllBytes("DMG_ROM.bin");
            emulator.InjectRom(romData);
            emulator.Run(47442); // stop just before opcode which causes infinite loop in Boot ROM because logo data in cart does not match DMG ROM
        }

        public static void RunBootRomAndCartRom(Emulator emulator)
        {
            var bootRomData = File.ReadAllBytes("DMG_ROM.bin");
            var cartRomData = File.ReadAllBytes("2048.gb.bin");
            emulator.InjectRom(bootRomData, cartRomData);
            emulator.Run(47443 + 510); // stop before the next invalid opcode
        }
    }
}