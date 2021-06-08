using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Threading;
using EmuLib.Contracts;
using EmuLib.Machines;
using EmuLib.Machines.TSConfig;
using ImGuiNET;
using SDLImGuiGL;
using static SDL2.SDL;

namespace AppIT
{
    internal static class Program
    {
        private const int SDL_MAX_BUFFER_SIZE = 4 * 1024 * 1024;
        private static ImGuiGLRenderer _renderer;
        private static bool _quit;
        private static IntPtr _window;
        private static IntPtr _glContext;
        private static IntPtr _memBuffer;
        private static uint _texture;


        private static IMachine _machine;

        public static void Main(string[] args)
        {
            Init();

            var renderThread = new Thread(() =>
            {
                _glContext = CreateGlContext(_window);
                _renderer = new ImGuiGLRenderer(_window, _glContext);
                _memBuffer = Marshal.AllocHGlobal(SDL_MAX_BUFFER_SIZE);
                GC.AddMemoryPressure(SDL_MAX_BUFFER_SIZE);

                _texture = ImGuiGL.LoadTexture(_memBuffer, 1024, 1024);


                while (!_quit)
                {
                    DoWork();
                    Render();
                }
            });

            renderThread.Start();

            while (!_quit)
            {
                DoEvents();
                Thread.Sleep(15);
            }

            renderThread.Join();

            Done();
        }



        private static void Init()
        {
            _machine = new TSConfig();
            _window = CreateWindowAndGlContext("SDL GL ImGui Renderer", 1024, 700);
            _machine.Init();
        }

        private static void Done()
        {
            _machine.Done();

            GL.DeleteTexture(_texture);

            if (_memBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_memBuffer);
                GC.RemoveMemoryPressure(SDL_MAX_BUFFER_SIZE);

                _memBuffer = IntPtr.Zero;
            }

            SDL_GL_DeleteContext(_glContext);
            SDL_DestroyWindow(_window);
            SDL_Quit();
        }

        private static void DoEvents()
        {

            while (SDL_PollEvent(out var e) != 0)
            {
                _renderer?.ProcessEvent(e);

                switch (e.type)
                {
                    

                    case SDL_EventType.SDL_QUIT:
                        {
                            _quit = true;
                            break;
                        }
                    case SDL_EventType.SDL_KEYDOWN:
                        {
                            switch (e.key.keysym.sym)
                            {
                                case SDL_Keycode.SDLK_ESCAPE:
                                case SDL_Keycode.SDLK_q:
                                    _quit = true;
                                    break;
                            }

                            break;
                        }
                }
            }
        }

        private static void DoWork()
        {
            _machine.Execute(20000);
        }

        private static void Render()
        {
            var (bufW, bufH) = _machine.VideoBuffer;
            _machine.Render(_memBuffer);
            LoadTexture(_texture, _memBuffer, bufW, bufH);

            _renderer.NewFrame();

            var bg = ImGui.GetBackgroundDrawList();
            SDL_GetWindowSize(_window, out var w, out var h);

            var scaleW = w / (float)bufW;
            var scaleH = h / (float)bufH;

            var minScale = MathF.Min(scaleW, scaleH);
            var imgW = bufW * minScale;
            var imgH = bufH * minScale;

            var dx = (w - imgW) / 2;
            var dy = (h - imgH) / 2;

            bg.AddImage((IntPtr)_texture, new System.Numerics.Vector2(dx, dy), new System.Numerics.Vector2(dx + imgW, dy + imgH));

            ImGui.ShowDemoWindow();
            _renderer.Render();
            SDL_GL_SwapWindow(_window);
        }

        static IntPtr CreateWindowAndGlContext(string title, int width, int height, bool fullscreen = false, bool highDpi = false)
        {
            // initialize SDL and set a few defaults for the OpenGL context
            SDL_Init(SDL_INIT_VIDEO);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL_GLcontext.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);

            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);

            // create the window which should be able to have a valid OpenGL context and is resizable
            var flags = SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
            if (fullscreen)
                flags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
            if (highDpi)
                flags |= SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;

            var window = SDL_CreateWindow(title, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, width, height, flags);
            
            return window;
        }

        static IntPtr CreateGlContext(IntPtr window)
        {
            var glContext = SDL_GL_CreateContext(window);
            if (glContext == IntPtr.Zero)
                throw new Exception("CouldNotCreateContext");

            SDL_GL_MakeCurrent(window, glContext);
            SDL_GL_SetSwapInterval(1);

            // initialize the screen to black as soon as possible
            GL.glClearColor(0f, 0f, 0f, 1f);
            GL.glClear(GL.ClearBufferMask.ColorBufferBit);
            SDL_GL_SwapWindow(window);

            Console.WriteLine($"GL Version: {GL.glGetString(GL.StringName.Version)}");

            return glContext;
        }

        private static void LoadTexture(uint textureId, IntPtr pixelData, int width, int height)
        {
            GL.glPixelStorei(GL.PixelStoreParameter.UnpackAlignment, 1);
            GL.glBindTexture(GL.TextureTarget.Texture2D, textureId);

            GL.glTexImage2D(GL.TextureTarget.Texture2D, 0, GL.PixelInternalFormat.Rgba, width, height, 0, GL.PixelFormat.Rgba, GL.PixelType.UnsignedByte, pixelData);
            GL.glTexParameteri(GL.TextureTarget.Texture2D, GL.TextureParameterName.TextureMagFilter, GL.TextureParameter.Linear);
            GL.glTexParameteri(GL.TextureTarget.Texture2D, GL.TextureParameterName.TextureMinFilter, GL.TextureParameter.Linear);

            GL.glBindTexture(GL.TextureTarget.Texture2D, 0);
        }
    }
}
