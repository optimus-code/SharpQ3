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

namespace SharpQ3.Engine.renderer
{
	public static class tr_local
	{
		#define GL_INDEX_TYPE		GL_UNSIGNED_INT
		typedef unsigned int glIndex_t;

		// fast float to int conversion
		#define	myftol(x) ((int)(x))


		// everything that is needed by the backend needs
		// to be double buffered to allow it to run in
		// parallel on a dual cpu machine
		#define	SMP_FRAMES		2

		// 12 bits
		// see QSORT_SHADERNUM_SHIFT
		#define	MAX_SHADERS				16384

		// can't be increased without changing bit packing for drawsurfs

		typedef struct dlight_s {
			vec3_t	origin;
			vec3_t	color;				// range from 0.0 to 1.0, should be color normalized
			float	radius;

			vec3_t	transformed;		// origin in local coordinate system
			int		additive;			// texture detail is lost tho when the lightmap is dark
		} dlight_t;


		// a trRefEntity_t has all the information passed in by
		// the client game, as well as some locally derived info
		typedef struct {
			refEntity_t	e;

			float		axisLength;		// compensate for non-normalized axis

			bool	needDlights;	// true for bmodels that touch a dlight
			bool	lightingCalculated;
			vec3_t		lightDir;		// normalized direction towards light
			vec3_t		ambientLight;	// color normalized to 0-255
			int			ambientLightInt;	// 32 bit rgba packed
			vec3_t		directedLight;
		} trRefEntity_t;


		typedef struct {
			vec3_t		origin;			// in world coordinates
			vec3_t		axis[3];		// orientation in world
			vec3_t		viewOrigin;		// viewParms->or.origin in local coordinates
			float		modelMatrix[16];
		} orientationr_t;

		typedef struct image_s {
			char		imgName[MAX_QPATH];		// game path, including extension
			int			width, height;				// source image
			int			uploadWidth, uploadHeight;	// after power of two and picmip but not including clamp to MAX_TEXTURE_SIZE
			GLuint		texnum;					// gl texture binding

			int			frameUsed;			// for texture usage in frame statistics

			int			internalFormat;

			bool	mipmap;
			bool	allowPicmip;
			int			wrapClampMode;		// GL_CLAMP or GL_REPEAT

		    int         index; // this image == tr.images[index]

			struct image_s*	next;
		} image_t;

		//===============================================================================

		typedef enum {
			SS_BAD,
			SS_PORTAL,			// mirrors, portals, viewscreens
			SS_ENVIRONMENT,		// sky box
			SS_OPAQUE,			// opaque

			SS_DECAL,			// scorch marks, etc.
			SS_SEE_THROUGH,		// ladders, grates, grills that may have small blended edges
								// in addition to alpha test
			SS_BANNER,

			SS_FOG,

			SS_UNDERWATER,		// for items that should be drawn in front of the water plane

			SS_BLEND0,			// regular transparency and filters
			SS_BLEND1,			// generally only used for additive type effects
			SS_BLEND2,
			SS_BLEND3,

			SS_BLEND6,
			SS_STENCIL_SHADOW,
			SS_ALMOST_NEAREST,	// gun smoke puffs

			SS_NEAREST			// blood blobs
		} shaderSort_t;


		#define MAX_SHADER_STAGES 8

		typedef enum {
			GF_NONE,

			GF_SIN,
			GF_SQUARE,
			GF_TRIANGLE,
			GF_SAWTOOTH, 
			GF_INVERSE_SAWTOOTH, 

			GF_NOISE

		} genFunc_t;


		typedef enum {
			DEFORM_NONE,
			DEFORM_WAVE,
			DEFORM_NORMALS,
			DEFORM_BULGE,
			DEFORM_MOVE,
			DEFORM_PROJECTION_SHADOW,
			DEFORM_AUTOSPRITE,
			DEFORM_AUTOSPRITE2,
			DEFORM_TEXT0,
			DEFORM_TEXT1,
			DEFORM_TEXT2,
			DEFORM_TEXT3,
			DEFORM_TEXT4,
			DEFORM_TEXT5,
			DEFORM_TEXT6,
			DEFORM_TEXT7
		} deform_t;

