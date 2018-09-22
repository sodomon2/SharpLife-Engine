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

using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Entities.MetaData.TypeConverters;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;

namespace SharpLife.Game.Server.Entities.Animation
{
    /// <summary>
    /// Base class for entities that use studio models
    /// </summary>
    [Networkable]
    public class BaseAnimating : NetworkedEntity
    {
        [Networked(TypeConverterType = typeof(FrameTypeConverter))]
        public float Frame { get; set; }

        [Networked]
        public float FrameRate { get; set; }

        public override bool KeyValue(string key, string value)
        {
            if (key == "framerate")
            {
                FrameRate = KeyValueUtils.ParseFloat(value);
                return true;
            }

            return base.KeyValue(key, value);
        }
    }
}