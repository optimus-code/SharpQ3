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

using SharpQ3.Engine;
using SharpQ3.Engine.qcommon;

namespace SharpQ3.Engine.qcommon
{
	// cm_load.c -- model loading
	public static class cm_load
	{
		// to allow boxes to be treated as brush models, we allocate
		// some extra indexes along with those needed by the map
		public const int BOX_BRUSHES = 1;
		public const int BOX_SIDES = 6;
		public const int BOX_LEAFS = 2;
		public const int BOX_PLANES = 12;

		#define LL(x) x=LittleLong(x)


		public static clipMap_t	cm;
		public static int c_pointcontents;
		public static int c_traces, c_brush_traces, c_patch_traces;


		public static byte* cmod_base;

		public static cvar_t cm_noAreas;
		public static cvar_t cm_noCurves;
		public static cvar_t		cm_playerCurveClip;

		public static cmodel_t	box_model;
		public static cplane_t[] box_planes;
		public static cbrush_t box_brush;



		//void	CM_InitBoxHull (void);
		//void	CM_FloodAreaConnections (void);


		/*
		===============================================================================

							MAP LOADING

		===============================================================================
		*/

		/*
		=================
		CMod_LoadShaders
		=================
		*/
		static void CMod_LoadShaders( lump_t *l ) 
		{
			dshader_t	*in, *out;
			int			i, count;

			in = (dshader_t*) (void *)(cmod_base + l.fileofs);
			if (l.filelen % sizeof(*in)) {
				common.Com_Error (errorParm_t.ERR_DROP, "CMod_LoadShaders: funny lump size");
			}
			count = l.filelen / sizeof(*in);

			if (count < 1) {
				common.Com_Error (errorParm_t.ERR_DROP, "Map with no shaders");
			}
			cm_local.cm.shaders = (dshader_t*) Hunk_Alloc( count * sizeof( *cm_local.cm.shaders ), h_high );
			cm_local.cm.numShaders = count;

			Com_Memcpy( cm_local.cm.shaders, in, count * sizeof( *cm_local.cm.shaders ) );

			out = cm_local.cm.shaders;
			for ( i=0 ; i<count ; i++, in++, out++ ) {
				out.contentFlags = LittleLong( out.contentFlags );
				out.surfaceFlags = LittleLong( out.surfaceFlags );
			}
		}


		/*
		=================
		CMod_LoadSubmodels
		=================
		*/
		static void CMod_LoadSubmodels( lump_t *l ) 
		{
			dmodel_t	*in;
			cmodel_t	*out;
			int			i, j, count;
			int			*indexes;

			in = (dmodel_t*) (void *)(cmod_base + l.fileofs);
			if (l.filelen % sizeof(*in))
				common.Com_Error (errorParm_t.ERR_DROP, "CMod_LoadSubmodels: funny lump size");
			count = l.filelen / sizeof(*in);

			if (count < 1)
				common.Com_Error (errorParm_t.ERR_DROP, "Map with no models");
			cm_local.cm.cmodels = (cmodel_t*) Hunk_Alloc( count * sizeof( *cm_local.cm.cmodels ), h_high );
			cm_local.cm.numSubModels = count;

			if ( count > MAX_SUBMODELS ) {
				common.Com_Error( errorParm_t.ERR_DROP, "MAX_SUBMODELS exceeded" );
			}

			for ( i=0 ; i<count ; i++, in++, out++)
			{
				out = &cm_local.cm.cmodels[i];

				for (j=0 ; j<3 ; j++)
				{	// spread the mins / maxs by a pixel
					out.mins[j] = LittleFloat (in.mins[j]) - 1;
					out.maxs[j] = LittleFloat (in.maxs[j]) + 1;
				}

				if ( i == 0 ) {
					continue;	// world model doesn't need other info
				}

				// make a "leaf" just to hold the model's brushes and surfaces
				out.leaf.numLeafBrushes = LittleLong( in.numBrushes );
				indexes = (int*) Hunk_Alloc( out.leaf.numLeafBrushes * 4, h_high );
				out.leaf.firstLeafBrush = indexes - cm_local.cm.leafbrushes;
				for ( j = 0 ; j < out.leaf.numLeafBrushes ; j++ ) {
					indexes[j] = LittleLong( in.firstBrush ) + j;
				}

				out.leaf.numLeafSurfaces = LittleLong( in.numSurfaces );
				indexes = (int*) Hunk_Alloc( out.leaf.numLeafSurfaces * 4, h_high );
				out.leaf.firstLeafSurface = indexes - cm_local.cm.leafsurfaces;
				for ( j = 0 ; j < out.leaf.numLeafSurfaces ; j++ ) {
					indexes[j] = LittleLong( in.firstSurface ) + j;
				}
			}
		}

