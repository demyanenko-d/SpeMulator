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
 *  Description: Z80 CPU Emulator [direct opcode part]
 *  Date: 13.04.2007
 * 
 */


namespace EmuLib.Hardware.Z80
{
    public partial class Z80Cpu
    {
        #region direct/DD/FD

        private void INA_NN_(byte cmd)      // IN A,(N) [11T] 
        {
            // 11T (4, 3, 4)

            Regs.MW = _bus.RdMem(Regs.PC++);
            Regs.MW += (ushort)(Regs.A << 8);

            Regs.A = _bus.RdPort(Regs.MW);
            Regs.MW++;
        }

        private void OUT_NN_A(byte cmd)     // OUT (N),A [11T]+ 
        {
            // 11T (4, 3, 4)

            Regs.MW = _bus.RdMem(Regs.PC++);
            Regs.MW += (ushort)(Regs.A << 8);

            _bus.WrPort(Regs.MW, Regs.A);
            Regs.ML++;
        }

        private void DI(byte cmd)
        {
            IFF1 = false;
            IFF2 = false;
        }

        private void EI(byte cmd)
        {
            IFF1 = true;
            IFF2 = true;
            BINT = true;
        }

        private void LDSPHL(byte cmd)       // LD SP,HL 
        {
            _bus.RdNoMREQ(Regs.IR, 2);
            Regs.SP = Regs.HL;
        }

        private void EX_SP_HL(byte cmd)     // EX (SP),HL
        {
            // 19T (4, 3, 4, 3, 5)

            ushort tmpsp = Regs.SP;
            Regs.MW = _bus.RdMem(tmpsp);
            tmpsp++;

            Regs.MW += (ushort)(_bus.RdMem(tmpsp) * 0x100);
            _bus.RdNoMREQ(tmpsp);

            _bus.WrMem(tmpsp, Regs.H);
            tmpsp--;

            _bus.WrMem(tmpsp, Regs.L);
            _bus.WrNoMREQ(tmpsp, 2);
            Regs.HL = Regs.MW;
        }

        private void JP_HL_(byte cmd)       // JP (HL) 
        {
            Regs.PC = Regs.HL;
        }

        private void EXDEHL(byte cmd)       // EX DE,HL 
        {
            ushort tmp;
            tmp = Regs.HL;      // ix префикс не действует!
            Regs.HL = Regs.DE;
            Regs.DE = tmp;
        }

        private void EXAFAF(byte cmd)       // EX AF,AF' 
        {
            var tmp = Regs.AF;
            Regs.AF = Regs._AF;
            Regs._AF = tmp;
        }

        private void EXX(byte cmd)          // EXX 
        {
            var tmp = Regs.BC;
            Regs.BC = Regs._BC;
            Regs._BC = tmp;
            tmp = Regs.DE;
            Regs.DE = Regs._DE;
            Regs._DE = tmp;
            tmp = Regs.HL;
            Regs.HL = Regs._HL;
            Regs._HL = tmp;
        }

        #endregion


        #region logical

        private void RLCA(byte cmd)
        {
            Regs.F = (byte)(CpuTables.Rlcaf[Regs.A] | (Regs.F & CpuFlags.SZP));
            int x = Regs.A;
            x <<= 1;
            if ((x & 0x100) != 0) x |= 0x01;
            Regs.A = (byte)x;
        }

        private void RRCA(byte cmd)
        {
            Regs.F = (byte)(CpuTables.Rrcaf[Regs.A] | (Regs.F & CpuFlags.SZP));
            int x = Regs.A;
            if ((x & 0x01) != 0) x = (x >> 1) | 0x80;
            else x >>= 1;
            Regs.A = (byte)x;
        }

        private void RLA(byte cmd)
        {
            var carry = (Regs.F & CpuFlags.C) != 0;
            Regs.F = (byte)(CpuTables.Rlcaf[Regs.A] | (Regs.F & CpuFlags.SZP)); // use same table with rlca
            Regs.A = (byte)(Regs.A << 1);
            if (carry) Regs.A |= 0x01;
        }