		typedef enum {
			AGEN_IDENTITY,
			AGEN_SKIP,
			AGEN_ENTITY,
			AGEN_ONE_MINUS_ENTITY,
			AGEN_VERTEX,
			AGEN_ONE_MINUS_VERTEX,
			AGEN_LIGHTING_SPECULAR,
			AGEN_WAVEFORM,
			AGEN_PORTAL,
			AGEN_CONST
		} alphaGen_t;

		typedef enum {
			CGEN_BAD,
			CGEN_IDENTITY_LIGHTING,	// tr.identityLight
			CGEN_IDENTITY,			// always (1,1,1,1)
			CGEN_ENTITY,			// grabbed from entity's modulate field
			CGEN_ONE_MINUS_ENTITY,	// grabbed from 1 - entity.modulate
			CGEN_EXACT_VERTEX,		// tess.vertexColors
			CGEN_VERTEX,			// tess.vertexColors * tr.identityLight
			CGEN_ONE_MINUS_VERTEX,
			CGEN_WAVEFORM,			// programmatically generated
			CGEN_LIGHTING_DIFFUSE,
			CGEN_FOG,				// standard fog
			CGEN_CONST				// fixed color
		} colorGen_t;

		typedef enum {
			TCGEN_BAD,
			TCGEN_IDENTITY,			// clear to 0,0
			TCGEN_LIGHTMAP,
			TCGEN_TEXTURE,
			TCGEN_ENVIRONMENT_MAPPED,
			TCGEN_FOG,
			TCGEN_VECTOR			// S and T from world coordinates
		} texCoordGen_t;

		typedef enum {
			ACFF_NONE,
			ACFF_MODULATE_RGB,
			ACFF_MODULATE_RGBA,
			ACFF_MODULATE_ALPHA
		} acff_t;

		typedef struct {
			genFunc_t	func;

			float base;
			float amplitude;
			float phase;
			float frequency;
		} waveForm_t;

		#define TR_MAX_TEXMODS 4

		typedef enum {
			TMOD_NONE,
			TMOD_TRANSFORM,
			TMOD_TURBULENT,
			TMOD_SCROLL,
			TMOD_SCALE,
			TMOD_STRETCH,
			TMOD_ROTATE,
			TMOD_ENTITY_TRANSLATE
		} texMod_t;

		#define	MAX_SHADER_DEFORMS	3
		typedef struct {
			deform_t	deformation;			// vertex coordinate modification type

			vec3_t		moveVector;
			waveForm_t	deformationWave;
			float		deformationSpread;

			float		bulgeWidth;
			float		bulgeHeight;
			float		bulgeSpeed;
		} deformStage_t;


		typedef struct {
			texMod_t		type;

			// used for TMOD_TURBULENT and TMOD_STRETCH
			waveForm_t		wave;

			// used for TMOD_TRANSFORM
			float			matrix[2][2];		// s' = s * m[0][0] + t * m[1][0] + trans[0]
			float			translate[2];		// t' = s * m[0][1] + t * m[0][1] + trans[1]

			// used for TMOD_SCALE
			float			scale[2];			// s *= scale[0]
			                                    // t *= scale[1]

			// used for TMOD_SCROLL
			float			scroll[2];			// s' = s + scroll[0] * time
												// t' = t + scroll[1] * time

			// + = clockwise
			// - = counterclockwise
			float			rotateSpeed;

		} texModInfo_t;


		#define	MAX_IMAGE_ANIMATIONS	8

		typedef struct {
			image_t			*image[MAX_IMAGE_ANIMATIONS];
			int				numImageAnimations;
			float			imageAnimationSpeed;

			texCoordGen_t	tcGen;
			vec3_t			tcGenVectors[2];

			int				numTexMods;
			texModInfo_t	*texMods;

			int				videoMapHandle;
			bool		isLightmap;
			bool		isVideoMap;
		} textureBundle_t;

		#define NUM_TEXTURE_BUNDLES 2

