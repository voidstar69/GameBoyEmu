using GameBoyEmu;
using NUnit.Framework;

namespace EmulatorTests
{
    public class OpcodeTests
    {
        // Cannot be a byte enum as individual enum values have to be casted to bytes
        private static class Op
        {
            public const byte LD_BC_d16 = (byte)OpCode.LD_BC_d16;
            public const byte LD_HL_d16 = (byte)OpCode.LD_HL_d16;
            public const byte LD_SP_d16 = (byte)OpCode.LD_SP_d16;
            public const byte LD_A_d8 = (byte)OpCode.LD_A_d8;
            public const byte LD_B_A = 0x47;
            public const byte LD_HLmem_A = (byte)OpCode.LD_HLmem_A;
            public const byte LD_A_HLmem = (byte)OpCode.LD_A_HLmem;
            public const byte XOR_A = (byte)OpCode.XOR_A;
            public const byte ADD_A_A = 0x87;
            public const byte ADC_A_A = 0x8F;
            public const byte SUB_B = 0x90;
            public const byte SUB_A = 0x97;
        }

        private Emulator emulator;

        [SetUp]
        public void Setup()
        {
            emulator = new Emulator();
        }

        [Test]
        public void NOP()
        {
            emulator.InjectRom(new byte[] { (byte)OpCode.NOP });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.C);
            Assert.AreEqual(0, register.HL);
            Assert.AreEqual(0, register.H);
            Assert.AreEqual(0, register.L);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void LD_BC_d16()
        {
            emulator.InjectRom(new byte[] { Op.LD_BC_d16, 0xcd, 0xab });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0xabcd, register.BC);
            Assert.AreEqual(0xab, register.B);
            Assert.AreEqual(0xcd, register.C);
            Assert.AreEqual(0, register.HL);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void INC_C()
        {
            emulator.InjectRom(new byte[] { (byte)OpCode.INC_C });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(1, register.C);
            Assert.AreEqual(Flag.None, register.F);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.HL);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void INC_memory_indexed_by_HL()
        {
            // increment memory location zero
            emulator.InjectRom(new byte[] { (byte)OpCode.INC_HLmem });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0, register.HL);
            Assert.AreEqual(Flag.None, register.F);
            byte[] memory = emulator.Memory;
            Assert.AreEqual(0x35, memory[0]);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.SP);

