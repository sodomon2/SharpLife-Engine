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

using System.Numerics;

namespace SharpLife.Game.Client.Renderer.Shared.Models.MDL
{
    public struct StudioRenderArguments
    {
        public struct AmbientLight
        {
            public Vector3 Color;
            public int Ambient;
            public Vector3 Normal;
            public int Shade;
        }

        public Vector4 RenderColor;

        public AmbientLight GlobalLight;

        private Vector4 _padding0;
    }
}
