/* 
 *  Copyright 2007, 2015 Alex Makeev
 * 
 *  This file is part of ZXMAK2 (ZX Spectrum virtual machine).
 *
 *  ZXMAK2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ZXMAK2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ZXMAK2.  If not, see <http://www.gnu.org/licenses/>.
 *  
 *  Description: Z80 CPU Emulator
 *  Date: 13.04.2007
 * 
 */

using System;
using System.Linq;

namespace EmuLib.Hardware.Z80
{
    public partial class Z80Cpu
    {
        public readonly CpuRegs Regs = new CpuRegs();
        public CpuType CpuType = CpuType.Z84;
        public int RzxCounter;
        //public long Tact;
        public bool HALTED;
        public bool IFF1;
        public bool IFF2;
        public byte IM;
        public bool BINT;       // last opcode was EI or DD/FD prefix (to prevent INT handling)
        public CpuModeIndex FX;
        public CpuModeEx XFX;
        public ushort LPC;      // last opcode PC

        public bool INT;
        public bool NMI;
        public bool RST;
        public byte BUS = 0xFF;     // state of free data bus

        private ICpuBus _bus;

        public Z80Cpu(ICpuBus bus)
        {
            _bus = bus;

            _pairGetters = Enumerable
                .Range(0, 4).Select(Regs.CreatePairGetter)
                .ToArray();
            _pairSetters = Enumerable
                .Range(0, 4).Select(Regs.CreatePairSetter)
                .ToArray();
            _regGetters = Enumerable
                .Range(0, 8).Select(Regs.CreateRegGetter)
                .ToArray();
            _regSetters = Enumerable
                .Range(0, 8).Select(Regs.CreateRegSetter)
                .ToArray();
            _alualg = CreateAluAlg();
            _opcodes = CreateOpcodes();
            _opcodesFx = CreateOpcodesFx();
            _opcodesEd = CreateOpcodesEd();
            _opcodesCb = CreateOpcodesCb();
            _opcodesFxCb = CreateOpcodesFxCb();

            Regs.AF = 0xFF;
            Regs.BC = 0xFF;
            Regs.DE = 0xFF;
            Regs.HL = 0xFF;
            Regs._AF = 0xFF;
            Regs._BC = 0xFF;
            Regs._DE = 0xFF;
            Regs._HL = 0xFF;
            Regs.IX = 0xFF;
            Regs.IY = 0xFF;
            Regs.IR = 0xFF;
            Regs.PC = 0xFF;
            Regs.SP = 0xFF;
            Regs.MW = 0xFF;
        }


