/*
===========================================================================
Copyright (C) 1999-2005 Id Software, Inc.

This file is part of Quake III Arena source code.

Quake III Arena source code is free software; you can redistribute it
and/or modify it under the terms of the GNU General Public License as
published by the Free Software Foundation; either version 2 of the License,
or (at your option) any later version.

Quake III Arena source code is distributed in the hope that it will be
useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Foobar; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
===========================================================================
*/

using System;
using System.Runtime.InteropServices;

namespace SharpQ3.Engine.qcommon
{
    public struct leafList_t
    {
        public int count;
        public int maxcount;
        public bool overflowed;
        public int[] list;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public vec3_t[] bounds;

        public int lastLeaf;       // for overflows where each leaf can't be stored individually
        public Action<leafList_t, int> storeLeafs;// void (* storeLeafs) ( struct leafList_s *ll, int nodenum );
    }

    public struct cNode_t
    {
        public cplane_t plane;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public int[] children;      // negative numbers are leafs
    }

    public struct cLeaf_t
    {
        public int cluster;
        public int area;

        public int firstLeafBrush;
        public int numLeafBrushes;

        public int firstLeafSurface;
        public int numLeafSurfaces;
    }

    public struct cmodel_t
    {
        public vec3_t mins, maxs;
        public cLeaf_t leaf;            // submodels don't reference the main tree
    }

    public struct cbrushside_t
    {
        public cplane_t plane;
        public int surfaceFlags;
        public int shaderNum;
    }

    public class cbrush_t
    {
        public int shaderNum;       // the shader that determined the contents
        public int contents;
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2)]
        public vec3_t[] bounds;
        public int numsides;
        public cbrushside_t[] sides;
        public int checkcount;      // to avoid repeated testings
    }

    public class cPatch_t
    {
        public int checkcount;              // to avoid repeated testings
        public int surfaceFlags;
        public int contents;
        public patchCollide_t pc;
    }

    public struct cArea_t
    {
        public int floodnum;
        public int floodvalid;
    }

    public struct clipMap_t
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = q_shared.MAX_QPATH )]
        public byte[] name;

        public int numShaders;
        public dshader_t[] shaders;

        public int numBrushSides;
        public cbrushside_t[] brushsides;

        public int numPlanes;
        public cplane_t[] planes;

        public int numNodes;
        public cNode_t[] nodes;

        public int numLeafs;
        public cLeaf_t[] leafs;

        public int numLeafBrushes;
        public int[] leafbrushes;

        public int numLeafSurfaces;
        public int[] leafsurfaces;

        public int numSubModels;
        public cmodel_t[] cmodels;

        public int numBrushes;
        public cbrush_t[] brushes;

        public int numClusters;
        public int clusterBytes;
        public byte[] visibility;
        public bool vised;          // if false, visibility is just a single cluster of ffs

        public int numEntityChars;
        public byte[] entityString;

        public int numAreas;
        public cArea_t[] areas;
        public int[] areaPortals;   // [ numAreas*numAreas ] reference counts

        public int numSurfaces;
        public cPatch_t[] surfaces;           // non-patches will be NULL

        public int floodvalid;
        public int checkcount;                  // incremented on each trace
    }

    // Used for oriented capsule collision detection
    public struct sphere_t
    {
        public bool use;
        public float radius;
        public float halfheight;
        public vec3_t offset;
    }

    public struct traceWork_t
    {
        public vec3_t start;
        public vec3_t end;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public vec3_t[] size;  // size of the box being swept through the model

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public vec3_t[] offsets;   // [signbits][x] = either size[0][x] or size[1][x]

        public float maxOffset; // longest corner length from origin
        public vec3_t extents;  // greatest of abs(size[0]) and abs(size[1])

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public vec3_t[] bounds;    // enclosing box of start and end surrounding by size

        public vec3_t modelOrigin;// origin of the model tracing through
        public int contents;    // ored contents of the model tracing through
        public bool isPoint;    // optimized case
        public trace_t trace;       // returned from trace call
        public sphere_t sphere;        // sphere for oriendted capsule collision
    }

    public static class cm_local
    {
        public const int MAX_SUBMODELS = 256;
        public const int BOX_MODEL_HANDLE = 255;
        public const int CAPSULE_MODEL_HANDLE = 254;

        // keep 1/8 unit away to keep the position valid before network snapping
        // and to avoid various numeric issues
        public const float SURFACE_CLIP_EPSILON = 0.125f;

        public static clipMap_t cm;
        public static int c_pointcontents;
        public static int c_traces, c_brush_traces, c_patch_traces;
        public static cvar_t cm_noAreas;
        public static cvar_t cm_noCurves;
        public static cvar_t cm_playerCurveClip;
    }
}
