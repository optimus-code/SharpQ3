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
using System;
using System.Linq;

namespace SharpQ3.Engine.qcommon
{
	public static class cm_test
	{
		/*
		==================
		CM_PointLeafnum_r

		==================
		*/
		public static int CM_PointLeafnum_r( vec3_t p, int num ) 
		{
			float		d;

			while (num >= 0)
			{
				var node = cm_local.cm.nodes[num];
				var plane = node.plane;
				
				if (plane.type < 3)
					d = p[plane.type] - plane.dist;
				else
					d = q_shared.DotProduct (plane.normal, p) - plane.dist;
				if (d < 0)
					num = node.children[1];
				else
					num = node.children[0];
			}

			cm_local.c_pointcontents++;		// optimize counter

			return -1 - num;
		}

		public static int CM_PointLeafnum( vec3_t p ) 
		{
			if ( cm_local.cm.numNodes  == 0 )	// map not loaded
				return 0;

			return CM_PointLeafnum_r (p, 0);
		}

		/*
		======================================================================

		LEAF LISTING

		======================================================================
		*/


		public static void CM_StoreLeafs( leafList_t ll, int nodenum ) 
		{
			int		leafNum;

			leafNum = -1 - nodenum;

			// store the lastLeaf even if the list is overflowed
			if ( cm_local.cm.leafs[leafNum].cluster != -1 ) {
				ll.lastLeaf = leafNum;
			}

			if ( ll.count >= ll.maxcount) 
			{
				ll.overflowed = true;
				return;
			}
			ll.list[ll.count++] = leafNum;
		}

		public static void CM_StoreBrushes( leafList_t ll, int nodenum ) 
		{
			int			i, k;
			int			leafnum;
			int			brushnum;

			leafnum = -1 - nodenum;

			var leaf = cm_local.cm.leafs[leafnum];

			for ( k = 0 ; k < leaf.numLeafBrushes ; k++ ) 
			{
				brushnum = cm_local.cm.leafbrushes[leaf.firstLeafBrush+k];
				var b = cm_local.cm.brushes[brushnum];

				if ( b.checkcount == cm_local.cm.checkcount )
					continue;	// already checked this brush in another leaf

				b.checkcount = cm_local.cm.checkcount;

				for ( i = 0 ; i < 3 ; i++ ) 
				{
					if ( b.bounds[0][i] >= ll.bounds[1][i] || b.bounds[1][i] <= ll.bounds[0][i] ) {
						break;
					}
				}

				if ( i != 3 )
					continue;

				if ( ll.count >= ll.maxcount) 
				{
					ll.overflowed = true;
					return;
				}
				var bytes = qcommon.StructureToBytes( ref b );
				// optimus-code - TODO - IS THIS RIGHT??
				Buffer.BlockCopy( bytes, 0, ll.list, ll.count * sizeof( int ), bytes.Length );
				//((cbrush_t **)ll.list[ ll.count++ ] = b;
			}
		}

		/*
		=============
		CM_BoxLeafnums

		Fills in a list of all the leafs touched
		=============
		*/
		public static void CM_BoxLeafnums_r( leafList_t ll, int nodenum ) 
		{
			cplane_t	plane;
			cNode_t		node;
			int			s;

			while ( true ) 
			{
				if ( nodenum < 0 ) 
				{
					ll.storeLeafs( ll, nodenum );
					return;
				}
			
				node = cm_local.cm.nodes[nodenum];
				plane = node.plane;
				s = q_math.BoxOnPlaneSide( ll.bounds[0], ll.bounds[1], plane );
				if ( s == 1 ) 
				{
					nodenum = node.children[0];
				} 
				else if (s == 2) 
				{
					nodenum = node.children[1];
				} 
				else 
				{
					// go down both
					CM_BoxLeafnums_r( ll, node.children[0] );
					nodenum = node.children[1];
				}

			}
		}

		/*
		==================
		CM_BoxLeafnums
		==================
		*/
		public static int CM_BoxLeafnums( vec3_t mins, vec3_t maxs, int[] list, int listsize, out int lastLeaf ) 
		{
			leafList_t	ll = new leafList_t();

			cm_local.cm.checkcount++;

			q_shared.VectorCopy( mins, out ll.bounds[0] );
			q_shared.VectorCopy( maxs, out ll.bounds[1] );
			ll.count = 0;
			ll.maxcount = listsize;
			ll.list = list;
			ll.storeLeafs = CM_StoreLeafs;
			ll.lastLeaf = 0;
			ll.overflowed = false;

			CM_BoxLeafnums_r( ll, 0 );

			lastLeaf = ll.lastLeaf;
			return ll.count;
		}

