using System;

namespace EmuLib.Hardware
{
    public interface ICpuBus
    {
        void Reset();
        void NmiAckM1(int tacts);
        void IntAckM1(int tacts);
        byte RdMemM1(ushort addr);
        byte RdMem(ushort addr);
        void WrMem(ushort addr, byte val);
        byte RdPort(ushort addr);
        void WrPort(ushort addr, byte val);
        void RdNoMREQ(ushort addr, int tacts = 1);
        void WrNoMREQ(ushort addr, int tacts = 1);
    }
}