using GameBoyEmu;
using NUnit.Framework;
using System.IO;

namespace EmulatorTests
{
    class RomTests
    {
        private Emulator emulator;

        [SetUp]
        public void Setup()
        {
            emulator = new Emulator();
        }

        [Test]
        public void RunBootRom()
        {
            var romData = File.ReadAllBytes("DMG_ROM.bin");
            emulator.InjectRom(romData);
            emulator.Run(3);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(7, register.PC);
            Assert.AreEqual(0xfffe, register.SP);
            Assert.AreEqual(0x9fff, register.HL);
            Assert.AreEqual(0x9f, register.H);
            Assert.AreEqual(0xff, register.L);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.C);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.D);
            Assert.AreEqual(0, register.E);
        }
    }
}