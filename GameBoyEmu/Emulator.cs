using System;

namespace GameBoyEmu
{
    public struct RegisterSet
    {
        public ushort PC;
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
        private static readonly byte[] InstructionSize =
        {
            // NOP | LD BC,d16 | LD (BC),A
            1, 3, 1
        };

        private readonly byte[] memory = new byte[100];

        // registers
        public RegisterSet Registers => reg;

        private RegisterSet reg;

        public Emulator()
        {
            memory[0] = 0x00;
            memory[1] = 0x01;
            memory[2] = 0xAB;
            memory[3] = 0xCD;

            reg.PC = 0;
        }

        public void Run(int numInstructions = -1)
        {
            while (numInstructions-- != 0)
            {
                byte opCode = memory[reg.PC];
                byte nextByte = memory[reg.PC + 1];
                byte nextNextByte = memory[reg.PC + 2];
                ushort nextShort = (ushort) ((nextByte << 8) + nextNextByte);
                switch (opCode)
                {
                    // NOP
                    case 0x00:
                        break;

                    // LD BC,d16
                    case 0x01:
                        reg.BC = nextShort;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                reg.PC += InstructionSize[opCode];
            }
        }
    }
}