		/*
		=================
		CMod_LoadNodes

		=================
		*/
		static void CMod_LoadNodes( lump_t *l ) 
		{
			dnode_t		*in;
			int			child;
			cNode_t		*out;
			int			i, j, count;
			
			in = (dnode_t*) (void *)(cmod_base + l.fileofs);
			if (l.filelen % sizeof(*in))
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");
			count = l.filelen / sizeof(*in);

			if (count < 1)
				common.Com_Error (errorParm_t.ERR_DROP, "Map has no nodes");
			cm_local.cm.nodes = (cNode_t*) Hunk_Alloc( count * sizeof( *cm_local.cm.nodes ), h_high );
			cm_local.cm.numNodes = count;

			out = cm_local.cm.nodes;

			for (i=0 ; i<count ; i++, out++, in++)
			{
				out.plane = cm_local.cm.planes + LittleLong( in.planeNum );
				for (j=0 ; j<2 ; j++)
				{
					child = LittleLong (in.children[j]);
					out.children[j] = child;
				}
			}

		}

		/*
		=================
		CM_BoundBrush

		=================
		*/
		static void CM_BoundBrush( cbrush_t *b )
		{
			b.bounds[0][0] = -b.sides[0].plane.dist;
			b.bounds[1][0] = b.sides[1].plane.dist;

			b.bounds[0][1] = -b.sides[2].plane.dist;
			b.bounds[1][1] = b.sides[3].plane.dist;

			b.bounds[0][2] = -b.sides[4].plane.dist;
			b.bounds[1][2] = b.sides[5].plane.dist;
		}


		/*
		=================
		CMod_LoadBrushes

		=================
		*/
		static void CMod_LoadBrushes( lump_t *l ) {
			dbrush_t	*in;
			cbrush_t	*out;
			int			i, count;

			in = (dbrush_t*) (void *)(cmod_base + l.fileofs);
			if (l.filelen % sizeof(*in)) {
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");
			}
			count = l.filelen / sizeof(*in);

			cm_local.cm.brushes = (cbrush_t*) Hunk_Alloc( ( BOX_BRUSHES + count ) * sizeof( *cm_local.cm.brushes ), h_high );
			cm_local.cm.numBrushes = count;

			out = cm_local.cm.brushes;

			for ( i=0 ; i<count ; i++, out++, in++ ) {
				out.sides = cm_local.cm.brushsides + LittleLong(in.firstSide);
				out.numsides = LittleLong(in.numSides);

				out.shaderNum = LittleLong( in.shaderNum );
				if ( out.shaderNum < 0 || out.shaderNum >= cm_local.cm.numShaders ) {
					common.Com_Error( errorParm_t.ERR_DROP, "CMod_LoadBrushes: bad shaderNum: %i", out.shaderNum );
				}
				out.contents = cm_local.cm.shaders[out.shaderNum].contentFlags;

				CM_BoundBrush( out );
			}

		}

