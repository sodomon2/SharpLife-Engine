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

using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using System;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists
{
    internal sealed class NetworkObject : INetworkObject
    {
        [Flags]
        private enum ObjectFlags : byte
        {
            None = 0,
            Destroyed = 1 << 0
        }

        public ObjectHandle Handle => Instance.Handle;

        public TypeMetaData MetaData { get; }

        public INetworkable Instance { get; }

        internal bool[] ChangeNotifications { get; }

        private ObjectFlags _flags;

        /// <summary>
        /// Whether this object has been destroyed and is pending removal
        /// </summary>
        internal bool Destroyed
        {
            get => (_flags & ObjectFlags.Destroyed) != 0;
            set
            {
                //This is to catch incorrect object destruction
                if (Destroyed)
                {
                    throw new InvalidOperationException("Cannot flag a network object as destroyed if it is already flagged as destroyed");
                }

                _flags |= ObjectFlags.Destroyed;
            }
        }

        internal NetworkObject(TypeMetaData metaData, INetworkable instance)
        {
            MetaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));

            if (metaData.ChangeNotificationMembersCount > 0)
            {
                ChangeNotifications = new bool[metaData.ChangeNotificationMembersCount];
            }
        }

        public void OnChange(string name)
        {
            //TODO: could keep track of changes in embedded types using dot notation
            var member = MetaData.FindMemberByName(name);

            if (member == null)
            {
                throw new ArgumentException($"No such member {name} in type {MetaData.Type.FullName}");
            }

            if (!member.ChangeNotificationIndex.HasValue)
            {
                throw new ArgumentException($"Member {name} in type {MetaData.Type.FullName} does not use change notifications");
            }

            ChangeNotifications[member.ChangeNotificationIndex.Value] = true;
        }

        internal MemberSnapshot[] TakeSnapshot(MemberSnapshot[] previousSnapshot)
        {
            var snapshot = MetaData.AllocateSnapshot();

            for (var i = 0; i < MetaData.Members.Count; ++i)
            {
                var member = MetaData.Members[i];

                var childInstance = MetaData.Accessor[Instance, member.Info.Name];

                snapshot[i].Value = member.Converter.Copy(childInstance);

                //Only mark it as changed it if it actually changed
                //This prevents client instances from being forcefully reset when the server isn't updating the value
                //TODO: needs to be more robust (better comparison), a way to force updates
                snapshot[i].Changed = snapshot[i].Value?.Equals(previousSnapshot?[i].Value) != true;
            }

            return snapshot;
        }

        internal void ApplySnapshot(MemberSnapshot[] snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Length != MetaData.Members.Count)
            {
                throw new InvalidOperationException($"Snapshot for object {Handle} and type {MetaData.Type.FullName} has the wrong size (got {snapshot.Length}, expected {MetaData.Members.Count}");
            }

            for (var i = 0; i < MetaData.Members.Count; ++i)
            {
                var member = MetaData.Members[i];

                var currentValue = MetaData.Accessor[Instance, member.Info.Name];

                if (snapshot[i].Changed)
                {
                    MetaData.Accessor[Instance, member.Info.Name] = member.Converter.CreateInstance(member.MetaData.Type, snapshot[i].Value);
                }
            }
        }
    }
}
