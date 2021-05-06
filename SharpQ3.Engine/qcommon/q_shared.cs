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

namespace SharpQ3.Engine
{
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

		typedef int intptr_t;

		private const string CPUSTRING = "generic";

		public static short BigShort( short l ) 
		{
			return q_shared.ShortSwap( l ); 
		}

		public static int BigLong( int l ) 
		{ 
			return q_shared.LongSwap( l ); 
		}

		public static float BigFloat(const float* l )
		{
			return FloatSwap( l ); 
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
		public const int MAX_OSPATH	 =		PATH_MAX;
		public const int MAX_OSPATH = 256;     // max length of a filesystem pathname

		public const int MAX_NAME_LENGTH = 32;      // max length of a client name

		public const int MAX_SAY_TEXT = 150;

		// paramters for command buffer stuffing
		public enum cbufExec_t
		{
			EXEC_NOW,           // don't return until completed, a VM should NEVER use this,
								// because some commands might cause the VM to be unloaded...
			EXEC_INSERT,        // insert at current position, but don't run yet
			EXEC_APPEND         // add to end of the command buffer (normal case)
		}

		//
		// these aren't needed by any of the VMs.  put in another header?
		//
		public const int MAX_MAP_AREA_BYTES = 32;	// bit vector of area visibility

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

		void* Hunk_Alloc( int size, ha_pref preference );

		#define Snd_Memset Com_Memset

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
		public static vec3_t bytedirs[NUMVERTEXNORMALS];

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
		#define ColorIndex(c)	( ( (c) - '0' ) & 7 )

		public const string S_COLOR_BLACK = "^0";
		public const string S_COLOR_RED = "^1";
		public const string S_COLOR_GREEN = "^2";
		public const string S_COLOR_YELLOW = "^3";
		public const string S_COLOR_BLUE = "^4";
		public const string S_COLOR_CYAN = "^5";
		public const string S_COLOR_MAGENTA = "^6";
		public const string S_COLOR_WHITE = "^7";

		public static vec4_t g_color_table[8];

		#define MAKERGB( v, r, g, b ) v[0]=r;v[1]=g;v[2]=b
		#define MAKERGBA( v, r, g, b, a ) v[0]=r;v[1]=g;v[2]=b;v[3]=a

		public static float DEG2RAD( float a ) 
		{
			return ( ( a ) * M_PI ) / 180.0F;
		}

		public static float RAD2DEG( float a )
		{
			return ( ( a ) * 180.0f ) / M_PI;
		}

		struct cplane_s;

		public static vec3_t vec3_origin;
		public static vec3_t axisDefault[3];

		public const int nanmask = (255 << 23);

		#define IS_NAN(x) (((*(int *)&x)&nanmask)==nanmask)

		#define SQRTFAST( x ) ( (x) * Q_rsqrt( x ) )

		#define DotProduct(x,y)			((x)[0]*(y)[0]+(x)[1]*(y)[1]+(x)[2]*(y)[2])
		#define VectorSubtract(a,b,c)	((c)[0]=(a)[0]-(b)[0],(c)[1]=(a)[1]-(b)[1],(c)[2]=(a)[2]-(b)[2])
		#define VectorAdd(a,b,c)		((c)[0]=(a)[0]+(b)[0],(c)[1]=(a)[1]+(b)[1],(c)[2]=(a)[2]+(b)[2])
		#define VectorCopy(a,b)			((b)[0]=(a)[0],(b)[1]=(a)[1],(b)[2]=(a)[2])
		#define VectorScale(v, s, o)	((o)[0]=(v)[0]*(s),(o)[1]=(v)[1]*(s),(o)[2]=(v)[2]*(s))
		#define VectorMA(v, s, b, o)	((o)[0]=(v)[0]+(b)[0]*(s),(o)[1]=(v)[1]+(b)[1]*(s),(o)[2]=(v)[2]+(b)[2]*(s))

		#define VectorClear(a)			((a)[0]=(a)[1]=(a)[2]=0)
		#define VectorNegate(a,b)		((b)[0]=-(a)[0],(b)[1]=-(a)[1],(b)[2]=-(a)[2])
		#define VectorSet(v, x, y, z)	((v)[0]=(x), (v)[1]=(y), (v)[2]=(z))
		#define Vector4Copy(a,b)		((b)[0]=(a)[0],(b)[1]=(a)[1],(b)[2]=(a)[2],(b)[3]=(a)[3])

		#define SnapVector(v) {v[0]=((int)(v[0]));v[1]=((int)(v[1]));v[2]=((int)(v[2]));}

		static int VectorCompare( const vec3_t v1, const vec3_t v2 )
		{
			if ( v1[0] != v2[0] || v1[1] != v2[1] || v1[2] != v2[2] )
			{
				return 0;
			}
			return 1;
		}

		static float VectorLength( const vec3_t v )
		{
			return ( float ) sqrt( v[0] * v[0] + v[1] * v[1] + v[2] * v[2] );
		}

		static float VectorLengthSquared( const vec3_t v )
		{
			return ( v[0] * v[0] + v[1] * v[1] + v[2] * v[2] );
		}

		static float Distance( const vec3_t p1, const vec3_t p2 )
		{
			vec3_t v;

			VectorSubtract( p2, p1, v );
			return VectorLength( v );
		}

		static float DistanceSquared( const vec3_t p1, const vec3_t p2 )
		{
			vec3_t v;

			VectorSubtract( p2, p1, v );
			return v[0] * v[0] + v[1] * v[1] + v[2] * v[2];
		}

		// fast vector normalize routine that does not check to make sure
		// that length != 0, nor does it return length, uses rsqrt approximation
		static void VectorNormalizeFast( vec3_t v )
		{
			float ilength;

			ilength = Q_rsqrt( DotProduct( v, v ) );

			v[0] *= ilength;
			v[1] *= ilength;
			v[2] *= ilength;
		}

		static void VectorInverse( vec3_t v )
		{
			v[0] = -v[0];
			v[1] = -v[1];
			v[2] = -v[2];
		}

		static void CrossProduct( const vec3_t v1, const vec3_t v2, vec3_t cross )
		{
			cross[0] = v1[1] * v2[2] - v1[2] * v2[1];
			cross[1] = v1[2] * v2[0] - v1[0] * v2[2];
			cross[2] = v1[0] * v2[1] - v1[1] * v2[0];
		}

		#define random()	((rand () & 0x7fff) / ((float)0x7fff))
		#define crandom()	(2.0 * (random() - 0.5))

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
			char string[MAX_TOKENLENGTH];
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

		public const int CVAR_ARCHIVE =	1;   // set to cause it to be saved to vars.rc
		// used for system variables, not for player
		// specific configurations
		public const int CVAR_USERINFO = 2;   // sent to server on connect or change
		public const int CVAR_SERVERINFO = 4;   // sent in response to front end requests
		public const int CVAR_SYSTEMINFO = 8;   // these cvars will be duplicated on all clients
		public const int CVAR_INIT = 16; // don't allow change from console at all,
		// but can be set from the command line
		public const int CVAR_LATCH = 32;    // will only change when C code next does
		// a Cvar_Get(), so it can't be changed
		// without proper initialization.  modified
		// will be set, even though the value hasn't
		// changed yet
		public const int CVAR_ROM = 64;  // display only, cannot be set by user at all
		public const int CVAR_USER_CREATED = 128; // created by a set command
		public const int CVAR_TEMP = 256; // can be set even when cheats are disabled, but is not archived
		public const int CVAR_CHEAT = 512; // can not be changed if cheats are disabled
		public const int CVAR_NORESTART = 1024;	// do not clear when a cvar_restart is issued

		// nothing outside the Cvar_*() functions should modify these fields!
		// Used more like a class
		public class cvar_t
		{
			string name;
			string @string;
			string resetString;      // cvar_restart will reset to this value
			string latchedString;        // for CVAR_LATCH vars
			int flags;
			bool modified;          // set each time the cvar is changed
			int modificationCount;  // incremented each time the cvar is changed
			float value;                // atof( string )
			int integer;            // atoi( string )
			cvar_t next;
			cvar_t hashNext;
		};

		public const int MAX_CVAR_VALUE_STRING = 256;

		typedef int cvarHandle_t;

		// the modules that run in the virtual machine can't access the cvar_t directly,
		// so they must ask for structured updates
		public struct vmCvar_t
		{
			cvarHandle_t handle;
			int modificationCount;
			float value;
			int integer;
			char string[MAX_CVAR_VALUE_STRING];
		}

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

		#define PlaneTypeForNormal(x) (x[0] == 1.0 ? PLANE_X : (x[1] == 1.0 ? PLANE_Y : (x[2] == 1.0 ? PLANE_Z : PLANE_NON_AXIAL) ) )

		// plane_t structure
		// !!! if this is changed, it must be changed in asm code too !!!
		public struct cplane_t
		{
			vec3_t normal;
			float dist;
			byte type;          // for fast side tests: 0,1,2 = axial, 3 = nonaxial
			byte signbits;      // signx + (signy<<1) + (signz<<2), used as lookup during collision
			byte pad[2];
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
			vec3_t axis[3];
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

		#define ANGLE2SHORT(x)	((int)((x)*65536/360) & 65535)
		#define SHORT2ANGLE(x)	((x)*(360.0/65536))

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
			int stringOffsets[MAX_CONFIGSTRINGS];
			char stringData[MAX_GAMESTATE_CHARS];
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
			int delta_angles[3];    // add to command angles to get view direction
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
			int events[MAX_PS_EVENTS];
			int eventParms[MAX_PS_EVENTS];

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

			int stats[MAX_STATS];
			int persistant[MAX_PERSISTANT]; // stats that aren't cleared on death
			int powerups[MAX_POWERUPS]; // level.time that the powerup runs out
			int ammo[MAX_WEAPONS];

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
			int angles[3];
			int buttons;
			byte weapon;           // weapon 
			signed char forwardmove, rightmove, upmove;
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

			int event;          // impulse events -- muzzle flashes, footsteps, etc
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
			qhandle_t glyph;  // handle to the shader with the glyph
			char shaderName[32];
		}

		public struct fontInfo_t
		{
			glyphInfo_t glyphs[GLYPHS_PER_FONT];
			float glyphScale;
			char name[MAX_QPATH];
		}

		#define Square(x) ((x)*(x))

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
		public static char *COM_SkipPath (char *pathname)
		{
			char	*last;
		
			last = pathname;
			while (*pathname)
			{
				if (*pathname=='/')
					last = pathname+1;
				pathname++;
			}
			return last;
		}

		/*
		============
		COM_StripExtension
		============
		*/
		public static void COM_StripExtension( const char *in, char *out ) {
			while ( *in && *in != '.' ) {
				*out++ = *in++;
			}
			*out = 0;
		}


		/*
		==================
		COM_DefaultExtension
		==================
		*/
		public static void COM_DefaultExtension (char *path, int maxSize, const char *extension ) {
			char	oldPath[MAX_QPATH];
			char    *src;

		//
		// if path doesn't have a .EXT, append extension
		// (extension should include the .)
		//
			src = path + (int)strlen(path) - 1;

			while (*src != '/' && src != path) {
				if ( *src == '.' ) {
					return;                 // it has an extension
				}
				src--;
			}

			Q_strncpyz( oldPath, path, sizeof( oldPath ) );
			Com_sprintf( path, maxSize, "%s%s", oldPath, extension );
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

		typedef union {
			float	f;
			unsigned int i;
		} _FloatByteUnion;

		public static float FloatSwap (const float *f) {
			const _FloatByteUnion *in;
			_FloatByteUnion out;

			in = (_FloatByteUnion *)f;
			out.i = LongSwap(in->i);

			return out.f;
		}

		public static FloatNoSwap (const float *f)
		{
			return *f;
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

		private static	char	com_token[MAX_TOKEN_CHARS];
		private static	char	com_parsename[MAX_TOKEN_CHARS];
		private static	int		com_lines;

		public static void COM_BeginParseSession( const char *name )
		{
			com_lines = 0;
			Com_sprintf(com_parsename, sizeof(com_parsename), "%s", name);
		}

		public static int COM_GetCurrentParseLine( )
		{
			return com_lines;
		}

		public static char *COM_Parse( char **data_p )
		{
			return COM_ParseExt( data_p, true );
		}

		public static void COM_ParseError( char *format, ... )
		{
			va_list argptr;
			static char string[4096];

			va_start (argptr, format);
			vsprintf (string, format, argptr);
			va_end (argptr);

			Com_Printf("ERROR: %s, line %d: %s\n", com_parsename, com_lines, string);
		}

		public static void COM_ParseWarning( char *format, ... )
		{
			va_list argptr;
			static char string[4096];

			va_start (argptr, format);
			vsprintf (string, format, argptr);
			va_end (argptr);

			Com_Printf("WARNING: %s, line %d: %s\n", com_parsename, com_lines, string);
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
		public static char *SkipWhitespace( char *data, bool *hasNewLines ) {
			int c;

			while( (c = *data) <= ' ') {
				if( !c ) {
					return NULL;
				}
				if( c == '\n' ) {
					com_lines++;
					*hasNewLines = true;
				}
				data++;
			}

			return data;
		}

		public static int COM_Compress( char *data_p ) {
			char *in, *out;
			int c;
			bool newline = false, whitespace = false;

			in = out = data_p;
			if (in) {
				while ((c = *in) != 0) {
					// skip double slash comments
					if ( c == '/' && in[1] == '/' ) {
						while (*in && *in != '\n') {
							in++;
						}
					// skip /* */ comments
					} else if ( c == '/' && in[1] == '*' ) {
						while ( *in && ( *in != '*' || in[1] != '/' ) ) 
							in++;
						if ( *in ) 
							in += 2;
								// record when we hit a newline
								} else if ( c == '\n' || c == '\r' ) {
									newline = true;
									in++;
								// record when we hit whitespace
								} else if ( c == ' ' || c == '\t') {
									whitespace = true;
									in++;
								// an actual token
					} else {
									// if we have a pending newline, emit it (and it counts as whitespace)
									if (newline) {
										*out++ = '\n';
										newline = false;
										whitespace = false;
									} if (whitespace) {
										*out++ = ' ';
										whitespace = false;
									}
	                            
									// copy quoted strings unmolested
									if (c == '"') {
											*out++ = c;
											in++;
											while (1) {
												c = *in;
												if (c && c != '"') {
													*out++ = c;
													in++;
												} else {
													break;
												}
											}
											if (c == '"') {
												*out++ = c;
												in++;
											}
									} else {
										*out = c;
										out++;
										in++;
									}
					}
				}
			}
			*out = 0;
			return out - data_p;
		}

		public static char *COM_ParseExt( char **data_p, bool allowLineBreaks )
		{
			int c = 0, len;
			bool hasNewLines = false;
			char *data;

			data = *data_p;
			len = 0;
			com_token[0] = 0;

			// make sure incoming data is valid
			if ( !data )
			{
				*data_p = NULL;
				return com_token;
			}

			while ( 1 )
			{
				// skip whitespace
				data = SkipWhitespace( data, &hasNewLines );
				if ( !data )
				{
					*data_p = NULL;
					return com_token;
				}
				if ( hasNewLines && !allowLineBreaks )
				{
					*data_p = data;
					return com_token;
				}

				c = *data;

				// skip double slash comments
				if ( c == '/' && data[1] == '/' )
				{
					data += 2;
					while (*data && *data != '\n') {
						data++;
					}
				}
				// skip /* */ comments
				else if ( c=='/' && data[1] == '*' ) 
				{
					data += 2;
					while ( *data && ( *data != '*' || data[1] != '/' ) ) 
					{
						data++;
					}
					if ( *data ) 
					{
						data += 2;
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
				data++;
				while (1)
				{
					c = *data++;
					if (c=='\"' || !c)
					{
						com_token[len] = 0;
						*data_p = ( char * ) data;
						return com_token;
					}
					if (len < MAX_TOKEN_CHARS)
					{
						com_token[len] = c;
						len++;
					}
				}
			}

			// parse a regular word
			do
			{
				if (len < MAX_TOKEN_CHARS)
				{
					com_token[len] = c;
					len++;
				}
				data++;
				c = *data;
				if ( c == '\n' )
					com_lines++;
			} while (c>32);

			if (len == MAX_TOKEN_CHARS)
			{
		//		Com_Printf ("Token exceeded %i chars, discarded.\n", MAX_TOKEN_CHARS);
				len = 0;
			}
			com_token[len] = 0;

			*data_p = ( char * ) data;
			return com_token;
		}




		/*
		==================
		COM_MatchToken
		==================
		*/
		public static void COM_MatchToken( char **buf_p, char *match ) {
			char	*token;

			token = COM_Parse( buf_p );
			if ( strcmp( token, match ) ) {
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
		public static void SkipBracedSection (char **program) {
			char			*token;
			int				depth;

			depth = 0;
			do {
				token = COM_ParseExt( program, true );
				if( token[1] == 0 ) {
					if( token[0] == '{' ) {
						depth++;
					}
					else if( token[0] == '}' ) {
						depth--;
					}
				}
			} while( depth && *program );
		}

		/*
		=================
		SkipRestOfLine
		=================
		*/
		public static void SkipRestOfLine ( char **data ) {
			char	*p;
			int		c;

			p = *data;
			while ( (c = *p++) != 0 ) {
				if ( c == '\n' ) {
					com_lines++;
					break;
				}
			}

			*data = p;
		}


		public static void Parse1DMatrix (char **buf_p, int x, float *m) {
			char	*token;
			int		i;

			COM_MatchToken( buf_p, "(" );

			for (i = 0 ; i < x ; i++) {
				token = COM_Parse(buf_p);
				m[i] = atof(token);
			}

			COM_MatchToken( buf_p, ")" );
		}

		public static void Parse2DMatrix (char **buf_p, int y, int x, float *m) {
			int		i;

			COM_MatchToken( buf_p, "(" );

			for (i = 0 ; i < y ; i++) {
				Parse1DMatrix (buf_p, x, m + i * x);
			}

			COM_MatchToken( buf_p, ")" );
		}

		public static void Parse3DMatrix (char **buf_p, int z, int y, int x, float *m) {
			int		i;

			COM_MatchToken( buf_p, "(" );

			for (i = 0 ; i < z ; i++) {
				Parse2DMatrix (buf_p, y, x, m + i * x*y);
			}

			COM_MatchToken( buf_p, ")" );
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

		public static char* Q_strrchr( const char* string, int c )
		{
			char cc = c;
			char *s;
			char *sp=(char *)0;

			s = (char*)string;

			while (*s)
			{
				if (*s == cc)
					sp = s;
				s++;
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
		public static void Q_strncpyz( char *dest, const char *src, int destsize ) {
		  // bk001129 - also NULL dest
		  if ( !dest ) {
			Com_Error( ERR_FATAL, "Q_strncpyz: NULL dest" );
		  }
			if ( !src ) {
				Com_Error( ERR_FATAL, "Q_strncpyz: NULL src" );
			}
			if ( destsize < 1 ) {
				Com_Error(ERR_FATAL,"Q_strncpyz: destsize < 1" ); 
			}

			strncpy( dest, src, destsize-1 );
		  dest[destsize-1] = 0;
		}
	                 
		public static int Q_stricmpn (const char *s1, const char *s2, int n) {
			int		c1, c2;

			// bk001129 - moved in 1.17 fix not in id codebase
				if ( s1 == NULL ) {
				   if ( s2 == NULL )
					 return 0;
				   else
					 return -1;
				}
				else if ( s2==NULL )
				  return 1;


		
			do {
				c1 = *s1++;
				c2 = *s2++;

				if (!n--) {
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
			} while (c1);
		
			return 0;		// strings are equal
		}

		public static int Q_strncmp (const char *s1, const char *s2, int n) {
			int		c1, c2;
		
			do {
				c1 = *s1++;
				c2 = *s2++;

				if (!n--) {
					return 0;		// strings are equal until end point
				}
			
				if (c1 != c2) {
					return c1 < c2 ? -1 : 1;
				}
			} while (c1);
		
			return 0;		// strings are equal
		}

		public static int Q_stricmp (const char *s1, const char *s2) {
			return (s1 && s2) ? Q_stricmpn (s1, s2, 99999) : -1;
		}


		public static char *Q_strlwr( char *s1 ) {
			char	*s;

			s = s1;
			while ( *s ) {
				*s = tolower(*s);
				s++;
			}
			return s1;
		}

		public static char *Q_strupr( char *s1 ) {
			char	*s;

			s = s1;
			while ( *s ) {
				*s = toupper(*s);
				s++;
			}
			return s1;
		}


		// never goes past bounds or leaves without a terminating 0
		public static void Q_strcat( char *dest, int size, const char *src ) {
			int		l1;

			l1 = (int)strlen( dest );
			if ( l1 >= size ) {
				Com_Error( ERR_FATAL, "Q_strcat: already overflowed" );
			}
			Q_strncpyz( dest + l1, src, size - l1 );
		}


		public static int Q_PrintStrlen( const char *string ) {
			int			len;
			const char	*p;

			if( !string ) {
				return 0;
			}

			len = 0;
			p = string;
			while( *p ) {
				if( Q_IsColorString( p ) ) {
					p += 2;
					continue;
				}
				p++;
				len++;
			}

			return len;
		}


		public static char *Q_CleanStr( char *string ) {
			char*	d;
			char*	s;
			int		c;

			s = string;
			d = string;
			while ((c = *s) != 0 ) {
				if ( Q_IsColorString( s ) ) {
					s++;
				}		
				else if ( c >= 0x20 && c <= 0x7E ) {
					*d++ = c;
				}
				s++;
			}
			*d = '\0';

			return string;
		}


		public static void Com_sprintf( char *dest, int size, const char *fmt, ...) {
			int		len;
			va_list		argptr;
			char	bigbuffer[32000];	// big, but small enough to fit in PPC stack

			va_start (argptr,fmt);
			len = vsprintf (bigbuffer,fmt,argptr);
			va_end (argptr);
			if ( len >= sizeof( bigbuffer ) ) {
				Com_Error( ERR_FATAL, "Com_sprintf: overflowed bigbuffer" );
			}
			if (len >= size) {
				Com_Printf ("Com_sprintf: overflow of %i in %i\n", len, size);
			}
			Q_strncpyz (dest, bigbuffer, size );
		}


		/*
		============
		va

		does a varargs printf into a temp buffer, so I don't need to have
		varargs versions of all text functions.
		FIXME: make this buffer size safe someday
		============
		*/
		public static char	* QDECL va( char *format, ... ) {
			va_list		argptr;
			static char		string[2][32000];	// in case va is called by nested functions
			static int		index = 0;
			char	*buf;

			buf = string[index & 1];
			index++;

			va_start (argptr, format);
			vsprintf (buf, format,argptr);
			va_end (argptr);

			return buf;
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
		public static char *Info_ValueForKey( const char *s, const char *key ) {
			char	pkey[BIG_INFO_KEY];
			static	char value[2][BIG_INFO_VALUE];	// use two buffers so compares
													// work without stomping on each other
			static	int	valueindex = 0;
			char	*o;
		
			if ( !s || !key ) {
				return "";
			}

			if ( (int)strlen( s ) >= BIG_INFO_STRING ) {
				Com_Error( ERR_DROP, "Info_ValueForKey: oversize infostring" );
			}

			valueindex ^= 1;
			if (*s == '\\')
				s++;
			while (1)
			{
				o = pkey;
				while (*s != '\\')
				{
					if (!*s)
						return "";
					*o++ = *s++;
				}
				*o = 0;
				s++;

				o = value[valueindex];

				while (*s != '\\' && *s)
				{
					*o++ = *s++;
				}
				*o = 0;

				if (!Q_stricmp (key, pkey) )
					return value[valueindex];

				if (!*s)
					break;
				s++;
			}

			return "";
		}


		/*
		===================
		Info_NextPair

		Used to itterate through all the key/value pairs in an info string
		===================
		*/
		public static void Info_NextPair( const char **head, char *key, char *value ) {
			char	*o;
			const char	*s;

			s = *head;

			if ( *s == '\\' ) {
				s++;
			}
			key[0] = 0;
			value[0] = 0;

			o = key;
			while ( *s != '\\' ) {
				if ( !*s ) {
					*o = 0;
					*head = s;
					return;
				}
				*o++ = *s++;
			}
			*o = 0;
			s++;

			o = value;
			while ( *s != '\\' && *s ) {
				*o++ = *s++;
			}
			*o = 0;

			*head = s;
		}


		/*
		===================
		Info_RemoveKey
		===================
		*/
		public static void Info_RemoveKey( char *s, const char *key ) {
			char	*start;
			char	pkey[MAX_INFO_KEY];
			char	value[MAX_INFO_VALUE];
			char	*o;

			if ( (int)strlen( s ) >= MAX_INFO_STRING ) {
				Com_Error( ERR_DROP, "Info_RemoveKey: oversize infostring" );
			}

			if (strchr (key, '\\')) {
				return;
			}

			while (1)
			{
				start = s;
				if (*s == '\\')
					s++;
				o = pkey;
				while (*s != '\\')
				{
					if (!*s)
						return;
					*o++ = *s++;
				}
				*o = 0;
				s++;

				o = value;
				while (*s != '\\' && *s)
				{
					if (!*s)
						return;
					*o++ = *s++;
				}
				*o = 0;

				if (!strcmp (key, pkey) )
				{
					strcpy (start, s);	// remove this part
					return;
				}

				if (!*s)
					return;
			}

		}

		/*
		===================
		Info_RemoveKey_Big
		===================
		*/
		public static void Info_RemoveKey_Big( char *s, const char *key ) {
			char	*start;
			char	pkey[BIG_INFO_KEY];
			char	value[BIG_INFO_VALUE];
			char	*o;

			if ( (int)strlen( s ) >= BIG_INFO_STRING ) {
				Com_Error( ERR_DROP, "Info_RemoveKey_Big: oversize infostring" );
			}

			if (strchr (key, '\\')) {
				return;
			}

			while (1)
			{
				start = s;
				if (*s == '\\')
					s++;
				o = pkey;
				while (*s != '\\')
				{
					if (!*s)
						return;
					*o++ = *s++;
				}
				*o = 0;
				s++;

				o = value;
				while (*s != '\\' && *s)
				{
					if (!*s)
						return;
					*o++ = *s++;
				}
				*o = 0;

				if (!strcmp (key, pkey) )
				{
					strcpy (start, s);	// remove this part
					return;
				}

				if (!*s)
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
		public static bool Info_Validate( const char *s ) {
			if ( strchr( s, '\"' ) ) {
				return false;
			}
			if ( strchr( s, ';' ) ) {
				return false;
			}
			return true;
		}

		/*
		==================
		Info_SetValueForKey

		Changes or adds a key/value pair
		==================
		*/
		public static void Info_SetValueForKey( char *s, const char *key, const char *value ) {
			char	newi[MAX_INFO_STRING];

			if ( (int)strlen( s ) >= MAX_INFO_STRING ) {
				Com_Error( ERR_DROP, "Info_SetValueForKey: oversize infostring" );
			}

			if (strchr (key, '\\') || strchr (value, '\\'))
			{
				Com_Printf ("Can't use keys or values with a \\\n");
				return;
			}

			if (strchr (key, ';') || strchr (value, ';'))
			{
				Com_Printf ("Can't use keys or values with a semicolon\n");
				return;
			}

			if (strchr (key, '\"') || strchr (value, '\"'))
			{
				Com_Printf ("Can't use keys or values with a \"\n");
				return;
			}

			Info_RemoveKey (s, key);
			if (!value || !strlen(value))
				return;

			Com_sprintf (newi, sizeof(newi), "\\%s\\%s", key, value);

			if (strlen(newi) + (int)strlen(s) > MAX_INFO_STRING)
			{
				Com_Printf ("Info string length exceeded\n");
				return;
			}

			strcat (newi, s);
			strcpy (s, newi);
		}

		/*
		==================
		Info_SetValueForKey_Big

		Changes or adds a key/value pair
		==================
		*/
		public static void Info_SetValueForKey_Big( char *s, const char *key, const char *value ) {
			char	newi[BIG_INFO_STRING];

			if ( (int)strlen( s ) >= BIG_INFO_STRING ) {
				Com_Error( ERR_DROP, "Info_SetValueForKey: oversize infostring" );
			}

			if (strchr (key, '\\') || strchr (value, '\\'))
			{
				Com_Printf ("Can't use keys or values with a \\\n");
				return;
			}

			if (strchr (key, ';') || strchr (value, ';'))
			{
				Com_Printf ("Can't use keys or values with a semicolon\n");
				return;
			}

			if (strchr (key, '\"') || strchr (value, '\"'))
			{
				Com_Printf ("Can't use keys or values with a \"\n");
				return;
			}

			Info_RemoveKey_Big (s, key);
			if (!value || !strlen(value))
				return;

			Com_sprintf (newi, sizeof(newi), "\\%s\\%s", key, value);

			if (strlen(newi) + (int)strlen(s) > BIG_INFO_STRING)
			{
				Com_Printf ("BIG Info string length exceeded\n");
				return;
			}

			strcat (s, newi);
		}
	}
}