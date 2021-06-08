using System;
using System.Runtime.InteropServices;
using System.IO;

namespace EmuLib.Machines.TSConfig
{
    public sealed unsafe class Memory: IDisposable
    {
        private const int SDL_ROM_SIZE = 512 * 1024;
        private const int SDL_RAM_SIZE = 4 * 1024 * 1024;
        private const int SDL_RAM_PAGE_SIZE = 16 * 1024;

        private byte* _rom;
        private byte* _ram;
        private byte* _trash;

        private readonly byte*[] _rdPages = new byte*[4];
        private readonly byte*[] _wrPages = new byte*[4];


        public byte this[ushort addr]
        {
            get
            {
                var pg = addr >> 14;
                var page = _rdPages[pg];
                return page[addr & 0x3fff];
            }
            set
            {
                var pg = addr >> 14;
                var page = _rdPages[pg];
                page[addr & 0x3fff] = value;
            }
        }

        public Memory()
        {
            _rom = (byte*)Marshal.AllocHGlobal(SDL_ROM_SIZE);
            _ram = (byte*)Marshal.AllocHGlobal(SDL_RAM_SIZE);
            _trash = (byte*)Marshal.AllocHGlobal(SDL_RAM_PAGE_SIZE);

            GC.AddMemoryPressure(SDL_RAM_SIZE + SDL_ROM_SIZE + SDL_RAM_PAGE_SIZE);
        }

        public void Init()
        {
            using var file = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "roms", "48.rom"));

            var buf = new byte[SDL_ROM_SIZE];
            file.Read(buf, 0, SDL_ROM_SIZE);
            Marshal.Copy(buf, 0, (IntPtr)_rom, SDL_ROM_SIZE);

            _rdPages[0] = _rdPages[1] = _rdPages[2] = _rdPages[3] = _trash;
            _wrPages[0] = _wrPages[1] = _wrPages[2] = _wrPages[3] = _trash;
        }

        ~Memory()
        {
            Dispose(false);
        }

        private void ReleaseUnmanagedResources()
        {
            if (_rom != null)
            {
                Marshal.FreeHGlobal((IntPtr)_rom);
                GC.RemoveMemoryPressure(SDL_ROM_SIZE);
                _rom = null;
            }

            if (_ram != null)
            {
                Marshal.FreeHGlobal((IntPtr)_ram);
                GC.RemoveMemoryPressure(SDL_RAM_SIZE);
                _ram = null;
            }

            if (_trash != null)
            {
                Marshal.FreeHGlobal((IntPtr)_trash);
                GC.RemoveMemoryPressure(SDL_RAM_PAGE_SIZE);
                _trash = null;
            }
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