		/*
		=================
		CMod_LoadLeafs
		=================
		*/
		static void CMod_LoadLeafs (lump_t *l)
		{
			int			i;
			cLeaf_t		*out;
			dleaf_t 	*in;
			int			count;
			
			in = (dleaf_t*) (void *)(cmod_base + l.fileofs);
			if (l.filelen % sizeof(*in))
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");
			count = l.filelen / sizeof(*in);

			if (count < 1)
				common.Com_Error (errorParm_t.ERR_DROP, "Map with no leafs");

			cm_local.cm.leafs = (cLeaf_t*) Hunk_Alloc( ( BOX_LEAFS + count ) * sizeof( *cm_local.cm.leafs ), h_high );
			cm_local.cm.numLeafs = count;

			out = cm_local.cm.leafs;	
			for ( i=0 ; i<count ; i++, in++, out++)
			{
				out.cluster = LittleLong (in.cluster);
				out.area = LittleLong (in.area);
				out.firstLeafBrush = LittleLong (in.firstLeafBrush);
				out.numLeafBrushes = LittleLong (in.numLeafBrushes);
				out.firstLeafSurface = LittleLong (in.firstLeafSurface);
				out.numLeafSurfaces = LittleLong (in.numLeafSurfaces);

				if (out.cluster >= cm_local.cm.numClusters)
					cm_local.cm.numClusters = out.cluster + 1;
				if (out.area >= cm_local.cm.numAreas)
					cm_local.cm.numAreas = out.area + 1;
			}

			cm_local.cm.areas = (cArea_t*) Hunk_Alloc( cm_local.cm.numAreas * sizeof( *cm_local.cm.areas ), h_high );
			cm_local.cm.areaPortals = (int*) Hunk_Alloc( cm_local.cm.numAreas * cm_local.cm.numAreas * sizeof( *cm_local.cm.areaPortals ), h_high );
		}

		/*
		=================
		CMod_LoadPlanes
		=================
		*/
		static void CMod_LoadPlanes (lump_t *l)
		{
			int			i, j;
			cplane_t	*out;
			dplane_t 	*in;
			int			count;
			int			bits;
			
			in = (dplane_t*) (void *)(cmod_base + l.fileofs);
			if (l.filelen % sizeof(*in))
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");
			count = l.filelen / sizeof(*in);

			if (count < 1)
				common.Com_Error (errorParm_t.ERR_DROP, "Map with no planes");
			cm_local.cm.planes = (cplane_t*) Hunk_Alloc( ( BOX_PLANES + count ) * sizeof( *cm_local.cm.planes ), h_high );
			cm_local.cm.numPlanes = count;

			out = cm_local.cm.planes;	

			for ( i=0 ; i<count ; i++, in++, out++)
			{
				bits = 0;
				for (j=0 ; j<3 ; j++)
				{
					out.normal[j] = LittleFloat (in.normal[j]);
					if (out.normal[j] < 0)
						bits |= 1<<j;
				}

				out.dist = LittleFloat (in.dist);
				out.type = PlaneTypeForNormal( out.normal );
				out.signbits = bits;
			}
		}

		/*
		=================
		CMod_LoadLeafBrushes
		=================
		*/
		static void CMod_LoadLeafBrushes (lump_t *l)
		{
			int			i;
			int			*out;
			int		 	*in;
			int			count;
			
			in = (int*) (void *)(cmod_base + l.fileofs);
			if (l.filelen % sizeof(*in))
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");
			count = l.filelen / sizeof(*in);

			cm_local.cm.leafbrushes = (int*) Hunk_Alloc( (count + BOX_BRUSHES) * sizeof( *cm_local.cm.leafbrushes ), h_high );
			cm_local.cm.numLeafBrushes = count;

			out = cm_local.cm.leafbrushes;

			for ( i=0 ; i<count ; i++, in++, out++) {
				*out = LittleLong (*in);
			}
		}

		/*
		=================
		CMod_LoadLeafSurfaces
		=================
		*/
		static void CMod_LoadLeafSurfaces( lump_t *l )
		{
			int			i;
			int			*out;
			int		 	*in;
			int			count;
			
			in = (int*) (void *)(cmod_base + l.fileofs);
			if (l.filelen % sizeof(*in))
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");
			count = l.filelen / sizeof(*in);

			cm_local.cm.leafsurfaces = (int*) Hunk_Alloc( count * sizeof( *cm_local.cm.leafsurfaces ), h_high );
			cm_local.cm.numLeafSurfaces = count;

			out = cm_local.cm.leafsurfaces;

			for ( i=0 ; i<count ; i++, in++, out++) {
				*out = LittleLong (*in);
			}
		}

