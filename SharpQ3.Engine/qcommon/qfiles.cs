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

namespace SharpQ3.Engine.qcommon
{
	// qfiles.h: quake file formats
	public static class qfiles
	{
		// surface geometry should not exceed these limits
		const int SHADER_MAX_VERTEXES = 1000;
		const int SHADER_MAX_INDEXES = 6*qfiles.SHADER_MAX_VERTEXES;


		// the maximum size of game relative pathnames
		const int MAX_QPATH = 64;

		/*
		========================================================================

		QVM files

		========================================================================
		*/

		const int VM_MAGIC = 0x12721444;

		private struct vmHeader_t {
			int		vmMagic;

			int		instructionCount;

			int		codeOffset;
			int		codeLength;

			int		dataOffset;
			int		dataLength;
			int		litLength;			// ( dataLength - litLength ) should be byteswapped on load
			int		bssLength;			// zero filled memory appended to datalength
		}


		/*
		========================================================================

		PCX files are used for 8 bit images

		========================================================================
		*/

		private struct pcx_t {
		    char	manufacturer;
		    char	version;
		    char	encoding;
		    char	bits_per_pixel;
		    unsigned short	xmin,ymin,xmax,ymax;
		    unsigned short	hres,vres;
		    unsigned char	palette[48];
		    char	reserved;
		    char	color_planes;
		    unsigned short	bytes_per_line;
		    unsigned short	palette_type;
		    char	filler[58];
		    unsigned char	data;			// unbounded
		}


		/*
		========================================================================

		TGA files are used for 24/32 bit images

		========================================================================
		*/

		private struct TargaHeader {
			unsigned char 	id_length, colormap_type, image_type;
			unsigned short	colormap_index, colormap_length;
			unsigned char	colormap_size;
			unsigned short	x_origin, y_origin, width, height;
			unsigned char	pixel_size, attributes;
		}



		/*
		========================================================================

		.MD3 triangle model file format

		========================================================================
		*/

		#define MD3_IDENT			(('3'<<24)+('P'<<16)+('D'<<8)+'I')
		#define MD3_VERSION			15

		// limits
		#define MD3_MAX_LODS		3
		#define	MD3_MAX_TRIANGLES	8192	// per surface
		#define MD3_MAX_VERTS		4096	// per surface
		#define MD3_MAX_SHADERS		256		// per surface
		#define MD3_MAX_FRAMES		1024	// per model
		#define	MD3_MAX_SURFACES	32		// per model
		#define MD3_MAX_TAGS		16		// per frame

		// vertex scales
		#define	MD3_XYZ_SCALE		(1.0/64)

		private struct md3Frame_t {
			vec3_t		bounds[2];
			vec3_t		localOrigin;
			float		radius;
			char		name[16];
		}

		private struct md3Tag_t {
			char		name[MAX_QPATH];	// tag name
			vec3_t		origin;
			vec3_t		axis[3];
		}

		/*
		** md3Surface_t
		**
		** CHUNK			SIZE
		** header			sizeof( md3Surface_t )
		** shaders			sizeof( md3Shader_t ) * numShaders
		** triangles[0]		sizeof( md3Triangle_t ) * numTriangles
		** st				sizeof( md3St_t ) * numVerts
		** XyzNormals		sizeof( md3XyzNormal_t ) * numVerts * numFrames
		*/
		private struct md3Surface_t {
			int		ident;				// 

			char	name[MAX_QPATH];	// polyset name

			int		flags;
			int		numFrames;			// all surfaces in a model should have the same

			int		numShaders;			// all surfaces in a model should have the same
			int		numVerts;

			int		numTriangles;
			int		ofsTriangles;

			int		ofsShaders;			// offset from start of md3Surface_t
			int		ofsSt;				// texture coords are common for all frames
			int		ofsXyzNormals;		// numVerts * numFrames

			int		ofsEnd;				// next surface follows
		}

		private struct md3Shader_t {
			char			name[MAX_QPATH];
			int				shaderIndex;	// for in-game use
		}

		private struct md3Triangle_t {
			int			indexes[3];
		}

		private struct md3St_t {
			float		st[2];
		}

		private struct md3XyzNormal_t {
			short		xyz[3];
			short		normal;
		}

