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
using System.Linq;

namespace SharpQ3.Engine.qcommon
{
	public class winding_t : IDisposable
	{
		public int numpoints;
		public vec3_t[] p;//[4];        // variable sized

        public void Dispose( )
        {
			p = null;
        }
    }

	// this is only used for visualization tools in cm_ debug functions
	public static class cm_polylib
	{
		const int MAX_POINTS_ON_WINDING = 64;

		public const int SIDE_FRONT = 0;
		public const int SIDE_BACK = 1;
		public const int SIDE_ON = 2;
		public const int SIDE_CROSS = 3;

		public const float CLIP_EPSILON = 0.1f;

		public const int MAX_MAP_BOUNDS = 65535;

		// you can define on_epsilon in the makefile as tighter
		public const float ON_EPSILON = 0.1f;

		// counters are only bumped when running single threaded,
		// because they are an awefull coherence problem
		static int	c_active_windings;
		static int c_peak_windings;
		static int c_winding_allocs;
		static int c_winding_points;

		static void pw(winding_t w)
		{
			int		i;
			for ( i = 0; i < w.numpoints; i++ )
				Console.WriteLine( SprintfNET.StringFormatter.PrintF( "(%5.1f, %5.1f, %5.1f)\n", w.p[i][0], w.p[i][1], w.p[i][2] ) );
		}


		/*
		=============
		AllocWinding
		=============
		*/
		public static winding_t AllocWinding (int points)
		{
			winding_t	w;
			int			s;

			c_winding_allocs++;
			c_winding_points += points;
			c_active_windings++;

			if (c_active_windings > c_peak_windings)
				c_peak_windings = c_active_windings;

			w = new winding_t( );
			w.p = new vec3_t[points];
			return w;
		}

		// Due to C# GC we don't really need to do anything here anymore, dispose clears the array reference in winding_t
		public static void FreeWinding (winding_t w)
		{
			if ( w?.p == null )
				common.Com_Error (errorParm_t.ERR_FATAL, "FreeWinding: freed a freed winding");

			c_active_windings--;

			w.Dispose( );
		}

		/*
		============
		RemoveColinearPoints
		============
		*/
		static int c_removed;

		static void RemoveColinearPoints (ref winding_t w)
		{
			int		i, j, k;
			vec3_t	v1, v2;
			int		nump;
			vec3_t[]	p = new vec3_t[MAX_POINTS_ON_WINDING];

			nump = 0;
			for (i=0 ; i<w.numpoints ; i++)
			{
				j = (i+1)%w.numpoints;
				k = (i+w.numpoints-1)%w.numpoints;
				q_shared.VectorSubtract (w.p[j], w.p[i], out v1 );
				q_shared.VectorSubtract (w.p[i], w.p[k], out v2 );
				q_math.VectorNormalize2(v1, out v1 );
				q_math.VectorNormalize2(v2,out v2);
				if ( q_shared.DotProduct(v1, v2) < 0.999)
				{
					q_shared.VectorCopy (w.p[i], out p[nump]);
					nump++;
				}
			}

			if (nump == w.numpoints)
				return;

			c_removed += w.numpoints - nump;
			w.numpoints = nump;
			w.p = p;
		}

		/*
		============
		WindingPlane
		============
		*/
		static void WindingPlane (winding_t w, out vec3_t normal, out float dist)
		{
			q_shared.VectorSubtract (w.p[1], w.p[0], out vec3_t v1 );
			q_shared.VectorSubtract (w.p[2], w.p[0], out vec3_t v2 );
			q_shared.CrossProduct (v2, v1, out normal );
			q_math.VectorNormalize2(normal, out normal);
			dist = q_shared.DotProduct (w.p[0], normal);
		}

		/*
		=============
		WindingArea
		=============
		*/
		static float WindingArea (winding_t w)
		{
			int		i;
			vec3_t	d1, d2, cross;
			float	total;

			total = 0;
			for (i=2 ; i<w.numpoints ; i++)
			{
				q_shared.VectorSubtract (w.p[i-1], w.p[0], out d1);
				q_shared.VectorSubtract (w.p[i], w.p[0], out d2 );
				q_shared.CrossProduct (d1, d2, out cross );
				total += 0.5f * q_shared.VectorLength ( cross );
			}
			return total;
		}

