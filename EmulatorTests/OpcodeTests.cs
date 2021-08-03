using GameBoyEmu;
using NUnit.Framework;

namespace EmulatorTests
{
    public class OpcodeTests
    {
        // Cannot be a byte enum as individual enum values have to be casted to bytes
        private static class Op
        {
            public const byte LD_HL_d16 = (byte)OpCode.LD_HL_d16;
            public const byte LD_BC_d16 = (byte)OpCode.LD_BC_d16;
            public const byte LD_SP_d16 = (byte)OpCode.LD_SP_d16;
            public const byte LD_A_d8 = (byte)OpCode.LD_A_d8;
            public const byte XOR_A = (byte)OpCode.XOR_A;
        }

        private Emulator emulator;

        [SetUp]
        public void Setup()
        {
            emulator = new Emulator();
        }

        [Test]
        public void Nop()
        {
            emulator.InjectRom(new byte[] { 0x00 });
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

        //[Test]
        public void Load_8bit_register_from_memory()
        {
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0xbc, 0x57, 0x6a }); // LD A,d8 | LD D,A | LD L,D
            emulator.Run(5); // TODO
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
        public void XOR_A()
        {
            emulator.InjectRom(new byte[] { Op.XOR_A });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0x0, register.A);

            // LD A,0xBC ; XOR A
            emulator.InjectRom(new byte[] { Op.LD_A_d8, 0xbc, Op.XOR_A });
            emulator.Run(2);
            register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0x0, register.A);
        }
    }
}