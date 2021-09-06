using System;
using System.Diagnostics;

namespace GameBoyEmu
{
    // most of the 256 possible byte values are to valid instructions (operation codes)
    public enum OpCode : byte
    {
        NOP = 0x00,
        LD_BC_d16 = 0x01,
        INC_C = 0x0c,
        LD_C_d8 = 0x0e,
        LD_DE_d16 = 0x11,
        RLA = 0x17,         // rotate A left through carry
        LD_HL_d16 = 0x21,
        LD_SP_d16 = 0x31,
        INC_HLmem = 0x34,   // INC (HL)
        LD_A_d8 = 0x3e,
        HALT = 0x76,
        LD_HLmem_A = 0x77,  // LD (HL),A
        LD_A_HLmem = 0x7e,  // LD A,(HL)
        XOR_A = 0xaf,
        JP_a16 = 0xc3,
        PUSH_BC = 0xc5,
        RET = 0xc9,         // return from a CALL
        RETI = 0xd9,        // return from a CALL and enable interrupts
        PREFIX_CB = 0xcb,   // prefix for expanded set of another 256 bit-wise instructions
        CALL_a16 = 0xcd,
        LDH_a8_A = 0xe0,    // LDH (a8),A  aka  LD ($FF00+a8),A
        LD_Cmem_A = 0xe2,   // LD (C),A    aka  LD ($FF00+C),A
        CP_d8 = 0xfe,       // compare (A - d8), set flags without storing result
        LD_a16_A = 0xea,    // LD (a16),A
        LDH_A_a8 = 0xf0,    // LDH A,(a8)
    }

    [Flags]
    public enum Flag : byte
    {
        None = 0,   // no flags set
        Z = 128,    // zero flag
        N = 64,     // subtract flag
        H = 32,     // half carry flag
        C = 16      // carry flag
    };

    public struct RegisterSet
    {
        // TODO: change these into 'file registers', an array of 8-bit registers?
        public byte A; // accumulator register
        public byte B;
        public byte C;
        public byte D;
        public byte E;
        public byte H;
        public byte L;
        public Flag F; // flags register

        public ushort SP; // stack pointer
        public ushort PC; // program counter

        public ushort IE; // interrupt enable (r/w). TODO: should be at memory address 0xffff
        public ushort IF; // interrupt flag (r/w). TODO: should be at memory address 0xff0f

        public bool IME; // interrupt master enable flag (TODO: write only). 1/true=enable all interrupts enabled in the IE register

        // TODO: implement handling of interrupts

        public ushort AF
        {
            get => (ushort)((A << 8) + (byte)F);
            set
            {
                A = (byte)(value >> 8);
                F = (Flag)(value & 0xff);
            }
        }

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

        public void SetFlags(bool zero, bool subtract, bool halfCarry, bool carry)
        {
            F = (zero ? Flag.Z : Flag.None) | (subtract ? Flag.N : Flag.None) | (halfCarry ? Flag.H : Flag.None) | (carry ? Flag.C : Flag.None);
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

        public ushort Get16BitRegister(int index)
        {
            return index switch
            {
                0 => BC,
                1 => DE,
                2 => HL,
                3 => SP,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "index of 16-bit register"),
            };
        }