        public void ExecCycle()
        {
            byte cmd;
            if (XFX == CpuModeEx.None && FX == CpuModeIndex.None)
            {
                if (ProcessSignals())
                    return;
                LPC = Regs.PC;
                cmd = _bus.RdMemM1(LPC);
            }
            else
            {
                if (ProcessSignals())
                    return;
                cmd = _bus.RdMem(Regs.PC);
            }
            Regs.PC++;
            switch (XFX)
            {
                case CpuModeEx.Cb:
                {
                    BINT = false;
                    if (FX != CpuModeIndex.None)
                    {
                        // elapsed T: 4, 4, 3
                        // will be T: 4, 4, 3, 5
                        int drel = (sbyte)cmd;

                        Regs.MW = FX == CpuModeIndex.Ix ? (ushort)(Regs.IX + drel) : (ushort)(Regs.IY + drel);
                        cmd = _bus.RdMem(Regs.PC);
                        _bus.RdNoMREQ(Regs.PC, 2);

                        Regs.PC++;
                        _opcodesFxCb[cmd](cmd, Regs.MW);
                    }
                    else
                    {
                        //Refresh();
                        Regs.R = (byte) (((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                        _bus.RdNoMREQ(Regs.R);
                            RzxCounter++;

                        _opcodesCb[cmd](cmd);
                    }
                    XFX = CpuModeEx.None;
                    FX = CpuModeIndex.None;
                    break;
                }
                case CpuModeEx.Ed:
                {
                    //Refresh();
                    Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                    _bus.RdNoMREQ(Regs.R);
                    RzxCounter++;

                    BINT = false;
                    var edop = _opcodesEd[cmd];
                    edop?.Invoke(cmd);
                    XFX = CpuModeEx.None;
                    FX = CpuModeIndex.None;
                    break;
                }
                default:
                {
                    switch (cmd)
                    {
                        case 0xDD:
                            //Refresh();
                            Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                            _bus.RdNoMREQ(Regs.R);
                            RzxCounter++;

                            FX = CpuModeIndex.Ix;
                            BINT = true;
                            break;
                        case 0xFD:
                            //Refresh();
                            Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                            _bus.RdNoMREQ(Regs.R);
                                RzxCounter++;

                            FX = CpuModeIndex.Iy;
                            BINT = true;
                            break;
                        case 0xCB:
                            //Refresh();
                            Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                            _bus.RdNoMREQ(Regs.R);
                                RzxCounter++;

                            XFX = CpuModeEx.Cb;
                            BINT = true;
                            break;
                        case 0xED:
                            //Refresh();
                            Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                            _bus.RdNoMREQ(Regs.R);
                                RzxCounter++;

                            XFX = CpuModeEx.Ed;
                            BINT = true;
                            break;
                        default:
                        {
                            //Refresh();
                            Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                            _bus.RdNoMREQ(Regs.R);
                                    RzxCounter++;

                            BINT = false;
                            var opdo = FX == CpuModeIndex.None ? _opcodes[cmd] : _opcodesFx[cmd];
                            opdo?.Invoke(cmd);
                            FX = CpuModeIndex.None;
                            break;
                        }
                    }

                    break;
                }
            }
        }

        private bool ProcessSignals()
        {
            if (RST)    // RESET
            {
                // 3T
                _bus.Reset();
                //Refresh();      //+1T
                Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                _bus.RdNoMREQ(Regs.R);
                RzxCounter++;


                FX = CpuModeIndex.None;
                XFX = CpuModeEx.None;
                HALTED = false;

                IFF1 = false;
                IFF2 = false;
                Regs.PC = 0;
                Regs.IR = 0;
                IM = 0;
                //regs.SP = 0xFFFF;
                //regs.AF = 0xFFFF;

                _bus.RdNoMREQ(Regs.PC);      // total should be 3T?
                return true;
            }
            else if (NMI)
            {
                // 11T (5, 3, 3)

                if (HALTED) // workaround for Z80 snapshot halt issue + comfortable debugging
                    Regs.PC++;

                // M1
                _bus.NmiAckM1(4);
                //Refresh();
                Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                _bus.RdNoMREQ(Regs.R);
                RzxCounter++;


                IFF2 = IFF1;
                IFF1 = false;
                HALTED = false;
                Regs.SP--;

                // M2
                _bus.WrMem(Regs.SP, (byte)(Regs.PC >> 8));
                Regs.SP--;

                // M3
                _bus.WrMem(Regs.SP, (byte)Regs.PC);
                Regs.PC = 0x0066;

                return true;
            }
            else if (INT && (!BINT) && IFF1)
            {
                // http://www.z80.info/interrup.htm
                // IM0: 13T (7,3,3) [RST]
                // IM1: 13T (7,3,3)
                // IM2: 19T (7,3,3,3,3)

                if (HALTED) // workaround for Z80 snapshot halt issue + comfortable debugging
                    Regs.PC++;


                _bus.IntAckM1(6);
                // M1: 7T = interrupt acknowledgement; SP--
                Regs.SP--;
                //if (HALTED) ??
                //    Tact += 2;
                //Refresh();
                //RzxCounter--;	// fix because INTAK should not be calculated
                Regs.R = (byte)(((Regs.R + 1) & 0x7F) | (Regs.R & 0x80));
                _bus.RdNoMREQ(Regs.R);


                IFF1 = false;
                IFF2 = false; // proof?
                HALTED = false;

                // M2
                _bus.WrMem(Regs.SP, (byte)(Regs.PC >> 8));   // M2: 3T write PCH; SP--
                Regs.SP--;

                // M3
                _bus.WrMem(Regs.SP, (byte)Regs.PC); // M3: 3T write PCL

                if (IM == 0)        // IM 0: execute instruction taken from BUS with timing T+2???
                {
                    Regs.MW = 0x0038; // workaround: just execute #FF
                }
                else if (IM == 1)   // IM 1: execute #FF with timing T+2 (11+2=13T)
                {
                    Regs.MW = 0x0038;
                }
                else                // IM 2: VH=reg.I; VL=BUS; PC=[V]
                {
                    // M4
                    var adr = (ushort)((Regs.IR & 0xFF00) | BUS);
                    Regs.MW = _bus.RdMem(adr);               // M4: 3T read VL

                    // M5
                    Regs.MW += (ushort)(_bus.RdMem(++adr) * 0x100);   // M5: 3T read VH, PC=V
                }
                Regs.PC = Regs.MW;

                return true;
            }
            return false;
        }
    }
}