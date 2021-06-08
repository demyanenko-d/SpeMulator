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
 *  Description: Z80 CPU Emulator [ED prefixed opcode part]
 *  Date: 13.04.2007
 * 
 */


namespace EmuLib.Hardware.Z80
{
    public partial class Z80Cpu
    {
        private void ED_LDI(byte cmd)
        {
            // 16T (4, 4, 3, 5)

            var val = _bus.RdMem(Regs.HL);

            _bus.WrMem(Regs.DE, val);
            _bus.WrNoMREQ(Regs.DE, 2);

            Regs.HL++;
            Regs.DE++;
            Regs.BC--;
            val += Regs.A;

            Regs.F = (byte)((Regs.F & CpuFlags.NotHPNF3F5) |
                (val & CpuFlags.F3) | ((val << 4) & CpuFlags.F5));
            if (Regs.BC != 0) Regs.F |= CpuFlags.P;
        }

        private void ED_CPI(byte cmd)
        {
            // 16T (4, 4, 3, 5)

            var cf = Regs.F & CpuFlags.C;
            var val = _bus.RdMem(Regs.HL);
            _bus.RdNoMREQ(Regs.HL, 5);

            Regs.HL++;
            Regs.F = (byte)(CpuTables.Cpf8b[Regs.A * 0x100 + val] + cf);
            if (--Regs.BC != 0) Regs.F |= CpuFlags.P;
            Regs.MW++;
        }

        private void ED_INI(byte cmd)   // INI [16T]
        {
            // 16T (4, 5, 4, 3)

            _bus.RdNoMREQ(Regs.IR);

            var val = _bus.RdPort(Regs.BC);

            _bus.WrMem(Regs.HL, val);

            Regs.MW = (ushort)(Regs.BC + 1);
            Regs.HL++;
            Regs.B--;

            //FUSE
            byte flgtmp = (byte)(val + Regs.C + 1);
            Regs.F = (byte)(CpuTables.Logf[Regs.B] & CpuFlags.NotP);
            if ((CpuTables.Logf[(flgtmp & 0x07) ^ Regs.B] & CpuFlags.P) != 0) Regs.F |= CpuFlags.P;
            if (flgtmp < val) Regs.F |= CpuFlags.HC;
            if ((val & 0x80) != 0) Regs.F |= CpuFlags.N;
        }

        private void ED_OUTI(byte cmd)  // OUTI [16T]
        {
            // 16 (4, 5, 3, 4)

            _bus.RdNoMREQ(Regs.IR);
            Regs.B--;

            var val = _bus.RdMem(Regs.HL);

            _bus.WrPort(Regs.BC, val);

            Regs.MW = (ushort)(Regs.BC + 1);
            Regs.HL++;

            //FUSE
            byte flgtmp = (byte)(val + Regs.L);
            Regs.F = (byte)(CpuTables.Logf[Regs.B] & CpuFlags.NotP);
            if ((CpuTables.Logf[(flgtmp & 0x07) ^ Regs.B] & CpuFlags.P) != 0) Regs.F |= CpuFlags.P;
            if (flgtmp < val) Regs.F |= CpuFlags.HC;
            if ((val & 0x80) != 0) Regs.F |= CpuFlags.N;
        }

        private void ED_LDD(byte cmd)
        {
            // 16T (4, 4, 3, 5)

            var val = _bus.RdMem(Regs.HL);

            _bus.WrMem(Regs.DE, val);
            _bus.WrNoMREQ(Regs.DE, 2);

            Regs.HL--;
            Regs.DE--;
            Regs.BC--;
            val += Regs.A;

            Regs.F = (byte)((Regs.F & CpuFlags.NotHPNF3F5) |
                (val & CpuFlags.F3) | ((val << 4) & CpuFlags.F5));
            if (Regs.BC != 0) Regs.F |= CpuFlags.P;
        }

