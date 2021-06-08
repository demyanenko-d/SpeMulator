using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmuLib.Contracts
{
    public interface IMachine
    {
        string Name { get; }
        (int, int) VideoBuffer { get; }
        string RomFile { get; }
        int MemorySize { get; }

        void Init();
        void Done();
        void Execute(int us);
        void Render(IntPtr buff);
    }
}
