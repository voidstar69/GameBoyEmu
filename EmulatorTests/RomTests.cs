using GameBoyEmu;
using NUnit.Framework;
using System.IO;

namespace EmulatorTests
{
    [TestFixture, Timeout(100)]
    class RomTests
    {
        private Emulator emulator;

        [SetUp]
        public void Setup()
        {
            emulator = new Emulator();
        }

        [Test]
        public void RunBootRom_3instructions()
        {
            var romData = File.ReadAllBytes("DMG_ROM.bin");
            emulator.InjectRom(romData);
            emulator.Run(3);

            RegisterSet register = emulator.Registers;
            Assert.AreEqual(7, register.PC);
            Assert.AreEqual(Flag.Z, register.F);
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

        [Test]
        public void RunBootRom_4instructions()
        {
            var romData = File.ReadAllBytes("DMG_ROM.bin");
            emulator.InjectRom(romData);
            Memory memory = emulator.Memory;
            Assert.AreEqual(0xdd, memory[0x9fff]);
            emulator.Run(4);

            memory = emulator.Memory;
            Assert.AreEqual(0x0, memory[0x9fff]);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(8, register.PC);
            Assert.AreEqual(Flag.Z, register.F);
            Assert.AreEqual(0xfffe, register.SP);
            Assert.AreEqual(0x9ffe, register.HL);
            Assert.AreEqual(0x9f, register.H);
            Assert.AreEqual(0xfe, register.L);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.C);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.D);
            Assert.AreEqual(0, register.E);
        }

        [Test]
        public void RunBootRom_5instructions()
        {
            var romData = File.ReadAllBytes("DMG_ROM.bin");
            emulator.InjectRom(romData);
            Memory memory = emulator.Memory;
            Assert.AreEqual(0xdd, memory[0x9fff]);
            Assert.AreEqual(0xdd, memory[0x9ffe]);
            emulator.Run(5);

            memory = emulator.Memory;
            Assert.AreEqual(0x0, memory[0x9fff]);
            Assert.AreEqual(0xdd, memory[0x9ffe]);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(10, register.PC);
            Assert.AreEqual(Flag.H, register.F);
            Assert.AreEqual(0xfffe, register.SP);
            Assert.AreEqual(0x9ffe, register.HL);
            Assert.AreEqual(0x9f, register.H);
            Assert.AreEqual(0xfe, register.L);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.C);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.D);
            Assert.AreEqual(0, register.E);
        }

        [Test]
        public void RunBootRom_7instructions()
        {
            var romData = File.ReadAllBytes("DMG_ROM.bin");
            emulator.InjectRom(romData);
            Memory memory = emulator.Memory;
            Assert.AreEqual(0xdd, memory[0x9fff]);
            Assert.AreEqual(0xdd, memory[0x9ffe]);
            emulator.Run(7);

            memory = emulator.Memory;
            Assert.AreEqual(0x0, memory[0x9fff]);
            Assert.AreEqual(0x0, memory[0x9ffe]);
            RegisterSet register = emulator.Registers;
            Assert.AreEqual(8, register.PC);
            Assert.AreEqual(Flag.H, register.F);
            Assert.AreEqual(0xfffe, register.SP);
            Assert.AreEqual(0x9ffd, register.HL);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.C);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.D);
            Assert.AreEqual(0, register.E);
        }

        // Emulate a short loop which zeroes the emulated memory between $8000-$9FFF
        [Test]
        public void RunBootRom_MemoryClearLoop()
        {
            var romData = File.ReadAllBytes("DMG_ROM.bin");
            emulator.InjectRom(romData);
            Memory memory = emulator.Memory;
            Assert.AreEqual(0xdd, memory[0x9fff]);
            Assert.AreEqual(0xdd, memory[0x9ffe]);
            Assert.AreEqual(0xdd, memory[0x8000]);
            Assert.AreEqual(0xdd, memory[0x7fff]);
            emulator.Run(3 + 8 * 1024 * 3);

            RegisterSet register = emulator.Registers;
            Assert.AreEqual(0xc, register.PC);
            Assert.AreEqual(Flag.Z | Flag.H, register.F);
            Assert.AreEqual(0xfffe, register.SP);
            Assert.AreEqual(0x7fff, register.HL);
            Assert.AreEqual(0, register.A);
            Assert.AreEqual(0, register.BC);
            Assert.AreEqual(0, register.B);
            Assert.AreEqual(0, register.C);
            Assert.AreEqual(0, register.DE);
            Assert.AreEqual(0, register.D);
            Assert.AreEqual(0, register.E);

            memory = emulator.Memory;
            Assert.AreEqual(0x0, memory[0x9fff]);
            Assert.AreEqual(0x0, memory[0x9ffe]);
            Assert.AreEqual(0x0, memory[0x8000]);
            Assert.AreEqual(0xdd, memory[0x7fff]);
        }
        
        [Test, Timeout(150)]
        public void RunBootRom_UntilLogoCheck()
        {
            var romData = File.ReadAllBytes("DMG_ROM.bin");
            emulator.InjectRom(romData);
            Memory memory = emulator.Memory;
            Assert.AreEqual(0xdd, memory[0x9fff]);
            Assert.AreEqual(0xdd, memory[0x9ffe]);
            Assert.AreEqual(0xdd, memory[0x8000]);
            Assert.AreEqual(0xdd, memory[0x7fff]);

            emulator.Run(47442); // stop just before opcode which causes infinite loop in Boot ROM because logo data in cart does not match DMG ROM

            RegisterSet register = emulator.Registers;
            Assert.AreEqual(0xe9, register.PC);

            memory = emulator.Memory;
            Assert.AreEqual(0x0, memory[0x9fff]);
            Assert.AreEqual(0x0, memory[0x9ffe]);
            Assert.AreEqual(0x0, memory[0x8000]);
            Assert.AreEqual(0xdd, memory[0x7fff]);
        }

        [Test, Timeout(200)]
        public void RunBootRomAndCartRom()
        {
            var bootRomData = File.ReadAllBytes("DMG_ROM.bin");
            var cartRomData = File.ReadAllBytes("2048.gb.bin");
            emulator.InjectRom(bootRomData, cartRomData);
            Memory memory = emulator.Memory;
            Assert.AreEqual(0xdd, memory[0x9fff]);
            Assert.AreEqual(0xdd, memory[0x9ffe]);
            Assert.AreEqual(0xdd, memory[0x8000]);
            Assert.AreEqual(0x0, memory[0x7fff]);

            emulator.Run(47443 + 1000); // TODO: Invalid opcode at PC=783. Actual value was 230. "AND d8"

            RegisterSet register = emulator.Registers;
            Assert.AreEqual(779, register.PC);

            memory = emulator.Memory;
            Assert.AreEqual(0x0, memory[0x9fff]);
            Assert.AreEqual(0x0, memory[0x9ffe]);
            Assert.AreEqual(0x0, memory[0x8000]);
            Assert.AreEqual(0x0, memory[0x7fff]);
        }
    }
}