        private void ED_CPD(byte cmd)
        {
            // 16T (4, 4, 3, 5)

            var cf = Regs.F & CpuFlags.C;
            var val = _bus.RdMem(Regs.HL);
            _bus.RdNoMREQ(Regs.HL, 5);

            Regs.HL--;
            Regs.BC--;
            Regs.MW--;
            Regs.F = (byte)(CpuTables.Cpf8b[Regs.A * 0x100 + val] + cf);
            if (Regs.BC != 0) Regs.F |= CpuFlags.P;
        }

        private void ED_IND(byte cmd)   // IND [16T]
        {
            // 16T (4, 5, 4, 3)

            _bus.RdNoMREQ(Regs.IR);

            var val = _bus.RdPort(Regs.BC);

            _bus.WrMem(Regs.HL, val);

            Regs.MW = (ushort)(Regs.BC - 1);
            Regs.HL--;
            Regs.B--;

            //FUSE
            byte flgtmp = (byte)(val + Regs.C - 1);
            Regs.F = (byte)(CpuTables.Logf[Regs.B] & CpuFlags.NotP);
            if ((CpuTables.Logf[(flgtmp & 0x07) ^ Regs.B] & CpuFlags.P) != 0) Regs.F |= CpuFlags.P;
            if (flgtmp < val) Regs.F |= CpuFlags.HC;
            if ((val & 0x80) != 0) Regs.F |= CpuFlags.N;
        }

        private void ED_OUTD(byte cmd)  // OUTD [16T]
        {
            // 16T (4, 5, 3, 4)

            _bus.RdNoMREQ(Regs.IR);

            Regs.B--;
            var val = _bus.RdMem(Regs.HL);

            _bus.WrPort(Regs.BC, val);

            Regs.MW = (ushort)(Regs.BC - 1);
            Regs.HL--;

            //FUSE
            byte flgtmp = (byte)(val + Regs.L);
            Regs.F = (byte)(CpuTables.Logf[Regs.B] & CpuFlags.NotP);
            if ((CpuTables.Logf[(flgtmp & 0x07) ^ Regs.B] & CpuFlags.P) != 0) Regs.F |= CpuFlags.P;
            if (flgtmp < val) Regs.F |= CpuFlags.HC;
            if ((val & 0x80) != 0) Regs.F |= CpuFlags.N;
        }

        private void ED_LDIR(byte cmd)
        {
            //BC==0 => 16T (4, 4, 3, 5)
            //BC!=0 => 21T (4, 4, 3, 5, 5)

            var val = _bus.RdMem(Regs.HL);

            _bus.WrMem(Regs.DE, val);
            _bus.WrNoMREQ(Regs.DE, 2);

            Regs.BC--;
            val += Regs.A;

            Regs.F = (byte)((Regs.F & CpuFlags.NotHPNF3F5) |
                (val & CpuFlags.F3) | ((val << 4) & CpuFlags.F5));
            if (Regs.BC != 0)
            {
                _bus.WrNoMREQ(Regs.DE, 5);
                Regs.PC--;
                Regs.MW = Regs.PC;
                Regs.PC--;
                Regs.F |= CpuFlags.P;
            }
            Regs.HL++;
            Regs.DE++;
        }

        private void ED_CPIR(byte cmd)
        {
            //BC==0 => 16T (4, 4, 3, 5)
            //BC!=0 => 21T (4, 4, 3, 5, 5)

            Regs.MW++;
            var cf = Regs.F & CpuFlags.C;
            var val = _bus.RdMem(Regs.HL);
            _bus.RdNoMREQ(Regs.HL, 5);

            Regs.BC--;
            Regs.F = (byte)(CpuTables.Cpf8b[Regs.A * 0x100 + val] + cf);

            if (Regs.BC != 0)
            {
                Regs.F |= CpuFlags.P;
                if ((Regs.F & CpuFlags.Z) == 0)
                {
                    _bus.RdNoMREQ(Regs.HL, 5);
                    Regs.PC--;
                    Regs.MW = Regs.PC;
                    Regs.PC--;
                }
            }
            Regs.HL++;
        }