        private void RRA(byte cmd)
        {
            var carry = (Regs.F & CpuFlags.C) != 0;
            Regs.F = (byte)(CpuTables.Rrcaf[Regs.A] | (Regs.F & CpuFlags.SZP)); // use same table with rrca
            Regs.A = (byte)(Regs.A >> 1);
            if (carry) Regs.A |= 0x80;
        }

        private void DAA(byte cmd)
        {
            Regs.AF = CpuTables.Daaf[Regs.A + 0x100 * ((Regs.F & 3) + ((Regs.F >> 2) & 4))];
        }

        private void CPL(byte cmd)
        {
            Regs.A = (byte)~Regs.A;
            Regs.F = (byte)((Regs.F & CpuFlags.NotF3F5) | CpuFlags.HN | (Regs.A & CpuFlags.F3F5));
        }

        private void SCF(byte cmd)
        {
            //regs.F = (byte)((regs.F & (int)~(ZFLAGS.H | ZFLAGS.N)) | (regs.A & (int)(ZFLAGS.F3 | ZFLAGS.F5)) | (int)ZFLAGS.C);
            Regs.F = (byte)((Regs.F & CpuFlags.SZP) |
                (Regs.A & CpuFlags.F3F5) |
                CpuFlags.C);
        }

        private void CCF(byte cmd)
        {
            //regs.F = (byte)(((regs.F & (int)~(ZFLAGS.N | ZFLAGS.H)) | ((regs.F << 4) & (int)ZFLAGS.H) | (regs.A & (int)(ZFLAGS.F3 | ZFLAGS.F5))) ^ (int)ZFLAGS.C);
            Regs.F = (byte)((Regs.F & CpuFlags.SZP) |
                ((Regs.F & CpuFlags.C) != 0 ? CpuFlags.H : CpuFlags.C) | 
                (Regs.A & CpuFlags.F3F5));
        }

        #endregion

        #region jmp/call/ret/jr

        private static readonly byte[] s_conds = new byte[4] 
        { 
            CpuFlags.Z, 
            CpuFlags.C, 
            CpuFlags.P, 
            CpuFlags.S 
        };

        private void DJNZ(byte cmd)      // DJNZ nn
        {
            // B==0 => 8T (5, 3)
            // B!=0 => 13T (5, 3, 5)

            _bus.RdNoMREQ(Regs.IR);

            int drel = (sbyte)_bus.RdMem(Regs.PC);
            Regs.PC++;

            if (--Regs.B != 0)
            {
                _bus.RdNoMREQ(Regs.PC, 5);
                Regs.MW = (ushort)(Regs.PC + drel);
                Regs.PC = Regs.MW;
            }
        }

        private void JRNN(byte cmd)      // JR nn
        {
            // 12T (4, 3, 5)

            int drel = (sbyte)_bus.RdMem(Regs.PC);
            Regs.PC++;

            _bus.RdNoMREQ(Regs.PC, 5);
            Regs.MW = (ushort)(Regs.PC + drel);
            Regs.PC = Regs.MW;
        }

        private void JRXNN(byte cmd)     // JR x,nn
        {
            // false => 7T (4, 3)
            // true  => 12 (4, 3, 5)
            var cond = (cmd & 0x18) >> 3;
            var mask = s_conds[cond >> 1];
            var f = Regs.AF & mask;
            if ((cond & 1) != 0) f ^= mask;

            int drel = (sbyte)_bus.RdMem(Regs.PC);
            Regs.PC++;
            
            if (f == 0)
            {
                _bus.RdNoMREQ(Regs.PC, 5);
                Regs.MW = (ushort)(Regs.PC + drel);
                Regs.PC = Regs.MW;
            }
        }

