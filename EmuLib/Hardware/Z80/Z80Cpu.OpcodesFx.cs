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
 *  Description: Z80 CPU Emulator [DD/FD prefixed opcode part]
 *  Date: 13.04.2007
 * 
 */

using System;

namespace EmuLib.Hardware.Z80
{
    public partial class Z80Cpu
    {
        #region FXxx ops...

        private void FX_LDSPHL(byte cmd)       // LD SP,IX 
        {
            _bus.RdNoMREQ(Regs.IR, 2);
            Regs.SP = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
        }

        private void FX_EX_SP_HL(byte cmd)     // EX (SP),IX
        {
            // 23T (4, 4, 3, 4, 3, 5)
            
            var tmpsp = Regs.SP;
            Regs.MW = _bus.RdMem(tmpsp);
            tmpsp++;

            Regs.MW += (ushort)(_bus.RdMem(tmpsp) * 0x100);
            _bus.RdNoMREQ(tmpsp);

            if (FX == CpuModeIndex.Ix)
            {
                _bus.WrMem(tmpsp, Regs.XH);
                tmpsp--;

                _bus.WrMem(tmpsp, Regs.XL);
                _bus.WrNoMREQ(tmpsp, 2);
                Regs.IX = Regs.MW;
            }
            else
            {
                _bus.WrMem(tmpsp, Regs.YH);
                tmpsp--;

                _bus.WrMem(tmpsp, Regs.YL);
                _bus.WrNoMREQ(tmpsp, 2);
                Regs.IY = Regs.MW;
            }
        }

        private void FX_JP_HL_(byte cmd)       // JP (IX) 
        {
            if (FX == CpuModeIndex.Ix)
                Regs.PC = Regs.IX;
            else
                Regs.PC = Regs.IY;
        }

        private void FX_PUSHIX(byte cmd)       // PUSH IX
        {
            // 15 (4, 5, 3, 3)

            _bus.RdNoMREQ(Regs.IR);
            var val = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
            Regs.SP--;

            _bus.WrMem(Regs.SP, (byte)(val >> 8));
            Regs.SP--;

            _bus.WrMem(Regs.SP, (byte)val);
        }

        private void FX_POPIX(byte cmd)        // POP IX
        {
            // 14T (4, 4, 3, 3)

            var val = (ushort)_bus.RdMem(Regs.SP);
            Regs.SP++;

            val |= (ushort)(_bus.RdMem(Regs.SP) << 8);
            Regs.SP++;
            if (FX == CpuModeIndex.Ix)
                Regs.IX = val;
            else
                Regs.IY = val;
        }

        private void FX_ALUAXH(byte cmd)       // ADD/ADC/SUB/SBC/AND/XOR/OR/CP A,XH
        {
            byte val;
            if (FX == CpuModeIndex.Ix)
                val = (byte)(Regs.IX >> 8);
            else
                val = (byte)(Regs.IY >> 8);
            _alualg[(cmd & 0x38) >> 3](val);
        }

        private void FX_ALUAXL(byte cmd)       // ADD/ADC/SUB/SBC/AND/XOR/OR/CP A,XL
        {
            byte val;
            if (FX == CpuModeIndex.Ix)
                val = (byte)Regs.IX;
            else
                val = (byte)Regs.IY;
            _alualg[(cmd & 0x38) >> 3](val);
        }

        private void FX_ALUA_IX_(byte cmd)     // ADD/ADC/SUB/SBC/AND/XOR/OR/CP A,(IX)
        {
            // 19T (4, 4, 3, 5, 3)
            var op = (cmd & 0x38) >> 3;

            int drel = (sbyte)_bus.RdMem(Regs.PC);

            _bus.RdNoMREQ(Regs.PC, 5);
            
            Regs.PC++;
            Regs.MW = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
            Regs.MW = (ushort)(Regs.MW + drel);

            var val = _bus.RdMem(Regs.MW);
            _alualg[op](val);
        }

