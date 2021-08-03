using System;
using System.Diagnostics;

namespace GameBoyEmu
{
    public struct RegisterSet
    {
        public ushort PC;
        public ushort SP;

        public byte B;
        public byte C;

        public ushort BC
        {
            get => (ushort) ((B << 8) + C);
            set
            {
                B = (byte) (value >> 8);
                C = (byte) (value & 0xff);
            }
        }
    }

    public class Emulator
    {
        private const byte InvalidOpCode1 = 0xdd;

        private static readonly byte[] InstructionSize =
        {
            // NOP | LD BC,d16 | LD (BC),A | ...
            1,3,1,1,1,1,2,1,3,1,1,1,1,1,2,1, // 0x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 1x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 2x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 3x
        };

        private readonly byte[] memory = new byte[512];

        // registers
        public RegisterSet Registers => reg;

        private RegisterSet reg;

        public Emulator()
        {
            memory[0] = 0x00;
            memory[1] = 0x01;
            memory[2] = 0xab;
            memory[3] = 0xcd;

            reg.PC = 0;

            InjectRom(new byte[]{ });
        }

        public void InjectRom(byte[] romData)
        {
            //Array.Clear(memory, 0, memory.Length);

            for(int i = romData.Length; i < memory.Length;i++)
                memory[i] = InvalidOpCode1;

            Array.Copy(romData, memory, romData.Length);
        }

        public void Run(int numInstructions = -1)
        {
            while (numInstructions-- != 0)
            {
                byte opCode = memory[reg.PC];
                byte nextByte = memory[reg.PC + 1];
                byte nextNextByte = memory[reg.PC + 2];
                ushort nextShort = (ushort) ((nextByte << 8) + nextNextByte);

                Console.WriteLine("Opcode: 0x{0:x}", opCode);
                //Debug.WriteLine("Foobar: {0}", opCode);
                //Trace.TraceInformation("Foobar: {0}", opCode);

                switch (opCode)
                {
                    // NOP
                    case 0x00:
                        break;

                    // LD BC,d16
                    case 0x01:
                        reg.BC = nextShort;
                        break;

                    // LD SP,d16
                    case 0x31:
                        reg.SP = nextShort;
                        break;

                    case InvalidOpCode1:
                        throw new ArgumentException("Invalid opcode: 0xdd");

                    default:
                        throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null);
                }

                reg.PC += InstructionSize[opCode];
            }
        }
    }
}