		private struct md3Header_t {
			int			ident;
			int			version;

			char		name[MAX_QPATH];	// model name

			int			flags;

			int			numFrames;
			int			numTags;			
			int			numSurfaces;

			int			numSkins;

			int			ofsFrames;			// offset for first frame
			int			ofsTags;			// numFrames * numTags
			int			ofsSurfaces;		// first surface, others follow

			int			ofsEnd;				// end of file
		}

		/*
		==============================================================================

		MD4 file format

		==============================================================================
		*/

		#define MD4_IDENT			(('4'<<24)+('P'<<16)+('D'<<8)+'I')
		#define MD4_VERSION			1
		#define	MD4_MAX_BONES		128

		private struct md4Weight_t {
			int			boneIndex;		// these are indexes into the boneReferences,
			float		   boneWeight;		// not the global per-frame bone list
			vec3_t		offset;
		}

		private struct md4Vertex_t {
			vec3_t		normal;
			vec2_t		texCoords;
			int			numWeights;
			md4Weight_t	weights[1];		// variable sized
		}

		private struct md4Triangle_t {
			int			indexes[3];
		}

		private struct md4Surface_t {
			int			ident;

			char		name[MAX_QPATH];	// polyset name
			char		shader[MAX_QPATH];
			int			shaderIndex;		// for in-game use

			int			ofsHeader;			// this will be a negative number

			int			numVerts;
			int			ofsVerts;

			int			numTriangles;
			int			ofsTriangles;

			// Bone references are a set of ints representing all the bones
			// present in any vertex weights for this surface.  This is
			// needed because a model may have surfaces that need to be
			// drawn at different sort times, and we don't want to have
			// to re-interpolate all the bones for each surface.
			int			numBoneReferences;
			int			ofsBoneReferences;

			int			ofsEnd;				// next surface follows
		}

		private struct md4Bone_t {
			float		matrix[3][4];
		}

		private struct md4Frame_t {
			vec3_t		bounds[2];			// bounds of all surfaces of all LOD's for this frame
			vec3_t		localOrigin;		// midpoint of bounds, used for sphere cull
			float		radius;				// dist from localOrigin to corner
			md4Bone_t	bones[1];			// [numBones]
		}

		private struct md4LOD_t {
			int			numSurfaces;
			int			ofsSurfaces;		// first surface, others follow
			int			ofsEnd;				// next lod follows
		}

		private struct md4Header_t {
			int			ident;
			int			version;

			char		name[MAX_QPATH];	// model name

			// frames and bones are shared by all levels of detail
			int			numFrames;
			int			numBones;
			int			ofsBoneNames;		// char	name[ MAX_QPATH ]
			int			ofsFrames;			// md4Frame_t[numFrames]

			// each level of detail has completely separate sets of surfaces
			int			numLODs;
			int			ofsLODs;

			int			ofsEnd;				// end of file
		}


		/*
		==============================================================================

		  .BSP file format

		==============================================================================
		*/


		#define BSP_IDENT	(('P'<<24)+('S'<<16)+('B'<<8)+'I')
				// little-endian "IBSP"

		#define BSP_VERSION			46


		// there shouldn't be any problem with increasing these values at the
		// expense of more memory allocation in the utilities
		const int MAX_MAP_MODELS = 0x400;
		const int MAX_MAP_BRUSHES = 0x8000;
		const int MAX_MAP_ENTITIES = 0x800;
		const int MAX_MAP_ENTSTRING = 0x40000;
		const int MAX_MAP_SHADERS = 0x400;

		const int MAX_MAP_AREAS = 0x100;	// MAX_MAP_AREA_BYTES in q_shared must match!
		const int MAX_MAP_FOGS = 0x100;
		const int MAX_MAP_PLANES = 0x20000;
		const int MAX_MAP_NODES = 0x20000;
		const int MAX_MAP_BRUSHSIDES = 0x20000;
		const int MAX_MAP_LEAFS = 0x20000;
		const int MAX_MAP_LEAFFACES = 0x20000;
		const int MAX_MAP_LEAFBRUSHES = 0x40000;
		const int MAX_MAP_PORTALS = 0x20000;
		const int MAX_MAP_LIGHTING = 0x800000;
		const int MAX_MAP_LIGHTGRID = 0x800000;
		const int MAX_MAP_VISIBILITY = 0x200000;