        private void FX_ADDIXRR(byte cmd)      // ADD IX,RR
        {
            var rr = (cmd & 0x30) >> 4;
            
            _bus.RdNoMREQ(Regs.IR, 7);
            
            ushort rde;
            switch (rr)
            {
                case 0: rde = Regs.BC; break;
                case 1: rde = Regs.DE; break;
                case 2: rde = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY; break;
                case 3: rde = Regs.SP; break;
                default: throw new ArgumentOutOfRangeException($"(cmd & 0x30) >> 4");
            }
            if (FX == CpuModeIndex.Ix)
            {
                Regs.MW = (ushort)(Regs.IX + 1);
                Regs.IX = ALU_ADDHLRR(Regs.IX, rde);
            }
            else
            {
                Regs.MW = (ushort)(Regs.IY + 1);
                Regs.IY = ALU_ADDHLRR(Regs.IY, rde);
            }
        }

        private void FX_DECIX(byte cmd)        // DEC IX
        {
            _bus.RdNoMREQ(Regs.IR, 2);
            if (FX == CpuModeIndex.Ix)
                Regs.IX--;
            else
                Regs.IY--;
        }

        private void FX_INCIX(byte cmd)        // INC IX
        {
            _bus.RdNoMREQ(Regs.IR, 2);
            if (FX == CpuModeIndex.Ix)
                Regs.IX++;
            else
                Regs.IY++;
        }

        private void FX_LDIX_N_(byte cmd)      // LD IX,(nnnn)
        {
            // 20 (4, 4, 3, 3, 3, 3)

            var adr = (ushort)_bus.RdMem(Regs.PC);
            Regs.PC++;

            adr += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;
            Regs.MW = (ushort)(adr + 1);

            var val = (ushort)_bus.RdMem(adr);

            val += (ushort)(_bus.RdMem(Regs.MW) * 0x100);
            if (FX == CpuModeIndex.Ix)
                Regs.IX = val;
            else
                Regs.IY = val;
        }

        private void FX_LD_NN_IX(byte cmd)     // LD (nnnn),IX
        {
            // 20 (4, 4, 3, 3, 3, 3)
            
            var hl = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
            var adr = (ushort)_bus.RdMem(Regs.PC);
            Regs.PC++;

            adr += (ushort)(_bus.RdMem(Regs.PC) * 0x100);
            Regs.PC++;
            Regs.MW = (ushort)(adr + 1);

            _bus.WrMem(adr, (byte) hl);
            _bus.WrMem(Regs.MW, (byte)(hl >> 8));
        }

        private void FX_LDIXNNNN(byte cmd)     // LD IX,nnnn
        {
            // 14 (4, 4, 3, 3)

            var val = (ushort)_bus.RdMem(Regs.PC);
            Regs.PC++;

            val |= (ushort)(_bus.RdMem(Regs.PC) << 8);
            Regs.PC++;
            if (FX == CpuModeIndex.Ix)
                Regs.IX = val;
            else
                Regs.IY = val;
        }

        private void FX_DEC_IX_(byte cmd)      // DEC (IX)
        {
            // 23T (4, 4, 3, 5, 4, 3)

            int drel = (sbyte)_bus.RdMem(Regs.PC);

            _bus.RdNoMREQ(Regs.PC, 5);
            
            Regs.PC++;
            Regs.MW = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
            Regs.MW = (ushort)(Regs.MW + drel);

            var val = _bus.RdMem(Regs.MW);
            _bus.RdNoMREQ(Regs.MW);
            val = ALU_DECR(val);
            _bus.WrMem(Regs.MW, val);
        }

        private void FX_INC_IX_(byte cmd)      // INC (IX)
        {
            //23T (4, 4, 3, 5, 4, 3)

            int drel = (sbyte)_bus.RdMem(Regs.PC);
            
            _bus.RdNoMREQ(Regs.PC, 5);

            Regs.PC++;
            Regs.MW = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
            Regs.MW = (ushort)(Regs.MW + drel);

            var val = _bus.RdMem(Regs.MW);
            _bus.RdNoMREQ(Regs.MW);
            val = ALU_INCR(val);
            _bus.WrMem(Regs.MW, val);
        }

        private void FX_LD_IX_NN(byte cmd)     // LD (IX),nn
        {
            // 19 (4, 4, 3, 5, 3)

            int drel = (sbyte)_bus.RdMem(Regs.PC);
            Regs.PC++;

            var val = _bus.RdMem(Regs.PC);
            _bus.RdNoMREQ(Regs.PC, 2);
            Regs.PC++;
            Regs.MW = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
            Regs.MW = (ushort)(Regs.MW + drel);
            _bus.WrMem(Regs.MW, val);
        }

