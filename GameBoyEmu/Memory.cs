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
            get => ReadMemory(address);
            set => WriteMemory(address, value);
        }

        public void InjectRom(byte[] bootRomData, byte[] cartRomData)
        {
            for (int i = Math.Max(bootRomData.Length, cartRomData.Length); i < memory.Length; i++)
                memory[i] = DefaultMemoryByteValue;

            Array.Copy(cartRomData, memory, cartRomData.Length);
            Array.Copy(bootRomData, memory, bootRomData.Length);

            // TODO: a hack so that the boot ROM does not have to wait long before it gets to the desired scan-line
            UpdateLcdYCoordinate(0x90);
        }

        public byte[] GetMemoryClone()
        {
            return (byte[])memory.Clone();
        }

        public byte[,] GetTileMap2D()
        {
            const int tileMapSize = 32;
            byte[,] tileMap = new byte[tileMapSize, tileMapSize];
            int tileMapAddr = ReadLcdControlRegister().HasFlag(LcdControlFlags.BackgroundTileMapArea) ? 0x9c00 : 0x9800;
            for (int row = 0; row < tileMapSize; row++)
            {
                for (int col = 0; col < tileMapSize; col++)
                {
                    tileMap[col, row] = memory[tileMapAddr++];
                }
            }
            return tileMap;
        }

        public byte[] GetTileMap1D()
        {
            int tileMapAddr = ReadLcdControlRegister().HasFlag(LcdControlFlags.BackgroundTileMapArea) ? 0x9c00 : 0x9800;
            byte[] tileMap = new byte[32 * 32];
            Array.Copy(memory, tileMapAddr, tileMap, 0, tileMap.Length);
            return tileMap;
        }

        public byte[] GetObjTileData()
        {
            const int tileDataAddr = 0x8800;
            byte[] tileData = new byte[256 * 16];
            Array.Copy(memory, tileDataAddr, tileData, 0, tileData.Length);
            return tileData;
        }

        public byte[] GetBackgroundAndWindowTileData()
        {
            int tileDataBlock1Addr = ReadLcdControlRegister().HasFlag(LcdControlFlags.BackgroundAndWindowTileDataArea) ? 0x8000 : 0x9000;
            const int tileDataBlock2Addr = 0x8800;

            byte[] tileData = new byte[256 * 16];
            Array.Copy(memory, tileDataBlock1Addr, tileData, 0, 128 * 16);
            Array.Copy(memory, tileDataBlock2Addr, tileData, 128 * 16, 128 * 16);
            return tileData;

            //int tileDataBlock1Addr = ReadLcdControlRegister().HasFlag(LcdControlFlags.BackgroundAndWindowTileDataArea) ? 0x8000 : 0x9000;
            //byte[] tileDataBlock1 = new byte[128 * 16];
            //Array.Copy(memory, tileDataBlock1Addr, tileDataBlock1, 0, tileDataBlock1.Length);

            //int tileDataBlock2Addr = 0x8800;
            //byte[] tileDataBlock2 = new byte[128 * 16];
            //Array.Copy(memory, tileDataBlock2Addr, tileDataBlock2, 0, tileDataBlock2.Length);
            //return (tileDataBlock1, tileDataBlock2);
        }

        public byte[,] DecodeTileDataToColours(byte[] tileData, byte tileId)
        {
            const int tileSize = 8;
            byte[,] tileColours = new byte[tileSize, tileSize];
            int addr = tileId * 16;

            for (int row = 0; row < tileSize; row++)
            {
                byte lsbData = tileData[addr++];
                byte msbData = tileData[addr++];
                for (int col = tileSize - 1; col >= 0; col--)
                {
                    tileColours[col, row] = (byte)(((msbData & 1) << 1) | (lsbData & 1));
                    lsbData >>= 1;
                    msbData >>= 1;
                }
            }
            return tileColours;
        }

        // TODO: a hack, not to be called from emulated opcodes
        public void UpdateLcdYCoordinate(byte value)
        {
            memory[0xff44] = value;
        }

        [Flags]
        private enum LcdControlFlags : byte
        {
            LcdPpuEnable = 128, // 0 = Off, 1 = On
            WindowTileMapArea = 64, // 0=9800-9BFF, 1=9C00-9FFF
            WindowEnable = 32, // 0 = Off, 1 = On
            BackgroundAndWindowTileDataArea = 16, // 0=8800-97FF, 1=8000-8FFF
            BackgroundTileMapArea = 8, // 0=9800-9BFF, 1=9C00-9FFF
            ObjSize = 4, // 0=8x8, 1=8x16
            ObjEnable = 2, // 0 = Off, 1 = On
            BackgroundAndWindowEnablePriority = 1 // 0 = Off, 1 = On
        }

        private LcdControlFlags ReadLcdControlRegister()
        {
            return (LcdControlFlags)memory[0xff40];
        }

        private byte ReadMemory(int address)
        {
            if (0xff00 <= address && address <= 0xff7f)
            {
                // I/O registers
                byte port = (byte)address;

                if (0x40 <= port && port <= 0x4b)
                {
                    return ReadLcdPort(port);
                }
                else
                    switch (address)
                    {
                        case 0xff42: // vertical scroll register, lower value means logo scrolls upwards
                            break;
                        case 0xff44: // LCD
                            return 0x90; // value expected when waiting for screen frame

                        default:
                            throw new NotImplementedException("Read from memory-mapped IO port " + port);
                    }
            }

            return memory[address];
        }

        private void WriteMemory(int address, byte value)
        {
            if (0xff00 <= address && address <= 0xff7f)
            {
                // I/O registers
                byte port = (byte)address;

                if (0x10 <= port && port <= 0x26)
                {
                    // TODO: Sound
                }
                else if (0x40 <= port && port <= 0x4b)
                {
                    WriteLcdPort(port, value);
                }
                else
                    switch (port)
                    {
                        //case 0x00: // Controller
                        //    throw new NotImplementedException();

                        // Communication
                        //case 0x01:
                        //case 0x02:
                        //    throw new NotImplementedException();

                        case 0x0f: // TODO: Cart ROM seems to write to this undocumented IO port.
                            break;

                        case 0x50: // Set to non-zero to disable boot ROM / turn off DMG ROM
                            break;

                        default:
                            throw new NotImplementedException("Write to memory-mapped IO port " + port);
                    }
            }

            memory[address] = value;
        }

        private void WriteLcdPort(byte port, byte _ /* value */)
        {
            switch (port)
            {
                case 0x40: // LCD Control. Set to 0x91 to turn on LCD, showing Background
                    break;
                case 0x41: // LCD Status (STAT). Only bits 3-6 can be written.
                    break;
                case 0x42: // Scroll Y (SCY). Vertical scroll register, top coordinate of visible pixel area within the larger BG map.
                    break;
                case 0x43: // Scroll X (SCX). Horizontal scroll register, left coordinate of visible pixel area within the larger BG map.
                    break;
                case 0x44: // LCD Y Coordinate (LY), read-only
                    throw new InvalidOperationException("LCD Y Coordinate register is read-only");
                case 0x45: // LY Compare (LYC). When LY = LYC, set flag in STAT register and optionally raise a STAT interrupt
                    break;
                case 0x47: // BG Palette Data (BGP)
                    break;
                case 0x48: // Object Palette 0 Data (OBP0)
                    break;
                //case 0x49: // Object Palette 1 Data (OBP0)
                //    break;
                //case 0x4a: // Window Y Position (WY). Top coordinate of the Window.
                //    break;
                //case 0x4b: // Window X Position (WX). Left coordinate of the Window.
                //    break;

                default:
                    throw new NotImplementedException("Write to LCD IO port " + port);
            }
        }

        private byte ReadLcdPort(byte port)
        {
            switch (port)
            {
                case 0x40: // set to 0x91 to turn on LCD, showing Background
                    break;
                case 0x41: // LCD Status (STAT). All bits can be read.
                    break;
                case 0x42: // Scroll Y (SCY). Vertical scroll register, top coordinate of visible pixel area within the larger BG map.
                    break;
                case 0x43: // Scroll X (SCX). Horizontal scroll register, left coordinate of visible pixel area within the larger BG map.
                    break;
                case 0x44: // LCD Y Coordinate (LY), read-only. Current horizontal line being drawn. Can hold any value from 0 to 153. The values from 144 to 153 indicate the VBlank period.
                    break;
                // The boot ROM waits for value 0x90. The cart ROM waits for value 0x91. So this hack increments the value whenever this register is read.
                //byte oldVal = memory[0xff00 + port];
                //memory[0xff00 + port] = (byte)((oldVal + 1) % 154);
                //return oldVal;
                case 0x45: // LY Compare (LYC). When LY = LYC, set flag in STAT register and optionally raise a STAT interrupt
                    break;
                //case 0x47: // BG Palette Data (BGP)
                //    break;
                case 0x48: // Object Palette 0 Data (OBP0)
                    break;
                //case 0x49: // Object Palette 1 Data (OBP0)
                //    break;

                default:
                    throw new NotImplementedException("Read from LCD IO port " + port);
            }

            return memory[0xff00 + port];
        }
    }
}