        private void ED_INIR(byte cmd)      // INIR [16T/21T]
        {
            // B==0 => 16T (4, 5, 4, 3)
            // B!=0 => 21T (4, 5, 4, 3, 5)

            _bus.RdNoMREQ(Regs.IR);

            Regs.MW = (ushort)(Regs.BC + 1);
            var val = _bus.RdPort(Regs.BC);

            _bus.WrMem(Regs.HL, val);
            Regs.B = ALU_DECR(Regs.B);

            if (Regs.B != 0)
            {
                _bus.WrNoMREQ(Regs.HL, 5);
                Regs.PC -= 2;
                Regs.F |= CpuFlags.P;
            }
            else Regs.F &= CpuFlags.NotP;
            Regs.HL++;
        }

        private void ED_OTIR(byte cmd)  // OTIR [16T/21T]
        {
            // B==0 => 16T (4, 5, 3, 4)
            // B!=0 => 21T (4, 5, 3, 4, 5)

            _bus.RdNoMREQ(Regs.IR);

            Regs.B = ALU_DECR(Regs.B);
            var val = _bus.RdMem(Regs.HL);

            _bus.WrPort(Regs.BC, val);

            Regs.HL++;
            if (Regs.B != 0)
            {
                _bus.RdNoMREQ(Regs.BC, 5);
                Regs.PC -= 2;
                Regs.F |= CpuFlags.P;
            }
            else Regs.F &= CpuFlags.NotP;
            Regs.F &= CpuFlags.NotC;
            if (Regs.L == 0) Regs.F |= CpuFlags.C;
            Regs.MW = (ushort)(Regs.BC + 1);
        }

        private void ED_LDDR(byte cmd)
        {
            //BC==0 => 16T (4, 4, 3, 5)
            //BC!=0 => 21T (4, 4, 3, 5, 5)

            var val = _bus.RdMem(Regs.HL);

            _bus.WrMem(Regs.DE, val);
            _bus.WrNoMREQ(Regs.DE, 2);

            Regs.BC--;
            val += Regs.A;

            Regs.F = (byte)((Regs.F & CpuFlags.NotHPNF3F5) |
                (val & CpuFlags.F3) | ((val << 4) & CpuFlags.F5));
            if (Regs.BC != 0)
            {
                _bus.WrNoMREQ(Regs.DE, 5);
                Regs.PC--;
                Regs.MW = Regs.PC;
                Regs.PC--;
                Regs.F |= CpuFlags.P;
            }
            Regs.HL--;
            Regs.DE--;
        }

        private void ED_CPDR(byte cmd)
        {
            // BC==0 => 16T (4, 4, 3, 5)
            // BC!=0 => 21T (4, 4, 3, 5, 5)

            Regs.MW--;
            var cf = Regs.F & CpuFlags.C;
            var val = _bus.RdMem(Regs.HL);

            _bus.RdNoMREQ(Regs.HL, 5);
            Regs.BC--;
            Regs.F = (byte)(CpuTables.Cpf8b[Regs.A * 0x100 + val] + cf);

            if (Regs.BC != 0)
            {
                Regs.F |= CpuFlags.P;
                if ((Regs.F & CpuFlags.Z) == 0)
                {
                    _bus.RdNoMREQ(Regs.HL, 5);
                    Regs.PC--;
                    Regs.MW = Regs.PC;
                    Regs.PC--;
                }
            }
            Regs.HL--;
        }

        private void ED_INDR(byte cmd)      // INDR [16T/21T]
        {
            // B==0 => 16 (4, 5, 4, 3)
            // B!=0 => 21 (4, 5, 4, 3, 5)

            _bus.RdNoMREQ(Regs.IR);

            Regs.MW = (ushort)(Regs.BC - 1);
            var val = _bus.RdPort(Regs.BC);

            _bus.WrMem(Regs.HL, val);

            Regs.B = ALU_DECR(Regs.B);

            if (Regs.B != 0)
            {
                _bus.WrNoMREQ(Regs.HL, 5);
                Regs.PC -= 2;
                Regs.F |= CpuFlags.P;
            }
            else Regs.F &= CpuFlags.NotP;
            Regs.HL--;
        }