		/*
		=================
		CMod_LoadBrushSides
		=================
		*/
		static void CMod_LoadBrushSides (lump_t *l)
		{
			int				i;
			cbrushside_t	*out;
			dbrushside_t 	*in;
			int				count;
			int				num;

			in = (dbrushside_t*) (void *)(cmod_base + l.fileofs);
			if ( l.filelen % sizeof(*in) ) {
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");
			}
			count = l.filelen / sizeof(*in);

			cm_local.cm.brushsides = (cbrushside_t*) Hunk_Alloc( ( BOX_SIDES + count ) * sizeof( *cm_local.cm.brushsides ), h_high );
			cm_local.cm.numBrushSides = count;

			out = cm_local.cm.brushsides;	

			for ( i=0 ; i<count ; i++, in++, out++) {
				num = LittleLong( in.planeNum );
				out.plane = &cm_local.cm.planes[num];
				out.shaderNum = LittleLong( in.shaderNum );
				if ( out.shaderNum < 0 || out.shaderNum >= cm_local.cm.numShaders ) {
					common.Com_Error( errorParm_t.ERR_DROP, "CMod_LoadBrushSides: bad shaderNum: %i", out.shaderNum );
				}
				out.surfaceFlags = cm_local.cm.shaders[out.shaderNum].surfaceFlags;
			}
		}


		/*
		=================
		CMod_LoadEntityString
		=================
		*/
		static void CMod_LoadEntityString( lump_t *l ) {
			cm_local.cm.entityString = (char*) Hunk_Alloc( l.filelen, h_high );
			cm_local.cm.numEntityChars = l.filelen;
			Com_Memcpy (cm_local.cm.entityString, cmod_base + l.fileofs, l.filelen);
		}

		/*
		=================
		CMod_LoadVisibility
		=================
		*/
		const int VIS_HEADER = 8;
		static void CMod_LoadVisibility( lump_t *l ) {
			int		len;
			byte	*buf;

		    len = l.filelen;
			if ( !len ) {
				cm_local.cm.clusterBytes = ( cm_local.cm.numClusters + 31 ) & ~31;
				cm_local.cm.visibility = (byte*) Hunk_Alloc( cm_local.cm.clusterBytes, h_high );
				Com_Memset( cm_local.cm.visibility, 255, cm_local.cm.clusterBytes );
				return;
			}
			buf = cmod_base + l.fileofs;

			cm_local.cm.vised = true;
			cm_local.cm.visibility = (byte*) Hunk_Alloc( len, h_high );
			cm_local.cm.numClusters = LittleLong( ((int *)buf)[0] );
			cm_local.cm.clusterBytes = LittleLong( ((int *)buf)[1] );
			Com_Memcpy (cm_local.cm.visibility, buf + VIS_HEADER, len - VIS_HEADER );
		}

		//==================================================================


		/*
		=================
		CMod_LoadPatches
		=================
		*/
		const int MAX_PATCH_VERTS = 1024;
		static void CMod_LoadPatches( lump_t *surfs, lump_t *verts ) {
			drawVert_t	*dv, *dv_p;
			dsurface_t	*in;
			int			count;
			int			i, j;
			int			c;
			cPatch_t	*patch;
			vec3_t		points[MAX_PATCH_VERTS];
			int			width, height;
			int			shaderNum;

			in = (dsurface_t*) (void *)(cmod_base + surfs.fileofs);
			if (surfs.filelen % sizeof(*in))
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");
			cm_local.cm.numSurfaces = count = surfs.filelen / sizeof(*in);
			cm_local.cm.surfaces = (cPatch_t**) Hunk_Alloc( cm_local.cm.numSurfaces * sizeof( cm_local.cm.surfaces[0] ), h_high );

			dv = (drawVert_t*) (void *)(cmod_base + verts.fileofs);
			if (verts.filelen % sizeof(*dv))
				common.Com_Error (errorParm_t.ERR_DROP, "MOD_LoadBmodel: funny lump size");

			// scan through all the surfaces, but only load patches,
			// not planar faces
			for ( i = 0 ; i < count ; i++, in++ ) {
				if ( LittleLong( in.surfaceType ) != MST_PATCH ) {
					continue;		// ignore other surfaces
				}
				// FIXME: check for non-colliding patches

				cm_local.cm.surfaces[ i ] = patch = (cPatch_t*) Hunk_Alloc( sizeof( *patch ), h_high );

				// load the full drawverts onto the stack
				width = LittleLong( in.patchWidth );
				height = LittleLong( in.patchHeight );
				c = width * height;
				if ( c > MAX_PATCH_VERTS ) {
					common.Com_Error( errorParm_t.ERR_DROP, "ParseMesh: MAX_PATCH_VERTS" );
				}

				dv_p = dv + LittleLong( in.firstVert );
				for ( j = 0 ; j < c ; j++, dv_p++ ) {
					points[j][0] = LittleFloat( dv_p.xyz[0] );
					points[j][1] = LittleFloat( dv_p.xyz[1] );
					points[j][2] = LittleFloat( dv_p.xyz[2] );
				}

				shaderNum = LittleLong( in.shaderNum );
				patch.contents = cm_local.cm.shaders[shaderNum].contentFlags;
				patch.surfaceFlags = cm_local.cm.shaders[shaderNum].surfaceFlags;

				// create the internal facet structure
				patch.pc = CM_GeneratePatchCollide( width, height, points );
			}
		}

