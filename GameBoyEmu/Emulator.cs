using System;

namespace GameBoyEmu
{
    public enum OpCode : byte
    {
        NOP = 0x00,
        LD_BC_d16 = 0x01,
        LD_HL_d16 = 0x21,
        LD_SP_d16 = 0x31,
        LD_A_d8 = 0x3e,
        HALT = 0x76,
        LD_HLmem_A = 0x77,  // LD (HL),A
        LD_A_HLmem = 0x7e,  // LD A,(HL)
        XOR_A = 0xaf
    }

    public struct RegisterSet
    {
        public ushort PC;
        public ushort SP;

        // TODO: change these into 'file registers', an array of 8-bit registers?
        public byte A;
        public byte B;
        public byte C;
        public byte D;
        public byte E;
        public byte H;
        public byte L;

        public ushort BC
        {
            get => (ushort)((B << 8) + C);
            set
            {
                B = (byte)(value >> 8);
                C = (byte)(value & 0xff);
            }
        }

        public ushort DE
        {
            get => (ushort)((D << 8) + E);
            set
            {
                D = (byte)(value >> 8);
                E = (byte)(value & 0xff);
            }
        }

        public ushort HL
        {
            get => (ushort)((H << 8) + L);
            set
            {
                H = (byte)(value >> 8);
                L = (byte)(value & 0xff);
            }
        }

        public byte Get8BitRegister(int index)
        {
            return index switch
            {
                0 => B,
                1 => C,
                2 => D,
                3 => E,
                4 => H,
                5 => L,
                6 => throw new ArgumentOutOfRangeException(nameof(index), index, "(HL) is not an 8-bit register"),
                7 => A,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "index of 8-bit register"),
            };
        }

        public void Set8BitRegister(int index, byte value)
        {
            switch (index)
            {
                case 0: B = value; break;
                case 1: C = value; break;
                case 2: D = value; break;
                case 3: E = value; break;
                case 4: H = value; break;
                case 5: L = value; break;
                case 6: throw new ArgumentOutOfRangeException(nameof(index), index, "(HL) is not an 8-bit register");
                case 7: A = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "index of 8-bit register");
            };
        }

        // TODO: Get/Set 16-bit registers
    }

    public class Emulator
    {
        private const byte InvalidOpCode1 = 0xDD;

        private static readonly byte[] InstructionSize =
        {
            // NOP | LD BC,d16 | LD (BC),A | ...
            1,3,1,1,1,1,2,1,3,1,1,1,1,1,2,1, // 0x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 1x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 2x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 3x

            // Not bothered yet to add XOR A (opcode 0xAF) yet. Special cased this in the code
        };

        public byte[] Memory => memory;

        private readonly byte[] memory = new byte[512];

        // registers
        public RegisterSet Registers => reg;

        private RegisterSet reg;

        public Emulator()
        {
            InjectRom(new byte[] { });
        }

        public void InjectRom(byte[] romData)
        {
            //Array.Clear(memory, 0, memory.Length);

            for (int i = romData.Length; i < memory.Length; i++)
                memory[i] = InvalidOpCode1;

            Array.Copy(romData, memory, romData.Length);

            reg.PC = 0; // TODO: correct behaviour?
        }

        public void Run(int numInstructions = -1)
        {
            while (numInstructions-- != 0)
            {
                byte opCode = memory[reg.PC];
                byte literal8Bit = memory[reg.PC + 1];
                byte nextNextByte = memory[reg.PC + 2];
                ushort literal16Bit = (ushort)((nextNextByte << 8) + literal8Bit);

                Console.WriteLine("Opcode=0x{0:x}, Literal8bit=0x{1:x}, Literal16bit=0x{2:x}", opCode, literal8Bit, literal16Bit);

                bool isBottomHalfBlock = ((opCode >> 7) & 1) == 1; // 0 = top half of opcodes, including 8-bit load ops. 1 = bottom half of opcodes, including arithmetic ops
                bool isQ2orQ4Block = ((opCode >> 6) & 1) == 1; // 0 = Arithmetic ops or top quarter opcodes. 1 = 8-bit load ops or bottom quarter opcodes.

                if(!isBottomHalfBlock && !isQ2orQ4Block)
                {
                    // Instruction decoder for (some of) instructions in top quarter block (0x00 to 0x3f)

                    // TODO
                    //throw new NotImplementedException("Top quarter block operations");
                }
                else if(isBottomHalfBlock != isQ2orQ4Block)
                {
                    // Instruction decoder for middle block of Load and Arithmetic instructions (0x40 to 0xbf)
                    // isBottomHalfBlock: 0 = Load, 1 = Arithmetic
                    // isTopQ2orQ4Block: 0 = Arithmetic, 1 = Load. Must be opposite of topOrBottom

                    if (isQ2orQ4Block == isBottomHalfBlock)
                        throw new InvalidOperationException("Should never occur!");

                    int srcReg8Index = opCode & 0x07; // 0..7 == B,C,D,E,H,L,(HL),A

                    if (isBottomHalfBlock)
                    {
                        // Arithmetic operation (for now XOR A is handled below)
                        if (opCode != (byte)OpCode.XOR_A)
                            throw new NotImplementedException("Arithmetic operations");
                    }
                    else
                    //if(opCode >= 0x40 && opCode <= 0x7f)
                    {
                        // 8-bit load operation (from register or memory, but not from 8-bit literal)

                        int destReg8Index = (opCode >> 3) & 0x07; // 0..7 == B,C,D,E,H,L,(HL),A

                        // TODO: handle HALT special case. Need to "halt until interrupt occurs"
                        if (opCode == (byte)OpCode.HALT)
                            throw new NotImplementedException("HALT");

                        // 'register' index 6 is a special case: read/write memory location indexed by HL register
                        byte regOrMemVal = srcReg8Index == 6 ? memory[reg.HL] : reg.Get8BitRegister(srcReg8Index);

                        if (destReg8Index == 6)
                            memory[reg.HL] = regOrMemVal;
                        else
                            reg.Set8BitRegister(destReg8Index, regOrMemVal);

                        reg.PC += 1;
                        continue;
                    }
                }
                else
                {
                    // Instruction decoder for (some of) instructions in bottom quarter block (0xc0 to 0xff)
                    if (!isBottomHalfBlock || !isQ2orQ4Block)
                        throw new NotImplementedException("Should never happen!");

                    // TODO
                    throw new NotImplementedException("Bottom quarter block operations");
                }

                // General instructions, handled individually rather than decoded from opcode
                switch (opCode)
                {
                    // NOP
                    case (byte)OpCode.NOP:
                        break;

                    // LD BC,d16
                    case (byte)OpCode.LD_BC_d16:
                        reg.BC = literal16Bit;
                        break;

                    // LD SP,d16
                    case (byte)OpCode.LD_SP_d16:
                        reg.SP = literal16Bit;
                        break;

                    // LD HL,d16
                    case (byte)OpCode.LD_HL_d16:
                        reg.HL = literal16Bit;
                        break;

                    // LD A,d8
                    case (byte)OpCode.LD_A_d8:
                        reg.A = literal8Bit;
                        break;

                    // XOR A
                    case (byte)OpCode.XOR_A:
                        reg.A = (byte)(reg.A ^ reg.A);
                        reg.PC += 1;
                        continue;

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