		const int MAX_MAP_DRAW_SURFS = 0x20000;
		const int MAX_MAP_DRAW_VERTS = 0x80000;
		const int MAX_MAP_DRAW_INDEXES = 0x80000;


		// key / value pair sizes in the entities lump
		const int MAX_KEY = 32;
		const int MAX_VALUE = 1024;

		// the editor uses these predefined yaw angles to orient entities up or down
		const int ANGLE_UP = -1;
		const int ANGLE_DOWN = -2;

		const int LIGHTMAP_WIDTH = 128;
		const int LIGHTMAP_HEIGHT = 128;

		const int MAX_WORLD_COORD = 128*1024;
		const int MIN_WORLD_COORD = -128*1024;
		const int WORLD_SIZE = qfiles.MAX_WORLD_COORD - qfiles.MIN_WORLD_COORD;

		//=============================================================================


		private struct lump_t {
			int		fileofs, filelen;
		}

		const int LUMP_ENTITIES = 0;
		const int LUMP_SHADERS = 1;
		const int LUMP_PLANES = 2;
		const int LUMP_NODES = 3;
		const int LUMP_LEAFS = 4;
		const int LUMP_LEAFSURFACES = 5;
		const int LUMP_LEAFBRUSHES = 6;
		const int LUMP_MODELS = 7;
		const int LUMP_BRUSHES = 8;
		const int LUMP_BRUSHSIDES = 9;
		const int LUMP_DRAWVERTS = 10;
		const int LUMP_DRAWINDEXES = 11;
		const int LUMP_FOGS = 12;
		const int LUMP_SURFACES = 13;
		const int LUMP_LIGHTMAPS = 14;
		const int LUMP_LIGHTGRID = 15;
		const int LUMP_VISIBILITY = 16;
		const int HEADER_LUMPS = 17;

		private struct dheader_t {
			int			ident;
			int			version;

			lump_t		lumps[HEADER_LUMPS];
		}

		private struct dmodel_t {
			float		mins[3], maxs[3];
			int			firstSurface, numSurfaces;
			int			firstBrush, numBrushes;
		}

		private struct dshader_t {
			char		shader[MAX_QPATH];
			int			surfaceFlags;
			int			contentFlags;
		}

		// planes x^1 is allways the opposite of plane x

		private struct dplane_t {
			float		normal[3];
			float		dist;
		}

		private struct dnode_t {
			int			planeNum;
			int			children[2];	// negative numbers are -(leafs+1), not nodes
			int			mins[3];		// for frustom culling
			int			maxs[3];
		}

		private struct dleaf_t {
			int			cluster;			// -1 = opaque cluster (do I still store these?)
			int			area;

			int			mins[3];			// for frustum culling
			int			maxs[3];

			int			firstLeafSurface;
			int			numLeafSurfaces;

			int			firstLeafBrush;
			int			numLeafBrushes;
		}

		private struct dbrushside_t {
			int			planeNum;			// positive plane side faces out of the leaf
			int			shaderNum;
		}

		private struct dbrush_t {
			int			firstSide;
			int			numSides;
			int			shaderNum;		// the shader that determines the contents flags
		}

		private struct dfog_t {
			char		shader[MAX_QPATH];
			int			brushNum;
			int			visibleSide;	// the brush side that ray tests need to clip against (-1 == none)
		}

		private struct drawVert_t {
			vec3_t		xyz;
			float		st[2];
			float		lightmap[2];
			vec3_t		normal;
			byte		color[4];
		}

		private enum mapSurfaceType_t {
			MST_BAD,
			MST_PLANAR,
			MST_PATCH,
			MST_TRIANGLE_SOUP,
			MST_FLARE
		}

		private struct dsurface_t {
			int			shaderNum;
			int			fogNum;
			int			surfaceType;

			int			firstVert;
			int			numVerts;

			int			firstIndex;
			int			numIndexes;

			int			lightmapNum;
			int			lightmapX, lightmapY;
			int			lightmapWidth, lightmapHeight;

			vec3_t		lightmapOrigin;
			vec3_t		lightmapVecs[3];	// for patches, [0] and [1] are lodbounds

			int			patchWidth;
			int			patchHeight;
		}
	}
}