		typedef struct shaderStage_s {
			bool		active;
			
			textureBundle_t	bundle[NUM_TEXTURE_BUNDLES];

			waveForm_t		rgbWave;
			colorGen_t		rgbGen;

			waveForm_t		alphaWave;
			alphaGen_t		alphaGen;

			byte			constantColor[4];			// for CGEN_CONST and AGEN_CONST

			unsigned		stateBits;					// GLS_xxxx mask

			acff_t			adjustColorsForFog;

			bool		isDetail;

			// VULKAN
			VkPipeline		vk_pipeline = VK_NULL_HANDLE;
			VkPipeline		vk_portal_pipeline = VK_NULL_HANDLE;
			VkPipeline		vk_mirror_pipeline = VK_NULL_HANDLE;

			// DX12
			ID3D12PipelineState* dx_pipeline = nullptr;
			ID3D12PipelineState* dx_portal_pipeline = nullptr;
			ID3D12PipelineState* dx_mirror_pipeline = nullptr;
			
		} shaderStage_t;

		struct shaderCommands_s;

		#define LIGHTMAP_2D			-4		// shader is for 2D rendering
		#define LIGHTMAP_BY_VERTEX	-3		// pre-lit triangle models
		#define LIGHTMAP_WHITEIMAGE	-2
		#define	LIGHTMAP_NONE		-1

		typedef enum {
			CT_FRONT_SIDED,
			CT_BACK_SIDED,
			CT_TWO_SIDED
		} cullType_t;

		typedef enum {
			FP_NONE,		// surface is translucent and will just be adjusted properly
			FP_EQUAL,		// surface is opaque but possibly alpha tested
			FP_LE			// surface is trnaslucent, but still needs a fog pass (fog surface)
		} fogPass_t;

		typedef struct {
			float		cloudHeight;
			image_t		*outerbox[6], *innerbox[6];
		} skyParms_t;

		typedef struct {
			vec3_t	color;
			float	depthForOpaque;
		} fogParms_t;


		typedef struct shader_s {
			char		name[MAX_QPATH];		// game path, including extension
			int			lightmapIndex;			// for a shader to match, both name and lightmapIndex must match

			int			index;					// this shader == tr.shaders[index]
			int			sortedIndex;			// this shader == tr.sortedShaders[sortedIndex]

			float		sort;					// lower numbered shaders draw before higher numbered

			bool	defaultShader;			// we want to return index 0 if the shader failed to
												// load for some reason, but R_FindShader should
												// still keep a name allocated for it, so if
												// something calls RE_RegisterShader again with
												// the same name, we don't try looking for it again

			bool	explicitlyDefined;		// found in a .shader file

			int			surfaceFlags;			// if explicitlyDefined, this will have SURF_* flags
			int			contentFlags;

			bool	entityMergable;			// merge across entites optimizable (smoke, blood)

			bool	isSky;
			skyParms_t	sky;
			fogParms_t	fogParms;

			float		portalRange;			// distance to fog out at

			int			multitextureEnv;		// 0, GL_MODULATE, GL_ADD (FIXME: put in stage)

			cullType_t	cullType;				// CT_FRONT_SIDED, CT_BACK_SIDED, or CT_TWO_SIDED
			bool	polygonOffset;			// set for decals and other items that must be offset 
			bool	noMipMaps;				// for console fonts, 2D elements, etc.
			bool	noPicMip;				// for images that must always be full resolution

			fogPass_t	fogPass;				// draw a blended pass, possibly with depth test equals

			bool	needsNormal;			// not all shaders will need all data to be gathered
			bool	needsST1;
			bool	needsST2;
			bool	needsColor;

			int			numDeforms;
			deformStage_t	deforms[MAX_SHADER_DEFORMS];

			int			numUnfoggedPasses;
			shaderStage_t	*stages[MAX_SHADER_STAGES];		

		    float clampTime;                                  // time this shader is clamped to
		    float timeOffset;                                 // current time offset for this shader

		    struct shader_s *remappedShader;                  // current shader this one is remapped too

			struct	shader_s	*next;
		} shader_t;

