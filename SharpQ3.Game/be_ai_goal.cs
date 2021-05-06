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

namespace SharpQ3.Game
{
	/*****************************************************************************
	 * name:		be_ai_goal.h
	 *
	 * desc:		goal AI
	 *
	 * $Archive: /source/code/botlib/be_ai_goal.h $
	 *
	 *****************************************************************************/
	public static class be_ai_goal
	{
		#define MAX_AVOIDGOALS			256
		#define MAX_GOALSTACK			8

		#define GFL_NONE				0
		#define GFL_ITEM				1
		#define GFL_ROAM				2
		#define GFL_DROPPED				4

		//a bot goal
		typedef struct bot_goal_s
		{
			vec3_t origin;				//origin of the goal
			int areanum;				//area number of the goal
			vec3_t mins, maxs;			//mins and maxs of the goal
			int entitynum;				//number of the goal entity
			int number;					//goal number
			int flags;					//goal flags
			int iteminfo;				//item information
		} bot_goal_t;
	}
}
