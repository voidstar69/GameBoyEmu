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

        // TODO: a hack, not to be called from emulated opcodes
        public void UpdateLcdYCoordinate(byte value)
        {
            memory[0xff44] = value;
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

        private void WriteLcdPort(byte port, byte value)
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