		/*
		==================
		CM_BoxBrushes
		==================
		*/
		public static int CM_BoxBrushes( vec3_t mins, vec3_t maxs, cbrush_t **list, int listsize )
		{
			leafList_t ll = new leafList_t( );

			cm_local.cm.checkcount++;

			q_shared.VectorCopy( mins, out ll.bounds[0] );
			q_shared.VectorCopy( maxs, out ll.bounds[1] );
			ll.count = 0;
			ll.maxcount = listsize;
			ll.list = (int*) (void *)list;
			ll.storeLeafs = CM_StoreBrushes;
			ll.lastLeaf = 0;
			ll.overflowed = false;
			
			CM_BoxLeafnums_r( ll, 0 );

			return ll.count;
		}


		//====================================================================


		/*
		==================
		CM_PointContents

		==================
		*/
		public static int CM_PointContents( vec3_t p, clipHandle_t model ) 
		{
			int			leafnum;
			int			i, k;
			int			brushnum;
			cLeaf_t		leaf;
			int			contents;
			float		d;
			cmodel_t	clipm;

			if (cm_local.cm.numNodes == 0) 	// map not loaded
				return 0;

			if ( model.ID != 0 ) 
			{
				clipm = cm_load.CM_ClipHandleToModel( model );
				leaf = clipm.leaf;
			} 
			else 
			{
				leafnum = CM_PointLeafnum_r (p, 0);
				leaf = cm_local.cm.leafs[leafnum];
			}

			contents = 0;
			for (k=0 ; k<leaf.numLeafBrushes ; k++) {
				brushnum = cm_local.cm.leafbrushes[leaf.firstLeafBrush+k];
				var b = cm_local.cm.brushes[brushnum];

				// see if the point is in the brush
				for ( i = 0 ; i < b.numsides ; i++ ) {
					d = q_shared.DotProduct( p, b.sides[i].plane.normal );
					// FIXME test for Cash
					//	if ( d >= b.sides[i].plane.dist ) {
					if ( d > b.sides[i].plane.dist ) {
						break;
					}
				}

				if ( i == b.numsides ) {
					contents |= b.contents;
				}
			}

			return contents;
		}

		/*
		==================
		CM_TransformedPointContents

		Handles offseting and rotation of the end points for moving and
		rotating entities
		==================
		*/
		public static int CM_TransformedPointContents( vec3_t p, clipHandle_t model, vec3_t origin, vec3_t angles ) 
		{
			// subtract origin offset
			q_shared.VectorSubtract (p, origin, out vec3_t p_l);

			// rotate start and end into the models frame of reference
			if ( model.ID != cm_local.BOX_MODEL_HANDLE && (angles[0] != 0 || angles[1] != 0 || angles[2] != 0 ) )
			{
				q_math.AngleVectors (angles, out var forward, out var right, out var up);

				q_shared.VectorCopy (p_l, out vec3_t temp );
				p_l[0] = q_shared.DotProduct (temp, forward);
				p_l[1] = -q_shared.DotProduct (temp, right);
				p_l[2] = q_shared.DotProduct (temp, up);
			}

			return CM_PointContents( p_l, model );
		}

		/*
		===============================================================================

		PVS

		===============================================================================
		*/

		public static byte[] CM_ClusterPVS (int cluster)
		{
			if (cluster < 0 || cluster >= cm_local.cm.numClusters || !cm_local.cm.vised )
				return cm_local.cm.visibility;

			var index = cluster * cm_local.cm.clusterBytes;
			// optimus-code - TODO - IS THIS RIGHT??
			return cm_local.cm.visibility.ToList().GetRange( index, cm_local.cm.clusterBytes ).ToArray();
		}

		/*
		===============================================================================

		AREAPORTALS

		===============================================================================
		*/

