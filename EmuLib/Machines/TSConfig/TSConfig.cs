using System;
using EmuLib.Core;
using EmuLib.Hardware;
using EmuLib.Hardware.Z80;

namespace EmuLib.Machines.TSConfig
{
    public enum TurboMode
    {
        Normal,
        Turbo7M,
        Turbo14M,
    }

    public partial class TSConfig : MachineBase, ICpuBus
    {
        private const int SDL_TACTS_IN_FRAME = 286720;

        public override string Name => "TS Config";
        public override (int, int) VideoBuffer => (720, 576);
        public override string RomFile => "evo.rom";
        public override int MemorySize => 4 * 1024 * 1024;

        private uint _fameCnt = 0;
        private int _tack = 0;
        private Z80Cpu _cpu;

        public TurboMode TurboMode = TurboMode.Normal;

        public Memory Memory => new Memory();

        public override void Init()
        {
            _cpu = new Z80Cpu(this);
            Memory.Init();
        }

        public override void Done()
        {
            
        }

        public override void Execute(int us)
        {
            _fameCnt+=1;
        }

        public override void Render(IntPtr buff)
        {
            unsafe
            {
                var ptr = (uint*)buff;
                for (uint y = 0; y < 576; y++)
                {
                    for (uint x = 0; x < 720; x++)
                    {
                        *ptr = 0xff000000 | (y << 16) | ((x + _fameCnt) & 0xffff);
                        ptr++;
                    }
                }
            }
        }

        public void Reset()
        {
            
        }

        public void NmiAckM1(int tacts)
        {
            ExecuteTacts(tacts);
        }

        public void IntAckM1(int tacts)
        {
            ExecuteTacts(tacts);
        }

        public byte RdMemM1(ushort addr)
        {
            ExecuteTacts(3);
            return 0xff;
        }

        public byte RdMem(ushort addr)
        {
            ExecuteTacts(3);
            return 0xff;
        }

        public void WrMem(ushort addr, byte val)
        {
            ExecuteTacts(3);
        }

        public byte RdPort(ushort addr)
        {
            ExecuteTacts(4);
            return 0xff;
        }

        public void WrPort(ushort addr, byte val)
        {
            ExecuteTacts(4);
        }

        public void RdNoMREQ(ushort addr, int tacts = 1)
        {
            ExecuteTacts(tacts);
        }

        public void WrNoMREQ(ushort addr, int tacts = 1)
        {
            ExecuteTacts(tacts);
        }

        private void ExecuteTacts(int tacts)
        {
            switch (TurboMode)
            {
                case TurboMode.Normal:
                    _tack += tacts << 2;
                    break;
                case TurboMode.Turbo7M:
                    _tack += tacts << 1;
                    break;
                case TurboMode.Turbo14M:
                    _tack += tacts;
                    break;
            }

            
        }
    }
}