        private void CALLNNNN(byte cmd)  // CALL
        {
            // 17T (4, 3, 4, 3, 3)

            Regs.MW = _bus.RdMem(Regs.PC);
            Regs.PC++;

            Regs.MW += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            _bus.RdNoMREQ(Regs.PC);
            Regs.PC++;
            Regs.SP--;

            _bus.WrMem(Regs.SP, (byte)(Regs.PC >> 8));
            Regs.SP--;

            _bus.WrMem(Regs.SP, (byte)Regs.PC);
            Regs.PC = Regs.MW;
        }

        private void CALLXNNNN(byte cmd) // CALL x,#nn
        {
            // false => 10T (4, 3, 3)
            // true  => 17T (4, 3, 4, 3, 3)
            var cond = (cmd & 0x38) >> 3;
            var mask = s_conds[cond >> 1];
            var f = Regs.AF & mask;
            if ((cond & 1) != 0) f ^= mask;

            Regs.MW = _bus.RdMem(Regs.PC);
            Regs.PC++;

            Regs.MW += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            if (f == 0)
            {
                _bus.RdNoMREQ(Regs.PC);
            }
            Regs.PC++;

            if (f == 0)
            {
                Regs.SP--;

                _bus.WrMem(Regs.SP, (byte)(Regs.PC >> 8));
                Regs.SP--;

                _bus.WrMem(Regs.SP, (byte)Regs.PC);
                Regs.PC = Regs.MW;
            }
        }

        private void RET(byte cmd)       // RET
        {
            // 10T (4, 3, 3)

            Regs.MW = _bus.RdMem(Regs.SP);
            Regs.SP++;

            Regs.MW += (ushort)(_bus.RdMem(Regs.SP) * 0x100);
            Regs.SP++;
            Regs.PC = Regs.MW;
        }

        private void RETX(byte cmd)      // RET x
        {
            // false => 5T (5)
            // true  => 11T (5, 3, 3)
            var cond = (cmd & 0x38) >> 3;
            var mask = s_conds[cond >> 1];
            var f = Regs.AF & mask;
            if ((cond & 1) != 0) f ^= mask;

            _bus.RdNoMREQ(Regs.IR);

            if (f == 0)
            {
                Regs.MW = _bus.RdMem(Regs.SP);
                Regs.SP++;

                Regs.MW += (ushort)(_bus.RdMem(Regs.SP) * 0x100);
                Regs.SP++;
                Regs.PC = Regs.MW;
            }
        }

        private void JPNNNN(byte cmd)    // JP nnnn
        {
            // 10T (4, 3, 3)

            Regs.MW = _bus.RdMem(Regs.PC);
            Regs.PC++;

            Regs.MW += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC = Regs.MW;
        }

        private void JPXNN(byte cmd)     // JP x,#nn ???
        {
            // 10T (4, 3, 3)
            var cond = (cmd & 0x38) >> 3;
            var mask = s_conds[cond >> 1];
            var f = Regs.AF & mask;
            if ((cond & 1) != 0) f ^= mask;

            Regs.MW = _bus.RdMem(Regs.PC);
            Regs.PC++;

            Regs.MW += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;

            if (f == 0)
                Regs.PC = Regs.MW;
        }

        private void RSTNN(byte cmd)     // RST #nn ?TIME?
        {
            // 11T (5, 3, 3)
            var rst = (ushort)(cmd & 0x38);

            _bus.RdNoMREQ(Regs.IR);
            Regs.SP--;

            _bus.WrMem(Regs.SP, (byte)(Regs.PC >> 8));
            Regs.SP--;

            _bus.WrMem(Regs.SP, (byte)Regs.PC);
            Regs.MW = rst;
            Regs.PC = Regs.MW;
        }

        #endregion

        #region push/pop

