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

        // Opcode 0x00
        [Test]
        public void Nop()
        {
            emulator.InjectRom(new byte[]{0x00});
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(1, register.PC);
            Assert.AreEqual(0x0, register.BC);
            Assert.AreEqual(0x0, register.B);
            Assert.AreEqual(0x0, register.C);
        }

        // Opcode 0x01
        [Test]
        public void LD_BC_d16()
        {
            emulator.InjectRom(new byte[]{0x01,0xab,0xcd});
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(3, register.PC);
            Assert.AreEqual(0xabcd, register.BC);
            Assert.AreEqual(0xab, register.B);
            Assert.AreEqual(0xcd, register.C);
        }

        // Opcode 0x31
        [Test]
        public void LD_SP_d16()
        {
            emulator.InjectRom(new byte[]{0x31,0xab,0xcd});
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(0xabcd, register.SP);
        }
    }
}