		// trRefdef_t holds everything that comes in refdef_t,
		// as well as the locally generated scene information
		typedef struct {
			int			x, y, width, height;
			float		fov_x, fov_y;
			vec3_t		vieworg;
			vec3_t		viewaxis[3];		// transformation matrix

			int			time;				// time in milliseconds for shader effects and other time dependent rendering issues
			int			rdflags;			// RDF_NOWORLDMODEL, etc

			// 1 bits will prevent the associated area from rendering at all
			byte		areamask[MAX_MAP_AREA_BYTES];
			bool	areamaskModified;	// true if areamask changed since last scene

			float		floatTime;			// tr.refdef.time / 1000.0

			// text messages for deform text shaders
			char		text[MAX_RENDER_STRINGS][MAX_RENDER_STRING_LENGTH];

			int			num_entities;
			trRefEntity_t	*entities;

			int			num_dlights;
			struct dlight_s	*dlights;

			int			numPolys;
			struct srfPoly_s	*polys;

			int			numDrawSurfs;
			struct drawSurf_s	*drawSurfs;


		} trRefdef_t;


		//=================================================================================

		// skins allow models to be retextured without modifying the model file
		typedef struct {
			char		name[MAX_QPATH];
			shader_t	*shader;
		} skinSurface_t;

		typedef struct skin_s {
			char		name[MAX_QPATH];		// game path, including extension
			int			numSurfaces;
			skinSurface_t	*surfaces[MD3_MAX_SURFACES];
		} skin_t;


		typedef struct {
			int			originalBrushNumber;
			vec3_t		bounds[2];

			unsigned	colorInt;				// in packed byte format
			float		tcScale;				// texture coordinate vector scales
			fogParms_t	parms;

			// for clipping distance in fog when outside
			bool	hasSurface;
			float		surface[4];
		} fog_t;

		typedef struct {
			orientationr_t	or;
			orientationr_t	world;
			vec3_t		pvsOrigin;			// may be different than or.origin for portals
			bool	isPortal;			// true if this view is through a portal
			bool	isMirror;			// the portal is a mirror, invert the face culling
			int			frameCount;			// copied from tr.frameCount
			cplane_t	portalPlane;		// clip anything behind this if mirroring
			int			viewportX, viewportY, viewportWidth, viewportHeight;
			float		fovX, fovY;
			float		projectionMatrix[16];
			cplane_t	frustum[4];
			vec3_t		visBounds[2];
			float		zFar;
		} viewParms_t;


		/*
		==============================================================================

		SURFACES

		==============================================================================
		*/

		// any changes in surfaceType must be mirrored in rb_surfaceTable[]
		typedef enum {
			SF_BAD,
			SF_SKIP,				// ignore
			SF_FACE,
			SF_GRID,
			SF_TRIANGLES,
			SF_POLY,
			SF_MD3,
			SF_MD4,
			SF_FLARE,
			SF_ENTITY,				// beams, rails, lightning, etc that can be determined by entity

			SF_NUM_SURFACE_TYPES,
			SF_MAX = 0x7fffffff			// ensures that sizeof( surfaceType_t ) == sizeof( int )
		} surfaceType_t;

		typedef struct drawSurf_s {
			unsigned			sort;			// bit combination for fast compares
			surfaceType_t		*surface;		// any of surface*_t
		} drawSurf_t;

		#define	MAX_FACE_POINTS		64

		#define	MAX_PATCH_SIZE		32			// max dimensions of a patch mesh in map file
		#define	MAX_GRID_SIZE		65			// max dimensions of a grid mesh in memory

		// when cgame directly specifies a polygon, it becomes a srfPoly_t
		// as soon as it is called
		typedef struct srfPoly_s {
			surfaceType_t	surfaceType;
			qhandle_t		hShader;
			int				fogIndex;
			int				numVerts;
			polyVert_t		*verts;
		} srfPoly_t;

		typedef struct srfFlare_s {
			surfaceType_t	surfaceType;
			vec3_t			origin;
			vec3_t			normal;
			vec3_t			color;
		} srfFlare_t;

		typedef struct srfGridMesh_s {
			surfaceType_t	surfaceType;

			// dynamic lighting information
			int				dlightBits[SMP_FRAMES];

			// culling information
			vec3_t			meshBounds[2];
			vec3_t			localOrigin;
			float			meshRadius;

			// lod information, which may be different
			// than the culling information to allow for
			// groups of curves that LOD as a unit
			vec3_t			lodOrigin;
			float			lodRadius;
			int				lodFixed;
			int				lodStitched;

			// vertexes
			int				width, height;
			float			*widthLodError;
			float			*heightLodError;
			drawVert_t		verts[1];		// variable sized
		} srfGridMesh_t;