        private void PUSHRR(byte cmd)    // PUSH RR ?TIME?
        {
            // 11T (5, 3, 3)
            var rr = (cmd & 0x30) >> 4;

            _bus.RdNoMREQ(Regs.IR);
            ushort val = rr == CpuRegId.Sp ? Regs.AF : _pairGetters[rr]();

            Regs.SP--;
            _bus.WrMem(Regs.SP, (byte)(val >> 8));

            Regs.SP--;
            _bus.WrMem(Regs.SP, (byte)val);
        }

        private void POPRR(byte cmd)     // POP RR
        {
            // 10T (4, 3, 3)
            var rr = (cmd & 0x30) >> 4;

            var val = (ushort)_bus.RdMem(Regs.SP);
            Regs.SP++;

            val |= (ushort)(_bus.RdMem(Regs.SP) << 8);
            Regs.SP++;
            if (rr == CpuRegId.Sp) Regs.AF = val;
            else _pairSetters[rr](val);
        }

        #endregion

        #region ALU

        private void ALUAN(byte cmd)
        {
            // 7T (4, 3)
            var op = (cmd & 0x38) >> 3;

            var val = _bus.RdMem(Regs.PC);
            Regs.PC++;
            _alualg[op](val);
        }

        private void ALUAR(byte cmd)     // ADD/ADC/SUB/SBC/AND/XOR/OR/CP A,R
        {
            var r = cmd & 0x07;
            var op = (cmd & 0x38) >> 3;

            _alualg[op](_regGetters[r]());
        }

        private void ALUA_HL_(byte cmd)     // ADD/ADC/SUB/SBC/AND/XOR/OR/CP A,(HL)
        {
            // 7T (4, 3)
            var op = (cmd & 0x38) >> 3;

            var val = _bus.RdMem(Regs.HL);
            _alualg[op](val);
        }

        private void ADDHLRR(byte cmd)   // ADD HL,RR
        {
            _bus.RdNoMREQ(Regs.IR, 7);
            Regs.MW = (ushort)(Regs.HL + 1);
            Regs.HL = ALU_ADDHLRR(Regs.HL, _pairGetters[(cmd & 0x30) >> 4]());
        }

        #endregion

        #region loads

        private void LDA_RR_(byte cmd)   // LD A,(RR)
        {
            // 7T (4, 3)
            var rr = (cmd & 0x30) >> 4;

            var rrValue = _pairGetters[rr]();
            Regs.A = _bus.RdMem(rrValue);
            Regs.MW = (ushort)(rrValue + 1);
        }

        private void LDA_NN_(byte cmd)   // LD A,(nnnn)
        {
            // 13T (4, 3, 3, 3)

            ushort adr = _bus.RdMem(Regs.PC);
            Regs.PC++;

            adr += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;

            Regs.A = _bus.RdMem(adr);
            Regs.MW = (ushort)(adr + 1);
        }

        private void LDHL_NN_(byte cmd)   // LD HL,(nnnn)
        {
            // 16T (4, 3, 3, 3, 3)

            var adr = (ushort)_bus.RdMem(Regs.PC);
            Regs.PC++;

            adr += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;

            var val = (ushort)_bus.RdMem(adr);
            Regs.MW = (ushort)(adr + 1);

            val += (ushort)(_bus.RdMem(Regs.MW) * 0x100);
            Regs.HL = val;
        }

        private void LD_RR_A(byte cmd)   // LD (RR),A
        {
            // 7T (4, 3)
            var rr = (cmd & 0x30) >> 4;

            var rrValue = _pairGetters[rr]();
            _bus.WrMem(rrValue, Regs.A);
            Regs.MH = Regs.A;
            Regs.ML = (byte)(rrValue + 1);
        }

        private void LD_NN_A(byte cmd)   // LD (nnnn),A
        {
            // 13T (4, 3, 3, 3)

            ushort adr = _bus.RdMem(Regs.PC);
            Regs.PC++;

            adr += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;

            _bus.WrMem(adr, Regs.A);
            Regs.MH = Regs.A;
            Regs.ML = (byte)(adr + 1);
        }

