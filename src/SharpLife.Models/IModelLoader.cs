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

using SharpLife.FileSystem;
using System.IO;

namespace SharpLife.Models
{
    /// <summary>
    /// Represents an object that can load a certain type of model
    /// </summary>
    public interface IModelLoader
    {
        /// <summary>
        /// Loads a model out of the given reader
        /// </summary>
        /// <param name="name">Name to associate with the model</param>
        /// <param name="fileSystem">Filesystem to use when loading additional files</param>
        /// <param name="reader"></param>
        /// <param name="addModelCallback">Callback to add more models if a model contains submodels</param>
        /// <param name="computeCRC">Whether to compute the CRC for this model</param>
        /// <returns>The model and the CRC</returns>
        IModel Load(string name, IFileSystem fileSystem, BinaryReader reader, Delegates.AddModel addModelCallback, bool computeCRC);
    }
}