		#define	VERTEXSIZE	8
		typedef struct {
			surfaceType_t	surfaceType;
			cplane_t	plane;

			// dynamic lighting information
			int			dlightBits[SMP_FRAMES];

			// triangle definitions (no normals at points)
			int			numPoints;
			int			numIndices;
			int			ofsIndices;
			float		points[1][VERTEXSIZE];	// variable sized
												// there is a variable length list of indices here also
		} srfSurfaceFace_t;


		// misc_models in maps are turned into direct geometry by q3map
		typedef struct {
			surfaceType_t	surfaceType;

			// dynamic lighting information
			int				dlightBits[SMP_FRAMES];

			// culling information (FIXME: use this!)
			vec3_t			bounds[2];
			vec3_t			localOrigin;
			float			radius;

			// triangle definitions
			int				numIndexes;
			int				*indexes;

			int				numVerts;
			drawVert_t		*verts;
		} srfTriangles_t;


		extern	void (*rb_surfaceTable[SF_NUM_SURFACE_TYPES])(void *);

		/*
		==============================================================================

		BRUSH MODELS

		==============================================================================
		*/


		//
		// in memory representation
		//

		public const int SIDE_FRONT = 0;
		public const int SIDE_BACK = 1;
		public const int SIDE_ON = 2;

		typedef struct msurface_s {
			int					viewCount;		// if == tr.viewCount, already added
			struct shader_s		*shader;
			int					fogIndex;

			surfaceType_t		*data;			// any of srf*_t
		} msurface_t;



		#define	CONTENTS_NODE		-1
		typedef struct mnode_s {
			// common with leaf and node
			int			contents;		// -1 for nodes, to differentiate from leafs
			int			visframe;		// node needs to be traversed if current
			vec3_t		mins, maxs;		// for bounding box culling
			struct mnode_s	*parent;

			// node specific
			cplane_t	*plane;
			struct mnode_s	*children[2];	

			// leaf specific
			int			cluster;
			int			area;

			msurface_t	**firstmarksurface;
			int			nummarksurfaces;
		} mnode_t;

		typedef struct {
			vec3_t		bounds[2];		// for culling
			msurface_t	*firstSurface;
			int			numSurfaces;
		} bmodel_t;

		typedef struct {
			char		name[MAX_QPATH];		// ie: maps/tim_dm2.bsp
			char		baseName[MAX_QPATH];	// ie: tim_dm2

			int			dataSize;

			int			numShaders;
			dshader_t	*shaders;

			bmodel_t	*bmodels;

			int			numplanes;
			cplane_t	*planes;

			int			numnodes;		// includes leafs
			int			numDecisionNodes;
			mnode_t		*nodes;

			int			numsurfaces;
			msurface_t	*surfaces;

			int			nummarksurfaces;
			msurface_t	**marksurfaces;

			int			numfogs;
			fog_t		*fogs;

			vec3_t		lightGridOrigin;
			vec3_t		lightGridSize;
			vec3_t		lightGridInverseSize;
			int			lightGridBounds[3];
			byte		*lightGridData;


			int			numClusters;
			int			clusterBytes;
			const byte	*vis;			// may be passed in by CM_LoadMap to save space

			byte		*novis;			// clusterBytes of 0xff

			char		*entityString;
			char		*entityParsePoint;
		} world_t;

		//======================================================================

		typedef enum {
			MOD_BAD,
			MOD_BRUSH,
			MOD_MESH,
			MOD_MD4
		} modtype_t;

		typedef struct model_s {
			char		name[MAX_QPATH];
			modtype_t	type;
			int			index;				// model = tr.models[model->index]

			int			dataSize;			// just for listing purposes
			bmodel_t	*bmodel;			// only if type == MOD_BRUSH
			md3Header_t	*md3[MD3_MAX_LODS];	// only if type == MOD_MESH
			md4Header_t	*md4;				// only if type == MOD_MD4

			int			 numLods;
		} model_t;


