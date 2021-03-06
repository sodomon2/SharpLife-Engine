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

namespace SharpLife.CommandSystem.Commands.VariableFilters
{
    /// <summary>
    /// Denies any non-numeric inputs
    /// </summary>
    public class NumberFilter : IVariableFilter
    {
        private readonly bool _integerOnly;

        /// <summary>
        /// Creates a new number filter
        /// </summary>
        /// <param name="integerOnly">If true, only integer numbers are allowed</param>
        public NumberFilter(bool integerOnly = false)
        {
            _integerOnly = integerOnly;
        }

        public bool Filter(ref string stringValue, ref float floatValue)
        {
            if (_integerOnly)
            {
                return int.TryParse(stringValue, out var _);
            }
            else
            {
                return float.TryParse(stringValue, out var _);
            }
        }
    }
}
