using EmuLib.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmuLib.Core
{
    public abstract class MachineBase : IMachine
    {
        public abstract string Name { get; }
        public abstract (int, int) VideoBuffer { get; }
        public abstract string RomFile { get; }
        public abstract int MemorySize { get; }
        
        public abstract void Init();
        public abstract void Done();

        public abstract void Execute(int us);

        public abstract void Render(IntPtr buff);
    }
}