		/*
		=============
		WindingBounds
		=============
		*/
		public static void WindingBounds (winding_t w, out vec3_t mins, out vec3_t maxs)
		{
			float	v;
			int		i,j;

			mins = new vec3_t( );
			maxs = new vec3_t( );
			mins.x = mins.y = mins.z = MAX_MAP_BOUNDS;
			maxs.x = maxs.y = maxs.z = -MAX_MAP_BOUNDS;

			for (i=0 ; i<w.numpoints ; i++)
			{
				for (j=0 ; j<3 ; j++)
				{
					v = w.p[i][j];
					if (v < mins[j])
						mins[j] = v;
					if (v > maxs[j])
						maxs[j] = v;
				}
			}
		}

		/*
		=============
		WindingCenter
		=============
		*/
		static void WindingCenter (winding_t w, vec3_t center)
		{
			int		i;
			float	scale;

			q_shared.VectorCopy ( q_math.vec3_origin, out center);
			for (i=0 ; i<w.numpoints ; i++)
				q_shared.VectorAdd (w.p[i], center, out center);

			scale = 1.0f/w.numpoints;
			q_shared.VectorScale (center, scale, out center);
		}

		/*
		=================
		BaseWindingForPlane
		=================
		*/
		public static winding_t BaseWindingForPlane (vec3_t normal, float dist)
		{
			int		i, x;
			float max, v;
			vec3_t	org, vright, vup;
			
		// find the major axis

			max = -MAX_MAP_BOUNDS;
			x = -1;
			for (i=0 ; i<3; i++)
			{
				v = Math.Abs(normal[i]);
				if (v > max)
				{
					x = i;
					max = v;
				}
			}
			if (x==-1)
				common.Com_Error (errorParm_t.ERR_DROP, "BaseWindingForPlane: no axis found");

			q_shared.VectorCopy (q_math.vec3_origin, out vup);	
			switch (x)
			{
			case 0:
			case 1:
				vup[2] = 1;
				break;		
			case 2:
				vup[0] = 1;
				break;		
			}

			v = q_shared.DotProduct (vup, normal);
			q_shared.VectorMA (vup, -v, normal, out vup );
			q_math.VectorNormalize2(vup, out vup );

			q_shared.VectorScale (normal, dist, out org );

			q_shared.CrossProduct (vup, normal, out vright );

			q_shared.VectorScale (vup, MAX_MAP_BOUNDS, out vup );
			q_shared.VectorScale (vright, MAX_MAP_BOUNDS, out vright );

		// project a really big	axis aligned box onto the plane
			var w = AllocWinding (4);

			q_shared.VectorSubtract (org, vright, out w.p[0]);
			q_shared.VectorAdd (w.p[0], vup, out w.p[0]);

			q_shared.VectorAdd (org, vright, out w.p[1]);
			q_shared.VectorAdd (w.p[1], vup, out w.p[1]);

			q_shared.VectorAdd (org, vright, out w.p[2]);
			q_shared.VectorSubtract (w.p[2], vup, out w.p[2]);
			
			q_shared.VectorSubtract (org, vright, out w.p[3]);
			q_shared.VectorSubtract (w.p[3], vup, out w.p[3]);
			
			w.numpoints = 4;
			
			return w;	
		}

		/*
		==================
		CopyWinding
		==================
		*/
		public static winding_t	CopyWinding (winding_t w)
		{
			var c = AllocWinding (w.numpoints);
			c.numpoints = w.numpoints;
			c.p = w.p.ToArray( );
			return c;
		}

		/*
		==================
		ReverseWinding
		==================
		*/
		public static winding_t ReverseWinding (winding_t w)
		{
			var c = CopyWinding( w );

			if ( c.p != null )
				c.p = c.p.Reverse( ).ToArray( );

			return c;
		}