		public static void CM_FloodArea_r( int areaNum, int floodnum) 
		{
			int i;
			int[] con;

			var area = cm_local.cm.areas[ areaNum ];

			if ( area.floodvalid == cm_local.cm.floodvalid )
			{
				if (area.floodnum == floodnum)
					return;
				common.Com_Error (errorParm_t.ERR_DROP, "FloodArea_r: reflooded");
			}

			area.floodnum = floodnum;
			area.floodvalid = cm_local.cm.floodvalid;
			//con = cm_local.cm.areaPortals + areaNum * cm_local.cm.numAreas;
			var index = areaNum * cm_local.cm.numAreas;
			// optimus-code - TODO - IS THIS RIGHT??
			con = cm_local.cm.areaPortals.ToList( ).GetRange( index, cm_local.cm.areaPortals.Length - index ).ToArray();

			for ( i=0 ; i < cm_local.cm.numAreas  ; i++ ) {
				if ( con[i] > 0 ) {
					CM_FloodArea_r( i, floodnum );
				}
			}
		}

		/*
		====================
		CM_FloodAreaConnections

		====================
		*/
		public static void	CM_FloodAreaConnections( ) 
		{
			int		i;
			cArea_t	area;
			int		floodnum;

			// all current floods are now invalid
			cm_local.cm.floodvalid++;
			floodnum = 0;

			for (i = 0 ; i < cm_local.cm.numAreas ; i++)
			{
				area = cm_local.cm.areas[i];

				if (area.floodvalid == cm_local.cm.floodvalid)
					continue;		// already flooded into

				floodnum++;
				CM_FloodArea_r (i, floodnum);
			}
		}

		/*
		====================
		CM_AdjustAreaPortalState

		====================
		*/
		public static void	CM_AdjustAreaPortalState( int area1, int area2, bool open ) 
		{
			if ( area1 < 0 || area2 < 0 ) {
				return;
			}

			if ( area1 >= cm_local.cm.numAreas || area2 >= cm_local.cm.numAreas )
				common.Com_Error (errorParm_t.ERR_DROP, "CM_ChangeAreaPortalState: bad area number");

			if ( open ) 
			{
				cm_local.cm.areaPortals[ area1 * cm_local.cm.numAreas + area2 ]++;
				cm_local.cm.areaPortals[ area2 * cm_local.cm.numAreas + area1 ]++;
			} 
			else 
			{
				cm_local.cm.areaPortals[ area1 * cm_local.cm.numAreas + area2 ]--;
				cm_local.cm.areaPortals[ area2 * cm_local.cm.numAreas + area1 ]--;
				if ( cm_local.cm.areaPortals[ area2 * cm_local.cm.numAreas + area1 ] < 0 )
				{
					common.Com_Error ( errorParm_t.ERR_DROP, "CM_AdjustAreaPortalState: negative reference count");
				}
			}

			CM_FloodAreaConnections ();
		}

		/*
		====================
		CM_AreasConnected

		====================
		*/
		public static bool	CM_AreasConnected( int area1, int area2 ) 
		{
			if ( cm_local.cm_noAreas.integer == 1 )
				return true;

			if ( area1 < 0 || area2 < 0 )
				return false;

			if (area1 >= cm_local.cm.numAreas || area2 >= cm_local.cm.numAreas)
				common.Com_Error (errorParm_t.ERR_DROP, "area >= cm.numAreas");

			if ( cm_local.cm.areas[area1].floodnum == cm_local.cm.areas[area2].floodnum)
				return true;

			return false;
		}


		/*
		=================
		CM_WriteAreaBits

		Writes a bit vector of all the areas
		that are in the same flood as the area parameter
		Returns the number of bytes needed to hold all the bits.

		The bits are OR'd in, so you can CM_WriteAreaBits from multiple
		viewpoints and get the union of all visible areas.

		This is used to cull non-visible entities from snapshots
		=================
		*/
		public static int CM_WriteAreaBits (ref byte[] buffer, int area)
		{
			int		i;
			int		floodnum;
			int		bytes;

			bytes = ( cm_local.cm.numAreas+7)>>3;

			if ( cm_local.cm_noAreas.integer == 1 || area == -1)
			{	// for debugging, send everything
				common.Com_Memset (buffer, 255, bytes);
			}
			else
			{
				floodnum = cm_local.cm.areas[area].floodnum;
				for (i=0 ; i< cm_local.cm.numAreas ; i++)
				{
					if ( cm_local.cm.areas[i].floodnum == floodnum || area == -1)
						buffer[i>>3] |= ( byte ) ( 1<<(i&7) );
				}
			}

			return bytes;
		}
	}
}
