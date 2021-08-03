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
            emulator.Run(1);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(0x0, register.BC);
            Assert.AreEqual(0x0, register.B);
            Assert.AreEqual(0x0, register.C);
        }
    }
}