		/*
		=============
		ClipWindingEpsilon
		=============
		*/
		static void ClipWindingEpsilon (winding_t @in, vec3_t normal, float dist, 
						float epsilon, out winding_t front,  out winding_t back)
		{
			float[]	dists = new float[MAX_POINTS_ON_WINDING+4];
			int[]		sides = new int[MAX_POINTS_ON_WINDING+4];
			int[]		counts = new int[3];
			int		i, j;
			vec3_t	p1, p2;
			vec3_t	mid = new vec3_t();
			winding_t	f, b;
			int		maxpts;
			
			counts[0] = counts[1] = counts[2] = 0;

			// determine sides for each point
			for (i=0 ; i<@in.numpoints ; i++)
			{
				dot = q_shared.DotProduct (@in.p[i], normal);
				dot -= dist;
				dists[i] = dot;
				if (dot > epsilon)
					sides[i] = SIDE_FRONT;
				else if (dot < -epsilon)
					sides[i] = SIDE_BACK;
				else
				{
					sides[i] = SIDE_ON;
				}
				counts[sides[i]]++;
			}
			sides[i] = sides[0];
			dists[i] = dists[0];
			
			front = back = null;

			if (counts[0] == 0)
			{
				back = CopyWinding (@in);
				return;
			}
			if (counts[1] == 0 )
			{
				front = CopyWinding (@in);
				return;
			}

			maxpts = @in.numpoints+4;	// cant use counts[0]+2 because
										// of fp grouping errors

			front = f = AllocWinding (maxpts);
			back = b = AllocWinding (maxpts);
				
			for (i=0 ; i<@in.numpoints ; i++)
			{
				p1 = @in.p[i];
				
				if (sides[i] == SIDE_ON)
				{
					q_shared.VectorCopy (p1, out f.p[f.numpoints]);
					f.numpoints++;
					q_shared.VectorCopy (p1, out b.p[b.numpoints]);
					b.numpoints++;
					continue;
				}
			
				if (sides[i] == SIDE_FRONT)
				{
					q_shared.VectorCopy (p1, out f.p[f.numpoints]);
					f.numpoints++;
				}
				if (sides[i] == SIDE_BACK)
				{
					q_shared.VectorCopy (p1, out b.p[b.numpoints]);
					b.numpoints++;
				}

				if (sides[i+1] == SIDE_ON || sides[i+1] == sides[i])
					continue;
					
			// generate a split point
				p2 = @in.p[(i+1)%@in.numpoints];
				
				dot = dists[i] / (dists[i]-dists[i+1]);
				for (j=0 ; j<3 ; j++)
				{	// avoid round off error when possible
					if (normal[j] == 1)
						mid[j] = dist;
					else if (normal[j] == -1)
						mid[j] = -dist;
					else
						mid[j] = p1[j] + dot*(p2[j]-p1[j]);
				}
					
				q_shared.VectorCopy (mid, out f.p[f.numpoints]);
				f.numpoints++;
				q_shared.VectorCopy (mid, out b.p[b.numpoints]);
				b.numpoints++;
			}
			
			if (f.numpoints > maxpts || b.numpoints > maxpts)
				common.Com_Error( errorParm_t.ERR_DROP, "ClipWinding: points exceeded estimate");
			if (f.numpoints > MAX_POINTS_ON_WINDING || b.numpoints > MAX_POINTS_ON_WINDING)
				common.Com_Error( errorParm_t.ERR_DROP, "ClipWinding: MAX_POINTS_ON_WINDING");
		}


		/*
		=============
		ChopWindingInPlace
		=============
		*/
		static float dot;       // VC 4.2 optimizer bug if not static