        private void ED_OTDR(byte cmd)  //OTDR [16T/21T]
        {
            // B==0 => 16T (4, 5, 3, 4)
            // B!=0 => 21T (4, 5, 3, 4, 5)

            _bus.RdNoMREQ(Regs.IR);

            var val = _bus.RdMem(Regs.HL);
            Regs.B = ALU_DECR(Regs.B);

            _bus.WrPort(Regs.BC, val);

            if (Regs.B != 0)
            {
                _bus.RdNoMREQ(Regs.BC, 5);
                Regs.PC -= 2;
                Regs.F |= CpuFlags.P;
            }
            else Regs.F &= CpuFlags.NotP;
            Regs.F &= CpuFlags.NotC;
            if (Regs.L == 0xFF) Regs.F |= CpuFlags.C;
            Regs.MW = (ushort)(Regs.BC - 1);
            Regs.HL--;
        }

        private void ED_INRC(byte cmd)      // in R,(c)  [12T] 
        {
            // 12T (4, 4, 4)
            var r = (cmd & 0x38) >> 3;

            Regs.MW = Regs.BC;
            var pval = _bus.RdPort(Regs.BC);
            Regs.MW++;
            if (r != CpuRegId.F)
                _regSetters[r](pval);
            Regs.F = (byte)(CpuTables.Logf[pval] | (Regs.F & CpuFlags.C));
        }

        private void ED_OUTCR(byte cmd)     // out (c),R [12T]
        {
            // 12T (4, 4, 4)
            var r = (cmd & 0x38) >> 3;

            Regs.MW = Regs.BC;
            if (r != CpuRegId.F)
                _bus.WrPort(Regs.BC, _regGetters[r]());
            else
                _bus.WrPort(Regs.BC, (byte)(CpuType == CpuType.Z80 ? 0x00 : 0xFF));	// 0 for Z80 and 0xFF for Z84
            Regs.MW++;
        }

        private void ED_ADCHLRR(byte cmd)   // adc hl,RR
        {
            var rr = (cmd & 0x30) >> 4;

            _bus.RdNoMREQ(Regs.IR, 7);

            Regs.MW = (ushort)(Regs.HL + 1);
            byte fl = (byte)((((Regs.HL & 0x0FFF) + (_pairGetters[rr]() & 0x0FFF) + (Regs.F & CpuFlags.C)) >> 8) & CpuFlags.H);
            uint tmp = (uint)((Regs.HL & 0xFFFF) + (_pairGetters[rr]() & 0xFFFF) + (Regs.F & CpuFlags.C));  // AF???
            if ((tmp & 0x10000) != 0) fl |= CpuFlags.C;
            if ((tmp & 0xFFFF) == 0) fl |= CpuFlags.Z;
            int ri = (int)(short)Regs.HL + (int)(short)_pairGetters[rr]() + (int)(Regs.F & CpuFlags.C);
            if (ri < -0x8000 || ri >= 0x8000) fl |= CpuFlags.P;
            Regs.HL = (ushort)tmp;
            Regs.F = (byte)(fl | (Regs.H & CpuFlags.SF3F5));
        }

        private void ED_SBCHLRR(byte cmd)   // sbc hl,RR
        {
            var rr = (cmd & 0x30) >> 4;

            _bus.RdNoMREQ(Regs.IR, 7);

            Regs.MW = (ushort)(Regs.HL + 1);
            byte fl = CpuFlags.N;
            fl |= (byte)((((Regs.HL & 0x0FFF) - (_pairGetters[rr]() & 0x0FFF) - (Regs.F & CpuFlags.C)) >> 8) & CpuFlags.H);
            uint tmp = (uint)((Regs.HL & 0xFFFF) - (_pairGetters[rr]() & 0xFFFF) - (Regs.F & CpuFlags.C));  // AF???
            if ((tmp & 0x10000) != 0) fl |= CpuFlags.C;
            if ((tmp & 0xFFFF) == 0) fl |= CpuFlags.Z;
            int ri = (int)(short)Regs.HL - (int)(short)_pairGetters[rr]() - (int)(Regs.F & CpuFlags.C);
            if (ri < -0x8000 || ri >= 0x8000) fl |= CpuFlags.P;
            Regs.HL = (ushort)tmp;
            Regs.F = (byte)(fl | (Regs.H & CpuFlags.SF3F5));
        }

