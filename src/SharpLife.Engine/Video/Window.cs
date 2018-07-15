﻿/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using SDL2;
using SharpLife.Engine.Configuration;
using SharpLife.Engine.Loop;
using SharpLife.Engine.Utility;
using SharpLife.FileSystem;
using SharpLife.Input;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpLife.Engine.Video
{
    /// <summary>
    /// Manages the SDL2 window
    /// </summary>
    public sealed class Window
    {
        private readonly IEngineLoop _engineLoop;

        public IntPtr WindowHandle { get; private set; }

        public IntPtr GLContextHandle { get; private set; }

        private readonly InputSystem _inputSystem = new InputSystem();

        public IInputSystem InputSystem => _inputSystem;

        public event Action Resized;

        public Window(ICommandLine commandLine, IFileSystem fileSystem, IEngineLoop engineLoop, EngineConfiguration engineConfiguration, GameConfiguration gameConfiguration)
        {
            _engineLoop = engineLoop ?? throw new ArgumentNullException(nameof(engineLoop));

            //This differs from vanilla GoldSource; set the OpenGL context version to 3.0 so we can use shaders
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL.SDL_GLcontext.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCELERATED_VISUAL, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 0);

            var gameWindowName = engineConfiguration.DefaultGameName;

            if (!string.IsNullOrWhiteSpace(gameConfiguration.GameName))
            {
                gameWindowName = gameConfiguration.GameName;
            }

            var flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;

            if (commandLine.Contains("-noborder"))
            {
                flags |= SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            }

            WindowHandle = SDL.SDL_CreateWindow(gameWindowName, 0, 0, 640, 480, flags);

            if (WindowHandle == IntPtr.Zero)
            {
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 16);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 3);
                WindowHandle = SDL.SDL_CreateWindow(gameWindowName, 0, 0, 640, 480, flags);

                if (WindowHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to create SDL Window");
                }
            }

            //Load the game icon
            try
            {
                var image = Image.Load(fileSystem.OpenRead("game.png"));

                if (image != null)
                {
                    var pixels = image.SavePixelData();

                    var nativeMemory = Marshal.AllocHGlobal(pixels.Length);

                    Marshal.Copy(pixels, 0, nativeMemory, pixels.Length);

                    var surface = SDL.SDL_CreateRGBSurfaceFrom(nativeMemory, image.Width, image.Height, 32, 4 * image.Width, 0xFF, 0xFF << 8, 0xFF << 16, unchecked((uint)(0xFF << 24)));

                    if (surface != IntPtr.Zero)
                    {
                        SDL.SDL_SetWindowIcon(WindowHandle, surface);
                        SDL.SDL_FreeSurface(surface);
                    }

                    Marshal.FreeHGlobal(nativeMemory);
                }
            }
            catch (FileNotFoundException)
            {
                //If image doesn't exist, just ignore it
            }

            SDL.SDL_ShowWindow(WindowHandle);

            GLContextHandle = SDL.SDL_GL_CreateContext(WindowHandle);

            if (GLContextHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create SDL Window");
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, out var r))
            {
                r = 0;
                Console.WriteLine("Failed to get GL RED size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, out var g))
            {
                g = 0;
                Console.WriteLine("Failed to get GL GREEN size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, out var b))
            {
                b = 0;
                Console.WriteLine("Failed to get GL BLUE size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, out var a))
            {
                a = 0;
                Console.WriteLine("Failed to get GL ALPHA size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, out var depth))
            {
                depth = 0;
                Console.WriteLine("Failed to get GL DEPTH size ({0})", SDL.SDL_GetError());
            }

            Console.WriteLine($"GL_SIZES:  r:{r} g:{g} b:{b} a:{a} depth:{depth}");

            if (r <= 4 || g <= 4 || b <= 4 || depth <= 15 /*|| gl_renderer && Q_strstr(gl_renderer, "GDI Generic")*/)
            {
                throw new InvalidOperationException("Failed to create SDL Window, unsupported video mode. A 16-bit color depth desktop is required and a supported GL driver");
            }
        }

        ~Window()
        {
            Destroy();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        private void DestroyWindow()
        {
            if (GLContextHandle != IntPtr.Zero)
            {
                SDL.SDL_GL_DeleteContext(GLContextHandle);
                GLContextHandle = IntPtr.Zero;
            }

            if (WindowHandle != IntPtr.Zero)
            {
                SDL.SDL_DestroyWindow(WindowHandle);
                WindowHandle = IntPtr.Zero;
            }
        }

        private void Destroy()
        {
            DestroyWindow();
        }

        public void CenterWindow()
        {
            SDL.SDL_GetWindowSize(WindowHandle, out var windowWidth, out var windowHeight);

            if (0 == SDL.SDL_GetDisplayBounds(0, out var bounds))
            {
                SDL.SDL_SetWindowPosition(WindowHandle, (bounds.w - windowWidth) / 2, (bounds.h - windowHeight) / 2);
            }
        }

        /// <summary>
        /// Sleep up to <paramref name="milliSeconds"/> milliseconds, waking to process events
        /// </summary>
        /// <param name="milliSeconds"></param>
        public void SleepUntilInput(int milliSeconds)
        {
            _inputSystem.ProcessEvents(milliSeconds);

            var snapshot = _inputSystem.Snapshot;

            for (var i = 0; i < snapshot.Events.Count; ++i)
            {
                var sdlEvent = snapshot.Events[i];

                switch (sdlEvent.type)
                {
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        {
                            switch (sdlEvent.window.windowEvent)
                            {
                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                                    {
                                        Resized?.Invoke();
                                        break;
                                    }

                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                    {
                                        _engineLoop.Exiting = true;
                                        DestroyWindow();
                                        break;
                                    }
                            }
                            break;
                        }
                    case SDL.SDL_EventType.SDL_QUIT:
                        {
                            _engineLoop.Exiting = true;
                            break;
                        }
                }
            }
        }
    }
}
