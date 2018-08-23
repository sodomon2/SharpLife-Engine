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

using Force.Crc32;
using SharpLife.FileFormats.BSP.Disk;
using SharpLife.FileFormats.WAD;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLife.FileFormats.BSP
{
    public class BSPLoader
    {
        private const int FaceSize = 20;

        private readonly BinaryReader _reader;

        private readonly long _startPosition;

        public BSPLoader(BinaryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            _startPosition = _reader.BaseStream.Position;
        }

        public BSPLoader(Stream stream, bool leaveOpen)
            : this(new BinaryReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, leaveOpen))
        {
        }

        public BSPLoader(Stream stream)
            : this(stream, false)
        {
        }

        public BSPLoader(string fileName)
            : this(File.OpenRead(fileName))
        {
        }

        /// <summary>
        /// Reads the header of a BSP file
        /// </summary>
        /// <returns></returns>
        private Header ReadHeader()
        {
            var version = EndianConverter.Little(_reader.ReadInt32());

            //Verify that we can load this BSP file
            if (!Enum.IsDefined(typeof(BSPVersion), version))
            {
                throw new InvalidBSPVersionException(version);
            }

            var header = new Header
            {
                Version = (BSPVersion)version
            };

            var lumps = new Lump[(int)LumpId.LastLump + 1];

            foreach (var i in Enumerable.Range((int)LumpId.FirstLump, (int)LumpId.LastLump + 1))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Lump>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                lumps[i] = Marshal.PtrToStructure<Lump>(handle.AddrOfPinnedObject());
                lumps[i].fileofs = EndianConverter.Little(lumps[i].fileofs);
                lumps[i].filelen = EndianConverter.Little(lumps[i].filelen);
                handle.Free();
            }

            header.Lumps = lumps;

            return header;
        }

        private List<MipTexture> ReadMipTextures(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = EndianConverter.Little(_reader.ReadInt32());

            var textureOffsets = new int[count];

            foreach (var i in Enumerable.Range(0, count))
            {
                textureOffsets[i] = EndianConverter.Little(_reader.ReadInt32());
            }

            var textures = new List<MipTexture>(count);

            foreach (var textureOffset in textureOffsets)
            {
                textures.Add(WADLoader.ReadMipTexture(_reader, lump.fileofs + textureOffset));
            }

            return textures;
        }

        private List<Plane> ReadPlanes(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Plane>();

            var planes = new List<Plane>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.Plane>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var plane = Marshal.PtrToStructure<Disk.Plane>(handle.AddrOfPinnedObject());
                handle.Free();

                plane.Normal = EndianTypeConverter.Little(plane.Normal);
                plane.Distance = EndianConverter.Little(plane.Distance);
                plane.Type = (PlaneType)EndianConverter.Little((int)plane.Type);

                planes.Add(new Plane { Data = plane });
            }

            return planes;
        }

        private unsafe void ReadBaseNode(IReadOnlyList<Plane> planes, ref Disk.BaseNode input, BaseNode output)
        {
            output.Plane = planes[EndianConverter.Little(input.planenum)];
            output.Children[0] = EndianConverter.Little(input.children[0]);
            output.Children[1] = EndianConverter.Little(input.children[1]);
        }

        private unsafe List<Node> ReadNodes(ref Lump lump, IReadOnlyList<Plane> planes, IReadOnlyList<Face> faces)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Node>();

            var nodes = new List<Node>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.Node>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var node = Marshal.PtrToStructure<Disk.Node>(handle.AddrOfPinnedObject());
                handle.Free();

                var result = new Node
                {
                    Mins = new Vector3(
                        EndianConverter.Little(node.mins[0]),
                        EndianConverter.Little(node.mins[1]),
                        EndianConverter.Little(node.mins[2])),
                    Maxs = new Vector3(
                        EndianConverter.Little(node.maxs[0]),
                        EndianConverter.Little(node.maxs[1]),
                        EndianConverter.Little(node.maxs[2])),
                };

                ReadBaseNode(planes, ref node.Data, result);

                node.firstface = EndianConverter.Little(node.firstface);
                node.numfaces = EndianConverter.Little(node.numfaces);

                result.Faces = new List<Face>(node.numfaces);

                foreach (var face in Enumerable.Range(node.firstface, node.numfaces))
                {
                    result.Faces.Add(faces[face]);
                }

                nodes.Add(result);
            }

            return nodes;
        }

        private List<ClipNode> ReadClipNodes(ref Lump lump, IReadOnlyList<Plane> planes)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.ClipNode>();

            var nodes = new List<ClipNode>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.ClipNode>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var node = Marshal.PtrToStructure<Disk.ClipNode>(handle.AddrOfPinnedObject());
                handle.Free();

                var result = new ClipNode();

                ReadBaseNode(planes, ref node.Data, result);

                nodes.Add(result);
            }

            return nodes;
        }

        private unsafe List<TextureInfo> ReadTextureInfos(ref Lump lump, List<MipTexture> mipTextures)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.TextureInfo>();

            var textureInfos = new List<TextureInfo>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.TextureInfo>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var info = Marshal.PtrToStructure<Disk.TextureInfo>(handle.AddrOfPinnedObject());
                textureInfos.Add(new TextureInfo
                {
                    SNormal = EndianTypeConverter.Little(new Vector3(info.vecs[0], info.vecs[1], info.vecs[2])),
                    SValue = EndianConverter.Little(info.vecs[3]),
                    TNormal = EndianTypeConverter.Little(new Vector3(info.vecs[4], info.vecs[5], info.vecs[6])),
                    TValue = EndianConverter.Little(info.vecs[7]),
                    MipTexture = mipTextures[EndianConverter.Little(info.miptex)],
                    Flags = (TextureFlags)EndianConverter.Little((int)info.flags)
                });
                handle.Free();
            }

            return textureInfos;
        }

        private List<Vector3> ReadVertexes(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Vector3>();

            var vertexes = new List<Vector3>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Vector3>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var vertex = Marshal.PtrToStructure<Vector3>(handle.AddrOfPinnedObject());
                handle.Free();

                vertexes.Add(EndianTypeConverter.Little(vertex));
            }

            return vertexes;
        }

        private List<Edge> ReadEdges(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Edge>();

            var edges = new List<Edge>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Edge>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var edge = Marshal.PtrToStructure<Edge>(handle.AddrOfPinnedObject());
                handle.Free();

                edge.start = EndianConverter.Little(edge.start);
                edge.end = EndianConverter.Little(edge.end);

                edges.Add(edge);
            }

            return edges;
        }

        private List<int> ReadSurfEdges(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<int>();

            var surfEdges = new List<int>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                surfEdges.Add(EndianConverter.Little(_reader.ReadInt32()));
            }

            return surfEdges;
        }

        private List<Face> ReadFaces(ref Lump lump,
            IReadOnlyList<Plane> planes,
            IReadOnlyList<Vector3> vertexes,
            IReadOnlyList<Edge> edges,
            IReadOnlyList<int> surfEdges,
            IReadOnlyList<TextureInfo> textureInfos)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / FaceSize;

            var faces = new List<Face>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var face = new Face();

                var planeNumber = _reader.ReadInt16();

                face.Plane = planes[EndianConverter.Little(planeNumber)];
                face.Side = EndianConverter.Little(_reader.ReadInt16()) != 0;

                var firstEdge = EndianConverter.Little(_reader.ReadInt32());
                var numEdges = EndianConverter.Little(_reader.ReadInt16());

                face.Points = new List<Vector3>(numEdges);

                for (var edge = 0; edge < numEdges; ++edge)
                {
                    //Surfedge indices can be negative
                    var edgeIndex = surfEdges[firstEdge + edge];

                    var edgeData = edges[Math.Abs(edgeIndex)];

                    face.Points.Add(vertexes[edgeIndex > 0 ? edgeData.start : edgeData.end]);
                }

                var texInfo = EndianConverter.Little(_reader.ReadInt16());

                face.TextureInfo = textureInfos[texInfo];

                face.Styles = new byte[Constants.MaxLightmaps];

                foreach (var style in Enumerable.Range(0, Constants.MaxLightmaps))
                {
                    face.Styles[style] = _reader.ReadByte();
                }

                face.LightOffset = EndianConverter.Little(_reader.ReadInt32());

                faces.Add(face);
            }

            return faces;
        }

        private List<int> ReadMarkSurfaces(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<ushort>();

            var markSurfaces = new List<int>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                markSurfaces.Add(EndianConverter.Little(_reader.ReadUInt16()));
            }

            return markSurfaces;
        }

        private unsafe List<Leaf> ReadLeafs(ref Lump lump, IReadOnlyList<int> markSurfaces, IReadOnlyList<Face> faces)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Leaf>();

            var leaves = new List<Leaf>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.Leaf>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var leaf = Marshal.PtrToStructure<Disk.Leaf>(handle.AddrOfPinnedObject());
                handle.Free();

                leaf.firstmarksurface = EndianConverter.Little(leaf.firstmarksurface);
                leaf.nummarksurfaces = EndianConverter.Little(leaf.nummarksurfaces);

                var leafFaces = new List<Face>(leaf.nummarksurfaces);

                foreach (var surface in Enumerable.Range(leaf.firstmarksurface, leaf.nummarksurfaces))
                {
                    leafFaces.Add(faces[markSurfaces[surface]]);
                }

                var result = new Leaf
                {
                    Contents = (Contents)EndianConverter.Little((int)leaf.contents),
                    Maxs = new Vector3(
                        EndianConverter.Little(leaf.maxs[0]),
                        EndianConverter.Little(leaf.maxs[1]),
                        EndianConverter.Little(leaf.maxs[2])),
                    Mins = new Vector3(
                        EndianConverter.Little(leaf.mins[0]),
                        EndianConverter.Little(leaf.mins[1]),
                        EndianConverter.Little(leaf.mins[2])),
                    Faces = leafFaces,
                    VisOffset = EndianConverter.Little(leaf.visofs)
                };

                foreach (var ambient in Enumerable.Range(0, (int)Ambient.LastAmbient + 1))
                {
                    result.AmbientLevel[ambient] = leaf.ambient_level[ambient];
                }

                leaves.Add(result);
            }

            return leaves;
        }

        private unsafe List<Model> ReadModels(ref Lump lump, IReadOnlyList<Face> faces)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Model>();

            var models = new List<Model>();

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.Model>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var model = Marshal.PtrToStructure<Disk.Model>(handle.AddrOfPinnedObject());
                handle.Free();

                var result = new Model
                {
                    Mins = model.mins,
                    Maxs = model.maxs,
                    Origin = model.origin,
                    NumVisLeaves = model.visleafs
                };

                foreach (var node in Enumerable.Range(0, Constants.MaxHulls))
                {
                    result.HeadNodes[node] = model.headnode[node];
                }

                result.Faces = new List<Face>(model.numfaces);

                foreach (var face in Enumerable.Range(model.firstface, model.numfaces))
                {
                    result.Faces.Add(faces[face]);
                }

                models.Add(result);
            }

            return models;
        }

        private string ReadEntities(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var rawBytes = _reader.ReadBytes(lump.filelen);

            var entityData = Encoding.UTF8.GetString(rawBytes);

            //Check if there's a null terminator, remove it if so
            if (entityData.EndsWith('\0'))
            {
                entityData = entityData.Remove(entityData.Length - 1);
            }

            return entityData;
        }

        private byte[] ReadVisibility(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            return _reader.ReadBytes(lump.filelen);
        }

        private byte[] ReadLighting(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            return _reader.ReadBytes(lump.filelen);
        }

        /// <summary>
        /// Reads the BSP file
        /// </summary>
        /// <returns></returns>
        public BSPFile ReadBSPFile()
        {
            _reader.BaseStream.Position = _startPosition;

            var header = ReadHeader();

            //Determine if this is a Blue Shift BSP file
            //This works by checking if the planes lump actually contains planes
            //It can only contain planes if the length is a multiple of the plane data structure size,
            //AND the entities lump is a multiple of the size
            //These 2 lumps are switched in those BSP files
            var isBlueShiftBSP = IsBlueShiftBSP(header);

            var entitiesLumpId = isBlueShiftBSP ? LumpId.Planes : LumpId.Entities;
            var planesLumpId = isBlueShiftBSP ? LumpId.Entities : LumpId.Planes;

            var mipTextures = ReadMipTextures(ref header.Lumps[(int)LumpId.Textures]);
            var planes = ReadPlanes(ref header.Lumps[(int)planesLumpId]);

            var textureInfos = ReadTextureInfos(ref header.Lumps[(int)LumpId.TexInfo], mipTextures);

            var vertexes = ReadVertexes(ref header.Lumps[(int)LumpId.Vertexes]);
            var edges = ReadEdges(ref header.Lumps[(int)LumpId.Edges]);
            var surfEdges = ReadSurfEdges(ref header.Lumps[(int)LumpId.SurfEdges]);
            var faces = ReadFaces(ref header.Lumps[(int)LumpId.Faces], planes, vertexes, edges, surfEdges, textureInfos);

            var nodes = ReadNodes(ref header.Lumps[(int)LumpId.Nodes], planes, faces);
            var clipNodes = ReadClipNodes(ref header.Lumps[(int)LumpId.ClipNodes], planes);

            var markSurfaces = ReadMarkSurfaces(ref header.Lumps[(int)LumpId.MarkSurfaces]);

            var leaves = ReadLeafs(ref header.Lumps[(int)LumpId.Leafs], markSurfaces, faces);

            var models = ReadModels(ref header.Lumps[(int)LumpId.Models], faces);

            var entities = ReadEntities(ref header.Lumps[(int)entitiesLumpId]);

            var visibility = ReadVisibility(ref header.Lumps[(int)LumpId.Visibility]);

            var lighting = ReadLighting(ref header.Lumps[(int)LumpId.Lighting]);

            var bspFile = new BSPFile
            {
                Version = header.Version,
                MipTextures = mipTextures,
                Planes = planes,
                Faces = faces,
                Leaves = leaves,
                Models = models,
                Nodes = nodes,
                ClipNodes = clipNodes,
                Entities = entities,
                Visibility = visibility,
                Lighting = lighting,
                HasBlueShiftLumpLayout = isBlueShiftBSP
            };

            return bspFile;
        }

        private static bool IsBlueShiftBSP(Header header)
        {
            //Determine if this is a Blue Shift BSP file
            //This works by checking if the planes lump actually contains planes
            //It can only contain planes if the length is a multiple of the plane data structure size,
            //AND the entities lump is a multiple of the size
            //These 2 lumps are switched in those BSP files
            return (header.Lumps[(int)LumpId.Planes].filelen % Marshal.SizeOf<Disk.Plane>()) != 0
                && (header.Lumps[(int)LumpId.Entities].filelen % Marshal.SizeOf<Disk.Plane>()) == 0;
        }

        /// <summary>
        /// Computes the CRC32 for this BSP file
        /// The stream position is left unmodified
        /// </summary>
        /// <returns></returns>
        public uint ComputeCRC()
        {
            var currentPosition = _reader.BaseStream.Position;

            _reader.BaseStream.Position = _startPosition;

            try
            {
                var header = ReadHeader();

                var isBlueShiftBSP = IsBlueShiftBSP(header);

                var ignoreLump = isBlueShiftBSP ? LumpId.Planes : LumpId.Entities;

                uint crc = 0;

                //Append each lump to CRC, except entities lump since servers should be able to run Ripented maps

                var buffer = new byte[1024];

                foreach (var i in Enumerable.Range((int)LumpId.FirstLump, (LumpId.LastLump - LumpId.FirstLump) + 1))
                {
                    if (i == (int)ignoreLump)
                    {
                        continue;
                    }

                    _reader.BaseStream.Position = header.Lumps[i].fileofs;

                    var bytesLeft = header.Lumps[i].filelen;

                    while (bytesLeft > 0)
                    {
                        var bytesToRead = bytesLeft < buffer.Length ? bytesLeft : buffer.Length;

                        var bytesRead = _reader.Read(buffer, 0, bytesToRead);

                        if (bytesRead != bytesToRead)
                        {
                            var totalRead = header.Lumps[i].filelen - bytesLeft - (bytesToRead - bytesRead);
                            throw new InvalidOperationException($"BSP lump {i} has invalid file length data, expected {header.Lumps[i].filelen}, got {totalRead}");
                        }

                        crc = Crc32Algorithm.Append(crc, buffer, 0, bytesToRead);

                        bytesLeft -= bytesToRead;
                    }
                }

                return crc;
            }
            finally
            {
                _reader.BaseStream.Position = currentPosition;
            }
        }
    }
}