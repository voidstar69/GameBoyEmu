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
        public void Load16BitValueIntoBC()
        {
            emulator.Run(2);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(0xabcd, register.BC);
            Assert.AreEqual(0xab, register.B);
            Assert.AreEqual(0xcd, register.C);
        }

        [Test]
        public void Nop()
        {
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(0x0, register.BC);
            Assert.AreEqual(0x0, register.B);
            Assert.AreEqual(0x0, register.C);
        }
    }
}