		#define	MAX_MOD_KNOWN	1024

		//====================================================
		extern	refimport_t		ri;

		#define	MAX_DRAWIMAGES			2048
		#define	MAX_LIGHTMAPS			256
		#define	MAX_SKINS				1024


		#define	MAX_DRAWSURFS			0x10000
		#define	DRAWSURF_MASK			(MAX_DRAWSURFS-1)

		/*

		the drawsurf sort data is packed into a single 32 bit value so it can be
		compared quickly during the qsorting process

		the bits are allocated as follows:

		21 - 31	: sorted shader index
		11 - 20	: entity index
		2 - 6	: fog index
		//2		: used to be clipped flag REMOVED - 03.21.00 rad
		0 - 1	: dlightmap index

			TTimo - 1.32
		17-31 : sorted shader index
		7-16  : entity index
		2-6   : fog index
		0-1   : dlightmap index
		*/
		#define	QSORT_SHADERNUM_SHIFT	17
		#define	QSORT_ENTITYNUM_SHIFT	7
		#define	QSORT_FOGNUM_SHIFT		2

		extern	int			gl_filter_min, gl_filter_max;

		/*
		** performanceCounters_t
		*/
		typedef struct {
			int		c_sphere_cull_patch_in, c_sphere_cull_patch_clip, c_sphere_cull_patch_out;
			int		c_box_cull_patch_in, c_box_cull_patch_clip, c_box_cull_patch_out;
			int		c_sphere_cull_md3_in, c_sphere_cull_md3_clip, c_sphere_cull_md3_out;
			int		c_box_cull_md3_in, c_box_cull_md3_clip, c_box_cull_md3_out;

			int		c_leafs;
			int		c_dlightSurfaces;
			int		c_dlightSurfacesCulled;
		} frontEndCounters_t;

		#define	FOG_TABLE_SIZE		256
		#define FUNCTABLE_SIZE		1024
		#define FUNCTABLE_SIZE2		10
		#define FUNCTABLE_MASK		(FUNCTABLE_SIZE-1)


		// the renderer front end should never modify glstate_t
		typedef struct {
			int			currenttextures[2];
			int			currenttmu;
			int			texEnv[2];
			int			faceCulling;
			unsigned long	glStateBits;
		} glstate_t;


		typedef struct {
			int		c_surfaces, c_shaders, c_vertexes, c_indexes, c_totalIndexes;
			
			int		c_dlightVertexes;
			int		c_dlightIndexes;

			int		msec;			// total msec for backend run
		} backEndCounters_t;

		// all state modified by the back end is seperated
		// from the front end state
		typedef struct {
			int			smpFrame;
			trRefdef_t	refdef;
			viewParms_t	viewParms;
			orientationr_t	or;
			backEndCounters_t	pc;
			bool	isHyperspace;
			trRefEntity_t	*currentEntity;

			bool	projection2D;	// if true, drawstretchpic doesn't need to change modes
			byte		color2D[4];
			trRefEntity_t	entity2D;	// currentEntity will point at this when doing 2D rendering
		} backEndState_t;