		public static void ChopWindingInPlace (ref winding_t inout, vec3_t normal, float dist, float epsilon )
		{
			float[] dists = new float[MAX_POINTS_ON_WINDING+4];
			int[]		sides = new int[MAX_POINTS_ON_WINDING+4];
			int[]		counts = new int[3];
			int		i, j;
			vec3_t	p1, p2;
			vec3_t	mid = new vec3_t();
			winding_t	f;
			int		maxpts;

			counts[0] = counts[1] = counts[2] = 0;

			// determine sides for each point
			for (i=0 ; i< inout.numpoints; i++)
			{
				dot = q_shared.DotProduct ( inout.p[i], normal);
				dot -= dist;
				dists[i] = dot;
				if (dot > epsilon)
					sides[i] = SIDE_FRONT;
				else if (dot < -epsilon)
					sides[i] = SIDE_BACK;
				else
				{
					sides[i] = SIDE_ON;
				}
				counts[sides[i]]++;
			}
			sides[i] = sides[0];
			dists[i] = dists[0];
			
			if (counts[0] == 0)
			{
				FreeWinding ( inout );
				inout = null;
				return;
			}
			if (counts[1] == 0)
				return;		// inout stays the same

			maxpts = inout.numpoints+4;	// cant use counts[0]+2 because
										// of fp grouping errors

			f = AllocWinding (maxpts);
				
			for (i=0 ; i< inout.numpoints ; i++)
			{
				p1 = inout.p[i];
				
				if (sides[i] == SIDE_ON)
				{
					q_shared.VectorCopy (p1, out f.p[f.numpoints]);
					f.numpoints++;
					continue;
				}
			
				if (sides[i] == SIDE_FRONT)
				{
					q_shared.VectorCopy (p1, out f.p[f.numpoints]);
					f.numpoints++;
				}

				if (sides[i+1] == SIDE_ON || sides[i+1] == sides[i])
					continue;
					
			// generate a split point
				p2 = inout.p[(i+1)% inout.numpoints];
				
				dot = dists[i] / (dists[i]-dists[i+1]);
				for (j=0 ; j<3 ; j++)
				{	// avoid round off error when possible
					if (normal[j] == 1)
						mid[j] = dist;
					else if (normal[j] == -1)
						mid[j] = -dist;
					else
						mid[j] = p1[j] + dot*(p2[j]-p1[j]);
				}
					
				q_shared.VectorCopy (mid, out f.p[f.numpoints]);
				f.numpoints++;
			}
			
			if (f.numpoints > maxpts)
				common.Com_Error (errorParm_t.ERR_DROP, "ClipWinding: points exceeded estimate");
			if (f.numpoints > MAX_POINTS_ON_WINDING)
				common.Com_Error (errorParm_t.ERR_DROP, "ClipWinding: MAX_POINTS_ON_WINDING");

			FreeWinding (inout);
			inout.p = f.p;
			inout.numpoints = f.numpoints;
		}


		/*
		=================
		ChopWinding

		Returns the fragment of in that is on the front side
		of the cliping plane.  The original is freed.
		=================
		*/
		static winding_t ChopWinding (winding_t @in, vec3_t normal, float dist)
		{
			winding_t	f, b;

			ClipWindingEpsilon (@in, normal, dist, ON_EPSILON, out f, out b);
			FreeWinding (@in);
			if (b != null)
				FreeWinding (b);
			return f;
		}


		/*
		=================
		CheckWinding

		=================
		*/
		static void CheckWinding (winding_t w)
		{
			int		i, j;
			vec3_t	p1, p2;
			float	d, edgedist;
			vec3_t	dir, edgenormal, facenormal;
			float	area;
			float	facedist;

			if (w.numpoints < 3)
				common.Com_Error ( errorParm_t.ERR_DROP, "CheckWinding: %i points",w.numpoints);
			
			area = WindingArea(w);
			if (area < 1)
				common.Com_Error ( errorParm_t.ERR_DROP, "CheckWinding: %f area", area);

			WindingPlane (w, out facenormal, out facedist);
			
			for (i=0 ; i<w.numpoints ; i++)
			{
				p1 = w.p[i];

				for (j=0 ; j<3 ; j++)
					if (p1[j] > MAX_MAP_BOUNDS || p1[j] < -MAX_MAP_BOUNDS)
						common.Com_Error ( errorParm_t.ERR_DROP, "CheckFace: BUGUS_RANGE: %f",p1[j]);

				j = i+1 == w.numpoints ? 0 : i+1;
				
			// check the point is on the face plane
				d = q_shared.DotProduct (p1, facenormal) - facedist;
				if (d < -ON_EPSILON || d > ON_EPSILON)
					common.Com_Error ( errorParm_t.ERR_DROP, "CheckWinding: point off plane");
			
			// check the edge isnt degenerate
				p2 = w.p[j];
				q_shared.VectorSubtract (p2, p1, out dir);
				
				if ( q_shared.VectorLength (dir) < ON_EPSILON)
					common.Com_Error ( errorParm_t.ERR_DROP, "CheckWinding: degenerate edge");

				q_shared.CrossProduct (facenormal, dir, out edgenormal);
				q_math.VectorNormalize2 (edgenormal, out edgenormal );
				edgedist = q_shared.DotProduct (p1, edgenormal);
				edgedist += ON_EPSILON;
				
			// all other points must be on front side
				for (j=0 ; j<w.numpoints ; j++)
				{
					if (j == i)
						continue;
					d = q_shared.DotProduct (w.p[j], edgenormal);
					if (d > edgedist)
						common.Com_Error (errorParm_t.ERR_DROP, "CheckWinding: non-convex");
				}
			}
		}


