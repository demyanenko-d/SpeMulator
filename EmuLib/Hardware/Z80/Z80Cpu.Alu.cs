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
 *  Description: Z80 CPU Emulator [ALU part]
 *  Date: 13.04.2007
 * 
 */


namespace EmuLib.Hardware.Z80
{
    public partial class Z80Cpu
    {
        #region ALU

        private void ALU_ADDR(byte src)
        {
            Regs.F = CpuTables.Adcf[Regs.A + src * 0x100];
            Regs.A += src;
        }
        
        private void ALU_ADCR(byte src)
        {
            var carry = Regs.F & CpuFlags.C;
            Regs.F = CpuTables.Adcf[Regs.A + src * 0x100 + 0x10000 * carry];
            Regs.A += (byte)(src + carry);
        }
        
        private void ALU_SUBR(byte src)
        {
            Regs.F = CpuTables.Sbcf[Regs.A * 0x100 + src];
            Regs.A -= src;
        }
        
        private void ALU_SBCR(byte src)
        {
            var carry = Regs.F & CpuFlags.C;
            Regs.F = CpuTables.Sbcf[Regs.A * 0x100 + src + 0x10000 * carry];
            Regs.A -= (byte)(src + carry);
        }
        
        private void ALU_ANDR(byte src)
        {
            Regs.A &= src;
            Regs.F = (byte)(CpuTables.Logf[Regs.A] | CpuFlags.H);
        }
        
        private void ALU_XORR(byte src)
        {
            Regs.A ^= src;
            Regs.F = CpuTables.Logf[Regs.A];
        }
        
        private void ALU_ORR(byte src)
        {
            Regs.A |= src;
            Regs.F = CpuTables.Logf[Regs.A];
        }
        
        private void ALU_CPR(byte src)
        {
            Regs.F = CpuTables.Cpf[Regs.A * 0x100 + src];
        }

        private byte ALU_INCR(byte x)
        {
            Regs.F = (byte)(CpuTables.Incf[x] | (Regs.F & CpuFlags.C));
            x++;
            return x;
        }
        
        private byte ALU_DECR(byte x)
        {
            Regs.F = (byte)(CpuTables.Decf[x] | (Regs.F & CpuFlags.C));
            x--;
            return x;
        }

        private ushort ALU_ADDHLRR(ushort rhl, ushort rde)
        {
            Regs.F = (byte)(Regs.F & CpuFlags.NotHNCF3F5);
            Regs.F |= (byte)((((rhl & 0x0FFF) + (rde & 0x0FFF)) >> 8) & CpuFlags.H);
            uint res = (uint)((rhl & 0xFFFF) + (rde & 0xFFFF));

            if ((res & 0x10000) != 0) Regs.F |= CpuFlags.C;
            Regs.F |= (byte)((res >> 8) & CpuFlags.F3F5);
            return (ushort)res;
        }

        #endregion

        #region ALU #CB

        private byte ALU_RLC(int x)
        {
            Regs.F = CpuTables.Rlcf[x];
            x <<= 1;
            if ((x & 0x100) != 0) x |= 0x01;
            return (byte)x;
        }
        
        private byte ALU_RRC(int x)
        {
            Regs.F = CpuTables.Rrcf[x];
            if ((x & 0x01) != 0) x = (x >> 1) | 0x80;
            else x >>= 1;
            return (byte)x;
        }
        
        private byte ALU_RL(int x)
        {
            if ((Regs.F & CpuFlags.C) != 0)
            {
                Regs.F = CpuTables.Rl1[x];
                x <<= 1;
                x++;
            }
            else
            {
                Regs.F = CpuTables.Rl0[x];
                x <<= 1;
            }
            return (byte)x;
        }
        
        private byte ALU_RR(int x)
        {
            if ((Regs.F & CpuFlags.C) != 0)
            {
                Regs.F = CpuTables.Rr1[x];
                x >>= 1;
                x += 0x80;
            }
            else
            {
                Regs.F = CpuTables.Rr0[x];
                x >>= 1;
            }
            return (byte)x;
        }
        
        private byte ALU_SLA(int x)
        {
            Regs.F = CpuTables.Rl0[x];
            x <<= 1;
            return (byte)x;
        }
        
        private byte ALU_SRA(int x)
        {
            Regs.F = CpuTables.Sraf[x];
            x = (x >> 1) + (x & 0x80);
            return (byte)x;
        }
        
        private byte ALU_SLL(int x)
        {
            Regs.F = CpuTables.Rl1[x];
            x <<= 1;
            x++;
            return (byte)x;
        }
        
        private byte ALU_SRL(int x)
        {
            Regs.F = CpuTables.Rr0[x];
            x >>= 1;
            return (byte)x;
        }
        
        private void ALU_BIT(byte src, int bit)
        {
            Regs.F = (byte)(CpuTables.Logf[src & (1 << bit)] | CpuFlags.H | (Regs.F & CpuFlags.C) | (src & CpuFlags.F3F5));
        }
        
        private void ALU_BITMEM(byte src, int bit)
        {
            Regs.F = (byte)(CpuTables.Logf[src & (1 << bit)] | CpuFlags.H | (Regs.F & CpuFlags.C));
            Regs.F = (byte)((Regs.F & CpuFlags.NotF3F5) | (Regs.MH & CpuFlags.F3F5));
        }

        #endregion
    }
}