		/*
		** trGlobals_t 
		**
		** Most renderer globals are defined here.
		** backend functions should never modify any of these fields,
		** but may read fields that aren't dynamically modified
		** by the frontend.
		*/
		typedef struct {
			bool				registered;		// cleared at shutdown, set at beginRegistration

			int						visCount;		// incremented every time a new vis cluster is entered
			int						frameCount;		// incremented every frame
			int						viewCount;		// incremented every view (twice a scene if portaled)
													// and every R_MarkFragments call

			int						smpFrame;		// toggles from 0 to 1 every endFrame

			bool				worldMapLoaded;
			world_t					*world;

			const byte				*externalVisData;	// from RE_SetWorldVisData, shared with CM_Load

			image_t					*defaultImage;
			image_t					*scratchImage[32];
			image_t					*fogImage;
			image_t					*dlightImage;	// inverse-quare highlight for projective adding
			image_t					*whiteImage;			// full of 0xff
			image_t					*identityLightImage;	// full of tr.identityLightByte

			shader_t				*defaultShader;
		    shader_t                *cinematicShader;
			shader_t				*shadowShader;
			shader_t				*projectionShadowShader;

			int						numLightmaps;
			image_t					*lightmaps[MAX_LIGHTMAPS];

			trRefEntity_t			*currentEntity;
			trRefEntity_t			worldEntity;		// point currentEntity at this when rendering world
			int						currentEntityNum;
			int						shiftedEntityNum;	// currentEntityNum << QSORT_ENTITYNUM_SHIFT
			model_t					*currentModel;

			viewParms_t				viewParms;

			float					identityLight;		// 1.0 / ( 1 << overbrightBits )
			int						identityLightByte;	// identityLight * 255
			int						overbrightBits;		// r_overbrightBits->integer, but set to 0 if no hw gamma

			orientationr_t			or;					// for current entity

			trRefdef_t				refdef;

			int						viewCluster;

			vec3_t					sunLight;			// from the sky shader for this level
			vec3_t					sunDirection;

			frontEndCounters_t		pc;
			int						frontEndMsec;		// not in pc due to clearing issue

			//
			// put large tables at the end, so most elements will be
			// within the +/32K indexed range on risc processors
			//
			model_t					*models[MAX_MOD_KNOWN];
			int						numModels;

			int						numImages;
			image_t					*images[MAX_DRAWIMAGES];

			// shader indexes from other modules will be looked up in tr.shaders[]
			// shader indexes from drawsurfs will be looked up in sortedShaders[]
			// lower indexed sortedShaders must be rendered first (opaque surfaces before translucent)
			int						numShaders;
			shader_t				*shaders[MAX_SHADERS];
			shader_t				*sortedShaders[MAX_SHADERS];

			int						numSkins;
			skin_t					*skins[MAX_SKINS];

			float					sinTable[FUNCTABLE_SIZE];
			float					squareTable[FUNCTABLE_SIZE];
			float					triangleTable[FUNCTABLE_SIZE];
			float					sawToothTable[FUNCTABLE_SIZE];
			float					inverseSawToothTable[FUNCTABLE_SIZE];
			float					fogTable[FOG_TABLE_SIZE];
		} trGlobals_t;

		enum RenderApi {
			RENDER_API_GL,
			RENDER_API_VK,
			RENDER_API_DX
		};


		#define	CULL_IN		0		// completely unclipped
		#define	CULL_CLIP	1		// clipped by one or more planes
		#define	CULL_OUT	2		// completely outside the clipping planes

		#define GLS_SRCBLEND_ZERO						0x00000001
		#define GLS_SRCBLEND_ONE						0x00000002
		#define GLS_SRCBLEND_DST_COLOR					0x00000003
		#define GLS_SRCBLEND_ONE_MINUS_DST_COLOR		0x00000004
		#define GLS_SRCBLEND_SRC_ALPHA					0x00000005
		#define GLS_SRCBLEND_ONE_MINUS_SRC_ALPHA		0x00000006
		#define GLS_SRCBLEND_DST_ALPHA					0x00000007
		#define GLS_SRCBLEND_ONE_MINUS_DST_ALPHA		0x00000008
		#define GLS_SRCBLEND_ALPHA_SATURATE				0x00000009
		#define		GLS_SRCBLEND_BITS					0x0000000f

		#define GLS_DSTBLEND_ZERO						0x00000010
		#define GLS_DSTBLEND_ONE						0x00000020
		#define GLS_DSTBLEND_SRC_COLOR					0x00000030
		#define GLS_DSTBLEND_ONE_MINUS_SRC_COLOR		0x00000040
		#define GLS_DSTBLEND_SRC_ALPHA					0x00000050
		#define GLS_DSTBLEND_ONE_MINUS_SRC_ALPHA		0x00000060
		#define GLS_DSTBLEND_DST_ALPHA					0x00000070
		#define GLS_DSTBLEND_ONE_MINUS_DST_ALPHA		0x00000080
		#define		GLS_DSTBLEND_BITS					0x000000f0

		#define GLS_DEPTHMASK_TRUE						0x00000100

		#define GLS_POLYMODE_LINE						0x00001000

		#define GLS_DEPTHTEST_DISABLE					0x00010000
		#define GLS_DEPTHFUNC_EQUAL						0x00020000