		/*
		============
		WindingOnPlaneSide
		============
		*/
		static int WindingOnPlaneSide (winding_t w, vec3_t normal, float dist)
		{
			bool	front, back;
			int			i;
			float		d;

			front = false;
			back = false;
			for (i=0 ; i<w.numpoints ; i++)
			{
				d = q_shared.DotProduct (w.p[i], normal) - dist;
				if (d < -ON_EPSILON)
				{
					if (front)
						return SIDE_CROSS;
					back = true;
					continue;
				}
				if (d > ON_EPSILON)
				{
					if (back)
						return SIDE_CROSS;
					front = true;
					continue;
				}
			}

			if (back)
				return SIDE_BACK;
			if (front)
				return SIDE_FRONT;
			return SIDE_ON;
		}


		/*
		=================
		AddWindingToConvexHull

		Both w and *hull are on the same plane
		=================
		*/
		const int MAX_HULL_POINTS = 128;
		static void	AddWindingToConvexHull( winding_t w, ref winding_t hull, vec3_t normal ) 
		{
			int			i, j, k;
			vec3_t		p, copy;
			vec3_t		dir;
			float		d;
			int			numHullPoints, numNew;
			vec3_t[]	hullPoints = new vec3_t[MAX_HULL_POINTS];
			vec3_t[]	newHullPoints = new vec3_t[MAX_HULL_POINTS];
			vec3_t[]	hullDirs = new vec3_t[MAX_HULL_POINTS];
			bool[]	hullSide = new bool[MAX_HULL_POINTS];
			bool	outside;

			if ( hull == null ) {
				hull = CopyWinding( w );
				return;
			}

			numHullPoints = hull.numpoints;
			Array.Copy( hull.p, hullPoints, hull.numpoints );

			for ( i = 0 ; i < w.numpoints ; i++ ) {
				p = w.p[i];

				// calculate hull side vectors
				for ( j = 0 ; j < numHullPoints ; j++ ) {
					k = ( j + 1 ) % numHullPoints;

					q_shared.VectorSubtract( hullPoints[k], hullPoints[j], out dir );
					q_math.VectorNormalize2( dir, out dir );
					q_shared.CrossProduct( normal, dir, out hullDirs[j] );
				}

				outside = false;
				for ( j = 0 ; j < numHullPoints ; j++ ) {
					q_shared.VectorSubtract( p, hullPoints[j], out dir );
					d = q_shared.DotProduct( dir, hullDirs[j] );
					if ( d >= ON_EPSILON ) {
						outside = true;
					}
					if ( d >= -ON_EPSILON ) {
						hullSide[j] = true;
					} else {
						hullSide[j] = false;
					}
				}

				// if the point is effectively inside, do nothing
				if ( !outside ) {
					continue;
				}

				// find the back side to front side transition
				for ( j = 0 ; j < numHullPoints ; j++ ) {
					if ( !hullSide[ j % numHullPoints ] && hullSide[ (j + 1) % numHullPoints ] ) {
						break;
					}
				}
				if ( j == numHullPoints ) {
					continue;
				}

				// insert the point here
				q_shared.VectorCopy( p, out newHullPoints[0] );
				numNew = 1;

				// copy over all points that aren't double fronts
				j = (j+1)%numHullPoints;
				for ( k = 0 ; k < numHullPoints ; k++ ) {
					if ( hullSide[ (j+k) % numHullPoints ] && hullSide[ (j+k+1) % numHullPoints ] ) {
						continue;
					}
					copy = hullPoints[ (j+k+1) % numHullPoints ];
					q_shared.VectorCopy( copy, out newHullPoints[numNew] );
					numNew++;
				}

				numHullPoints = numNew;

				Array.Copy( newHullPoints, hullPoints, numHullPoints );
			}

			FreeWinding( hull );
			w = AllocWinding( numHullPoints );
			w.numpoints = numHullPoints;
			hull = w;

			Array.Copy( hullPoints, w.p, numHullPoints );
		}
	}
}
