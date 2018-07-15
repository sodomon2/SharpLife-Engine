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

namespace SharpLife.Engine
{
    /// <summary>
    /// Framework constants
    /// </summary>
    public static class Framework
    {
        /// <summary>
        /// This is the default language that game data is localized in
        /// </summary>
        public const string DefaultLanguage = "english";

        public static class Directory
        {
            public const string Graphics = "gfx";
            public const string EnvironmentMaps = "env";
            public const string Shaders = "shaders";
        }

        public static class Path
        {
            /// <summary>
            /// 2D skybox textures are stored here
            /// </summary>
            public static readonly string EnvironmentMaps = System.IO.Path.Combine(Directory.Graphics, Directory.EnvironmentMaps);

            /// <summary>
            /// Shared are stored here
            /// </summary>
            public static readonly string Shaders = System.IO.Path.Combine(Directory.Graphics, Directory.Shaders);
        }
    }
}