		//==================================================================

		static unsigned CM_LumpChecksum(lump_t *lump) {
			return LittleLong (Com_BlockChecksum (cmod_base + lump.fileofs, lump.filelen));
		}

		static unsigned CM_Checksum(dheader_t *header) {
			unsigned checksums[16];
			checksums[0] = CM_LumpChecksum(&header.lumps[LUMP_SHADERS]);
			checksums[1] = CM_LumpChecksum(&header.lumps[LUMP_LEAFS]);
			checksums[2] = CM_LumpChecksum(&header.lumps[LUMP_LEAFBRUSHES]);
			checksums[3] = CM_LumpChecksum(&header.lumps[LUMP_LEAFSURFACES]);
			checksums[4] = CM_LumpChecksum(&header.lumps[LUMP_PLANES]);
			checksums[5] = CM_LumpChecksum(&header.lumps[LUMP_BRUSHSIDES]);
			checksums[6] = CM_LumpChecksum(&header.lumps[LUMP_BRUSHES]);
			checksums[7] = CM_LumpChecksum(&header.lumps[LUMP_MODELS]);
			checksums[8] = CM_LumpChecksum(&header.lumps[LUMP_NODES]);
			checksums[9] = CM_LumpChecksum(&header.lumps[LUMP_SURFACES]);
			checksums[10] = CM_LumpChecksum(&header.lumps[LUMP_DRAWVERTS]);

			return LittleLong(Com_BlockChecksum(checksums, 11 * 4));
		}

		/*
		==================
		CM_LoadMap

		Loads in the map and all submodels
		==================
		*/
		static void CM_LoadMap( const char *name, bool clientload, int *checksum ) {
			int				*buf;
			int				i;
			dheader_t		header;
			int				length;
			static unsigned	last_checksum;

			if ( !name || !name[0] ) {
				common.Com_Error( errorParm_t.ERR_DROP, "CM_LoadMap: NULL name" );
			}

			cm_noAreas = Cvar_Get ("cm_noAreas", "0", CVAR_CHEAT);
			cm_noCurves = Cvar_Get ("cm_noCurves", "0", CVAR_CHEAT);
			cm_playerCurveClip = Cvar_Get ("cm_playerCurveClip", "1", CVAR_ARCHIVE|CVAR_CHEAT );
			Com_DPrintf( "CM_LoadMap( %s, %i )\n", name, clientload );

			if ( !strcmp( cm_local.cm.name, name ) && clientload ) {
				*checksum = last_checksum;
				return;
			}

			// free old stuff
			Com_Memset( &cm, 0, sizeof( cm ) );
			CM_ClearLevelPatches();

			if ( !name[0] ) {
				cm_local.cm.numLeafs = 1;
				cm_local.cm.numClusters = 1;
				cm_local.cm.numAreas = 1;
				cm_local.cm.cmodels = (cmodel_t*) Hunk_Alloc( sizeof( *cm_local.cm.cmodels ), h_high );
				*checksum = 0;
				return;
			}

			//
			// load the file
			//
			length = FS_ReadFile( name, (void **)&buf );

			if ( !buf ) {
				common.Com_Error (errorParm_t.ERR_DROP, "Couldn't load %s", name);
			}

			last_checksum = LittleLong (Com_BlockChecksum (buf, length));
			*checksum = last_checksum;

			header = *(dheader_t *)buf;
			for (i=0 ; i<sizeof(dheader_t)/4 ; i++) {
				((int *)&header)[i] = LittleLong ( ((int *)&header)[i]);
			}

			if ( header.version != BSP_VERSION ) {
				common.Com_Error (errorParm_t.ERR_DROP, "CM_LoadMap: %s has wrong version number (%i should be %i)"
				, name, header.version, BSP_VERSION );
			}

			cmod_base = (byte *)buf;

			// load into heap
			CMod_LoadShaders( &header.lumps[LUMP_SHADERS] );
			CMod_LoadLeafs (&header.lumps[LUMP_LEAFS]);
			CMod_LoadLeafBrushes (&header.lumps[LUMP_LEAFBRUSHES]);
			CMod_LoadLeafSurfaces (&header.lumps[LUMP_LEAFSURFACES]);
			CMod_LoadPlanes (&header.lumps[LUMP_PLANES]);
			CMod_LoadBrushSides (&header.lumps[LUMP_BRUSHSIDES]);
			CMod_LoadBrushes (&header.lumps[LUMP_BRUSHES]);
			CMod_LoadSubmodels (&header.lumps[LUMP_MODELS]);
			CMod_LoadNodes (&header.lumps[LUMP_NODES]);
			CMod_LoadEntityString (&header.lumps[LUMP_ENTITIES]);
			CMod_LoadVisibility( &header.lumps[LUMP_VISIBILITY] );
			CMod_LoadPatches( &header.lumps[LUMP_SURFACES], &header.lumps[LUMP_DRAWVERTS] );

			// we are NOT freeing the file, because it is cached for the ref
			FS_FreeFile (buf);

			CM_InitBoxHull ();

			CM_FloodAreaConnections ();

			// allow this to be cached if it is loaded by the server
			if ( !clientload ) {
				Q_strncpyz( cm_local.cm.name, name, sizeof( cm_local.cm.name ) );
			}
		}

