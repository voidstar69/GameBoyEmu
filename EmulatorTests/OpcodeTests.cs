using GameBoyEmu;
using NUnit.Framework;

namespace EmulatorTests
{
    public class OpcodeTests
    {
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
            emulator.InjectRom(new byte[] { 0x01, 0xcd, 0xab });
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
            emulator.InjectRom(new byte[] { 0x31, 0xcd, 0xab });
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
            emulator.InjectRom(new byte[] { 0x21, 0xcd, 0xab });
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
            emulator.InjectRom(new byte[] { 0x3e, 0xbc });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(2, register.PC);
            Assert.AreEqual(0xbc, register.A);
        }

        [Test]
        public void XOR_A()
        {
            emulator.InjectRom(new byte[] { 0xaf });
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0x0, register.A);

            // LD A,0xBC ; XOR A
            emulator.InjectRom(new byte[] { 0x3e, 0xbc, 0xaf });
            emulator.Run(2);
            register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0x0, register.A);
        }
    }
}