            // cause a zero result (carry flag not affected by INC instructions)
            emulator.InjectRom(new byte[] { Op.LD_HL_d16, 0x04, 0x00, (byte)OpCode.INC_HLmem, 0xff });
            emulator.Run(2);
            register = emulator.Registers;
            Assert.AreEqual(4, register.PC);
            Assert.AreEqual(0x4, register.HL);
            Assert.AreEqual(Flag.Z | Flag.H, register.F);
            memory = emulator.Memory;
            Assert.AreEqual(0x00, memory[4]);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.SP);

            // cause a half carry
            emulator.InjectRom(new byte[] { Op.LD_HL_d16, 0x04, 0x00, (byte)OpCode.INC_HLmem, 0x0f });
            emulator.Run(2);
            register = emulator.Registers;
            Assert.AreEqual(4, register.PC);
            Assert.AreEqual(0x4, register.HL);
            Assert.AreEqual(Flag.H, register.F);
            memory = emulator.Memory;
            Assert.AreEqual(0x10, memory[4]);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void LD_SP_d16()
        {
            emulator.InjectRom(new byte[] { Op.LD_SP_d16, 0xcd, 0xab });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0xabcd, register.SP);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.HL);
        }

        [Test]
        public void LD_HL_d16()
        {
            emulator.InjectRom(new byte[] { Op.LD_HL_d16, 0xcd, 0xab });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0xabcd, register.HL);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void LD_A_d8()
        {
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0xbc });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(2, register.PC);
            Assert.AreEqual(0xbc, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.C);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.D);
            Assert.AreEqual(0, register.E);
            Assert.AreEqual(0, register.HL);
            Assert.AreEqual(0, register.H);
            Assert.AreEqual(0, register.L);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void LD_C_d8()
        {
            emulator.InjectRom(new byte[] { (byte)OpCode.LD_C_d8, 0xbc });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(2, register.PC);
            Assert.AreEqual(0xbc, register.C);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.HL);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void Load_8bit_register_from_register()
        {
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0xbc, 0x57, 0x6a }); // LD A,d8 | LD D,A | LD L,D
            emulator.Run(3);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(4, register.PC);
            Assert.AreEqual(0xbc, register.A);
            Assert.AreEqual(0xbc, register.D);
            Assert.AreEqual(0xbc, register.L);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.E);
            Assert.AreEqual(0, register.H);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void Load_memory_from_any_8bit_register_A()
        {
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0xbc, Op.LD_HLmem_A }); // LD A,d8 | LD (HL),A
            emulator.Run(2);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0xbc, register.A);
            Assert.AreEqual(0, register.HL);
            byte[] memory = emulator.Memory;
            Assert.AreEqual(0xbc, memory[0]);
            Assert.AreEqual(0xbc, memory[1]);
            Assert.AreEqual(0x77, memory[2]);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void Load_memory_from_any_8bit_register_C()
        {
            emulator.InjectRom(new byte[] { Op.LD_BC_d16, 0xcd, 0xab, 0x71 }); // LD BC,d16 | LD (HL),C
            emulator.Run(2);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(4, register.PC);
            Assert.AreEqual(0xabcd, register.BC);
            Assert.AreEqual(0xcd, register.C);
            Assert.AreEqual(0, register.HL);
            byte[] memory = emulator.Memory;
            Assert.AreEqual(0xcd, memory[0]);
            Assert.AreEqual(0xcd, memory[1]);
            Assert.AreEqual(0xab, memory[2]);
            Assert.AreEqual(0x71, memory[3]);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void Load_any_8bit_register_from_memory_A()
        {
            emulator.InjectRom(new byte[] { Op.LD_A_HLmem }); // LD A,(HL)
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0x7e, register.A);
            Assert.AreEqual(0, register.HL);
            byte[] memory = emulator.Memory;
            Assert.AreEqual(0x7e, memory[0]);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void Load_accumulator_into_memory_indexed_by_BC_register()
        {
            const int memAddr = 0x9abc;
            emulator.InjectRom(new byte[] { Op.LD_BC_d16, 0xbc, 0x9a, 0x02 }); // LD BC,d16 | LD (BC),A
            emulator.Run(2);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(4, register.PC);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(memAddr, register.BC);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.HL);
            Assert.AreEqual(0, register.SP);
            byte[] memory = emulator.Memory;
            Assert.AreEqual(Op.LD_BC_d16, memory[0]);
            Assert.AreEqual(Emulator.DefaultMemoryByteValue, memory[4]);
            Assert.AreEqual(Emulator.DefaultMemoryByteValue, memory[memAddr - 1]);
            Assert.AreEqual(0, memory[memAddr]);
            Assert.AreEqual(Emulator.DefaultMemoryByteValue, memory[memAddr + 1]);
        }

        [Test]
        public void Load_memory_from_accumulator_and_decrement_HL_index_register()
        {
            emulator.InjectRom(new byte[] { 0x32 }); // LD (HL-),A
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0xffff, register.HL); // underflows from 0 to reach (2^16)-1
            byte[] memory = emulator.Memory;
            Assert.AreEqual(0x0, memory[0]);
            Assert.AreEqual(Emulator.DefaultMemoryByteValue, memory[1]);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void Load_memory_from_accumulator_and_increment_HL_index_register()
        {
            emulator.InjectRom(new byte[] { 0x22 }); // LD (HL+),A
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0x1, register.HL);
            byte[] memory = emulator.Memory;
            Assert.AreEqual(0x0, memory[0]);
            Assert.AreEqual(Emulator.DefaultMemoryByteValue, memory[1]);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.SP);
        }

        [Test]
        public void HALT()
        {
            emulator.InjectRom(new byte[] { (byte)OpCode.HALT });
            Assert.Throws<System.NotImplementedException>(() => emulator.Run());
        }

        [Test]
        public void ADD_A_r_and_ADC_A_r()
        {
            // cause a half carry
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0x48, Op.ADD_A_A });
            emulator.Run(2);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0x90, register.A);
            Assert.AreEqual(Flag.H, register.F);

            // do not cause a half carry (only if bottom 4 bits overflow)
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0x44, Op.ADD_A_A });
            emulator.Run(2);
            register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0x88, register.A);
            Assert.AreEqual(Flag.None, register.F);

            // cause a zero result
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0x80, Op.ADD_A_A });
            emulator.Run(2);
            register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0x0, register.A);
            Assert.AreEqual(Flag.Z | Flag.C, register.F);

            // this depends on the carry flag being previously set
            emulator.InjectRom(new byte[] { Op.ADC_A_A });
            emulator.Run(1);
            register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0x1, register.A);
            Assert.AreEqual(Flag.None, register.F);
        }

        // TODO: also test SBC A,r
        [Test]
        public void SUB_r()
        {
            // cause a zero result
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0xab, Op.SUB_A });
            emulator.Run(2);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0x0, register.A);
            Assert.AreEqual(Flag.N | Flag.Z, register.F);

            // cause a carry
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0x02, Op.LD_B_A, Op.LD_A_d8, 0x01, Op.SUB_B });
            emulator.Run(4);
            register = emulator.Registers;
            Assert.AreEqual(6, register.PC);
            Assert.AreEqual(0xff, register.A);
            Assert.AreEqual(Flag.N | Flag.H | Flag.C, register.F);

            // cause a half carry
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0x22, Op.LD_B_A, Op.LD_A_d8, 0x51, Op.SUB_B });
            emulator.Run(4);
            register = emulator.Registers;
            Assert.AreEqual(6, register.PC);
            Assert.AreEqual(0x2f, register.A);
            Assert.AreEqual(Flag.N | Flag.H, register.F);
        }

        [Test]
        public void XOR_A()
        {
            emulator.InjectRom(new byte[] { Op.XOR_A });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0x0, register.A);
            Assert.AreEqual(Flag.Z, register.F);

            // LD A,0xBC ; XOR A
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0xbc, Op.XOR_A });
            emulator.Run(2);
            register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0x0, register.A);
            Assert.AreEqual(Flag.Z, register.F);
        }
    }
}