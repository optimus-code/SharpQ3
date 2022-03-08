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
using SharpQ3.Engine.renderer;
using System;
using System.Runtime.InteropServices;

namespace SharpQ3.Engine.qcommon
{
	public class patchCollide_t
	{
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
		public vec3_t[] bounds;
		public int numPlanes;          // surface planes plus edge planes
		public patchPlane_t[] planes;
		public int numFacets;
		public facet_t[] facets;
	}

	public struct patchPlane_t
	{
		public vec4_t plane;
		public int signbits;       // signx + (signy<<1) + (signz<<2), used as lookup during collision
	}

	public class facet_t
	{
		public int surfacePlane;
		public int numBorders;     // 3 or four + 6 axial bevels + 4 or 3 * 4 edge bevels

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = ( 4 + 6 + 16 ) )]
		public int[] borderPlanes;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = ( 4 + 6 + 16 ) )]
		public int[] borderInward;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = ( 4 + 6 + 16 ) )]
		public bool[] borderNoAdjust;
	}
	
	public struct cGrid_t
	{
		public int width;
		public int height;
		public bool wrapWidth;
		public bool wrapHeight;
		public vec3_t[][] points;//[MAX_GRID_SIZE][MAX_GRID_SIZE];	// [width][height]
	}

	public static class cm_patch
	{
		public const int MAX_FACETS = 1024;
		public const int MAX_PATCH_PLANES = 2048;
		public const int MAX_GRID_SIZE = 129;
		public const float SUBDIVIDE_DISTANCE = 16; //4	// never more than this units away from curve
		public const float PLANE_TRI_EPSILON = 0.1f;
		public const float WRAP_POINT_EPSILON = 0.1f;

		/*

		This file does not reference any globals, and has these entry points:

		void CM_ClearLevelPatches( void );
		struct patchCollide_s	*CM_GeneratePatchCollide( int width, int height, const vec3_t *points );
		void CM_TraceThroughPatchCollide( traceWork_t *tw, const struct patchCollide_s *pc );
		bool CM_PositionTestInPatchCollide( traceWork_t *tw, const struct patchCollide_s *pc );
		void CM_DrawDebugSurface( void (*drawPoly)(int color, int numPoints, flaot *points) );


		WARNING: this may misbehave with meshes that have rows or columns that only
		degenerate a few triangles.  Completely degenerate rows and columns are handled
		properly.
		*/

		/*
		#define	MAX_FACETS			1024
		#define	MAX_PATCH_PLANES	2048

		typedef struct {
			float	plane[4];
			int		signbits;		// signx + (signy<<1) + (signz<<2), used as lookup during collision
		} patchPlane_t;

		typedef struct {
			int			surfacePlane;
			int			numBorders;		// 3 or four + 6 axial bevels + 4 or 3 * 4 edge bevels
			int			borderPlanes[4+6+16];
			int			borderInward[4+6+16];
			bool	borderNoAdjust[4+6+16];
		} facet_t;

		typedef struct patchCollide_s {
			vec3_t	bounds[2];
			int		numPlanes;			// surface planes plus edge planes
			patchPlane_t	*planes;
			int		numFacets;
			facet_t	*facets;
		} patchCollide_t;


		#define	MAX_GRID_SIZE	129

		typedef struct {
			int			width;
			int			height;
			bool	wrapWidth;
			bool	wrapHeight;
			vec3_t	points[MAX_GRID_SIZE][MAX_GRID_SIZE];	// [width][height]
		} cGrid_t;

		#define	SUBDIVIDE_DISTANCE	16	//4	// never more than this units away from curve
		#define	PLANE_TRI_EPSILON	0.1
		#define	WRAP_POINT_EPSILON	0.1
		*/

		static int c_totalPatchBlocks;
		static int c_totalPatchSurfaces;
		static int c_totalPatchEdges;

		static patchCollide_t	debugPatchCollide;
        static facet_t debugFacet;
		static bool		debugBlock;
		static vec3_t[]		debugBlockPoints = new vec3_t[4];

		static cvar_t cv;
		static cvar_t cv2;

		/*
		=================
		CM_ClearLevelPatches
		=================
		*/
		public static void CM_ClearLevelPatches( ) 
		{
			debugPatchCollide = null;
			debugFacet = null;
		}

		/*
		=================
		CM_SignbitsForNormal
		=================
		*/
		public static int CM_SignbitsForNormal( vec3_t normal )
		{
			var bits = 0;
			for (var j=0 ; j<3 ; j++)
			{
				if ( normal[j] < 0 )
				{
					bits |= 1<<j;
				}
			}
			return bits;
		}

		/*
		=====================
		CM_PlaneFromPoints

		Returns false if the triangle is degenrate.
		The normal will point out of the clock for clockwise ordered points
		=====================
		*/
		public static bool CM_PlaneFromPoints( out vec4_t plane, vec3_t a, vec3_t b, vec3_t c ) 
		{
			plane = vec4_t.zero;
			vec3_t tempPlane = new vec3_t( );
			q_shared.VectorSubtract( b, a, out vec3_t d1 );
			q_shared.VectorSubtract( c, a, out vec3_t d2 );
			q_shared.CrossProduct( d2, d1, out tempPlane );

			if ( q_math.VectorNormalize( ref tempPlane ) == 0 )
				return false;

			plane = tempPlane;
			plane.w = q_shared.DotProduct( a, plane );
			return true;
		}


		/*
		================================================================================

		GRID SUBDIVISION

		================================================================================
		*/

		/*
		=================
		CM_NeedsSubdivision

		Returns true if the given quadratic curve is not flat enough for our
		collision detection purposes
		=================
		*/
		static bool	CM_NeedsSubdivision( vec3_t a, vec3_t b, vec3_t c ) 
		{
			vec3_t cmid = new vec3_t();
			vec3_t lmid = new vec3_t( );
			float dist;

			// calculate the linear midpoint
			for ( var i = 0 ; i < 3 ; i++ )
			{
				lmid[i] = 0.5f*(a[i] + c[i]);
			}

			// calculate the exact curve midpoint
			for ( var i = 0 ; i < 3 ; i++ ) 
			{
				cmid[i] = 0.5f * ( 0.5f*(a[i] + b[i]) + 0.5f*(b[i] + c[i]) );
			}

			// see if the curve is far enough away from the linear mid
			q_shared.VectorSubtract( cmid, lmid, out vec3_t delta );
			dist = q_shared.VectorLength( delta );
			
			return dist >= SUBDIVIDE_DISTANCE;
		}

		/*
		===============
		CM_Subdivide

		a, b, and c are control points.
		the subdivided sequence will be: a, out1, out2, out3, c
		===============
		*/
		static void CM_Subdivide( vec3_t a, vec3_t b, vec3_t c, out vec3_t out1, out vec3_t out2, out vec3_t out3 ) 
		{
			out1 = new vec3_t( );
			out2 = new vec3_t( );
			out3 = new vec3_t( );

			for ( var i = 0 ; i < 3 ; i++ ) 
			{
				out1[i] = 0.5f * (a[i] + b[i]);
				out3[i] = 0.5f * (b[i] + c[i]);
				out2[i] = 0.5f * (out1[i] + out3[i]);
			}
		}

		/*
		=================
		CM_TransposeGrid

		Swaps the rows and columns in place
		=================
		*/
		static void CM_TransposeGrid( ref cGrid_t grid ) 
		{
			int			l;
			vec3_t		temp;
			bool	tempWrap;

			if ( grid.width > grid.height ) 
			{
				for ( var i = 0 ; i < grid.height ; i++ ) 
				{
					for ( var j = i + 1 ; j < grid.width ; j++ ) 
					{
						if ( j < grid.height ) 
						{
							// swap the value
							q_shared.VectorCopy( grid.points[i][j], out temp );
							q_shared.VectorCopy( grid.points[j][i], out grid.points[i][j] );
							q_shared.VectorCopy( temp, out grid.points[j][i] );
						} 
						else 
						{
							// just copy
							q_shared.VectorCopy( grid.points[j][i], out grid.points[i][j] );
						}
					}
				}
			} 
			else 
			{
				for ( var i = 0 ; i < grid.width ; i++ ) 
				{
					for ( var j = i + 1 ; j < grid.height ; j++ ) 
					{
						if ( j < grid.width ) 
						{
							// swap the value
							q_shared.VectorCopy( grid.points[j][i], out temp );
							q_shared.VectorCopy( grid.points[i][j], out grid.points[j][i] );
							q_shared.VectorCopy( temp, out grid.points[i][j] );
						} 
						else
						{
							// just copy
							q_shared.VectorCopy( grid.points[i][j], out grid.points[j][i] );
						}
					}
				}
			}

			l = grid.width;
			grid.width = grid.height;
			grid.height = l;

			tempWrap = grid.wrapWidth;
			grid.wrapWidth = grid.wrapHeight;
			grid.wrapHeight = tempWrap;
		}

		/*
		===================
		CM_SetGridWrapWidth

		If the left and right columns are exactly equal, set grid.wrapWidth true
		===================
		*/
		static void CM_SetGridWrapWidth( ref cGrid_t grid ) 
		{
			float	d;
			int i, j;

			for ( i = 0 ; i < grid.height ; i++ ) 
			{
				for ( j = 0 ; j < 3 ; j++ ) 
				{
					d = grid.points[0][i][j] - grid.points[grid.width-1][i][j];
					if ( d < -WRAP_POINT_EPSILON || d > WRAP_POINT_EPSILON ) 
						break;
				}
				if ( j != 3 ) 
					break;
			}

			if ( i == grid.height )
				grid.wrapWidth = true;
			else
				grid.wrapWidth = false;
		}

		/*
		=================
		CM_SubdivideGridColumns

		Adds columns as necessary to the grid until
		all the aproximating points are within SUBDIVIDE_DISTANCE
		from the true curve
		=================
		*/
		static void CM_SubdivideGridColumns( ref cGrid_t grid )
		{
			int	j, k;

			for ( var i = 0 ; i < grid.width - 2 ;  )
			{
				// grid.points[i][x] is an interpolating control point
				// grid.points[i+1][x] is an aproximating control point
				// grid.points[i+2][x] is an interpolating control point

				//
				// first see if we can collapse the aproximating collumn away
				//
				for ( j = 0 ; j < grid.height ; j++ ) 
				{
					if ( CM_NeedsSubdivision( grid.points[i][j], grid.points[i+1][j], grid.points[i+2][j] ) ) 
					{
						break;
					}
				}
				if ( j == grid.height ) 
				{
					// all of the points were close enough to the linear midpoints
					// that we can collapse the entire column away
					for ( j = 0 ; j < grid.height ; j++ ) 
					{
						// remove the column
						for ( k = i + 2 ; k < grid.width ; k++ ) 
						{
							q_shared.VectorCopy( grid.points[k][j], out grid.points[k-1][j] );
						}
					}

					grid.width--;

					// go to the next curve segment
					i++;
					continue;
				}

				//
				// we need to subdivide the curve
				//
				for ( j = 0 ; j < grid.height ; j++ ) 
				{
					vec3_t	prev, mid, next;

					// save the control points now
					q_shared.VectorCopy( grid.points[i][j], out prev );
					q_shared.VectorCopy( grid.points[i+1][j], out mid );
					q_shared.VectorCopy( grid.points[i+2][j], out next );

					// make room for two additional columns in the grid
					// columns i+1 will be replaced, column i+2 will become i+4
					// i+1, i+2, and i+3 will be generated
					for ( k = grid.width - 1 ; k > i + 1 ; k-- ) 
					{
						q_shared.VectorCopy( grid.points[k][j], out grid.points[k+2][j] );
					}

					// generate the subdivided points
					CM_Subdivide( prev, mid, next, out grid.points[i+1][j], out grid.points[i+2][j], out grid.points[i+3][j] );
				}

				grid.width += 2;

				// the new aproximating point at i+1 may need to be removed
				// or subdivided farther, so don't advance i
			}
		}

		/*
		======================
		CM_ComparePoints
		======================
		*/
		public const float POINT_EPSILON = 0.1f;

		static bool CM_ComparePoints( vec3_t a, vec3_t b ) 
		{
			var d = a.x - b.x;

			if ( d < -POINT_EPSILON || d > POINT_EPSILON ) 
				return false;

			d = a.y - b.y;

			if ( d < -POINT_EPSILON || d > POINT_EPSILON ) 
				return false;

			d = a.z - b.z;

			if ( d < -POINT_EPSILON || d > POINT_EPSILON ) 
				return false;

			return true;
		}

		/*
		=================
		CM_RemoveDegenerateColumns

		If there are any identical columns, remove them
		=================
		*/
		static void CM_RemoveDegenerateColumns( ref cGrid_t grid ) 
		{
			int		i, j, k;

			for ( i = 0 ; i < grid.width - 1 ; i++ ) 
			{
				for ( j = 0 ; j < grid.height ; j++ ) 
				{
					if ( !CM_ComparePoints( grid.points[i][j], grid.points[i+1][j] ) )
					{
						break;
					}
				}

				if ( j != grid.height ) {
					continue;	// not degenerate
				}

				for ( j = 0 ; j < grid.height ; j++ )
				{
					// remove the column
					for ( k = i + 2 ; k < grid.width ; k++ ) {
						q_shared.VectorCopy( grid.points[k][j], out grid.points[k-1][j] );
					}
				}
				grid.width--;

				// check against the next column
				i--;
			}
		}

		/*
		================================================================================

		PATCH COLLIDE GENERATION

		================================================================================
		*/

		static	int				numPlanes;
		static	patchPlane_t[]	planes = new patchPlane_t[cm_patch.MAX_PATCH_PLANES];

		static	int				numFacets;
		static	facet_t[]		facets = new facet_t[cm_patch.MAX_PATCH_PLANES]; //maybe MAX_FACETS ??

		public const float NORMAL_EPSILON = 0.0001f;
		public const float DIST_EPSILON = 0.02f;

		/*
		==================
		CM_PlaneEqual
		==================
		*/
		static bool CM_PlaneEqual( patchPlane_t p, vec4_t plane/*[4]*/, out bool flipped ) 
		{
			vec4_t invplane;

			if (
			   MathF.Abs( p.plane.x - plane.x ) < NORMAL_EPSILON
			&& MathF.Abs( p.plane.y - plane.y ) < NORMAL_EPSILON
			&& MathF.Abs( p.plane.z - plane.z ) < NORMAL_EPSILON
			&& MathF.Abs( p.plane.w - plane.w ) < DIST_EPSILON )
			{
				flipped = false;
				return true;
			}

			q_shared.VectorNegate(plane, out invplane);
			invplane[3] = -plane[3];

			if (
			   MathF.Abs( p.plane.x - invplane.x ) < NORMAL_EPSILON
			&& MathF.Abs( p.plane.y - invplane.y ) < NORMAL_EPSILON
			&& MathF.Abs( p.plane.z - invplane.z ) < NORMAL_EPSILON
			&& MathF.Abs( p.plane.w - invplane.w ) < DIST_EPSILON )
			{
				flipped = true;
				return true;
			}

			flipped = false;
			return false;
		}

		/*
		==================
		CM_SnapVector
		==================
		*/
		static void CM_SnapVector(vec3_t normal) {
			int		i;

			for (i=0 ; i<3 ; i++)
			{
				if ( MathF.Abs(normal[i] - 1) < NORMAL_EPSILON )
				{
					q_shared.VectorClear (ref normal);
					normal[i] = 1;
					break;
				}
				if ( MathF.Abs( normal[i] - -1) < NORMAL_EPSILON )
				{
					q_shared.VectorClear (ref normal);
					normal[i] = -1;
					break;
				}
			}
		}

		/*
		==================
		CM_FindPlane2
		==================
		*/
		static int CM_FindPlane2(vec4_t plane/*[4]*/, out bool flipped) 
		{
			// see if the points are close enough to an existing plane
			for ( var i = 0 ; i < numPlanes ; i++ ) 
			{
				if (CM_PlaneEqual(planes[i], plane, out flipped)) 
					return i;
			}

			// add a new plane
			if ( numPlanes == MAX_PATCH_PLANES ) {
				common.Com_Error( errorParm_t.ERR_DROP, "MAX_PATCH_PLANES" );
			}

			q_shared.Vector4Copy( plane, out planes[numPlanes].plane );
			planes[numPlanes].signbits = CM_SignbitsForNormal( plane );

			numPlanes++;

			flipped = false;

			return numPlanes-1;
		}

		/*
		==================
		CM_FindPlane
		==================
		*/
		static int CM_FindPlane( vec3_t p1, vec3_t p2, vec3_t p3 )
		{
			int		i;
			float	d;

			if ( !CM_PlaneFromPoints( out var plane, p1, p2, p3 ) )
				return -1;

			// see if the points are close enough to an existing plane
			for ( i = 0 ; i < numPlanes ; i++ ) 
			{
				if ( q_shared.DotProduct( plane, planes[i].plane ) < 0 ) 
					continue;	// allow backwards planes?

				d = q_shared.DotProduct( p1, planes[i].plane ) - planes[i].plane[3];

				if ( d < -PLANE_TRI_EPSILON || d > PLANE_TRI_EPSILON )
					continue;

				d = q_shared.DotProduct( p2, planes[i].plane ) - planes[i].plane[3];

				if ( d < -PLANE_TRI_EPSILON || d > PLANE_TRI_EPSILON )
					continue;

				d = q_shared.DotProduct( p3, planes[i].plane ) - planes[i].plane[3];

				if ( d < -PLANE_TRI_EPSILON || d > PLANE_TRI_EPSILON )
					continue;

				// found it
				return i;
			}

			// add a new plane
			if ( numPlanes == MAX_PATCH_PLANES )
				common.Com_Error( errorParm_t.ERR_DROP, "MAX_PATCH_PLANES" );

			q_shared.Vector4Copy( plane, out planes[numPlanes].plane );
			planes[numPlanes].signbits = CM_SignbitsForNormal( plane );

			numPlanes++;

			return numPlanes-1;
		}

		/*
		==================
		CM_PointOnPlaneSide
		==================
		*/
		static int CM_PointOnPlaneSide( vec3_t p, int planeNum ) 
		{
			if ( planeNum == -1 )
				return cm_polylib.SIDE_ON;

			var plane = planes[ planeNum ].plane;
			var d = q_shared.DotProduct( p, plane ) - plane[3];

			if ( d > PLANE_TRI_EPSILON )
				return cm_polylib.SIDE_FRONT;

			if ( d < -PLANE_TRI_EPSILON ) 
				return cm_polylib.SIDE_BACK;

			return cm_polylib.SIDE_ON;
		}

		/*
		==================
		CM_GridPlane
		==================
		*/
		static int	CM_GridPlane( int[][][] gridPlanes/*[MAX_GRID_SIZE][MAX_GRID_SIZE][2]*/, int i, int j, int tri ) 
		{
			int		p;

			p = gridPlanes[i][j][tri];

			if ( p != -1 )
				return p;

			p = gridPlanes[i][j][tri == 0 ? 1 : 0];

			if ( p != -1 )
				return p;

			// should never happen
			common.Com_Printf( "WARNING: CM_GridPlane unresolvable\n" );
			return -1;
		}

		/*
		==================
		CM_EdgePlaneNum
		==================
		*/
		static int CM_EdgePlaneNum( cGrid_t grid, int[][][] gridPlanes/*[MAX_GRID_SIZE][MAX_GRID_SIZE][2]*/, int i, int j, int k ) 
		{
			vec3_t	p1, p2;
			vec3_t		up;
			int			p;

			switch ( k ) 
			{
				case 0:	// top border
					p1 = grid.points[i][j];
					p2 = grid.points[i+1][j];
					p = CM_GridPlane( gridPlanes, i, j, 0 );
					q_shared.VectorMA( p1, 4, planes[ p ].plane, out up );
					return CM_FindPlane( p1, p2, up );

				case 2:	// bottom border
					p1 = grid.points[i][j+1];
					p2 = grid.points[i+1][j+1];
					p = CM_GridPlane( gridPlanes, i, j, 1 );
					q_shared.VectorMA( p1, 4, planes[ p ].plane, out up );
					return CM_FindPlane( p2, p1, up );

				case 3: // left border
					p1 = grid.points[i][j];
					p2 = grid.points[i][j+1];
					p = CM_GridPlane( gridPlanes, i, j, 1 );
					q_shared.VectorMA( p1, 4, planes[ p ].plane, out up );
					return CM_FindPlane( p2, p1, up );

				case 1:	// right border
					p1 = grid.points[i+1][j];
					p2 = grid.points[i+1][j+1];
					p = CM_GridPlane( gridPlanes, i, j, 0 );
					q_shared.VectorMA( p1, 4, planes[ p ].plane, out up );
					return CM_FindPlane( p1, p2, up );

				case 4:	// diagonal out of triangle 0
					p1 = grid.points[i+1][j+1];
					p2 = grid.points[i][j];
					p = CM_GridPlane( gridPlanes, i, j, 0 );
					q_shared.VectorMA( p1, 4, planes[ p ].plane, out up );
					return CM_FindPlane( p1, p2, up );

				case 5:	// diagonal out of triangle 1
					p1 = grid.points[i][j];
					p2 = grid.points[i+1][j+1];
					p = CM_GridPlane( gridPlanes, i, j, 1 );
					q_shared.VectorMA( p1, 4, planes[ p ].plane, out up );
					return CM_FindPlane( p1, p2, up );
			}

			common.Com_Error( errorParm_t.ERR_DROP, "CM_EdgePlaneNum: bad k" );
			return -1;
		}

		/*
		===================
		CM_SetBorderInward
		===================
		*/
		static void CM_SetBorderInward( facet_t facet, cGrid_t grid, int[][][] gridPlanes/*[MAX_GRID_SIZE][MAX_GRID_SIZE][2]*/,
								  int i, int j, int which ) 
		{
			int		k, l;
			vec3_t[]	points = new vec3_t[4];
			int		numPoints;

			switch ( which ) 
			{
				case -1:
					points[0] = grid.points[i][j];
					points[1] = grid.points[i+1][j];
					points[2] = grid.points[i+1][j+1];
					points[3] = grid.points[i][j+1];
					numPoints = 4;
					break;
				case 0:
					points[0] = grid.points[i][j];
					points[1] = grid.points[i+1][j];
					points[2] = grid.points[i+1][j+1];
					numPoints = 3;
					break;
				case 1:
					points[0] = grid.points[i+1][j+1];
					points[1] = grid.points[i][j+1];
					points[2] = grid.points[i][j];
					numPoints = 3;
					break;
				default:
					common.Com_Error( errorParm_t.ERR_FATAL, "CM_SetBorderInward: bad parameter" );
					numPoints = 0;
					break;
			}

			for ( k = 0 ; k < facet.numBorders ; k++ ) 
			{
				int		front, back;

				front = 0;
				back = 0;

				for ( l = 0 ; l < numPoints ; l++ )
				{
					int		side;

					side = CM_PointOnPlaneSide( points[l], facet.borderPlanes[k] );
					if ( side == cm_polylib.SIDE_FRONT )
					{
						front++;
					} 
					if ( side == cm_polylib.SIDE_BACK ) 
					{
						back++;
					}
				}

				if ( front != 0 && back == 0 )
				{
					facet.borderInward[k] = 1;
				} 
				else if ( back != 0 && front == 0 ) 
				{
					facet.borderInward[k] = 0;
				} 
				else if ( front == 0 && back == 0 ) 
				{
					// flat side border
					facet.borderPlanes[k] = -1;
				} 
				else 
				{
					// bisecting side border
					common.Com_DPrintf( "WARNING: CM_SetBorderInward: mixed plane sides\n" );
					facet.borderInward[k] = 0;

					if ( !debugBlock ) 
					{
						debugBlock = true;
						q_shared.VectorCopy( grid.points[i][j], out debugBlockPoints[0] );
						q_shared.VectorCopy( grid.points[i+1][j], out debugBlockPoints[1] );
						q_shared.VectorCopy( grid.points[i+1][j+1], out debugBlockPoints[2] );
						q_shared.VectorCopy( grid.points[i][j+1], out debugBlockPoints[3] );
					}
				}
			}
		}

		/*
		==================
		CM_ValidateFacet

		If the facet isn't bounded by its borders, we screwed up.
		==================
		*/
		static bool CM_ValidateFacet( facet_t facet ) 
		{
			int j;
			winding_t w;
			vec3_t[] bounds = new vec3_t[2];

			if ( facet.surfacePlane == -1 )
				return false;

			q_shared.Vector4Copy( planes[ facet.surfacePlane ].plane, out var plane );
			w = cm_polylib.BaseWindingForPlane( plane,  plane[3] );
			for ( j = 0 ; j < facet.numBorders && w != null; j++ ) 
			{
				if ( facet.borderPlanes[j] == -1 ) 
					return false;

				q_shared.Vector4Copy( planes[ facet.borderPlanes[j] ].plane, out plane );

				if ( facet.borderInward[j] == 0 )
				{
					q_shared.VectorSubtract( q_math.vec3_origin, plane, out plane );
					plane[3] = -plane[3];
				}
				cm_polylib.ChopWindingInPlace( ref w, plane, plane[3], 0.1f );
			}

			if ( w == null )
				return false;       // winding was completely chopped away

			// see if the facet is unreasonably large
			cm_polylib.WindingBounds( w, out bounds[0], out bounds[1] );
			cm_polylib.FreeWinding( w );
			
			for ( j = 0 ; j < 3 ; j++ ) 
			{
				if ( bounds[1][j] - bounds[0][j] > cm_polylib.MAX_MAP_BOUNDS ) 
					return false;		// we must be missing a plane

				if ( bounds[0][j] >= cm_polylib.MAX_MAP_BOUNDS )
					return false;

				if ( bounds[1][j] <= -cm_polylib.MAX_MAP_BOUNDS )
					return false;
			}
			return true;		// winding is fine
		}

		/*
		==================
		CM_AddFacetBevels
		==================
		*/
		static void CM_AddFacetBevels( ref facet_t facet ) 
		{
			int i, j, k, l;
			int axis, dir, order;
			bool flipped;
			float d;
			winding_t w, w2;
			vec4_t newplane;
			vec3_t mins, maxs, vec, vec2 = new vec3_t();

			q_shared.Vector4Copy( planes[ facet.surfacePlane ].plane, out var plane );

			w = cm_polylib.BaseWindingForPlane( plane.xyz, plane.w );
			for ( j = 0 ; j < facet.numBorders && w != null; j++ ) 
			{
				if (facet.borderPlanes[j] == facet.surfacePlane) continue;
				q_shared.Vector4Copy( planes[ facet.borderPlanes[j] ].plane, out plane );

				if ( facet.borderInward[j] == 0 ) 
				{
					q_shared.VectorSubtract( q_math.vec3_origin, plane, out plane );
					plane.w = -plane.w;
				}

				cm_polylib.ChopWindingInPlace( ref w, plane, plane[3], 0.1f );
			}
			if ( w == null ) 
				return;

			cm_polylib.WindingBounds(w, out mins, out maxs);

			// add the axial planes
			order = 0;
			for ( axis = 0 ; axis < 3 ; axis++ )
			{
				for ( dir = -1 ; dir <= 1 ; dir += 2, order++ )
				{
					q_shared.VectorClear(ref plane);
					plane[axis] = dir;

					if (dir == 1) 
						plane.w = maxs[axis];
					else
						plane.w = -mins[axis];

					//if it's the surface plane
					if (CM_PlaneEqual(planes[facet.surfacePlane], plane, out flipped ) ) 
					{
						continue;
					}
					// see if the plane is allready present
					for ( i = 0 ; i < facet.numBorders ; i++ ) 
					{
						if (CM_PlaneEqual(planes[facet.borderPlanes[i]], plane, out flipped))
							break;
					}

					if ( i == facet.numBorders ) 
					{
						if (facet.numBorders > 4 + 6 + 16) 
							common.Com_Printf("ERROR: too many bevels\n");

						facet.borderPlanes[facet.numBorders] = CM_FindPlane2(plane, out flipped );
						facet.borderNoAdjust[facet.numBorders] = false;
						facet.borderInward[facet.numBorders] = flipped ? 1 : 0;
						facet.numBorders++;
					}
				}
			}
			//
			// add the edge bevels
			//
			// test the non-axial plane edges
			for ( j = 0 ; j < w.numpoints ; j++ )
			{
				k = (j+1)%w.numpoints;
				q_shared.VectorSubtract (w.p[j], w.p[k], out vec);
				//if it's a degenerate edge
				if ( q_math.VectorNormalize (ref vec) < 0.5)
					continue;
				CM_SnapVector(vec);
				for ( k = 0; k < 3 ; k++ )
					if ( vec[k] == -1 || vec[k] == 1 )
						break;	// axial
				if ( k < 3 )
					continue;	// only test non-axial edges

				// try the six possible slanted axials from this edge
				for ( axis = 0 ; axis < 3 ; axis++ )
				{
					for ( dir = -1 ; dir <= 1 ; dir += 2 )
					{
						// construct a plane
						q_shared.VectorClear (ref vec2);
						vec2[axis] = dir;
						q_shared.CrossProduct (vec, vec2, out var tplane);
						plane.xyz = tplane;
						if (q_math.VectorNormalize (ref plane) < 0.5)
							continue;
						plane[3] = q_shared.DotProduct (w.p[j], plane);

						// if all the points of the facet winding are
						// behind this plane, it is a proper edge bevel
						for ( l = 0 ; l < w.numpoints ; l++ )
						{
							d = q_shared.DotProduct (w.p[l], plane) - plane[3];
							if (d > 0.1)
								break;	// point in front
						}

						if ( l < w.numpoints )
							continue;

						//if it's the surface plane
						if (CM_PlaneEqual(planes[facet.surfacePlane], plane, out flipped)) 
							continue;

						// see if the plane is allready present
						for ( i = 0 ; i < facet.numBorders ; i++ ) 
						{
							if (CM_PlaneEqual(planes[facet.borderPlanes[i]], plane, out flipped)) 
								break;
						}

						if ( i == facet.numBorders ) 
						{
							if (facet.numBorders > 4 + 6 + 16)
								common.Com_Printf("ERROR: too many bevels\n");

							facet.borderPlanes[facet.numBorders] = CM_FindPlane2(plane, out flipped);

							for ( k = 0 ; k < facet.numBorders ; k++ ) 
							{
								if (facet.borderPlanes[facet.numBorders] == facet.borderPlanes[k])
									common.Com_Printf("WARNING: bevel plane already used\n");
							}

							facet.borderNoAdjust[facet.numBorders] = false;
							facet.borderInward[facet.numBorders] = flipped ? 1 : 0;
							//
							w2 = cm_polylib.CopyWinding(w);
							q_shared.Vector4Copy(planes[facet.borderPlanes[facet.numBorders]].plane, out newplane);
							if (facet.borderInward[facet.numBorders] == 0)
							{
								q_shared.VectorNegate(newplane, out newplane);
								newplane.w = -newplane.w;
							} //end if
							cm_polylib.ChopWindingInPlace( ref w2, newplane, newplane[3], 0.1f );
							if (w2 == null) 
							{
								common.Com_DPrintf("WARNING: CM_AddFacetBevels... invalid bevel\n");
								continue;
							}
							else 
							{
								cm_polylib.FreeWinding(w2);
							}
							//
							facet.numBorders++;
							//already got a bevel
		//					break;
						}
					}
				}
			}
			cm_polylib.FreeWinding( w );

			//add opposite plane
			facet.borderPlanes[facet.numBorders] = facet.surfacePlane;
			facet.borderNoAdjust[facet.numBorders] = false;
			facet.borderInward[facet.numBorders] = 1;
			facet.numBorders++;
		}

		public enum edgeName_t
		{
			EN_TOP,
			EN_RIGHT,
			EN_BOTTOM,
			EN_LEFT
		}

		/*
		==================
		CM_PatchCollideFromGrid
		==================
		*/
		static void CM_PatchCollideFromGrid( cGrid_t grid, patchCollide_t pf )
		{
			int				i, j;
			vec3_t			p1, p2, p3;
			int[][][]		gridPlanes = new int[MAX_GRID_SIZE][][];//[MAX_GRID_SIZE][2];
			facet_t			facet;
			int[] borders = new int[4];
			int[] noAdjust = new int[4];

			numPlanes = 0;
			numFacets = 0;

			// Initialise the jagged aray for the grid
			for ( i = 0; i < MAX_GRID_SIZE; i++ )
            {
				gridPlanes[i] = new int[MAX_GRID_SIZE][];

				for ( j = 0; i < MAX_GRID_SIZE; i++ )
					gridPlanes[i][j] = new int[2];
			}

			// find the planes for each triangle of the grid
			for ( i = 0 ; i < grid.width - 1 ; i++ ) 
			{
				for ( j = 0 ; j < grid.height - 1 ; j++ ) 
				{
					p1 = grid.points[i][j];
					p2 = grid.points[i+1][j];
					p3 = grid.points[i+1][j+1];
					gridPlanes[i][j][0] = CM_FindPlane( p1, p2, p3 );

					p1 = grid.points[i+1][j+1];
					p2 = grid.points[i][j+1];
					p3 = grid.points[i][j];
					gridPlanes[i][j][1] = CM_FindPlane( p1, p2, p3 );
				}
			}

			// create the borders for each facet
			for ( i = 0 ; i < grid.width - 1 ; i++ ) 
			{
				for ( j = 0 ; j < grid.height - 1 ; j++ ) 
				{					 
					borders[( int ) edgeName_t.EN_TOP] = -1;

					if ( j > 0 )
						borders[( int ) edgeName_t.EN_TOP] = gridPlanes[i][j-1][1];
					else if ( grid.wrapHeight )
						borders[( int ) edgeName_t.EN_TOP] = gridPlanes[i][grid.height-2][1];

					noAdjust[( int ) edgeName_t.EN_TOP] = ( borders[( int ) edgeName_t.EN_TOP] == gridPlanes[i][j][0] ? 1 : 0 );

					if ( borders[( int ) edgeName_t.EN_TOP] == -1 || noAdjust[( int ) edgeName_t.EN_TOP] != 0 )
						borders[( int ) edgeName_t.EN_TOP] = CM_EdgePlaneNum( grid, gridPlanes, i, j, 0 );

					borders[( int ) edgeName_t.EN_BOTTOM] = -1;

					if ( j < grid.height - 2 )
						borders[( int ) edgeName_t.EN_BOTTOM] = gridPlanes[i][j+1][0];
					else if ( grid.wrapHeight )
						borders[( int ) edgeName_t.EN_BOTTOM] = gridPlanes[i][0][0];

					noAdjust[( int ) edgeName_t.EN_BOTTOM] = ( borders[( int ) edgeName_t.EN_BOTTOM] == gridPlanes[i][j][1] ? 1 : 0 );

					if ( borders[( int ) edgeName_t.EN_BOTTOM] == -1 || noAdjust[( int ) edgeName_t.EN_BOTTOM] != 0 )
						borders[( int ) edgeName_t.EN_BOTTOM] = CM_EdgePlaneNum( grid, gridPlanes, i, j, 2 );					

					borders[( int ) edgeName_t.EN_LEFT] = -1;

					if ( i > 0 )
						borders[( int ) edgeName_t.EN_LEFT] = gridPlanes[i-1][j][0];
					else if ( grid.wrapWidth )
						borders[( int ) edgeName_t.EN_LEFT] = gridPlanes[grid.width-2][j][0];

					noAdjust[( int ) edgeName_t.EN_LEFT] = ( borders[( int ) edgeName_t.EN_LEFT] == gridPlanes[i][j][1] ? 1 : 0 );

					if ( borders[( int ) edgeName_t.EN_LEFT] == -1 || noAdjust[( int ) edgeName_t.EN_LEFT] != 0 )
						borders[( int ) edgeName_t.EN_LEFT] = CM_EdgePlaneNum( grid, gridPlanes, i, j, 3 );

					borders[( int ) edgeName_t.EN_RIGHT] = -1;

					if ( i < grid.width - 2 )
						borders[( int ) edgeName_t.EN_RIGHT] = gridPlanes[i+1][j][1];
					else if ( grid.wrapWidth )
						borders[( int ) edgeName_t.EN_RIGHT] = gridPlanes[0][j][1];

					noAdjust[( int ) edgeName_t.EN_RIGHT] = ( borders[( int ) edgeName_t.EN_RIGHT] == gridPlanes[i][j][0] ? 1 : 0 );

					if ( borders[( int ) edgeName_t.EN_RIGHT] == -1 || noAdjust[( int ) edgeName_t.EN_RIGHT] != 0 ) 
						borders[( int ) edgeName_t.EN_RIGHT] = CM_EdgePlaneNum( grid, gridPlanes, i, j, 1 );

					if ( numFacets == MAX_FACETS )
						common.Com_Error( errorParm_t.ERR_DROP, "MAX_FACETS" );

					facet = facets[numFacets];
					facet.surfacePlane = default;
					facet.borderPlanes = default;
					facet.borderInward = default;
					facet.borderPlanes = default;
					facet.borderNoAdjust = default;
					facet.numBorders = default;

					if ( gridPlanes[i][j][0] == gridPlanes[i][j][1] ) 
					{
						if ( gridPlanes[i][j][0] == -1 )
							continue;		// degenrate

						facet.surfacePlane = gridPlanes[i][j][0];
						facet.numBorders = 4;
						facet.borderPlanes[0] = borders[( int ) edgeName_t.EN_TOP];
						facet.borderNoAdjust[0] = (bool) ( noAdjust[( int ) edgeName_t.EN_TOP] != 0 );
						facet.borderPlanes[1] = borders[( int ) edgeName_t.EN_RIGHT];
						facet.borderNoAdjust[1] = (bool) ( noAdjust[( int ) edgeName_t.EN_RIGHT] != 0 );
						facet.borderPlanes[2] = borders[( int ) edgeName_t.EN_BOTTOM];
						facet.borderNoAdjust[2] = (bool) ( noAdjust[( int ) edgeName_t.EN_BOTTOM] != 0 );
						facet.borderPlanes[3] = borders[( int ) edgeName_t.EN_LEFT];
						facet.borderNoAdjust[3] = (bool) ( noAdjust[( int ) edgeName_t.EN_LEFT] != 0 );
						CM_SetBorderInward( facet, grid, gridPlanes, i, j, -1 );
						if ( CM_ValidateFacet( facet ) ) 
						{
							CM_AddFacetBevels( ref facet );
							numFacets++;
						}
					} 
					else
					{
						// two seperate triangles
						facet.surfacePlane = gridPlanes[i][j][0];
						facet.numBorders = 3;
						facet.borderPlanes[0] = borders[( int ) edgeName_t.EN_TOP];
						facet.borderNoAdjust[0] = (bool) ( noAdjust[( int ) edgeName_t.EN_TOP] != 0 );
						facet.borderPlanes[1] = borders[( int ) edgeName_t.EN_RIGHT];
						facet.borderNoAdjust[1] = (bool)( noAdjust[( int ) edgeName_t.EN_RIGHT] != 0 );
						facet.borderPlanes[2] = gridPlanes[i][j][1];
						if ( facet.borderPlanes[2] == -1 ) 
						{
							facet.borderPlanes[2] = borders[( int ) edgeName_t.EN_BOTTOM];
							if ( facet.borderPlanes[2] == -1 ) 
							{
								facet.borderPlanes[2] = CM_EdgePlaneNum( grid, gridPlanes, i, j, 4 );
							}
						}
 						CM_SetBorderInward( facet, grid, gridPlanes, i, j, 0 );
						if ( CM_ValidateFacet( facet ) ) 
						{
							CM_AddFacetBevels( ref facet );
							numFacets++;
						}

						if ( numFacets == MAX_FACETS )
							common.Com_Error( errorParm_t.ERR_DROP, "MAX_FACETS" );

						facet = facets[numFacets];
						facet.surfacePlane = default;
						facet.borderPlanes = default;
						facet.borderInward = default;
						facet.borderPlanes = default;
						facet.borderNoAdjust = default;
						facet.numBorders = default;

						facet.surfacePlane = gridPlanes[i][j][1];
						facet.numBorders = 3;
						facet.borderPlanes[0] = borders[( int ) edgeName_t.EN_BOTTOM];
						facet.borderNoAdjust[0] = (bool) ( noAdjust[( int ) edgeName_t.EN_BOTTOM] != 0 );
						facet.borderPlanes[1] = borders[( int ) edgeName_t.EN_LEFT];
						facet.borderNoAdjust[1] = (bool) ( noAdjust[( int ) edgeName_t.EN_LEFT] != 0 );
						facet.borderPlanes[2] = gridPlanes[i][j][0];
						if ( facet.borderPlanes[2] == -1 ) 
						{
							facet.borderPlanes[2] = borders[( int ) edgeName_t.EN_TOP];
							if ( facet.borderPlanes[2] == -1 ) 
							{
								facet.borderPlanes[2] = CM_EdgePlaneNum( grid, gridPlanes, i, j, 5 );
							}
						}
						CM_SetBorderInward( facet, grid, gridPlanes, i, j, 1 );
						if ( CM_ValidateFacet( facet ) ) 
						{
							CM_AddFacetBevels( ref facet );
							numFacets++;
						}
					}
				}
			}

			// copy the results out
			pf.numPlanes = numPlanes;
			pf.numFacets = numFacets;

			pf.facets = new facet_t[numFacets];
			Array.Copy( facets, pf.facets, numFacets );

			pf.planes = new patchPlane_t[numPlanes];
			Array.Copy( planes, pf.planes, numPlanes );
		}


		/*
		===================
		CM_GeneratePatchCollide

		Creates an internal structure that will be used to perform
		collision detection with a patch mesh.

		Points is packed as concatenated rows.
		===================
		*/
		static patchCollide_t CM_GeneratePatchCollide( int width, int height, vec3_t[] points )
		{
			patchCollide_t	pf;
			cGrid_t			grid;
			int				i, j;

			if ( width <= 2 || height <= 2 || points == null ) 
			{
				common.Com_Error( errorParm_t.ERR_DROP, "CM_GeneratePatchFacets: bad parameters: (%i, %i, %p)",
					width, height, points );
			}

			if ( (width & 1) == 0 || (height & 1) == 0 )
				common.Com_Error( errorParm_t.ERR_DROP, "CM_GeneratePatchFacets: even sizes are invalid for quadratic meshes" );

			if ( width > MAX_GRID_SIZE || height > MAX_GRID_SIZE )
				common.Com_Error( errorParm_t.ERR_DROP, "CM_GeneratePatchFacets: source is > MAX_GRID_SIZE" );

			// build a grid
			grid.width = width;
			grid.height = height;
			grid.wrapWidth = false;
			grid.wrapHeight = false;
			grid.points = new vec3_t[width][];
			for ( i = 0 ; i < width ; i++ )
			{
				grid.points[i] = new vec3_t[height];

				for ( j = 0 ; j < height ; j++ ) {
					q_shared.VectorCopy( points[j*width + i], out grid.points[i][j] );
				}
			}

			// subdivide the grid
			CM_SetGridWrapWidth( ref grid );
			CM_SubdivideGridColumns( ref grid );
			CM_RemoveDegenerateColumns( ref grid );

			CM_TransposeGrid( ref grid );

			CM_SetGridWrapWidth( ref grid );
			CM_SubdivideGridColumns( ref grid );
			CM_RemoveDegenerateColumns( ref grid );

			// we now have a grid of points exactly on the curve
			// the aproximate surface defined by these points will be
			// collided against
			pf = new patchCollide_t( );
			q_math.ClearBounds( ref pf.bounds[0], ref pf.bounds[1] );
			for ( i = 0 ; i < grid.width ; i++ ) 
			{
				for ( j = 0 ; j < grid.height ; j++ ) 
				{
					q_math.AddPointToBounds( grid.points[i][j], ref pf.bounds[0], ref pf.bounds[1] );
				}
			}

			c_totalPatchBlocks += ( grid.width - 1 ) * ( grid.height - 1 );

			// generate a bsp tree for the surface
			CM_PatchCollideFromGrid( grid, pf );

			// expand by one unit for epsilon purposes
			pf.bounds[0][0] -= 1;
			pf.bounds[0][1] -= 1;
			pf.bounds[0][2] -= 1;

			pf.bounds[1][0] += 1;
			pf.bounds[1][1] += 1;
			pf.bounds[1][2] += 1;

			return pf;
		}

		/*
		================================================================================

		TRACE TESTING

		================================================================================
		*/

		/*
		====================
		CM_TracePointThroughPatchCollide

		  special case for point traces because the patch collide "brushes" have no volume
		====================
		*/
		static void CM_TracePointThroughPatchCollide( ref traceWork_t tw, patchCollide_t pc ) 
		{
			bool[]	frontFacing = new bool[cm_patch.MAX_PATCH_PLANES];
			float[]		intersection = new float[cm_patch.MAX_PATCH_PLANES];
			float		intersect;
			patchPlane_t	planes;
			facet_t	facet;
			int			i, j, k;
			float		offset;
			float		d1, d2;

			if ( cm_load.cm_playerCurveClip.integer == 0 || !tw.isPoint )
				return;

			// determine the trace's relationship to all planes
			
			for ( i = 0 ; i < pc.numPlanes ; i++ ) 
			{
				planes = pc.planes[i];
				offset = q_shared.DotProduct( tw.offsets[ planes.signbits ], planes.plane );
				d1 = q_shared.DotProduct( tw.start, planes.plane ) - planes.plane[3] + offset;
				d2 = q_shared.DotProduct( tw.end, planes.plane ) - planes.plane[3] + offset;

				if ( d1 <= 0 )
					frontFacing[i] = false;
				else
					frontFacing[i] = true;

				if ( d1 == d2 )
				{
					intersection[i] = 99999;
				} 
				else 
				{
					intersection[i] = d1 / ( d1 - d2 );

					if ( intersection[i] <= 0 ) 
						intersection[i] = 99999;
				}
			}

			// see if any of the surface planes are intersected
			
			for ( i = 0 ; i < pc.numFacets ; i++ )
			{
				facet = pc.facets[i];

				if ( !frontFacing[facet.surfacePlane] )
					continue;

				intersect = intersection[facet.surfacePlane];

				if ( intersect < 0 )
					continue;		// surface is behind the starting point

				if ( intersect > tw.trace.fraction )
					continue;		// already hit something closer

				for ( j = 0 ; j < facet.numBorders ; j++ ) 
				{
					k = facet.borderPlanes[j];
					if ( ( ( frontFacing[k] ? 1 : 0 ) ^ facet.borderInward[j] ) != 0 ) 
					{
						if ( intersection[k] > intersect ) 
							break;
					} 
					else 
					{
						if ( intersection[k] < intersect ) 
							break;
					}
				}
				if ( j == facet.numBorders )
				{
					// we hit this facet
					if (cv == null) 
					{
						cv = Cvar.Cvar_Get( "r_debugSurfaceUpdate", "1", 0 );
					}
					if (cv.integer != 0) 
					{
						debugPatchCollide = pc;
						debugFacet = facet;
					}
					planes = pc.planes[facet.surfacePlane];

					// calculate intersection with a slight pushoff
					offset = q_shared.DotProduct( tw.offsets[ planes.signbits ], planes.plane );
					d1 = q_shared.DotProduct( tw.start, planes.plane ) - planes.plane[3] + offset;
					d2 = q_shared.DotProduct( tw.end, planes.plane ) - planes.plane[3] + offset;
					tw.trace.fraction = ( d1 - cm_local.SURFACE_CLIP_EPSILON ) / ( d1 - d2 );

					if ( tw.trace.fraction < 0 ) {
						tw.trace.fraction = 0;
					}

					q_shared.VectorCopy( planes.plane, out tw.trace.plane.normal );
					tw.trace.plane.dist = planes.plane.w;
				}
			}
		}

		/*
		====================
		CM_CheckFacetPlane
		====================
		*/
		static bool CM_CheckFacetPlane(vec4_t plane, vec3_t start, vec3_t end, ref float enterFrac, ref float leaveFrac, out bool hit) 
		{
			float d1, d2, f;

			hit = false;

			d1 = q_shared.DotProduct( start, plane ) - plane[3];
			d2 = q_shared.DotProduct( end, plane ) - plane[3];

			// if completely in front of face, no intersection with the entire facet
			if (d1 > 0 && ( d2 >= cm_local.SURFACE_CLIP_EPSILON || d2 >= d1 ) )
				return false;

			// if it doesn't cross the plane, the plane isn't relevent
			if (d1 <= 0 && d2 <= 0 )
				return true;

			// crosses face
			if (d1 > d2) 
			{	// enter
				f = (d1-cm_local.SURFACE_CLIP_EPSILON) / (d1-d2);

				if ( f < 0 )
					f = 0;

				//always favor previous plane hits and thus also the surface plane hit
				if (f > enterFrac) 
				{
					enterFrac = f;
					hit = true;
				}
			} 
			else // leave 
			{	
				f = (d1+ cm_local.SURFACE_CLIP_EPSILON ) / (d1-d2);
				if ( f > 1 )
					f = 1;

				if (f < leaveFrac)
					leaveFrac = f;
			}
			return true;
		}

		/*
		====================
		CM_TraceThroughPatchCollide
		====================
		*/
		static void CM_TraceThroughPatchCollide( ref traceWork_t tw, patchCollide_t pc )
		{
			int i, j, hitnum;
			bool hit;
			float offset, enterFrac, leaveFrac, t;
			patchPlane_t planes;
			facet_t	facet;
			vec4_t plane = new vec4_t(), bestplane = new vec4_t( );
			vec3_t startp, endp;

			if (tw.isPoint) 
			{
				CM_TracePointThroughPatchCollide( ref tw, pc );
				return;
			}

			for ( i = 0 ; i < pc.numFacets ; i++ ) 
			{
				facet = pc.facets[i];
				enterFrac = -1.0f;
				leaveFrac = 1.0f;
				hitnum = -1;
				//
				planes = pc.planes[ facet.surfacePlane ];
				q_shared.VectorCopy(planes.plane, out plane );
				plane.w = planes.plane.w;
				if ( tw.sphere.use ) 
				{
					// adjust the plane distance apropriately for radius
					plane.w += tw.sphere.radius;

					// find the closest point on the capsule to the plane
					t = q_shared.DotProduct( plane, tw.sphere.offset );
					if ( t > 0.0f ) 
					{
						q_shared.VectorSubtract( tw.start, tw.sphere.offset, out startp );
						q_shared.VectorSubtract( tw.end, tw.sphere.offset, out endp );
					}
					else 
					{
						q_shared.VectorAdd( tw.start, tw.sphere.offset, out startp );
						q_shared.VectorAdd( tw.end, tw.sphere.offset, out endp );
					}
				}
				else 
				{
					offset = q_shared.DotProduct( tw.offsets[ planes.signbits ], plane);
					plane[3] -= offset;
					q_shared.VectorCopy( tw.start, out startp );
					q_shared.VectorCopy( tw.end, out endp );
				}

				if (!CM_CheckFacetPlane(plane, startp, endp, ref enterFrac, ref leaveFrac, out hit))
					continue;

				if (hit)
					q_shared.Vector4Copy(plane, out bestplane );

				for ( j = 0; j < facet.numBorders; j++ ) {
					planes = pc.planes[ facet.borderPlanes[j] ];
					if (facet.borderInward[j] != 0) 
					{
						q_shared.VectorNegate(planes.plane, out plane );
						plane.w = -planes.plane.w;
					}
					else 
					{
						q_shared.VectorCopy(planes.plane, out plane );
						plane.w = planes.plane.w;
					}
					if ( tw.sphere.use ) 
					{
						// adjust the plane distance apropriately for radius
						plane.w += tw.sphere.radius;

						// find the closest point on the capsule to the plane
						t = q_shared.DotProduct( plane, tw.sphere.offset );
						if ( t > 0.0f )
						{
							q_shared.VectorSubtract( tw.start, tw.sphere.offset, out startp );
							q_shared.VectorSubtract( tw.end, tw.sphere.offset, out endp );
						}
						else 
						{
							q_shared.VectorAdd( tw.start, tw.sphere.offset, out startp );
							q_shared.VectorAdd( tw.end, tw.sphere.offset, out endp );
						}
					}
					else 
					{
						// NOTE: this works even though the plane might be flipped because the bbox is centered
						offset = q_shared.DotProduct( tw.offsets[ planes.signbits ], plane);
						plane.w += MathF.Abs(offset);
						q_shared.VectorCopy( tw.start, out startp );
						q_shared.VectorCopy( tw.end, out endp );
					}

					if (!CM_CheckFacetPlane(plane, startp, endp, ref enterFrac, ref leaveFrac, out hit)) 
					{
						break;
					}
					if (hit) 
					{
						hitnum = j;
						q_shared.Vector4Copy(plane, out bestplane );
					}
				}

				if (j < facet.numBorders) 
					continue;

				//never clip against the back side
				if (hitnum == facet.numBorders - 1) 
					continue;

				if (enterFrac < leaveFrac && enterFrac >= 0) 
				{
					if (enterFrac < tw.trace.fraction) 
					{
						if (enterFrac < 0) {
							enterFrac = 0;
						}
						if (cv == null) {
							cv = Cvar.Cvar_Get( "r_debugSurfaceUpdate", "1", 0 );
						}
						if (cv?.integer != 0) {
							debugPatchCollide = pc;
							debugFacet = facet;
						}

						tw.trace.fraction = enterFrac;
						q_shared.VectorCopy( bestplane, out tw.trace.plane.normal );
						tw.trace.plane.dist = bestplane[3];
					}
				}
			}
		}


		/*
		=======================================================================

		POSITION TEST

		=======================================================================
		*/

		/*
		====================
		CM_PositionTestInPatchCollide
		====================
		*/
		public static bool CM_PositionTestInPatchCollide( traceWork_t tw, patchCollide_t pc ) 
		{
			int i, j;
			float offset, t;
			patchPlane_t planes;
			facet_t	facet;
			vec3_t plane;
			vec3_t startp;

			if (tw.isPoint)
				return false;

			for ( i = 0 ; i < pc.numFacets; i++ ) 
			{	
				facet = pc.facets[i];
				planes = pc.planes[ facet.surfacePlane ];
				q_shared.VectorCopy(planes.plane, out plane);
				plane[3] = planes.plane[3];
				if ( tw.sphere.use ) 
				{
					// adjust the plane distance apropriately for radius
					plane[3] += tw.sphere.radius;

					// find the closest point on the capsule to the plane
					t = q_shared.DotProduct( plane, tw.sphere.offset );
					if ( t > 0 )
					{
						q_shared.VectorSubtract( tw.start, tw.sphere.offset, out startp );
					}
					else 
					{
						q_shared.VectorAdd( tw.start, tw.sphere.offset, out startp );
					}
				}
				else 
				{
					offset = q_shared.DotProduct( tw.offsets[ planes.signbits ], plane);
					plane[3] -= offset;
					q_shared.VectorCopy( tw.start, out startp );
				}

				if ( q_shared.DotProduct( plane, startp ) - plane[3] > 0.0f ) 
					continue;

				for ( j = 0; j < facet.numBorders; j++ ) {
					planes = pc.planes[ facet.borderPlanes[j] ];
					if (facet.borderInward[j] != 0) 
					{
						q_shared.VectorNegate(planes.plane, out plane );
						plane[3] = -planes.plane[3];
					}
					else 
					{
						q_shared.VectorCopy(planes.plane, out plane );
						plane[3] = planes.plane[3];
					}
					if ( tw.sphere.use ) 
					{
						// adjust the plane distance apropriately for radius
						plane[3] += tw.sphere.radius;

						// find the closest point on the capsule to the plane
						t = q_shared.DotProduct( plane, tw.sphere.offset );
						if ( t > 0.0f ) {
							q_shared.VectorSubtract( tw.start, tw.sphere.offset, out startp );
						}
						else {
							q_shared.VectorAdd( tw.start, tw.sphere.offset, out startp );
						}
					}
					else
					{
						// NOTE: this works even though the plane might be flipped because the bbox is centered
						offset = q_shared.DotProduct( tw.offsets[ planes.signbits ], plane);
						plane[3] += MathF.Abs(offset);
						q_shared.VectorCopy( tw.start, out startp );
					}

					if ( q_shared.DotProduct( plane, startp ) - plane[3] > 0.0f ) {
						break;
					}
				}

				if (j < facet.numBorders) 
					continue;

				// inside this patch facet
				return true;
			}
			return false;
		}

		/*
		=======================================================================

		DEBUGGING

		=======================================================================
		*/


		/*
		==================
		CM_DrawDebugSurface

		Called from the renderer
		==================
		*/
		//void BotDrawDebugPolygons(void (*drawPoly)(int color, int numPoints, float *points), int value);
		
		public delegate void DrawPolyDelegate( int color, int numPoints, float[] points );
		public static Action<DrawPolyDelegate, int> botDrawDebugPolygons;

		static void CM_DrawDebugSurface( DrawPolyDelegate drawPoly ) 
		{			
			patchCollide_t	pc;
			facet_t			facet;
			winding_t		w;
			int				i, j, k, n;
			int				curplanenum, planenum;
			bool inward, curinward;
			vec4_t			plane;
			vec3_t mins = new vec3_t ( -15, -15, -28), maxs = new vec3_t( 15, 15, 28 );
			//vec3_t mins = {0, 0, 0}, maxs = {0, 0, 0};
			vec3_t v1 = new vec3_t(), v2 = new vec3_t( );

			if ( cv2 == null)
				cv2 = Cvar.Cvar_Get( "r_debugSurface", "0", 0 );

			if (cv2.integer != 1)
			{
				botDrawDebugPolygons?.Invoke(drawPoly, cv2.integer);
				return;
			}

			if ( debugPatchCollide == null )
				return;

			if ( cv == null )
				cv = Cvar.Cvar_Get( "cm_debugSize", "2", 0 );

			pc = debugPatchCollide;

			for ( i = 0; i < pc.numFacets; i++ ) 
			{
				facet = pc.facets[i];
				for ( k = 0 ; k < facet.numBorders + 1; k++ ) {
					//
					if (k < facet.numBorders) {
						planenum = facet.borderPlanes[k];
						inward = facet.borderInward[k] == 1 ? true : false;
					}
					else {
						planenum = facet.surfacePlane;
						inward = false;
						//continue;
					}

					q_shared.Vector4Copy( pc.planes[ planenum ].plane, out plane );

					//planenum = facet.surfacePlane;
					if ( inward ) {
						q_shared.VectorSubtract( q_math.vec3_origin, plane, out plane );
						plane.w = -plane.w;
					}

					plane[3] += cv.value;
					//*
					for (n = 0; n < 3; n++)
					{
						if (plane[n] > 0) v1[n] = maxs[n];
						else v1[n] = mins[n];
					} //end for
					q_shared.VectorNegate(plane, out v2);
					plane[3] += MathF.Abs( q_shared.DotProduct(v1, v2));
					//*/

					w = cm_polylib.BaseWindingForPlane( plane.xyz,  plane.w );
					for ( j = 0 ; j < facet.numBorders + 1 && w != null; j++ ) {
						//
						if (j < facet.numBorders) {
							curplanenum = facet.borderPlanes[j];
							curinward = facet.borderInward[j] == 1 ? true : false;
						}
						else {
							curplanenum = facet.surfacePlane;
							curinward = false;
							//continue;
						}
						//
						if (curplanenum == planenum) continue;

						q_shared.Vector4Copy( pc.planes[ curplanenum ].plane, out plane );
						if ( !curinward )
						{
						q_shared.VectorSubtract( q_math.vec3_origin, plane, out plane );
						plane.w = -plane.w;
						}
				//			if ( !facet.borderNoAdjust[j] ) {
							plane.w -= cv.value;
				//			}
						for (n = 0; n < 3; n++)
						{
							if (plane[n] > 0) v1[n] = maxs[n];
							else v1[n] = mins[n];
						} //end for
						q_shared.VectorNegate(plane, out v2);
						plane[3] -= MathF.Abs(q_shared.DotProduct(v1, v2));

						cm_polylib.ChopWindingInPlace( ref w, plane, plane[3], 0.1f );
					}
					if ( w != null ) 
					{
						if ( facet == debugFacet ) 
						{
							drawPoly( 4, w.numpoints, w.p[0].ToArray() );
							//Com_Printf("blue facet has %d border planes\n", facet.numBorders);
						} 
						else 
						{
							drawPoly( 1, w.numpoints, w.p[0].ToArray( ) );
						}
						cm_polylib.FreeWinding( w );
					}
					else
						common.Com_Printf("winding chopped away by border planes\n");
				}
			}

			// draw the debug block
			{
				vec3_t[]			v = new vec3_t[3];

				q_shared.VectorCopy( debugBlockPoints[0], out v[0] );
				q_shared.VectorCopy( debugBlockPoints[1], out v[1] );
				q_shared.VectorCopy( debugBlockPoints[2], out v[2] );
				drawPoly( 2, 3, v[0].ToArray( ) );

				q_shared.VectorCopy( debugBlockPoints[2], out v[0] );
				q_shared.VectorCopy( debugBlockPoints[3], out v[1] );
				q_shared.VectorCopy( debugBlockPoints[0], out v[2] );
				drawPoly( 2, 3, v[0].ToArray( ) );
			}
		}
	}
}
