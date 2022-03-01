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

using SharpQ3.Engine.qcommon;
using SprintfNET;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpQ3.Engine
{
	// the modules that run in the virtual machine can't access the cvar_t directly,
	// so they must ask for structured updates
	public class vmCvar_t
	{
		public int handle;
		public int modificationCount;
		public float value;
		public int integer;
		public string @string;
	}

	// paramters for command buffer stuffing
	public enum cbufExec_t
	{
		EXEC_NOW,           // don't return until completed, a VM should NEVER use this,
							// because some commands might cause the VM to be unloaded...
		EXEC_INSERT,        // insert at current position, but don't run yet
		EXEC_APPEND         // add to end of the command buffer (normal case)
	}

	// print levels from renderer (FIXME: set up for game / cgame?)
	public enum printParm_t
	{
		PRINT_ALL,
		PRINT_DEVELOPER,        // only print when "developer 1"
		PRINT_WARNING,
		PRINT_ERROR
	}

	// parameters to the main Error routine
	public enum errorParm_t
	{
		ERR_FATAL,                  // exit the entire game with a popup window
		ERR_DROP,                   // print to console and disconnect from game
		ERR_SERVERDISCONNECT,       // don't kill server
		ERR_DISCONNECT,             // client disconnected from the server
		ERR_NEED_CD                 // pop up the need-cd dialog
	}

	// q_shared.h -- included first by ALL program modules.
	public static class q_shared
	{
		public const string Q3_VERSION = "Q3 1.32b C# edition";
		// 1.32 released 7-10-2002

		public const int MAX_TEAMNAME = 32;

		/**********************************************************************
		  VM Considerations

		  The VM can not use the standard system headers because we aren't really
		  using the compiler they were meant for.  We use bg_lib.h which contains
		  prototypes for the functions we define for our own use in bg_lib.c.

		  When writing mods, please add needed headers HERE, do not start including
		  stuff like <stdio.h> in the various .c files that make up each of the VMs
		  since you will be including system headers files can will have issues.

		  Remember, if you use a C library function that is not defined in bg_lib.c,
		  you will have to add your own version for support in the VM.

		 **********************************************************************/

		//typedef int intptr_t;

		public const string CPUSTRING = "generic";

		public static short BigShort( short l ) 
		{
			return ShortSwap( l ); 
		}

		public static int BigLong( int l ) 
		{ 
			return LongSwap( l ); 
		}

		public static float BigFloat(ref float l )
		{
			return FloatSwap( ref l ); 
		}


		public const char PATH_SEP = '\\';

		//typedef int qhandle_t;
		//typedef int sfxHandle_t;
		//typedef int fileHandle_t;
		//typedef int clipHandle_t;


		public const int MAX_QINT = 0x7fffffff;
		public const int MIN_QINT = ( -MAX_QINT - 1 );

		// angle indexes
		public const int PITCH = 0;   // up / down
		public const int YAW = 1;    // left / right
		public const int ROLL = 2;	// fall over

		// the game guarantees that no string from the network will ever
		// exceed MAX_STRING_CHARS
		public const int MAX_STRING_CHARS = 1024;   // max length of a string passed to Cmd_TokenizeString
		public const int MAX_STRING_TOKENS = 1024;   // max tokens resulting from Cmd_TokenizeString
		public const int MAX_TOKEN_CHARS = 1024;// max length of an individual token

		public const int MAX_INFO_STRING = 1024;
		public const int MAX_INFO_KEY = 1024;
		public const int MAX_INFO_VALUE = 1024;

		public const int BIG_INFO_STRING = 8192; // used for system info key only
		public const int BIG_INFO_KEY = 8192;
		public const int BIG_INFO_VALUE = 8192;


		public const int MAX_QPATH =		64; // max length of a quake game pathname
		//public const int MAX_OSPATH	 =		PATH_MAX;
		public const int MAX_OSPATH = 256;     // max length of a filesystem pathname

		public const int MAX_NAME_LENGTH = 32;      // max length of a client name

		public const int MAX_SAY_TEXT = 150;


		//
		// these aren't needed by any of the VMs.  put in another header?
		//
		public const int MAX_MAP_AREA_BYTES = 32;	// bit vector of area visibility


		// font rendering values used by ui and cgame

		public const int PROP_GAP_WIDTH	= 3;
		public const int PROP_SPACE_WIDTH = 8;
		public const int PROP_HEIGHT = 27;
		public const double PROP_SMALL_SIZE_SCALE = 0.75;

		public const int BLINK_DIVISOR = 200;
		public const int PULSE_DIVISOR = 75;

		public const int UI_LEFT = 0x00000000; // default
		public const int UI_CENTER = 0x00000001;
		public const int UI_RIGHT = 0x00000002;
		public const int UI_FORMATMASK = 0x00000007;
		public const int UI_SMALLFONT = 0x00000010;
		public const int UI_BIGFONT = 0x00000020; // default
		public const int UI_GIANTFONT = 0x00000040;
		public const int UI_DROPSHADOW = 0x00000800;
		public const int UI_BLINK = 0x00001000;
		public const int UI_INVERSE = 0x00002000;
		public const int UI_PULSE = 0x00004000;

		public enum ha_pref
		{
			h_high,
			h_low,
			h_dontcare
		}

		//void* Hunk_Alloc( int size, ha_pref preference );

		//#define Snd_Memset Com_Memset

		public const int CIN_system = 1;
		public const int CIN_loop = 2;
		public const int CIN_hold = 4;
		public const int CIN_silent = 8;
		public const int CIN_shader = 16;

		/*
		==============================================================

		MATHLIB

		==============================================================
		*/

		public struct vec2_t
		{
			public float x, y;
		}

		public struct vec3_t
		{
			public float x, y, z;
		}

		public struct vec4_t
		{
			public float x, y, z, w;
		}
		public struct vec5_t
		{
			public float x, y, z, w, n;
		}

		public const float M_PI = 3.14159265358979323846f;    // matches value in gcc v2 math.h

		public const int NUMVERTEXNORMALS = 162;
		public static vec3_t[] bytedirs = new vec3_t[NUMVERTEXNORMALS];

		// all drawing is done to a 640*480 virtual screen size
		// and will be automatically scaled to the real resolution
		public const int SCREEN_WIDTH = 640;
		public const int SCREEN_HEIGHT = 480;

		public const int TINYCHAR_WIDTH = ( SMALLCHAR_WIDTH);
		public const int TINYCHAR_HEIGHT = ( SMALLCHAR_HEIGHT / 2 );

		public const int SMALLCHAR_WIDTH = 8;
		public const int SMALLCHAR_HEIGHT = 16;

		public const int BIGCHAR_WIDTH = 16;
		public const int BIGCHAR_HEIGHT = 16;

		public const int GIANTCHAR_WIDTH = 32;
		public const int GIANTCHAR_HEIGHT = 48;

		public static vec4_t colorBlack;
		public static vec4_t colorRed;
		public static vec4_t colorGreen;
		public static vec4_t colorBlue;
		public static vec4_t colorYellow;
		public static vec4_t colorMagenta;
		public static vec4_t colorCyan;
		public static vec4_t colorWhite;
		public static vec4_t colorLtGrey;
		public static vec4_t colorMdGrey;
		public static vec4_t colorDkGrey;

		public const char Q_COLOR_ESCAPE = '^';

		public static bool Q_IsColorString( string p, int index ) 
		{
			if ( index + 2 >= p.Length )
				return false;

			var subText = p.Substring( index, 2 );

			return subText.StartsWith( Q_COLOR_ESCAPE ) && !subText.EndsWith( Q_COLOR_ESCAPE );
		}

		public const char COLOR_BLACK	= '0';
		public const char COLOR_RED = '1';
		public const char COLOR_GREEN = '2';
		public const char COLOR_YELLOW = '3';
		public const char COLOR_BLUE = '4';
		public const char COLOR_CYAN = '5';
		public const char COLOR_MAGENTA = '6';
		public const char COLOR_WHITE = '7';
		//#define ColorIndex(c)	( ( (c) - '0' ) & 7 )

		public const string S_COLOR_BLACK = "^0";
		public const string S_COLOR_RED = "^1";
		public const string S_COLOR_GREEN = "^2";
		public const string S_COLOR_YELLOW = "^3";
		public const string S_COLOR_BLUE = "^4";
		public const string S_COLOR_CYAN = "^5";
		public const string S_COLOR_MAGENTA = "^6";
		public const string S_COLOR_WHITE = "^7";

		public static vec4_t[] g_color_table = new vec4_t[8];

		//#define MAKERGB( v, r, g, b ) v[0]=r;v[1]=g;v[2]=b
		//#define MAKERGBA( v, r, g, b, a ) v[0]=r;v[1]=g;v[2]=b;v[3]=a

		public static float DEG2RAD( float a ) 
		{
			return ( ( a ) * M_PI ) / 180.0F;
		}

		public static float RAD2DEG( float a )
		{
			return ( ( a ) * 180.0f ) / M_PI;
		}

		//struct cplane_s;

		public static vec3_t vec3_origin;
		public static vec3_t[] axisDefault = new vec3_t[3];

		public const int nanmask = (255 << 23);

		//#define IS_NAN(x) (((*(int *)&x)&nanmask)==nanmask)

		//#define SQRTFAST( x ) ( (x) * Q_rsqrt( x ) )

		//#define DotProduct(x,y)			((x)[0]*(y)[0]+(x)[1]*(y)[1]+(x)[2]*(y)[2])
		//#define VectorSubtract(a,b,c)	((c)[0]=(a)[0]-(b)[0],(c)[1]=(a)[1]-(b)[1],(c)[2]=(a)[2]-(b)[2])
		//#define VectorAdd(a,b,c)		((c)[0]=(a)[0]+(b)[0],(c)[1]=(a)[1]+(b)[1],(c)[2]=(a)[2]+(b)[2])
		//#define VectorCopy(a,b)			((b)[0]=(a)[0],(b)[1]=(a)[1],(b)[2]=(a)[2])
		//#define VectorScale(v, s, o)	((o)[0]=(v)[0]*(s),(o)[1]=(v)[1]*(s),(o)[2]=(v)[2]*(s))
		//#define VectorMA(v, s, b, o)	((o)[0]=(v)[0]+(b)[0]*(s),(o)[1]=(v)[1]+(b)[1]*(s),(o)[2]=(v)[2]+(b)[2]*(s))

		//#define VectorClear(a)			((a)[0]=(a)[1]=(a)[2]=0)
		//#define VectorNegate(a,b)		((b)[0]=-(a)[0],(b)[1]=-(a)[1],(b)[2]=-(a)[2])
		//#define VectorSet(v, x, y, z)	((v)[0]=(x), (v)[1]=(y), (v)[2]=(z))
		//#define Vector4Copy(a,b)		((b)[0]=(a)[0],(b)[1]=(a)[1],(b)[2]=(a)[2],(b)[3]=(a)[3])

		//#define SnapVector(v) {v[0]=((int)(v[0]));v[1]=((int)(v[1]));v[2]=((int)(v[2]));}

		static int VectorCompare( vec3_t v1, vec3_t v2 )
		{
			if ( v1.x != v2.x|| v1.y != v2.y || v1.z != v2.z )
			{
				return 0;
			}
			return 1;
		}

		static float VectorLength( vec3_t v )
		{
			return ( float ) Math.Sqrt( v.x* v.x + v.y * v.y + v.z * v.z );
		}

		static float VectorLengthSquared( vec3_t v )
		{
			return ( v.x * v.x + v.y * v.y + v.z * v.z );
		}

		static float Distance( vec3_t p1, vec3_t p2 )
		{
			vec3_t v;

			VectorSubtract( p2, p1, v );
			return VectorLength( v );
		}

		static float DistanceSquared( vec3_t p1, vec3_t p2 )
		{
			vec3_t v;

			VectorSubtract( p2, p1, v );
			return v.x * v.x + v.y * v.y + v.z * v.z;
		}

		// fast vector normalize routine that does not check to make sure
		// that length != 0, nor does it return length, uses rsqrt approximation
		static void VectorNormalizeFast( vec3_t v )
		{
			float ilength;

			ilength = Q_rsqrt( DotProduct( v, v ) );

			v.x *= ilength;
			v.y *= ilength;
			v.z *= ilength;
		}

		static void VectorInverse( vec3_t v )
		{
			v.x = -v.x;
			v.y = -v.y;
			v.z = -v.z;
		}

		static void CrossProduct( vec3_t v1, vec3_t v2, vec3_t cross )
		{
			cross.x = v1.x * v2.z - v1.z * v2.y;
			cross.y = v1.z * v2.x - v1.x * v2.z;
			cross.z = v1.x * v2.y - v1.y * v2.x;
		}

		static Random rand = new Random();
		public static int  random()
		{
			return rand.Next();
		}
		public static Double  crandom()
		{
			return rand.NextDouble();
		}
		//#define random()	((rand () & 0x7fff) / ((float)0x7fff))
		//#define crandom()	(2.0 * (random() - 0.5))

		//int		COM_ParseInfos( char *buf, int max, char infos[][MAX_INFO_STRING] );

		public const int MAX_TOKENLENGTH = 1024;

		//token types
		public const int TT_STRING = 1;         // string
		public const int TT_LITERAL = 2;     // literal
		public const int TT_NUMBER = 3;     // number
		public const int TT_NAME = 4;    // name
		public const int TT_PUNCTUATION = 5;		// punctuation

		public struct pc_token_t
		{
			int type;
			int subtype;
			int intvalue;
			float floatvalue;
			string @string;//[MAX_TOKENLENGTH];
		}

		// mode parm for FS_FOpenFile
		public enum fsMode_t
		{
			FS_READ,
			FS_WRITE,
			FS_APPEND,
			FS_APPEND_SYNC
		}

		public enum fsOrigin_t
		{
			FS_SEEK_CUR,
			FS_SEEK_END,
			FS_SEEK_SET
		}

		//=============================================

		// 64-bit integers for global rankings interface
		// implemented as a struct for qvm compatibility
		public struct qint64
		{
			public byte b0;
			public byte b1;
			public byte b2;
			public byte b3;
			public byte b4;
			public byte b5;
			public byte b6;
			public byte b7;
		}

		/*
		==========================================================

		CVARS (console variables)

		Many variables can be used for cheating purposes, so when
		cheats is zero, force all unspecified variables to their
		default values.
		==========================================================
		*/
		

		public const int MAX_CVAR_VALUE_STRING = 256;

		//typedef int cvarHandle_t;


		/*
		==============================================================

		COLLISION DETECTION

		==============================================================
		*/

		// plane types are used to speed some tests
		// 0-2 are axial planes
		public const int PLANE_X = 0;
		public const int PLANE_Y = 1;
		public const int PLANE_Z = 2;
		public const int PLANE_NON_AXIAL = 3;


		/*
		=================
		PlaneTypeForNormal
		=================
		*/

		public static int PlaneTypeForNormal( vec3_t vec ) 
		{
			return vec.x == 1.0 ? PLANE_X : ( vec.y == 1.0 ? PLANE_Y : ( vec.z == 1.0 ? PLANE_Z : PLANE_NON_AXIAL ) );
		}

		// plane_t structure
		// !!! if this is changed, it must be changed in asm code too !!!
		public struct cplane_t
		{
			vec3_t normal;
			float dist;
			byte type;          // for fast side tests: 0,1,2 = axial, 3 = nonaxial
			byte signbits;      // signx + (signy<<1) + (signz<<2), used as lookup during collision
			byte pad1;
			byte pad2;
		}

		// a trace is returned when a box is swept through the world
		public struct trace_t
		{
			bool allsolid;  // if true, plane is not valid
			bool startsolid;    // if true, the initial point was in a solid area
			float fraction; // time completed, 1.0 = didn't hit anything
			vec3_t endpos;      // final position
			cplane_t plane;     // surface normal at impact, transformed to world space
			int surfaceFlags;   // surface hit
			int contents;   // contents on other side of surface hit
			int entityNum;	// entity the contacted sirface is a part of
		}

		// trace->entityNum can also be 0 to (MAX_GENTITIES-1)
		// or ENTITYNUM_NONE, ENTITYNUM_WORLD


		// markfragments are returned by CM_MarkFragments()
		public struct markFragment_t
		{
			int firstPoint;
			int numPoints;
		} ;

		public struct orientation_t
		{
			vec3_t origin;
			vec3_t axis;
		}

		//=====================================================================


		// in order from highest priority to lowest
		// if none of the catchers are active, bound key strings will be executed
		public const int KEYCATCH_CONSOLE = 0x0001;
		public const int KEYCATCH_UI = 0x0002;
		public const int KEYCATCH_MESSAGE = 0x0004;
		public const int KEYCATCH_CGAME = 0x0008;


		// sound channels
		// channel 0 never willingly overrides
		// other channels will allways override a playing sound on that channel
		public enum soundChannel_t 
		{
			CHAN_AUTO,
			CHAN_LOCAL,     // menu sounds, etc
			CHAN_WEAPON,
			CHAN_VOICE,
			CHAN_ITEM,
			CHAN_BODY,
			CHAN_LOCAL_SOUND,   // chat messages, etc
			CHAN_ANNOUNCER      // announcer voices, etc
		}

		/*
		========================================================================

		  ELEMENTS COMMUNICATED ACROSS THE NET

		========================================================================
		*/

		public static int ANGLE2SHORT( double x) 
		{			
			return ( int ) ( ( x ) * 65536 / 360 ) & 65535;
		}

		public static double SHORT2ANGLE(int x) 
		{
			return ( ( x ) * ( 360.0 / 65536 ) );
		}

		public const int SNAPFLAG_RATE_DELAYED = 1;
		public const int SNAPFLAG_NOT_ACTIVE = 2 ;  // snapshot used during connection and for zombies
		public const int SNAPFLAG_SERVERCOUNT = 4;	// toggled every map_restart so transitions can be detected

		//
		// per-level limits
		//
		public const int MAX_CLIENTS = 64;   // absolute limit
		public const int MAX_LOCATIONS = 64;

		public const int GENTITYNUM_BITS = 10;     // don't need to send any more
		public const int MAX_GENTITIES = ( 1 << GENTITYNUM_BITS );

		// entitynums are communicated with GENTITY_BITS, so any reserved
		// values that are going to be communcated over the net need to
		// also be in this range
		public const int ENTITYNUM_NONE = ( MAX_GENTITIES-1);
		public const int ENTITYNUM_WORLD = ( MAX_GENTITIES - 2 );
		public const int ENTITYNUM_MAX_NORMAL = ( MAX_GENTITIES - 2 );


		public const int MAX_MODELS = 256;   // these are sent over the net as 8 bits
		public const int MAX_SOUNDS = 256;   // so they cannot be blindly increased


		public const int MAX_CONFIGSTRINGS = 1024;

		// these are the only configstrings that the system reserves, all the
		// other ones are strictly for servergame to clientgame communication
		public const int CS_SERVERINFO = 0 ;      // an info string with all the serverinfo cvars
		public const int CS_SYSTEMINFO = 1;       // an info string for server system to client system configuration (timescale, etc)

		public const int RESERVED_CONFIGSTRINGS = 2;  // game can't modify below this, only the system can

		public const int MAX_GAMESTATE_CHARS = 16000;
		public struct gameState_t
		{
			[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_CONFIGSTRINGS )]
			int[] stringOffsets;

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_GAMESTATE_CHARS )]
			char[] stringData;

			int dataCount;
		}

		//=========================================================

		// bit field limits
		public const int MAX_STATS = 16;
		public const int MAX_PERSISTANT = 16;
		public const int MAX_POWERUPS = 16;
		public const int MAX_WEAPONS = 16;

		public const int MAX_PS_EVENTS = 2;

		public const int PS_PMOVEFRAMECOUNTBITS = 6;

		// playerState_t is the information needed by both the client and server
		// to predict player motion and actions
		// nothing outside of pmove should modify these, or some degree of prediction error
		// will occur

		// you can't add anything to this without modifying the code in msg.c

		// playerState_t is a full superset of entityState_t as it is used by players,
		// so if a playerState_t is transmitted, the entityState_t can be fully derived
		// from it.
		public struct playerState_t
		{
			int commandTime;    // cmd->serverTime of last executed command
			int pm_type;
			int bobCycle;       // for view bobbing and footstep generation
			int pm_flags;       // ducked, jump_held, etc
			int pm_time;

			vec3_t origin;
			vec3_t velocity;
			int weaponTime;
			int gravity;
			int speed;

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
			int[] delta_angles;    // add to command angles to get view direction
									// changed by spawns, rotating objects, and teleporters

			int groundEntityNum;// ENTITYNUM_NONE = in air

			int legsTimer;      // don't change low priority animations until this runs out
			int legsAnim;       // mask off ANIM_TOGGLEBIT

			int torsoTimer;     // don't change low priority animations until this runs out
			int torsoAnim;      // mask off ANIM_TOGGLEBIT

			int movementDir;    // a number 0 to 7 that represents the reletive angle
								// of movement to the view angle (axial and diagonals)
								// when at rest, the value will remain unchanged
								// used to twist the legs during strafing

			vec3_t grapplePoint;    // location of grapple to pull towards if PMF_GRAPPLE_PULL

			int eFlags;         // copied to entityState_t->eFlags

			int eventSequence;  // pmove generated events

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_PS_EVENTS )]
			int[] events;

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_PS_EVENTS )]
			int[] eventParms;

			int externalEvent;  // events set on player from another source
			int externalEventParm;
			int externalEventTime;

			int clientNum;      // ranges from 0 to MAX_CLIENTS-1
			int weapon;         // copied to entityState_t->weapon
			int weaponstate;

			vec3_t viewangles;      // for fixed views
			int viewheight;

			// damage feedback
			int damageEvent;    // when it changes, latch the other parms
			int damageYaw;
			int damagePitch;
			int damageCount;

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_STATS )]
			int[] stats;

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_PERSISTANT )]
			int[] persistant; // stats that aren't cleared on death

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_POWERUPS )]
			int[] powerups; // level.time that the powerup runs out

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_WEAPONS )]
			int[] ammo;

			int generic1;
			int loopSound;
			int jumppad_ent;    // jumppad entity hit this frame

			// not communicated over the net at all
			int ping;           // server to game info for scoreboard
			int pmove_framecount;   // FIXME: don't transmit over the network
			int jumppad_frame;
			int entityEventSequence;
		}


		//====================================================================


		//
		// usercmd_t->button bits, many of which are generated by the client system,
		// so they aren't game/cgame only definitions
		//
		public const int BUTTON_ATTACK = 1;
		public const int BUTTON_TALK = 2;         // displays talk balloon and disables actions
		public const int BUTTON_USE_HOLDABLE = 4;
		public const int BUTTON_GESTURE = 8;
		public const int BUTTON_WALKING = 16;       // walking can't just be infered from MOVE_RUN
													// because a key pressed late in the frame will
													// only generate a small move value for that frame
													// walking will use different animations and
													// won't generate footsteps
		public const int BUTTON_AFFIRMATIVE = 32;
		public const int BUTTON_NEGATIVE = 64;

		public const int BUTTON_GETFLAG = 128;
		public const int BUTTON_GUARDBASE = 256;
		public const int BUTTON_PATROL = 512;
		public const int BUTTON_FOLLOWME = 1024;

		public const int BUTTON_ANY = 2048;     // any key whatsoever

		public const int MOVE_RUN = 120;	// if forwardmove or rightmove are >= MOVE_RUN,
		// then BUTTON_WALKING should be set

		// usercmd_t is sent to the server each client frame
		public struct usercmd_t
		{
			int serverTime;
			[MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
			int[] angles;
			int buttons;
			byte weapon;           // weapon 
			sbyte forwardmove, rightmove, upmove;
		}

		//===================================================================

		// if entityState->solid == SOLID_BMODEL, modelindex is an inline model number
		public const int SOLID_BMODEL = 0xffffff;

		public enum trType_t
		{
			TR_STATIONARY,
			TR_INTERPOLATE,             // non-parametric, but interpolate between snapshots
			TR_LINEAR,
			TR_LINEAR_STOP,
			TR_SINE,                    // value = base + sin( time / duration ) * delta
			TR_GRAVITY
		}

		public struct trajectory_t
		{
			trType_t trType;
			int trTime;
			int trDuration;         // if non 0, trTime + trDuration = stop time
			vec3_t trBase;
			vec3_t trDelta;			// velocity, etc
		}

		// entityState_t is the information conveyed from the server
		// in an update message about entities that the client will
		// need to render in some way
		// Different eTypes may use the information in different ways
		// The messages are delta compressed, so it doesn't really matter if
		// the structure size is fairly large

		public struct entityState_t
		{
			int number;         // entity index
			int eType;          // entityType_t
			int eFlags;

			trajectory_t pos;   // for calculating position
			trajectory_t apos;  // for calculating angles

			int time;
			int time2;

			vec3_t origin;
			vec3_t origin2;

			vec3_t angles;
			vec3_t angles2;

			int otherEntityNum; // shotgun sources, etc
			int otherEntityNum2;

			int groundEntityNum;    // -1 = in air

			int constantLight;  // r + (g<<8) + (b<<16) + (intensity<<24)
			int loopSound;      // constantly loop this sound

			int modelindex;
			int modelindex2;
			int clientNum;      // 0 to (MAX_CLIENTS - 1), for players and corpses
			int frame;

			int solid;          // for client side prediction, trap_linkentity sets this properly

			int @event;          // impulse events -- muzzle flashes, footsteps, etc
			int eventParm;

			// for players
			int powerups;       // bit flags
			int weapon;         // determines weapon and flash model, etc
			int legsAnim;       // mask off ANIM_TOGGLEBIT
			int torsoAnim;      // mask off ANIM_TOGGLEBIT

			int generic1;
		}

		public enum connstate_t
		{
			CA_UNINITIALIZED,
			CA_DISCONNECTED,    // not talking to a server
			CA_AUTHORIZING,     // not used any more, was checking cd key 
			CA_CONNECTING,      // sending request packets to the server
			CA_CHALLENGING,     // sending challenge packets to the server
			CA_CONNECTED,       // netchan_t established, getting gamestate
			CA_LOADING,         // only during cgame initialization, never during main loop
			CA_PRIMED,          // got gamestate, waiting for first frame
			CA_ACTIVE,          // game views should be displayed
			CA_CINEMATIC        // playing a cinematic or a static pic, not connected to a server
		}

	// font support 

		public const int GLYPH_START = 0;
		public const int GLYPH_END = 255;
		public const int GLYPH_CHARSTART = 32;
		public const int GLYPH_CHAREND = 127;
		public const int GLYPHS_PER_FONT = GLYPH_END - GLYPH_START + 1;

		public struct glyphInfo_t
		{
			int height;       // number of scan lines
			int top;          // top of glyph in buffer
			int bottom;       // bottom of glyph in buffer
			int pitch;        // width for copying
			int xSkip;        // x adjustment
			int imageWidth;   // width of actual image
			int imageHeight;  // height of actual image
			float s;          // x offset in image where glyph starts
			float t;          // y offset in image where glyph starts
			float s2;
			float t2;
			int glyph;  // handle to the shader with the glyph

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = 32 )]
			char[] shaderName;
		}

		public struct fontInfo_t
		{
			[MarshalAs( UnmanagedType.ByValArray, SizeConst = GLYPHS_PER_FONT )]
			glyphInfo_t[] glyphs;
			float glyphScale;

			[MarshalAs( UnmanagedType.ByValArray, SizeConst = q_shared.MAX_QPATH )]
			char[] name;
		}

		public static int Square( int x) 
		{ 
			return ( ( x ) * ( x ) );
		}

		public static float Square( float x )
		{
			return ( ( x ) * ( x ) );
		}

		public static double Square( double x )
		{
			return ( ( x ) * ( x ) );
		}

		// real time
		//=============================================


		public struct qtime_t
		{
			int tm_sec;     /* seconds after the minute - [0,59] */
			int tm_min;     /* minutes after the hour - [0,59] */
			int tm_hour;    /* hours since midnight - [0,23] */
			int tm_mday;    /* day of the month - [1,31] */
			int tm_mon;     /* months since January - [0,11] */
			int tm_year;    /* years since 1900 */
			int tm_wday;    /* days since Sunday - [0,6] */
			int tm_yday;    /* days since January 1 - [0,365] */
			int tm_isdst;   /* daylight savings time flag */
		}


		// server browser sources
		// TTimo: AS_MPLAYER is no longer used
		public const int AS_LOCAL = 0;
		public const int AS_MPLAYER = 1;
		public const int AS_GLOBAL = 2;
		public const int AS_FAVORITES = 3;

		// cinematic states
		public enum e_status
		{
			FMV_IDLE,
			FMV_PLAY,       // play
			FMV_EOF,        // all other conditions, i.e. stop/EOF/abort
			FMV_ID_BLT,
			FMV_ID_IDLE,
			FMV_LOOPED,
			FMV_ID_WAIT
		}

		public enum flagStatus_t
		{
			FLAG_ATBASE = 0,
			FLAG_TAKEN,         // CTF
			FLAG_TAKEN_RED,     // One Flag CTF
			FLAG_TAKEN_BLUE,    // One Flag CTF
			FLAG_DROPPED
		}

		public const int MAX_GLOBAL_SERVERS = 4096;
		public const int MAX_OTHER_SERVERS = 128;
		public const int MAX_PINGREQUESTS = 32;
		public const int MAX_SERVERSTATUSREQUESTS = 16;

		public const int SAY_ALL = 0;
		public const int SAY_TEAM = 1;
		public const int SAY_TELL = 2;

		public static float Com_Clamp( float min, float max, float value )
		{
			if ( value < min ) {
				return min;
			}
			if ( value > max ) {
				return max;
			}
			return value;
		}


		/*
		============
		COM_SkipPath
		============
		*/
		public static string COM_SkipPath( string pathname )
		{
			var pathnameIndex = 0;
			string last;
		
			last = pathname;
			while ( pathnameIndex < pathname.Length)
			{
				if (pathname[pathnameIndex]== '/')
					last = pathname.Substring(pathnameIndex + 1);
				pathnameIndex++;
			}
			return last;
		}

		/*
		============
		COM_StripExtension
		============
		*/
		public static void COM_StripExtension( string file, out string result ) 
		{
			result = Path.GetFileNameWithoutExtension( file );
		}

		/*
		==================
		COM_DefaultExtension
		==================
		*/
		public static void COM_DefaultExtension( string path, int maxSize, string extension ) 
		{
			//
			// if path doesn't have a .EXT, append extension
			// (extension should include the .)
			//
			if ( !System.IO.Path.HasExtension( path ) )
				path += extension;
		}

		/*
		============================================================================

							BYTE ORDER FUNCTIONS

		============================================================================
		*/
		/*
		// can't just use function pointers, or dll linkage can
		// mess up when qcommon is included in multiple places
		static short	(*_BigShort) (short l);
		static short	(*_LittleShort) (short l);
		static int		(*_BigLong) (int l);
		static int		(*_LittleLong) (int l);
		static qint64	(*_BigLong64) (qint64 l);
		static qint64	(*_LittleLong64) (qint64 l);
		static float	(*_BigFloat) (const float *l);
		static float	(*_LittleFloat) (const float *l);

		short	BigShort(short l){return _BigShort(l);}
		short	LittleShort(short l) {return _LittleShort(l);}
		int		BigLong (int l) {return _BigLong(l);}
		int		LittleLong (int l) {return _LittleLong(l);}
		qint64 	BigLong64 (qint64 l) {return _BigLong64(l);}
		qint64 	LittleLong64 (qint64 l) {return _LittleLong64(l);}
		float	BigFloat (const float *l) {return _BigFloat(l);}
		float	LittleFloat (const float *l) {return _LittleFloat(l);}
		*/

		public static short ShortSwap (short l)
		{
			byte    b1,b2;

			b1 = ( byte ) ( l & 255 );
			b2 = ( byte ) ( ( l >> 8 ) & 255 );

			return ( byte ) ( ( b1 << 8 ) + b2 );
		}

		public static short	ShortNoSwap (short l)
		{
			return l;
		}

		public static int LongSwap (int l)
		{
			var b1 = ( byte ) ( l & 255 );
			var b2 = ( byte ) ( ( l >> 8 ) & 255 );
			var b3 = ( byte ) ( ( l >> 16 ) & 255 );
			var b4 = ( byte ) ( ( l >> 24 ) & 255 );

			return ((int)b1<<24) + ((int)b2<<16) + ((int)b3<<8) + b4;
		}

		public static int	LongNoSwap (int l)
		{
			return l;
		}

		public static qint64 Long64Swap (qint64 ll)
		{
			qint64	result;

			result.b0 = ll.b7;
			result.b1 = ll.b6;
			result.b2 = ll.b5;
			result.b3 = ll.b4;
			result.b4 = ll.b3;
			result.b5 = ll.b2;
			result.b6 = ll.b1;
			result.b7 = ll.b0;

			return result;
		}

		public static qint64 Long64NoSwap (qint64 ll)
		{
			return ll;
		}

		public struct _FloatByteUnion
		{
			public float	f;
			public uint i;
		}

		public static float FloatSwap( ref float f) {
			_FloatByteUnion @in;
			_FloatByteUnion @out;

			@in = (_FloatByteUnion )f;
			@out.i = LongSwap(@in.i);

			return @out.f;
		}

		public static float FloatNoSwap (ref float f)
		{
			return f;
		}

		/*
		================
		Swap_Init
		================
		*/
		/*
		void Swap_Init (void)
		{
			byte	swaptest[2] = {1,0};

		// set the byte swapping variables in a portable manner	
			if ( *(short *)swaptest == 1)
			{
				_BigShort = ShortSwap;
				_LittleShort = ShortNoSwap;
				_BigLong = LongSwap;
				_LittleLong = LongNoSwap;
				_BigLong64 = Long64Swap;
				_LittleLong64 = Long64NoSwap;
				_BigFloat = FloatSwap;
				_LittleFloat = FloatNoSwap;
			}
			else
			{
				_BigShort = ShortNoSwap;
				_LittleShort = ShortSwap;
				_BigLong = LongNoSwap;
				_LittleLong = LongSwap;
				_BigLong64 = Long64NoSwap;
				_LittleLong64 = Long64Swap;
				_BigFloat = FloatNoSwap;
				_LittleFloat = FloatSwap;
			}

		}
		*/

		/*
		============================================================================

		PARSING

		============================================================================
		*/

		private static char[] com_token = new char[MAX_TOKEN_CHARS];
		private static char[] com_parsename = new char[MAX_TOKEN_CHARS];
		private static	int		com_lines;

		public static void COM_BeginParseSession( string name )
		{
			com_lines = 0;
			Com_sprintf(com_parsename, sizeof(com_parsename), "%s", name);
		}

		public static int COM_GetCurrentParseLine( )
		{
			return com_lines;
		}

		public static string COM_Parse( ref string data_p )
		{
			return COM_ParseExt( ref data_p, true );
		}

		public static void COM_ParseError( string format, params object[] parameters )
		{
			var str = StringFormatter.PrintF( format, parameters );

			Com_Printf("ERROR: %s, line %d: %s\n", com_parsename, com_lines, str );
		}

		public static void COM_ParseWarning( string format, params object[] parameters )
		{
			var str = StringFormatter.PrintF( format, parameters );

			Com_Printf("WARNING: %s, line %d: %s\n", com_parsename, com_lines, str );
		}

		/*
		==============
		COM_Parse

		Parse a token out of a string
		Will never return NULL, just empty strings

		If "allowLineBreaks" is true then an empty
		string will be returned if the next token is
		a newline.
		==============
		*/
		public static string SkipWhitespace( string data, out bool hasNewLines )
		{
			int c;
			hasNewLines = false;
			var dataI = 0;

			while ( (c = data[dataI]) <= ' ') 
			{
				if ( dataI >= data.Length ) 
				{
					return null;
				}
				if ( c == '\n' ) {
					com_lines++;
					hasNewLines = true;
				}
				dataI++;
			}

			return data;
		}

		public static void COM_Compress( ref string data_p ) {
			char[] @in, @out;
			int inI = 0, outI = 0;
			int c;
			bool newline = false, whitespace = false;

			@in = @out = data_p.ToCharArray();
			if ( @in != null ) {
				while ( ( c = @in[inI]) != 0) {
					// skip double slash comments
					if ( c == '/' && @in[1] == '/' ) {
						while (inI < @in.Length && @in[inI] != '\n') {
							inI++;
						}
					// skip /* */ comments
					} else if ( c == '/' && @in[1] == '*' ) {
						while ( inI < @in.Length && ( @in[inI] != '*' || @in[1] != '/' ) )
							inI++;
						if ( inI < @in.Length )
							inI += 2;
								// record when we hit a newline
								} else if ( c == '\n' || c == '\r' ) {
									newline = true;
									inI++;
								// record when we hit whitespace
								} else if ( c == ' ' || c == '\t') {
									whitespace = true;
									inI++;
								// an actual token
					} else {
									// if we have a pending newline, emit it (and it counts as whitespace)
									if (newline) {
										outI++;
										@out[outI] = '\n';
										newline = false;
										whitespace = false;
									} if (whitespace) {
										outI++;
										@out[outI] = ' ';
										whitespace = false;
									}
	                            
									// copy quoted strings unmolested
									if (c == '"') {							
											outI++;
											@out[outI] = (char)c;
											inI++;
											while (true) {
												c = @in[inI];
												if ( inI < @in.Length && c != '"') {												
													outI++;
													@out[outI] = (char)c;
													inI++;
												} else {
													break;
												}
											}
											if (c == '"') {															
												outI++;
												@out[outI] = (char)c;
												inI++;
											}
									} else {				
										@out[outI] = (char)c;
										outI++;
										inI++;
									}
					}
				}
			}

			@out[outI] = ( char ) 0;
			data_p = @out.ToString();
		}

		public static string COM_ParseExt( ref string data_p, bool allowLineBreaks )
		{
			int c = 0, len;
			bool hasNewLines = false;
			var dataI = 0;
			char[] data = data_p.ToCharArray();
			len = 0;
			com_token[0] = (char)0;

			// make sure incoming data is valid
			if ( data == null)
			{
				data_p = null;
				return com_token.ToString();
			}

			while ( true )
			{
				// skip whitespace
				data = SkipWhitespace( data.ToString(), out hasNewLines ).ToCharArray();
				if ( data == null )
				{
					data_p = null;
					return com_token.ToString();
				}
				if ( hasNewLines && !allowLineBreaks )
				{
					data_p = data.ToString();
					return com_token.ToString();
				}

				c = data[dataI];

				// skip double slash comments
				if ( c == '/' && data[1] == '/' )
				{
					dataI += 2;
					while ( dataI < data.Length && data[dataI] != '\n') {
						dataI++;
					}
				}
				// skip /* */ comments
				else if ( c=='/' && data[1] == '*' ) 
				{
					dataI += 2;
					while ( dataI < data.Length && ( data[dataI] != '*' || data[dataI + 1] != '/' ) ) 
					{
						dataI++;
					}
					if ( dataI < data.Length ) 
					{
						dataI += 2;
					}
				}
				else
				{
					break;
				}
			}

			// handle quoted strings
			if (c == '\"')
			{
				dataI++;
				while ( true )
				{
					dataI++;
					if ( dataI >= data.Length )
					{
						com_token[len] = (char) 0;
						data_p = data.ToString();
						return com_token.ToString();

					}
					c = data[dataI];
					if (c=='\"' || c == 0)
					{
						com_token[len] = (char)0;
						data_p = data.ToString();
						return com_token.ToString();
					}
					if (len < MAX_TOKEN_CHARS)
					{
						com_token[len] = (char)c;
						len++;
					}
				}
			}

			// parse a regular word
			do
			{
				if (len < MAX_TOKEN_CHARS)
				{
					com_token[len] = ( char ) c;
					len++;
				}
				dataI++;
				c = data[dataI];
				if ( c == '\n' )
					com_lines++;
			} while (c>32);

			if (len == MAX_TOKEN_CHARS)
			{
		//		Com_Printf ("Token exceeded %i chars, discarded.\n", MAX_TOKEN_CHARS);
				len = 0;
			}
			com_token[len] = ( char ) 0;

			data_p = data.ToString();
			return com_token.ToString();
		}




		/*
		==================
		COM_MatchToken
		==================
		*/
		public static void COM_MatchToken( ref string buf_p, string match ) {
			string token;

			token = COM_Parse( ref buf_p );
			if ( token.IndexOf( match ) == -1 ) {
				Com_Error( ERR_DROP, "MatchToken: %s != %s", token, match );
			}
		}


		/*
		=================
		SkipBracedSection

		The next token should be an open brace.
		Skips until a matching close brace is found.
		Internal brace depths are properly skipped.
		=================
		*/
		public static void SkipBracedSection (ref string program) {
			string token;
			int				depth;

			depth = 0;
			do {
				token = COM_ParseExt( ref program, true );
				if( token[1] == 0 ) {
					if( token[0] == '{' ) {
						depth++;
					}
					else if( token[0] == '}' ) {
						depth--;
					}
				}
			} while( depth > 0 && program != null );
		}

		/*
		=================
		SkipRestOfLine
		=================
		*/
		public static void SkipRestOfLine( ref string data ) {
			char[] p;
			int		c;
			var pI = 0;

			p = data.ToCharArray();
			while ( (c = p[pI++]) != 0 ) {
				if ( c == '\n' ) {
					com_lines++;
					break;
				}
			}

			data = p.ToString();
		}


		public static void Parse1DMatrix(ref string buf_p, int x, ref float[] m) {
			string token;
			int		i;

			COM_MatchToken( ref buf_p, "(" );

			for (i = 0 ; i < x ; i++) {
				token = COM_Parse(ref buf_p);
				float.TryParse( token, out m[i] );
			}

			COM_MatchToken( ref buf_p, ")" );
		}

		public static void Parse2DMatrix( ref string buf_p, int y, int x, float[] m) {
			int		i;

			COM_MatchToken( ref buf_p, "(" );

			for (i = 0 ; i < y ; i++) {
				Parse1DMatrix( ref buf_p, x, m + i * x);
			}

			COM_MatchToken( ref buf_p, ")" );
		}

		public static void Parse3DMatrix( ref string buf_p, int z, int y, int x, float[] m) 
		{
			int		i;

			COM_MatchToken( ref buf_p, "(" );

			for (i = 0 ; i < z ; i++) {
				Parse2DMatrix( ref buf_p, y, x, m + i * x*y );
			}

			COM_MatchToken( ref buf_p, ")" );
		}


		/*
		============================================================================

							LIBRARY REPLACEMENT FUNCTIONS

		============================================================================
		*/

		public static int Q_isprint( int c )
		{
			if ( c >= 0x20 && c <= 0x7E )
				return ( 1 );
			return ( 0 );
		}

		public static int Q_islower( int c )
		{
			if (c >= 'a' && c <= 'z')
				return ( 1 );
			return ( 0 );
		}

		public static int Q_isupper( int c )
		{
			if (c >= 'A' && c <= 'Z')
				return ( 1 );
			return ( 0 );
		}

		public static int Q_isalpha( int c )
		{
			if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
				return ( 1 );
			return ( 0 );
		}

		public static string Q_strrchr( string @string, int c )
		{
			char cc = (char)c;
			string s = @string;
			string sp = null;
			var sI = 0;

			while ( sI < @string.Length )
			{
				if (@string[sI] == cc)
					sp = s;
				sI++;
			}
			if (cc == 0)
				sp = s;

			return sp;
		}

		/*
		=============
		Q_strncpyz
	 
		Safe strncpy that ensures a trailing zero
		=============
		*/
		public static void Q_strncpyz( out string dest, string src, int destsize ) {
		  // bk001129 - also NULL dest
			if ( src == null ) {
				Com_Error( ERR_FATAL, "Q_strncpyz: NULL src" );
			}
			if ( destsize < 1 ) {
				Com_Error(ERR_FATAL,"Q_strncpyz: destsize < 1" ); 
			}

			dest = new string( destsize < src.Length ? src.Substring( destsize ) : src );
		}
	                 
		public static int Q_stricmpn (string s1, string s2, int n) 
		{
			int		c1, c2;
			int s1I = 0, s2I = 0;

			// bk001129 - moved in 1.17 fix not in id codebase
			if ( s1 == null ) {
				if ( s2 == null )
					return 0;
				else
					return -1;
			}
			else if ( s2== null )
				return 1;
		
			do {
				c1 = s1[s1I++];
				c2 = s2[s2I++];

				if (n - 1 == 0) {
					return 0;		// strings are equal until end point
				}
			
				if (c1 != c2) {
					if (c1 >= 'a' && c1 <= 'z') {
						c1 -= ('a' - 'A');
					}
					if (c2 >= 'a' && c2 <= 'z') {
						c2 -= ('a' - 'A');
					}
					if (c1 != c2) {
						return c1 < c2 ? -1 : 1;
					}
				}
			} while (c1 > 0);
		
			return 0;		// strings are equal
		}

		public static int Q_strncmp (string s1, string s2, int n) {
			int		c1, c2;
			int s1I = 0, s2I = 0;

			do {
				c1 = s1[s1I++];
				c2 = s2[s2I++];

				if (n - 1 == 0) {
					return 0;		// strings are equal until end point
				}
			
				if (c1 != c2) {
					return c1 < c2 ? -1 : 1;
				}
			} while (c1 > 0);
		
			return 0;		// strings are equal
		}

		public static int Q_stricmp (string s1, string s2) {
			return (s1 != null && s2 != null ) ? Q_stricmpn (s1, s2, 99999) : -1;
		}


		public static string Q_strlwr( string s1 ) {
			return s1.ToLower();
		}

		public static string Q_strupr( string s1 ) {
			return s1.ToUpper();
		}

		// never goes past bounds or leaves without a terminating 0
		public static void Q_strcat( ref string dest, int size, string src ) {
			int		l1;

			l1 = (int) dest.Length;
			if ( l1 >= size ) {
				Com_Error( ERR_FATAL, "Q_strcat: already overflowed" );
			}
			Q_strncpyz( dest + l1, src, size - l1 );
		}


		public static int Q_PrintStrlen( string @string ) {
			int			len;
			string p;
			var pI = 0;

			if( @string == null ) {
				return 0;
			}

			len = 0;
			p = @string;
			while( pI < p.Length ) {
				if( q_shared.Q_IsColorString( p, pI ) ) {
					pI += 2;
					continue;
				}
				pI++;
				len++;
			}

			return len;
		}


		public static string Q_CleanStr( string @string ) 
		{
			char[] d;
			string	s;
			int		c;

			int sI = 0, dI = 0;
			s = @string;
			d = @string.ToCharArray();
			while (sI < s.Length && (c = s[sI]) != 0 ) {
				if ( Q_IsColorString( s, sI ) ) {
					sI++;
				}		
				else if ( c >= 0x20 && c <= 0x7E ) {
					d[dI++] = ( char ) c;
				}
				sI++;
			}

			d[dI] = '\0';

			return d.ToString();
		}


		public static void Com_sprintf( out string dest, int size, string fmt, params object[] parameters ) 
		{
			int		len;
			char[]	bigbuffer = new char[32000];    // big, but small enough to fit in PPC stack

			var output = StringFormatter.PrintF( fmt, parameters );

			if ( output.Length >= size ) 
			{
				common.Com_Printf( "Com_sprintf: overflow of %i in %i\n", len, size);
			}
			Q_strncpyz( out dest, output, size );
		}


		/*
		=====================================================================

		  INFO STRINGS

		=====================================================================
		*/

		/*
		===============
		Info_ValueForKey

		Searches the string for the given
		key and returns the associated value, or an empty string.
		FIXME: overflow check?
		===============
		*/
		static int valueindex = 0;

		public static string Info_ValueForKey( string s, string key ) {
			char[]	pkey = new char[BIG_INFO_KEY];
			char[][] value = new char[2][]; // use two buffers so compares
			value[0] = new char[BIG_INFO_VALUE];
			value[1] = new char[BIG_INFO_VALUE];

			// work without stomping on each other
			char[] o;

			int sI = 0;
			int oI = 0;
		
			if ( s == null || key == null ) {
				return "";
			}

			if ( s.Length >= BIG_INFO_STRING ) {
				Com_Error( ERR_DROP, "Info_ValueForKey: oversize infostring" );
			}

			valueindex ^= 1;

			if (s[sI] == '\\')
				sI++;

			while ( true )
			{
				o = pkey;
				while ( s[sI] != '\\')
				{
					if ( sI >= s.Length )
						return "";
					o[oI++] = s[sI++];
				}
				o[oI] = (char)0;
				sI++;

				o = value[valueindex];

				while ( sI < s.Length && s[sI] != '\\' )
				{
					o[oI++] = s[sI++];
				}
				o[oI] = (char)0;

				if (Q_stricmp( key, pkey.ToString() ) <= 0)
					return value[valueindex].ToString();

				if ( sI >= s.Length )
					break;
				sI++;
			}

			return "";
		}


		/*
		===================
		Info_NextPair

		Used to itterate through all the key/value pairs in an info string
		===================
		*/
		public static void Info_NextPair( ref string head, string key, string value ) {
			char[] o;
			char[] s;
			int sI = 0, oI = 0;
			s = head.ToCharArray();

			if ( s[sI] == '\\' ) {
				sI++;
			}
			//key[0] = 0;
			//value[0] = 0;

			o = key.ToCharArray();
			while ( sI < s.Length && s[sI] != '\\' ) {
				if ( sI >= s.Length ) {
					o[oI] = ( char ) 0;
					head = s.ToString();
					return;
				}
				o[oI++] = s[sI++];
			}
			o[oI] = (char)0;
			sI++;

			o = value.ToCharArray();
			while ( sI < s.Length && s[sI] != '\\' ) {
				o[oI++] = s[sI++];
			}
			o[oI] = (char)0;

			head = s.ToString();
		}


		/*
		===================
		Info_RemoveKey
		===================
		*/
		public static void Info_RemoveKey( string s, string key )
		{
			char[] start;
			char[] pkey = new char[MAX_INFO_KEY];
			char[] value = new char[MAX_INFO_VALUE];
			char[] o;
			int sI = 0, oI = 0;

			if ( s.Length >= MAX_INFO_STRING ) {
				Com_Error( ERR_DROP, "Info_RemoveKey: oversize infostring" );
			}

			if (strchr (key, '\\')) {
				return;
			}

			while ( true )
			{
				start = s.ToCharArray();
				sI = 0;

				if (s[sI] == '\\')
					sI++;
				o = pkey;
				oI = 0;
				while ( s[sI] != '\\')
				{
					if ( sI >= s.Length )
						return;
					o[oI++] = s[sI++];
				}
				o[oI] = (char)0;
				sI++;

				o = value;
				while ( sI < s.Length && s[sI] != '\\' )
				{
					if ( sI >= s.Length )
						return;
					o[oI++] = s[sI++];
				}
				o[oI] = ( char ) 0;

				if (Q_stricmp( key, pkey.ToString() ) <= 0 )
				{
					start = s.ToCharArray();	// remove this part
					return;
				}

				if ( sI >= s.Length )
					return;
			}
		}

		/*
		===================
		Info_RemoveKey_Big
		===================
		*/
		public static void Info_RemoveKey_Big( string s, string key ) {
			char[] start;
			char[] pkey = new char[MAX_INFO_KEY];
			char[] value = new char[MAX_INFO_VALUE];
			char[] o;
			int sI = 0, oI = 0;

			if ( s.Length >= BIG_INFO_STRING ) {
				Com_Error( ERR_DROP, "Info_RemoveKey_Big: oversize infostring" );
			}

			if (strchr (key, '\\')) {
				return;
			}

			while ( true )
			{
				start = s.ToCharArray();
				sI = 0;
				if (s[sI] == '\\')
					sI++;
				o = pkey;
				while ( s[sI] != '\\')
				{
					if ( sI >= s.Length )
						return;
					o[oI++] = s[sI++];
				}
				o[oI] = (char)0;
				sI++;

				o = value;
				while ( sI < s.Length && s[sI] != '\\' )
				{
					if ( sI >= s.Length )
						return;
					o[oI++] = s[sI++];
				}
				o[oI] = ( char ) 0;

				if ( Q_stricmp( key, pkey.ToString() ) <= 0 )
				{
					start = s.ToCharArray(); 	// remove this part
					return;
				}

				if ( sI >= s.Length )
					return;
			}

		}

		/*
		==================
		Info_Validate

		Some characters are illegal in info strings because they
		can mess up the server's parsing
		==================
		*/
		public static bool Info_Validate( string s ) 
		{
			if ( s.IndexOf( '\"' ) != -1 ) 
				return false;
			
			if ( s.IndexOf( ';' ) != -1 )
				return false;
			
			return true;
		}

		/*
		==================
		Info_SetValueForKey

		Changes or adds a key/value pair
		==================
		*/
		public static void Info_SetValueForKey( string s, string key, string value ) {
			string newi;//[MAX_INFO_STRING];

			if ( s.Length >= MAX_INFO_STRING ) {
				common.Com_Error( ERR_DROP, "Info_SetValueForKey: oversize infostring" );
			}

			if (strchr (key, '\\') || strchr (value, '\\'))
			{
				common.Com_Printf ("Can't use keys or values with a \\\n");
				return;
			}

			if (strchr (key, ';') || strchr (value, ';'))
			{
				common.Com_Printf ("Can't use keys or values with a semicolon\n");
				return;
			}

			if (strchr (key, '\"') || strchr (value, '\"'))
			{
				common.Com_Printf ("Can't use keys or values with a \"\n");
				return;
			}

			Info_RemoveKey (s, key);
			if (value == null || value.Length == 0)
				return;

			Com_sprintf( newi, sizeof(newi), "\\%s\\%s", key, value);

			if ( newi.Length + s.Length > MAX_INFO_STRING)
			{
				Com_Printf ("Info string length exceeded\n");
				return;
			}

			strcat( newi, s );
			strcpy( s, newi );
		}

		/*
		==================
		Info_SetValueForKey_Big

		Changes or adds a key/value pair
		==================
		*/
		public static void Info_SetValueForKey_Big( string s, string key, string value ) 
		{
			char[]	newi = new char[BIG_INFO_STRING];

			if ( s.Length >= BIG_INFO_STRING ) 
			{
				Com_Error( ERR_DROP, "Info_SetValueForKey: oversize infostring" );
			}

			if (key.IndexOf( '\\') != -1 || value.IndexOf( '\\' ) != -1 )
			{
				Com_Printf ("Can't use keys or values with a \\\n");
				return;
			}

			if (key.IndexOf( ';' ) != -1 || value.IndexOf( ';' ) != -1 )
			{
				common.Com_Printf ("Can't use keys or values with a semicolon\n");
				return;
			}

			if (key.IndexOf( '\"' ) != -1 || value.IndexOf( '\"' ) != -1 )
			{
				Com_Printf ("Can't use keys or values with a \"\n");
				return;
			}

			Info_RemoveKey_Big (s, key);
			if ( value == null || value.Length == 0 )
				return;

			Com_sprintf (newi, sizeof(newi), "\\%s\\%s", key, value);

			if ( newi.Length + s.Length > BIG_INFO_STRING)
			{
				Com_Printf ("BIG Info string length exceeded\n");
				return;
			}

			s += newi;
		}
	}


	// nothing outside the Cvar_*() functions should modify these fields!
	// Used more like a class
	public class cvar_t
	{
		public string name;
		public string @string;
		public string resetString;      // cvar_restart will reset to this value
		public string latchedString;        // for CVAR_LATCH vars
		public CVAR flags;
		public bool modified;          // set each time the cvar is changed
		public int modificationCount;  // incremented each time the cvar is changed
		public float value;                // atof( string )
		public int integer;            // atoi( string )
		public cvar_t next;
		public cvar_t hashNext;
	}

	[Flags]
	public enum CVAR
	{
		NONE = 0,
		ARCHIVE = 1,   // set to cause it to be saved to vars.rc
					   // used for system variables, not for player
					   // specific configurations
		USERINFO = 2,   // sent to server on connect or change
		SERVERINFO = 4,   // sent in response to front end requests
		SYSTEMINFO = 8,   // these cvars will be duplicated on all clients
		INIT = 16, // don't allow change from console at all,
				   // but can be set from the command line
		LATCH = 32,    // will only change when C code next does
					   // a Cvar_Get(), so it can't be changed
					   // without proper initialization.  modified
					   // will be set, even though the value hasn't
					   // changed yet

		ROM = 64,  // display only, cannot be set by user at all
		USER_CREATED = 128, // created by a set command
		TEMP = 256, // can be set even when cheats are disabled, but is not archived
		CHEAT = 512, // can not be changed if cheats are disabled
		NORESTART = 1024    // do not clear when a cvar_restart is issued
	}
}