		/*
		==================
		CM_ClearMap
		==================
		*/
		public static void CM_ClearMap( ) 
		{
			Com_Memset( &cm, 0, sizeof( cm ) );
			CM_ClearLevelPatches();
		}

		/*
		==================
		CM_ClipHandleToModel
		==================
		*/
		public static cmodel_t?	CM_ClipHandleToModel( clipHandle_t handle ) 
		{
			if ( handle.ID < 0 ) 
			{
				common.Com_Error( errorParm_t.ERR_DROP, "CM_ClipHandleToModel: bad handle %i", handle );
			}
			if ( handle.ID < cm_local.cm.numSubModels ) 
			{
				return cm_local.cm.cmodels[handle.ID];
			}
			if ( handle.ID == cm_local.BOX_MODEL_HANDLE )
				return box_model;

			if ( handle.ID < cm_local.MAX_SUBMODELS ) 
			{
				common.Com_Error( errorParm_t.ERR_DROP, "CM_ClipHandleToModel: bad handle %i < %i < %i",
					cm_local.cm.numSubModels, handle, cm_local.MAX_SUBMODELS );
			}

			common.Com_Error( errorParm_t.ERR_DROP, "CM_ClipHandleToModel: bad handle %i", handle.ID + cm_local.MAX_SUBMODELS );

			return null;

		}

		/*
		==================
		CM_InlineModel
		==================
		*/
		static clipHandle_t	CM_InlineModel( int index ) {
			if ( index < 0 || index >= cm_local.cm.numSubModels ) {
				common.Com_Error (errorParm_t.ERR_DROP, "CM_InlineModel: bad number");
			}
			return index;
		}

		static int CM_NumClusters( ) 
{
			return cm_local.cm.numClusters;
		}

		static int CM_NumInlineModels( )
		{
			return cm_local.cm.numSubModels;
		}

		static string CM_EntityString( ) 
		{
			return cm_local.cm.entityString;
		}

		static int CM_LeafCluster( int leafnum )
{
			if (leafnum < 0 || leafnum >= cm_local.cm.numLeafs) {
				common.Com_Error (errorParm_t.ERR_DROP, "CM_LeafCluster: bad number");
			}
			return cm_local.cm.leafs[leafnum].cluster;
		}