        private void FX_LD_IX_R(byte cmd)      // LD (IX),R
        {
            // 19T (4, 4, 3, 5, 3)
            var r = cmd & 0x07;

            int drel = (sbyte)_bus.RdMem(Regs.PC);

            _bus.RdNoMREQ(Regs.PC, 5);

            Regs.PC++;
            Regs.MW = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
            Regs.MW = (ushort)(Regs.MW + drel);
            _bus.WrMem(Regs.MW, _regGetters[r]());
        }

        private void FX_LDR_IX_(byte cmd)      // LD R,(IX)
        {
            // 19T (4, 4, 3, 5, 3)
            var r = (cmd & 0x38) >> 3;

            int drel = (sbyte)_bus.RdMem(Regs.PC);
            
            _bus.RdNoMREQ(Regs.PC, 5);
            
            Regs.PC++;
            Regs.MW = FX == CpuModeIndex.Ix ? Regs.IX : Regs.IY;
            Regs.MW = (ushort)(Regs.MW + drel);

            _regSetters[r](_bus.RdMem(Regs.MW));
        }

        #endregion

        #region RdRs...

        private void FX_LDHL(byte cmd)
        {
            if (FX == CpuModeIndex.Ix)
                Regs.XH = Regs.XL;
            else
                Regs.YH = Regs.YL;
        }

        private void FX_LDLH(byte cmd)
        {
            if (FX == CpuModeIndex.Ix)
                Regs.XL = Regs.XH;
            else
                Regs.YL = Regs.YH;
        }

        private void FX_LDRL(byte cmd)
        {
            var r = (cmd & 0x38) >> 3;

            if (FX == CpuModeIndex.Ix)
                _regSetters[r](Regs.XL);
            else
                _regSetters[r](Regs.YL);
        }

        private void FX_LDRH(byte cmd)
        {
            var r = (cmd & 0x38) >> 3;
            
            if (FX == CpuModeIndex.Ix)
                _regSetters[r](Regs.XH);
            else
                _regSetters[r](Regs.YH);
        }

        private void FX_LDLR(byte cmd)
        {
            var r = cmd & 0x07;
            
            if (FX == CpuModeIndex.Ix)
                Regs.XL = _regGetters[r]();
            else
                Regs.YL = _regGetters[r]();
        }

        private void FX_LDHR(byte cmd)
        {
            var r = cmd & 0x07;
            
            if (FX == CpuModeIndex.Ix)
                Regs.XH = _regGetters[r]();
            else
                Regs.YH = _regGetters[r]();
        }

        private void FX_LDLNN(byte cmd)     // LD XL,nn
        {
            // 11T (4, 4, 3)
            
            if (FX == CpuModeIndex.Ix)
                Regs.XL = _bus.RdMem(Regs.PC);
            else
                Regs.YL = _bus.RdMem(Regs.PC);
            Regs.PC++;
        }

        private void FX_LDHNN(byte cmd)     // LD XH,nn
        {
            // 11T (4, 4, 3)
            
            if (FX == CpuModeIndex.Ix)
                Regs.XH = _bus.RdMem(Regs.PC);
            else
                Regs.YH = _bus.RdMem(Regs.PC);
            Regs.PC++;
        }

        private void FX_INCL(byte cmd)      // INC XL
        {
            if (FX == CpuModeIndex.Ix)
                Regs.XL = ALU_INCR(Regs.XL);
            else
                Regs.YL = ALU_INCR(Regs.YL);
        }

        private void FX_INCH(byte cmd)      // INC XH
        {
            if (FX == CpuModeIndex.Ix)
                Regs.XH = ALU_INCR(Regs.XH);
            else
                Regs.YH = ALU_INCR(Regs.YH);
        }

        private void FX_DECL(byte cmd)      // DEC XL
        {
            if (FX == CpuModeIndex.Ix)
                Regs.XL = ALU_DECR(Regs.XL);
            else
                Regs.YL = ALU_DECR(Regs.YL);
        }

        private void FX_DECH(byte cmd)      // DEC XH
        {
            if (FX == CpuModeIndex.Ix)
                Regs.XH = ALU_DECR(Regs.XH);
            else
                Regs.YH = ALU_DECR(Regs.YH);
        }

        #endregion
    }
}