        private void LD_NN_HL(byte cmd)   // LD (nnnn),HL
        {
            // 16T (4, 3, 3, 3, 3)

            ushort adr = _bus.RdMem(Regs.PC);
            Regs.PC++;

            adr += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;

            _bus.WrMem(adr, Regs.L);
            Regs.MW = (ushort)(adr + 1);

            _bus.WrMem(Regs.MW, Regs.H);
        }

        private void LDRRNNNN(byte cmd)  // LD RR,nnnn
        {
            // 10T (4, 3, 3)
            var rr = (cmd & 0x30) >> 4;

            ushort val = _bus.RdMem(Regs.PC);
            Regs.PC++;

            val |= (ushort)(_bus.RdMem(Regs.PC) << 8);
            Regs.PC++;
            _pairSetters[rr](val);
        }

        private void LDRNN(byte cmd)     // LD R,nn
        {
            // 7T (4, 3)
            var r = (cmd & 0x38) >> 3;

            _regSetters[r](_bus.RdMem(Regs.PC));
            Regs.PC++;
        }

        private void LD_HL_NN(byte cmd)     // LD (HL),nn
        {
            // 10T (4, 3, 3)

            var val = _bus.RdMem(Regs.PC);
            Regs.PC++;

            _bus.WrMem(Regs.HL, val);
        }

        private void LDRdRs(byte cmd)     // LD R1,R2
        {
            var rsrc = cmd & 0x07;
            var rdst = (cmd & 0x38) >> 3;
            _regSetters[rdst](_regGetters[rsrc]());
        }

        private void LD_HL_R(byte cmd)    // LD (HL),R
        {
            // 7T (4, 3)
            var r = cmd & 0x07;

            _bus.WrMem(Regs.HL, _regGetters[r]());
        }

        private void LDR_HL_(byte cmd)    // LD R,(HL)
        {
            // 7T (4, 3)
            var r = (cmd & 0x38) >> 3;
            
            _regSetters[r](_bus.RdMem(Regs.HL));
        }

        #endregion

        #region INC/DEC

        private void DECRR(byte cmd)     // DEC RR
        {
            var rr = (cmd & 0x30) >> 4;

            _bus.RdNoMREQ(Regs.IR, 2);
            _pairSetters[rr]((ushort)(_pairGetters[rr]() - 1));
        }

        private void INCRR(byte cmd)     // INC RR
        {
            var rr = (cmd & 0x30) >> 4;

            _bus.RdNoMREQ(Regs.IR, 2);
            _pairSetters[rr]((ushort)(_pairGetters[rr]() + 1));
        }

        private void DECR(byte cmd)      // DEC R
        {
            var r = (cmd & 0x38) >> 3;

            _regSetters[r](ALU_DECR(_regGetters[r]()));
        }

        private void INCR(byte cmd)      // INC R
        {
            var r = (cmd & 0x38) >> 3;

            _regSetters[r](ALU_INCR(_regGetters[r]()));
        }

        private void DEC_HL_(byte cmd)      // DEC (HL)
        {
            // 11T (4, 4, 3)

            var val = _bus.RdMem(Regs.HL);
            _bus.RdNoMREQ(Regs.HL);
            val = ALU_DECR(val);

            _bus.WrMem(Regs.HL, val);
        }

        private void INC_HL_(byte cmd)      // INC (HL)
        {
            // 11T (4, 4, 3)

            var val = _bus.RdMem(Regs.HL);
            _bus.RdNoMREQ(Regs.HL);
            val = ALU_INCR(val);

            _bus.WrMem(Regs.HL, val);
        }

        #endregion

        private void HALT(byte cmd)
        {
            HALTED = true;
            Regs.PC--;      // workaround for Z80 snapshot halt issue + comfortable debugging
        }
    }
}