		static int CM_LeafArea( int leafnum ) 
{
			if ( leafnum < 0 || leafnum >= cm_local.cm.numLeafs ) {
				common.Com_Error (errorParm_t.ERR_DROP, "CM_LeafArea: bad number");
			}
			return cm_local.cm.leafs[leafnum].area;
		}

		//=======================================================================


		/*
		===================
		CM_InitBoxHull

		Set up the planes and nodes so that the six floats of a bounding box
		can just be stored out and get a proper clipping hull structure.
		===================
		*/
		static void CM_InitBoxHull ()
		{
			int			i;
			int			side;
			cplane_t	p;
			cbrushside_t	s;

			cm_load.box_planes = cm_local.cm.planes[cm_local.cm.numPlanes];

			cm_load.box_brush = cm_local.cm.brushes[cm_local.cm.numBrushes];
			cm_load.box_brush.numsides = 6;
			cm_load.box_brush.sides = cm_local.cm.brushsides + cm_local.cm.numBrushSides;
			cm_load.box_brush.contents = CONTENTS_BODY;

			cm_load.box_model.leaf.numLeafBrushes = 1;
		//	box_model.leaf.firstLeafBrush = cm_local.cm.numBrushes;
			cm_load.box_model.leaf.firstLeafBrush = cm_local.cm.numLeafBrushes;
			cm_local.cm.leafbrushes[cm_local.cm.numLeafBrushes] = cm_local.cm.numBrushes;

			for (i=0 ; i<6 ; i++)
			{
				side = i&1;

				// brush sides
				s = cm_local.cm.brushsides[cm_local.cm.numBrushSides+i];
				s.plane = 	cm_local.cm.planes + (cm_local.cm.numPlanes+i*2+side);
				s.surfaceFlags = 0;

				// planes
				p = cm_load.box_planes[i*2];
				p.type = i>>1;
				p.signbits = 0;
				q_shared.VectorClear (ref p.normal);
				p.normal[i>>1] = 1;

				p = cm_load.box_planes[i*2+1];
				p.type = 3 + (i>>1);
				p.signbits = 0;
				q_shared.VectorClear (ref p.normal);
				p.normal[i>>1] = -1;

				q_math.SetPlaneSignbits( ref p );
			}	
		}

		/*
		===================
		CM_TempBoxModel

		To keep everything totally uniform, bounding boxes are turned into small
		BSP trees instead of being compared directly.
		Capsules are handled differently though.
		===================
		*/
		static clipHandle_t CM_TempBoxModel( vec3_t mins, vec3_t maxs, int capsule ) 
		{
			q_shared.VectorCopy( mins, out cm_load.box_model.mins );
			q_shared.VectorCopy( maxs, out cm_load.box_model.maxs );

			if ( capsule != 0 )
				return new clipHandle_t( cm_local.CAPSULE_MODEL_HANDLE );

			cm_load.box_planes[0].dist = maxs[0];
			cm_load.box_planes[1].dist = -maxs[0];
			cm_load.box_planes[2].dist = mins[0];
			cm_load.box_planes[3].dist = -mins[0];
			cm_load.box_planes[4].dist = maxs[1];
			cm_load.box_planes[5].dist = -maxs[1];
			cm_load.box_planes[6].dist = mins[1];
			cm_load.box_planes[7].dist = -mins[1];
			cm_load.box_planes[8].dist = maxs[2];
			cm_load.box_planes[9].dist = -maxs[2];
			cm_load.box_planes[10].dist = mins[2];
			cm_load.box_planes[11].dist = -mins[2];

			q_shared.VectorCopy( mins, out cm_load.box_brush.bounds[0] );
			q_shared.VectorCopy( maxs, out cm_load.box_brush.bounds[1] );

			return new clipHandle_t( cm_local.BOX_MODEL_HANDLE );
		}

		/*
		===================
		CM_ModelBounds
		===================
		*/
		public static void CM_ModelBounds( clipHandle_t model, out vec3_t mins, out vec3_t maxs ) 
		{
			var cmod = CM_ClipHandleToModel( model );

			if ( !cmod.HasValue )
				common.Com_Error( errorParm_t.ERR_DROP, "Model has a null value in CL_ModelBounds");

			q_shared.VectorCopy( cmod.Value.mins, out mins );
			q_shared.VectorCopy( cmod.Value.maxs, out maxs );
		}
	}
}