        public void Set16BitRegister(int index, ushort value)
        {
            switch (index)
            {
                case 0: BC = value; break;
                case 1: DE = value; break;
                case 2: HL = value; break;
                case 3: SP = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "index of 16-bit register");
            };
        }
    }

    public class Emulator
    {
        // Public
        public static byte DefaultMemoryByteValue { get => InvalidOpCode1; }

        public RegisterSet Registers => reg;

        public byte[] Memory => memory;

        // Private
        private const byte InvalidOpCode1 = 0xDD;

        private static readonly byte[] MiscOpCodesSize =
        {
            // NOP | LD BC,d16 | LD (BC),A | ...
            1,3,1,1,1,1,2,1,3,1,1,1,1,1,2,1, // 0x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 1x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 2x
            2,3,1,1,1,1,2,1,2,1,1,1,1,1,2,1, // 3x

            // some higher opcodes have hardcoded sizes, and do not depend on this table

            // TODO: zero values have not been filled in yet
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 4x
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 5x
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 6x
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 7x
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8x
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 9x
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // ax
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // bx
            0,0,0,3,0,0,0,0,0,0,0,0,3,0,2,0, // cx
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // dx
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // ex
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // fx
        };

        // TODO: ensure mapped memory is large enough for boot ROM, which writes to VRAM at 0x9fff, and writes to 0xff26 to "setup audio".
        // TODO: Later on map some high memory accesses directly to VRAM instead of system memory.
        private readonly byte[] memory = new byte[0x10000];

        // registers
        private RegisterSet reg;

        private static readonly byte[] emptyRom = new byte[0];

        public Emulator()
        {
            InjectRom(emptyRom, emptyRom);
        }

        public void InjectRom(byte[] bootRomData)
        {
            InjectRom(bootRomData, emptyRom);
        }

        public void InjectRom(byte[] bootRomData, byte[] cartRomData)
        {
            //Array.Clear(memory, 0, memory.Length);

            for (int i = Math.Max(bootRomData.Length, cartRomData.Length); i < memory.Length; i++)
                memory[i] = DefaultMemoryByteValue;

            Array.Copy(cartRomData, memory, cartRomData.Length);
            Array.Copy(bootRomData, memory, bootRomData.Length);

            reg.PC = 0;
        }

        private byte Get8BitRegisterOrMem(int index)
        {
            // 'register' index 6 is a special case: read memory location indexed by HL register
            return index == 6 ? memory[reg.HL] : reg.Get8BitRegister(index);
        }

        private void Set8BitRegisterOrMem(int index, byte value)
        {
            // 'register' index 6 is a special case: write memory location indexed by HL register
            if (index == 6)
                memory[reg.HL] = value;
            else
                reg.Set8BitRegister(index, value);
        }

        public void Run(int numInstructions = -1)
        {
            byte previousOpCode = 0;
            ushort previousPC = 1000;

            while (numInstructions-- != 0)
            {
                byte opCode = memory[reg.PC];
                byte literal8Bit = memory[reg.PC + 1];
                byte nextNextByte = memory[reg.PC + 2];
                ushort literal16Bit = (ushort)((nextNextByte << 8) + literal8Bit);

                if (reg.PC == previousPC)
                    throw new InvalidOperationException($"Opcode ({previousOpCode}) did not increase PC ({reg.PC}). Emulator bug or ROM infinite loop!");
                previousPC = reg.PC;
                previousOpCode = opCode;

                //Console.WriteLine("Opcode=0x{0:x}, Literal8bit=0x{1:x}, Literal16bit=0x{2:x}", opCode, literal8Bit, literal16Bit);
                Console.Write(",{0}:{1:x}", reg.PC, opCode);

                bool isBottomHalfBlock = ((opCode >> 7) & 1) == 1; // 0 = top half of opcodes, including 8-bit load ops. 1 = bottom half of opcodes, including arithmetic ops
                bool isQ2orQ4Block = ((opCode >> 6) & 1) == 1; // 0 = Arithmetic ops or top quarter opcodes. 1 = 8-bit load ops or bottom quarter opcodes.

                if (!isBottomHalfBlock && !isQ2orQ4Block)
                {
                    // Instruction decoder for (some of) instructions in top quarter block (0x00 to 0x3f)

                    int opCodeFamily = opCode & 0x07; // 0..7, index of column in left or right half of opcode block
                    bool isRightHalfBlock = ((opCode >> 3) & 0x01) == 1; // 0 = left half of opcodes. 1 = right half of opcodes.
                    int opCodeRegIndex = (opCode >> 4) & 0x03; // 0..3, index of row in opcode block Q1

                    if (ExecuteOpcode_FirstQuarterOpcodes(opCode, opCodeFamily, isRightHalfBlock, opCodeRegIndex, literal8Bit, literal16Bit))
                        continue;
                }
                else if (isBottomHalfBlock != isQ2orQ4Block)
                {
                    // Instruction decoder for middle block of Load and Arithmetic instructions (0x40 to 0xbf)
                    // isBottomHalfBlock: 0 = Load, 1 = Arithmetic
                    // isTopQ2orQ4Block: 0 = Arithmetic, 1 = Load. Must be opposite of topOrBottom

                    if (isQ2orQ4Block == isBottomHalfBlock)
                        throw new InvalidOperationException("Should never occur!");

                    int srcReg8Index = opCode & 0x07; // 0..7 == B,C,D,E,H,L,(HL),A

                    if (isBottomHalfBlock)
                    {
                        // Arithmetic operation
                        bool isRightHalfBlock = ((opCode >> 3) & 0x01) == 1; // 0 = left half of opcodes. 1 = right half of opcodes.
                        int opCodeRowIndex = (opCode >> 4) & 0x03; // 0..3, index of row in opcode block Q3

                        if (ExecuteOpcode_Arithmetic_Main(opCode, srcReg8Index, isRightHalfBlock, opCodeRowIndex))
                            continue;
                    }
                    else
                    {
                        // 8-bit load operation from register or memory, but not from 8-bit literal
                        ExecuteOpcode_LoadRegOrMem_Main(opCode, srcReg8Index);
                        continue;
                    }
                }
                else
                {
                    // Instruction decoder for (some of) instructions in bottom quarter block (0xc0 to 0xff)
                    if (!isBottomHalfBlock || !isQ2orQ4Block)
                        throw new NotImplementedException("Should never happen!");

                    if (opCode == (byte)OpCode.PREFIX_CB)
                    {
                        ExecuteOpcode_ExpandedOpcodes(expandedOpCode: literal8Bit);
                        continue;
                    }
                    else
                    {
                        int opCodeFamily = opCode & 0x07; // 0..7, index of column in left or right half of opcode block
                        bool isRightHalfBlock = ((opCode >> 3) & 0x01) == 1; // 0 = left half of opcodes. 1 = right half of opcodes.
                        int opCodeRegIndex = (opCode >> 4) & 0x03; // 0..3, index of row in opcode block Q1

                        if (ExecuteOpcode_LastQuarterOpcodes(opCode, opCodeFamily, isRightHalfBlock, opCodeRegIndex, literal8Bit, literal16Bit))
                            continue;
                    }
                }

                ExecuteOpcode_Misc(opCode, literal8Bit);
            }
        }

        private bool ExecuteOpcode_FirstQuarterOpcodes(byte opCode, int opCodeFamily, bool isRightHalfBlock,
            int opCodeRegIndex, byte literal8Bit, ushort literal16Bit)
        {
            Debug.Assert(opCode >= 0x00 && opCode <= 0x3f);
            Debug.Assert(opCodeFamily >= 0 && opCodeFamily <= 7);
            Debug.Assert(opCodeRegIndex >= 0 && opCodeRegIndex <= 3);

            // TODO: only needed for some opcode families
            int reg8Index = (opCodeRegIndex << 1) + (isRightHalfBlock ? 1 : 0); // recombine 1+2 bits = 3 bits (but in opposite order)

            // TODO: handle other opcode families
            switch (opCodeFamily)
            {
                // Relative jump and other opcodes
                case 0:
                    sbyte offset = (sbyte)literal8Bit;
                    reg.PC += 2;
                    if (isRightHalfBlock)
                    {
                        switch (opCodeRegIndex)
                        {
                            // TODO: implement 'LD (a16),SP'. 3 byte instruction.
                            case 0:
                                return false;
                            //throw new NotImplementedException("LD (a16),SP");

                            // Jump Relative

                            // JR r8
                            case 1:
                                reg.PC = (ushort)(reg.PC + offset);
                                return true;

                            // JR Z r8
                            case 2:
                                if ((reg.F & Flag.Z) != 0)
                                    reg.PC = (ushort)(reg.PC + offset);
                                return true;

                            // JR C r8
                            case 3:
                                if ((reg.F & Flag.C) != 0)
                                    reg.PC = (ushort)(reg.PC + offset);
                                return true;
                        }
                    }
                    else
                    {
                        switch (opCodeRegIndex)
                        {
                            // NOP
                            case 0:
                                reg.PC -= 1; // undo the 'PC += 2' done above
                                return true;

                            // TODO: implement STOP. 2 byte instruction.
                            case 1: throw new NotImplementedException("STOP");

                            // Jump Relative

                            // JR NZ r8
                            case 2:
                                if ((reg.F & Flag.Z) == 0)
                                    reg.PC = (ushort)(reg.PC + offset);
                                return true;

                            // JR NC r8
                            case 3:
                                if ((reg.F & Flag.C) == 0)
                                    reg.PC = (ushort)(reg.PC + offset);
                                return true;
                        }
                    }
                    throw new NotImplementedException("Should never reach here!");

                case 1:
                    if (isRightHalfBlock)
                    {
                        // TODO: ADD HL,rr
                        return false;
                    }
                    else
                    {
                        // LD rr,d16
                        reg.Set16BitRegister(opCodeRegIndex, literal16Bit);
                        reg.PC += 3;
                        return true;
                    }

                // LD (reg16),A or LD A,(reg16) with mandatory post-increment or post-decrement if HL is the 16-bit register
                case 2:
                    int reg16Index = opCodeRegIndex == 3 ? 2 : opCodeRegIndex; // 4th row is HL- instead of SP
                    int memoryAddr = reg.Get16BitRegister(reg16Index);

                    if (!isRightHalfBlock)
                        // LD (reg16),A
                        memory[memoryAddr] = reg.A;
                    else
                        // LD A,(reg16)
                        reg.A = memory[memoryAddr];

                    if (opCodeRegIndex == 2)
                        reg.HL++; // carry flag not set if HL overflows
                    else if (opCodeRegIndex == 3)
                        reg.HL--; // underflow never sets any flags

                    reg.PC += 1;
                    return true;

                // INC reg16 or DEC reg16
                case 3:
                    if (isRightHalfBlock)
                    {
                        // DEC reg16
                        reg.Set16BitRegister(opCodeRegIndex, (ushort)(reg.Get16BitRegister(opCodeRegIndex) - 1));
                        reg.PC++;
                        return true;
                    }
                    else
                    {
                        // INC reg16
                        reg.Set16BitRegister(opCodeRegIndex, (ushort)(reg.Get16BitRegister(opCodeRegIndex) + 1));
                        reg.PC++;
                        return true;
                    }

                // INC reg8 or INC (HL)
                case 4:
                    byte value = Get8BitRegisterOrMem(reg8Index);
                    value++;
                    Set8BitRegisterOrMem(reg8Index, value);

                    // set flags register: Z 0 H -
                    reg.SetFlags(zero: value == 0, subtract: false, halfCarry: (value & 0x0f) == 0, carry: (reg.F & Flag.C) != 0);
                    reg.PC += 1;
                    return true;

                // DEC reg8 or DEC (HL)
                case 5:
                    value = Get8BitRegisterOrMem(reg8Index);
                    value--;
                    Set8BitRegisterOrMem(reg8Index, value);

                    // set flags register: Z 1 H -
                    reg.SetFlags(zero: value == 0, subtract: true, halfCarry: (value & 0x0f) == 0x0f, carry: (reg.F & Flag.C) != 0);
                    reg.PC += 1;
                    return true;

                // LD r,d8 or LD (HL),d8
                case 6:
                    Set8BitRegisterOrMem(reg8Index, literal8Bit);
                    reg.PC += 2;
                    return true;

                // TODO: make default case throw exception once all cases are accounted for?
                default:
                    return false;
            }
        }

        // Some arithmetic opcodes are outside the scope of this method
        private bool ExecuteOpcode_Arithmetic_Main(byte opCode, int srcReg8Index, bool isRightHalfBlock, int opCodeRowIndex)
        {
            Debug.Assert(opCode >= 0x80 && opCode <= 0xbf);
            Debug.Assert(0 <= srcReg8Index && srcReg8Index <= 7);
            Debug.Assert(0 <= opCodeRowIndex && opCodeRowIndex <= 3);

            byte operandVal = Get8BitRegisterOrMem(srcReg8Index);

            byte newVal;
            switch (opCodeRowIndex)
            {
                // ADD / ADC (add with carry)
                case 0:
                    newVal = (byte)(reg.A + operandVal);
                    if (isRightHalfBlock && (reg.F & Flag.C) != 0)
                        newVal++;

                    // set flags register: Z 0 H C
                    reg.SetFlags(zero: newVal == 0, subtract: false, halfCarry: (newVal & 0x0f) < (reg.A & 0x0f), carry: newVal < reg.A);

                    reg.A = newVal;
                    break;

                // SUB / SBC (subtract with carry)
                case 1:
                    newVal = (byte)(reg.A - operandVal);
                    if (isRightHalfBlock && (reg.F & Flag.C) != 0)
                        newVal--;

                    // set flags register: Z 1 H C
                    reg.SetFlags(zero: newVal == 0, subtract: true, halfCarry: (newVal & 0x0f) > (reg.A & 0x0f), carry: newVal > reg.A);

                    reg.A = newVal;
                    break;

                // AND / XOR
                case 2:
                    if (isRightHalfBlock)
                    {
                        // XOR
                        reg.A = (byte)(reg.A ^ operandVal);
                        reg.SetFlags(zero: reg.A == 0, subtract: false, halfCarry: false, carry: false);
                    }
                    else
                    {
                        // AND
                        reg.A = (byte)(reg.A & operandVal);
                        reg.SetFlags(zero: reg.A == 0, subtract: false, halfCarry: true, carry: false);
                    }
                    break;

                // TODO: handle other opcode families

                // OR / CP
                case 3:
                    if (isRightHalfBlock)
                    {
                        // CP r8 or CP (HL)
                        newVal = (byte)(reg.A - operandVal);

                        // set flags register: Z 1 H C
                        reg.SetFlags(zero: newVal == 0, subtract: true, halfCarry: (newVal & 0x0f) > (reg.A & 0x0f), carry: newVal > reg.A);

                        reg.PC++;
                        return true;
                    }
                    else
                    {
                        // OR
                        return false;
                    }

                default:
                    return false;
                    //throw new NotImplementedException("Some arithmetic operations not yet implemented");
            }

            reg.PC++;
            return true;
        }

        private bool ExecuteOpcode_LastQuarterOpcodes(byte opCode, int opCodeFamily, bool isRightHalfBlock, int opCodeRegIndex, byte literal8Bit, ushort literal16Bit)
        {
            Debug.Assert(opCode >= 0xc0 && opCode <= 0xff);
            Debug.Assert(opCodeFamily >= 0 && opCodeFamily <= 7);
            Debug.Assert(opCodeRegIndex >= 0 && opCodeRegIndex <= 3);

            // POP rr
            if (opCodeFamily == 1 && !isRightHalfBlock)
            {
                ushort value = (ushort)((memory[reg.SP + 1] << 8) + memory[reg.SP]);
                reg.SP += 2;

                // 4th row is AF instead of SP
                if (opCodeRegIndex == 3)
                    reg.AF = value;
                else
                    reg.Set16BitRegister(opCodeRegIndex, value);

                reg.PC++;
                return true;
            }

            // CALL f,a16
            if (opCodeFamily == 4)
            {
                reg.PC += 3;
                Flag flagBitToBranch = (Flag)(isRightHalfBlock ? 1 : 0);

                switch (opCodeRegIndex)
                {
                    // CALL NZ,a16 / CALL Z,a16
                    case 0:
                        if ((reg.F & Flag.Z) != flagBitToBranch) return true; // no branch, move to next opcode
                        break;

                    // CALL NC,a16 / CALL C,a16
                    case 1:
                        if ((reg.F & Flag.C) != flagBitToBranch) return true; // no branch, move to next opcode
                        break;

                    // Invalid opcodes - the real machine crashes here!
                    default:
                        throw new ArgumentException($"Invalid opcode {opCode} at PC=" + reg.PC);
                }

                // push next opcode in sequence onto stack, and branch to 16-bit address
                reg.SP -= 2;
                memory[reg.SP] = (byte)reg.PC;
                memory[reg.SP + 1] = (byte)(reg.PC >> 8);
                reg.PC = literal16Bit;
                return true;
            }

            // PUSH rr
            if (opCodeFamily == 5 && !isRightHalfBlock)
            {
                ushort value = opCodeRegIndex == 3 ? reg.AF : reg.Get16BitRegister(opCodeRegIndex); // 4th row is AF instead of SP
                reg.SP -= 2;
                memory[reg.SP] = (byte)value;
                memory[reg.SP + 1] = (byte)(value >> 8);
                reg.PC++;
                return true;
            }

            switch (opCode)
            {
                // LDH (a8),A  aka  LD ($FF00+a8),A
                case (byte)OpCode.LDH_a8_A:
                    memory[0xFF00 + literal8Bit] = reg.A;
                    reg.PC += 2;
                    return true;

                // LDH A,(a8)  aka  LD A,($FF00+a8)
                case (byte)OpCode.LDH_A_a8:
                    reg.A = memory[0xFF00 + literal8Bit];
                    reg.PC += 2;
                    return true;

                // LD (a16),A
                case (byte)OpCode.LD_a16_A:
                    memory[literal16Bit] = reg.A;
                    reg.PC += 3;
                    return true;

                // LD (C),A  aka  LD ($FF00+C),A
                case (byte)OpCode.LD_Cmem_A:
                    memory[0xFF00 + reg.C] = reg.A;
                    reg.PC += 2;
                    return true;

                case (byte)OpCode.CALL_a16:
                    reg.PC += 3;
                    reg.SP -= 2;
                    memory[reg.SP] = (byte)reg.PC;
                    memory[reg.SP + 1] = (byte)(reg.PC >> 8);
                    reg.PC = literal16Bit;
                    return true;

                case (byte)OpCode.RET:
                    reg.PC = (ushort)((memory[reg.SP + 1] << 8) + memory[reg.SP]);
                    reg.SP += 2;
                    return true;

                // RETurn and enable interrupts
                case (byte)OpCode.RETI:
                    reg.PC = (ushort)((memory[reg.SP + 1] << 8) + memory[reg.SP]);
                    reg.SP += 2;
                    reg.IME = true;
                    return true;

                case (byte)OpCode.CP_d8:
                    byte newVal = (byte)(reg.A - literal8Bit);

                    // set flags register: Z 1 H C
                    reg.SetFlags(zero: newVal == 0, subtract: true, halfCarry: (newVal & 0x0f) > (reg.A & 0x0f), carry: newVal > reg.A);

                    reg.PC += 2;
                    return true;

                // JP a16
                case (byte)OpCode.JP_a16:
                    reg.PC = literal16Bit;
                    return true;

                // DI - disable interrupt
                case 0xf3:
                    reg.IME = false;
                    reg.PC += 1;
                    return true;

                // EI - enable interrupt
                case 0xfb:
                    reg.IME = true;
                    reg.PC += 1;
                    return true;

                default:
                    return false;
            }
        }

        // 8-bit load operation from register or memory, but not from 8-bit literal
        // Outside the scope of this method: loading a literal into a register, and (some) indirect memory read/writes
        private void ExecuteOpcode_LoadRegOrMem_Main(byte opCode, int srcReg8Index)
        {
            Debug.Assert(opCode >= 0x40 && opCode <= 0x7f);
            Debug.Assert(0 <= srcReg8Index && srcReg8Index <= 7);

            int destReg8Index = (opCode >> 3) & 0x07; // 0..7 == B,C,D,E,H,L,(HL),A

            // TODO: handle HALT special case. Need to "halt until interrupt occurs"
            if (opCode == (byte)OpCode.HALT)
                throw new NotImplementedException("HALT");

            Set8BitRegisterOrMem(destReg8Index, Get8BitRegisterOrMem(srcReg8Index));
            reg.PC++;
        }

        // All 2-byte opcodes with a 0xCB prefix. Generally bitwise operations.
        private void ExecuteOpcode_ExpandedOpcodes(byte expandedOpCode)
        {
            reg.PC += 2;

            int reg8Index = expandedOpCode & 0x07; // 0..7 == B,C,D,E,H,L,(HL),A
            int opCodeIndex = expandedOpCode >> 3; // 0..31, index of half-row in expanded opcode table

            if (opCodeIndex >= 16 && opCodeIndex <= 23)
            {
                // RES #,r8 - reset bit. Opcodes 0x80 to 0xbf.
                int bitPosition = opCodeIndex - 16;
                byte bitMask = (byte) ~(1 << bitPosition);
                Set8BitRegisterOrMem(reg8Index, (byte) (Get8BitRegisterOrMem(reg8Index) & bitMask));
            }
            else if (opCodeIndex >= 24)
            {
                // SET #,r8 - set bit. Opcodes 0xc0 to 0xff.
                int bitPosition = opCodeIndex - 24;
                byte bitMask = (byte) (1 << bitPosition);
                Set8BitRegisterOrMem(reg8Index, (byte) (Get8BitRegisterOrMem(reg8Index) | bitMask));
            }
            else if (expandedOpCode == 0x7C)
            {
                // BIT 7,H
                const int bitPosition = 7;

                // test bit 7 (the highest bit)
                bool bitNotSet = (reg.H & (1 << bitPosition)) == 0;

                // set flags register: Z 0 1 -
                reg.SetFlags(zero: bitNotSet, subtract: false, halfCarry: true, carry: (reg.F & Flag.C) != 0);
            }
            else if (expandedOpCode == 0x01)
            {
                // RLC C - rotate left
                byte oldVal = reg.C;
                reg.C = (byte)((oldVal << 1) | (oldVal >> 7));

                // set flags register: Z 0 0 C
                // TODO: guessing how to set carry flag
                reg.SetFlags(zero: reg.C == 0, subtract: false, halfCarry: false, carry: (oldVal & 0x80) != 0);
            }
            else if (expandedOpCode == 0x11)
            {
                // RL C - rotate left through carry
                // TODO: guessing how to use carry flag to add a bit to register
                byte oldVal = reg.C;
                reg.C = (byte)((oldVal << 1) | ((reg.F & Flag.C) != 0 ? 1 : 0));

                // set flags register: Z 0 0 C
                // TODO: guessing how to set carry flag
                reg.SetFlags(zero: reg.C == 0, subtract: false, halfCarry: false, carry: (oldVal & 0x80) != 0);
            }
            else
                throw new NotImplementedException("Some CB expanded opcodes not yet implemented: " + expandedOpCode);
        }

        private void ExecuteOpcode_Misc(byte opCode, byte literal8Bit)
        {
            // General instructions, handled individually rather than decoded from opcode
            switch (opCode)
            {
                // LD A,d8
                case (byte)OpCode.LD_A_d8:
                    reg.A = literal8Bit;
                    break;

                case (byte)OpCode.RLA:
                    // RLA - rotate A left through carry
                    // TODO: guessing how to use carry flag to add a bit to register
                    byte oldVal = reg.A;
                    reg.A = (byte)((oldVal << 1) | ((reg.F & Flag.C) != 0 ? 1 : 0));

                    // set flags register: 0 0 0 C
                    // TODO: guessing how to set carry flag
                    reg.SetFlags(zero: false, subtract: false, halfCarry: false, carry: (oldVal & 0x80) != 0);
                    break;

                case InvalidOpCode1:
                    throw new ArgumentException("Invalid opcode 0xdd at PC=" + reg.PC);

                default:
                    // TODO: experiment to skip over known but unimplemented opcodes
                    //if (opCode >= MiscOpCodesSize.Length)
                        throw new ArgumentOutOfRangeException(nameof(opCode), opCode, "Invalid opcode at PC=" + reg.PC);
                    //Console.Write('*');
                    //break;
            }

            reg.PC += MiscOpCodesSize[opCode];
        }
    }
}