		#define GLS_ATEST_GT_0							0x10000000
		#define GLS_ATEST_LT_80							0x20000000
		#define GLS_ATEST_GE_80							0x40000000
		#define		GLS_ATEST_BITS						0x70000000

		#define GLS_DEFAULT			GLS_DEPTHMASK_TRUE


		/*
		====================================================================

		TESSELATOR/SHADER DECLARATIONS

		====================================================================
		*/
		typedef byte color4ub_t[4];

		typedef struct stageVars
		{
			color4ub_t	colors[SHADER_MAX_VERTEXES];
			vec2_t		texcoords[NUM_TEXTURE_BUNDLES][SHADER_MAX_VERTEXES];
		} stageVars_t;

		typedef struct shaderCommands_s 
		{
			glIndex_t	indexes[SHADER_MAX_INDEXES];
			vec4_t		xyz[SHADER_MAX_VERTEXES];
			vec4_t		normal[SHADER_MAX_VERTEXES];
			vec2_t		texCoords[SHADER_MAX_VERTEXES][2];
			color4ub_t	vertexColors[SHADER_MAX_VERTEXES];
			int			vertexDlightBits[SHADER_MAX_VERTEXES];

			stageVars_t	svars;

			color4ub_t	constantColor255[SHADER_MAX_VERTEXES];

			shader_t	*shader;
		  float   shaderTime;
			int			fogNum;

			int			dlightBits;	// or together of all vertexDlightBits

			int			numIndexes;
			int			numVertexes;

			// info extracted from current shader
			int			numPasses;
			shaderStage_t	**xstages;
		} shaderCommands_t;

		/*
		=============================================================

		RENDERER BACK END COMMAND QUEUE

		=============================================================
		*/

		#define	MAX_RENDER_COMMANDS	0x40000

		typedef struct {
			byte	cmds[MAX_RENDER_COMMANDS];
			int		used;
		} renderCommandList_t;

		typedef struct {
			int		commandId;
			float	color[4];
		} setColorCommand_t;

		typedef struct {
			int		commandId;
			int		buffer;
		} drawBufferCommand_t;

		typedef struct {
			int		commandId;
			image_t	*image;
			int		width;
			int		height;
			void	*data;
		} subImageCommand_t;

		typedef struct {
			int		commandId;
		} swapBuffersCommand_t;

		typedef struct {
			int		commandId;
			int		buffer;
		} endFrameCommand_t;

		typedef struct {
			int		commandId;
			shader_t	*shader;
			float	x, y;
			float	w, h;
			float	s1, t1;
			float	s2, t2;
		} stretchPicCommand_t;

		typedef struct {
			int		commandId;
			trRefdef_t	refdef;
			viewParms_t	viewParms;
			drawSurf_t *drawSurfs;
			int		numDrawSurfs;
		} drawSurfsCommand_t;

		typedef struct {
			int commandId;
			int x;
			int y;
			int width;
			int height;
			char *fileName;
			bool jpeg;
		} screenshotCommand_t;

		typedef enum {
			RC_END_OF_LIST,
			RC_SET_COLOR,
			RC_STRETCH_PIC,
			RC_DRAW_SURFS,
			RC_DRAW_BUFFER,
			RC_SWAP_BUFFERS,
			RC_SCREENSHOT
		} renderCommand_t;


		// these are sort of arbitrary limits.
		// the limits apply to the sum of all scenes in a frame --
		// the main view, all the 3D icons, etc
		#define	MAX_POLYS		600
		#define	MAX_POLYVERTS	3000

		// all of the information needed by the back end must be
		// contained in a backEndData_t.  This entire structure is
		// duplicated so the front and back end can run in parallel
		// on an SMP machine
		typedef struct {
			drawSurf_t	drawSurfs[MAX_DRAWSURFS];
			dlight_t	dlights[MAX_DLIGHTS];
			trRefEntity_t	entities[MAX_ENTITIES];
			srfPoly_t	*polys;//[MAX_POLYS];
			polyVert_t	*polyVerts;//[MAX_POLYVERTS];
			renderCommandList_t	commands;
		} backEndData_t;
	}
}
