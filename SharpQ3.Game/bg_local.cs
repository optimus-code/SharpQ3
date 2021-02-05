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
	public static class bg_local
	{
		// bg_local.h -- local definitions for the bg (both games) files

		#define	MIN_WALK_NORMAL	0.7f		// can't walk on very steep slopes

		#define	STEPSIZE		18

		#define	JUMP_VELOCITY	270

		#define	TIMER_LAND		130
		#define	TIMER_GESTURE	(34*66+50)

		#define	OVERCLIP		1.001f

		// all of the locals will be zeroed before each
		// pmove, just to make damn sure we don't have
		// any differences when running on client or server
		typedef struct {
			vec3_t		forward, right, up;
			float		frametime;

			int			msec;

			bool	walking;
			bool	groundPlane;
			trace_t		groundTrace;

			float		impactSpeed;

			vec3_t		previous_origin;
			vec3_t		previous_velocity;
			int			previous_waterlevel;
		} pml_t;
	}
}