        private void ED_LDRR_NN_(byte cmd)  // ld RR,(NN)
        {
            // 20T (4, 4, 3, 3, 3, 3)
            var rr = (cmd & 0x30) >> 4;

            var adr = (ushort)_bus.RdMem(Regs.PC);
            Regs.PC++;

            adr += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;
            Regs.MW = (ushort)(adr + 1);

            var val = (ushort)_bus.RdMem(adr);

            val += (ushort)(_bus.RdMem(Regs.MW) * 0x100);
            _pairSetters[rr](val);
        }

        private void ED_LD_NN_RR(byte cmd)  // ld (NN),RR
        {
            // 20 (4, 4, 3, 3, 3, 3)
            var rr = (cmd & 0x30) >> 4;

            var adr = (ushort)_bus.RdMem(Regs.PC);
            Regs.PC++;

            adr += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;
            Regs.MW = (ushort)(adr + 1);
            var val = _pairGetters[rr]();

            _bus.WrMem(adr, (byte)val);
            _bus.WrMem(Regs.MW, (byte)(val >> 8));
        }

        private void ED_RETN(byte cmd)      // reti/retn
        {
            // 14T (4, 4, 3, 3)

            IFF1 = IFF2;
            var adr = (ushort)_bus.RdMem(Regs.SP);

            adr += (ushort)(_bus.RdMem(++Regs.SP) * 0x100);
            ++Regs.SP;
            Regs.PC = adr;
            Regs.MW = adr;
        }

        private void ED_IM(byte cmd)        // im X
        {
            var mode = (byte)((cmd & 0x18) >> 3);
            if (mode < 2) mode = 1;
            mode--;

            IM = mode;
        }

        private void ED_LDXRA(byte cmd)     // ld I/R,a
        {
            var ir = (cmd & 0x08) == 0;
            
            _bus.RdNoMREQ(Regs.IR);

            if (ir)   // I
                Regs.I = Regs.A;
            else
                Regs.R = Regs.A;
        }

        private void ED_LDAXR(byte cmd)     // ld a,I/R
        {
            var ir = (cmd & 0x08) == 0;

            _bus.RdNoMREQ(Regs.IR);

            if (ir)   // I
                Regs.A = Regs.I;
            else
                Regs.A = Regs.R;

            Regs.F = (byte)(((Regs.F & CpuFlags.C) | CpuTables.Logf[Regs.A]) & CpuFlags.NotP);

            if (!(INT && IFF1) && IFF2)
            {
                Regs.F |= CpuFlags.P;
            }
        }

        private void ED_RRD(byte cmd)       // RRD
        {
            // 18T (4, 4, 3, 4, 3)

            var tmp = _bus.RdMem(Regs.HL);

            _bus.RdNoMREQ(Regs.HL, 4);

            Regs.MW = (ushort)(Regs.HL + 1);
            var val = (byte)((Regs.A << 4) | (tmp >> 4));

            _bus.WrMem(Regs.HL, val);
            Regs.A = (byte)((Regs.A & 0xF0) | (tmp & 0x0F));
            Regs.F = (byte)(CpuTables.Logf[Regs.A] | (Regs.F & CpuFlags.C));
        }

        private void ED_RLD(byte cmd)       // RLD
        {
            // 18T (4, 4, 3, 4, 3)

            var tmp = _bus.RdMem(Regs.HL);

            _bus.RdNoMREQ(Regs.HL, 4);

            Regs.MW = (ushort)(Regs.HL + 1);
            var val = (byte)((Regs.A & 0x0F) | (tmp << 4));

            _bus.WrMem(Regs.HL, val);
            Regs.A = (byte)((Regs.A & 0xF0) | (tmp >> 4));
            Regs.F = (byte)(CpuTables.Logf[Regs.A] | (Regs.F & CpuFlags.C));
        }

        private void ED_NEG(byte cmd)       // NEG
        {
            Regs.F = CpuTables.Sbcf[Regs.A];
            Regs.A = (byte)-Regs.A;
        }
    }
}
