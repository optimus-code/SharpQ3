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
// qcommon.h -- definitions common between client and server, but not game.or ref modules

using SprintfNET;
using System.Runtime.InteropServices;

//#define	PRE_RELEASE_DEMO

//============================================================================
public static class QCommon
{ 
	//
	// msg.c
	//

	public struct msg_t
	{
		bool	allowoverflow;	// if false, do a Com_Error
		bool	overflowed;		// set to true if the buffer size failed (with allowoverflow set)
		bool	oob;			// set to true if the buffer size failed (with allowoverflow set)
		byte[]	data;
		int		maxsize;
		int		cursize;
		int		readcount;
		int		bit;				// for bitwise reads and writes
	}

	/*
	==============================================================

	NET

	==============================================================
	*/

	public const int PACKET_BACKUP = 32;// number of old messages that must be kept on client and
	// server for delta comrpession and ping estimation
	public const int PACKET_MASK = ( PACKET_BACKUP - 1 );

	public const int MAX_PACKET_USERCMDS = 32;     // max number of usercmd_t in a packet

	public const int PORT_ANY = -1;
	
	public const int MAX_RELIABLE_COMMANDS = 64;		// max string commands buffered for restransmit

	public enum netadrtype_t
	{
		NA_BOT,
		NA_BAD,					// an address lookup failed
		NA_LOOPBACK,
		NA_BROADCAST,
		NA_IP,
		NA_IPX,
		NA_BROADCAST_IPX
	}

	public enum netsrc_t
	{
		NS_CLIENT,
		NS_SERVER
	}

	public struct netadr_t
	{
		netadrtype_t	type;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 4)]
		byte[]	ip;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 10 )]
		byte[]	ipx;

		ushort port;
	}

	public const int MAX_MSGLEN = 16384;    // max length of a message, which may
											// be fragmented into multiple packets

	public const int MAX_DOWNLOAD_WINDOW = 8;    // max of eight download frames
	public const int MAX_DOWNLOAD_BLKSIZE = 2048;	// 2048 byte block chunks
 

	/*
	Netchan handles packet fragmentation and out of order / duplicate suppression
	*/

	public struct netchan_t
	{
		netsrc_t	sock;

		int			dropped;			// between last packet and previous

		netadr_t	remoteAddress;
		int			qport;				// qport value to write when transmitting

		// sequencing variables
		int			incomingSequence;
		int			outgoingSequence;

		// incoming fragment assembly buffer
		int			fragmentSequence;
		int			fragmentLength;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_MSGLEN )]
		byte[]		fragmentBuffer;

		// outgoing fragment buffer
		// we need to space out the sending of large fragmented messages
		bool	unsentFragments;
		int			unsentFragmentStart;
		int			unsentLength;
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_MSGLEN )]
		byte[]		unsentBuffer;
	}

	/*
	==============================================================

	PROTOCOL

	==============================================================
	*/

	public const int PROTOCOL_VERSION = 68;
	// 1.31 - 67

	// maintain a list of compatible protocols for demo playing
	// NOTE: that stuff only works with two digits protocols
	public static int[] demo_protocols;

	public const string UPDATE_SERVER_NAME = "update.quake3arena.com";
	// override on command line, config files etc.
	#ifndef MASTER_SERVER_NAME
	public const string MASTER_SERVER_NAME = "master.quake3arena.com";
	#endif
	#ifndef AUTHORIZE_SERVER_NAME
	public const string AUTHORIZE_SERVER_NAME = "authorize.quake3arena.com";
	#endif

	public const int PORT_MASTER = 27950;
	public const int PORT_UPDATE = 27951;
	#ifndef PORT_AUTHORIZE
	public const int PORT_AUTHORIZE = 27952;
	#endif
	public const int PORT_SERVER = 27960;
	public const int NUM_SERVER_PORTS = 4;	// broadcast scan this many ports after
	// PORT_SERVER so a single machine can
	// run multiple servers


	// the svc_strings[] array in cl_parse.c should mirror this
	//
	// server to client
	//
	public enum svc_ops_e 
	{
		svc_bad,
		svc_nop,
		svc_gamestate,
		svc_configstring,			// [short] [string] only in gamestate messages
		svc_baseline,				// only in gamestate messages
		svc_serverCommand,			// [string] to be executed by client game module
		svc_download,				// [short] size [size bytes]
		svc_snapshot,
		svc_EOF
	}


	//
	// client to server
	//
	public enum clc_ops_e 
	{
		clc_bad,
		clc_nop, 		
		clc_move,				// [[usercmd_t]
		clc_moveNoDelta,		// [[usercmd_t]
		clc_clientCommand,		// [string] message
		clc_EOF
	}

	/*
	==============================================================

	VIRTUAL MACHINE

	==============================================================
	*/

	typedef struct vm_s vm_t;

	public enum vmInterpret_t
	{
		VMI_NATIVE,
		VMI_BYTECODE
	}

	public enum sharedTraps_t
	{
		TRAP_MEMSET = 100,
		TRAP_MEMCPY,
		TRAP_STRNCPY,
		TRAP_SIN,
		TRAP_COS,
		TRAP_ATAN2,
		TRAP_SQRT,
		TRAP_MATRIXMULTIPLY,
		TRAP_ANGLEVECTORS,
		TRAP_PERPENDICULARVECTOR,
		TRAP_FLOOR,
		TRAP_CEIL,

		TRAP_TESTPRINTINT,
		TRAP_TESTPRINTFLOAT
	}

	/*
	==============================================================

	CMD

	Command text buffering and command execution

	==============================================================
	*/

	/*

	Any number of commands can be added in a frame, from several different sources.
	Most commands come from either keybindings or console line input, but entire text
	files can be execed.

	*/

	//===========================================================================

	/*

	Command execution takes a null terminated string, breaks it into tokens,
	then searches for a command or variable that matches the first token.

	*/

	delegate void xcommand_t( );


	/*
	==============================================================

	CVAR

	==============================================================
	*/

	/*

	cvar_t variables are used to hold scalar or string variables that can be changed
	or displayed at the console or prog code as well as accessed directly
	in C code.

	The user can access cvars from the console in three ways:
	r_draworder			prints the current value
	r_draworder 0		sets the current value to 0
	set r_draworder 0	as above, but creates the cvar if not present

	Cvars are restricted from having the same names as commands to keep this
	interface from being ambiguous.

	The are also occasionally used to communicated information between different
	modules of the program.

	*/

	public static	int			cvar_modifiedFlags;
	// whenever a cvar is modifed, its flags will be OR'd into this, so
	// a single check can determine if any CVAR_USERINFO, CVAR_SERVERINFO,
	// etc, variables have been modified since the last check.  The bit
	// can then be cleared to allow another change detection.

	/*
	==============================================================

	FILESYSTEM

	No stdio calls should be used by any part of the game, because
	we need to deal with all sorts of directory and seperator char
	issues.
	==============================================================
	*/

	// referenced flags
	// these are in loop specific order so don't change the order
	public const int FS_GENERAL_REF = 0x01;
	public const int FS_UI_REF = 0x02;
	public const int FS_CGAME_REF = 0x04;
	public const int FS_QAGAME_REF = 0x08;
	// number of id paks that will never be autodownloaded from baseq3
	public const int NUM_ID_PAKS = 9;

	public const int MAX_FILE_HANDLES = 64;

	public const string BASEGAME = "baseq3";

	/*
	==============================================================

	Edit fields and command line history/completion

	==============================================================
	*/

	public const int MAX_EDIT_LINE = 256;
	public struct field_t
	{
		int		cursor;
		int		scroll;
		int		widthInChars;
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = MAX_EDIT_LINE )]
		char[]	buffer;
	}

	/*
	==============================================================

	MISC

	==============================================================
	*/

	public static string Q_vsnprintf( string fmt, params object[] parameters )
	{
		return StringFormatter.PrintF( fmt, parameters );
	}

	// centralizing the declarations for cl_cdkey
	// https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=470
	public static string cl_cdkey;//[34];

	// returnbed by Sys_GetProcessorId
	public const int CPUID_GENERIC = 0;        // any unrecognized processor

	// TTimo
	// centralized and cleaned, that's the max string you can send to a Com_Printf / Com_DPrintf (above gets truncated)
	public const int MAXPRINTMSG =	4096;

	public static cvar_t	com_developer;
	public static cvar_t	com_dedicated;
	public static cvar_t	com_speeds;
	public static cvar_t	com_timescale;
	public static cvar_t	com_sv_running;
	public static cvar_t	com_cl_running;
	public static cvar_t	com_viewlog;           // 0 = hidden, 1 = visible, 2 = minimized
	public static cvar_t	com_version;
	public static cvar_t	com_blood;
	public static cvar_t	com_buildScript;       // for building release pak files
	public static cvar_t	com_journal;
	public static cvar_t	com_cameraMode;

	// both client and server must agree to pause
	public static cvar_t	cl_paused;
	public static cvar_t	sv_paused;

	// com_speeds times
	public static int		time_game;
	public static int		time_frontend;
	public static int		time_backend;       // renderer backend time

	public static int		com_frameTime;
	public static int		com_frameMsec;

	public static bool	com_errorEntered;

	public static fileHandle_t	com_journalFile;
	public static fileHandle_t	com_journalDataFile;

	public enum memtag_t
	{
		TAG_FREE,
		TAG_GENERAL,
		TAG_BOTLIB,
		TAG_RENDERER,
		TAG_SMALL,
		TAG_STATIC
	}

	/*

	--- low memory ----
	server vm
	server clipmap
	---mark---
	renderer initialization (shaders, etc)
	UI vm
	cgame vm
	renderer map
	renderer models

	---free---

	temp file loading
	--- high memory ---

	*/

	#if defined(_DEBUG) && !defined(BSPC)
		#define ZONE_DEBUG
	#endif

	#ifdef ZONE_DEBUG
	#define Z_TagMalloc(size, tag)			Z_TagMallocDebug(size, tag, #size, __FILE__, __LINE__)
	#define Z_Malloc(size)					Z_MallocDebug(size, #size, __FILE__, __LINE__)
	#define S_Malloc(size)					S_MallocDebug(size, #size, __FILE__, __LINE__)
	#endif


	/*
	==============================================================

	CLIENT / SERVER SYSTEMS

	==============================================================
	*/

	//
	// client interface
	//	

	/*
	==============================================================

	NON-PORTABLE SYSTEM SERVICES

	==============================================================
	*/

	public enum joystickAxis_t
	{
		AXIS_SIDE,
		AXIS_FORWARD,
		AXIS_UP,
		AXIS_ROLL,
		AXIS_YAW,
		AXIS_PITCH,
		MAX_JOYSTICK_AXIS
	}

	public enum sysEventType_t
	{
	  // bk001129 - make sure SE_NONE is zero
		SE_NONE = 0,	// evTime is still valid
		SE_KEY,		// evValue is a key code, evValue2 is the down flag
		SE_CHAR,	// evValue is an ascii char
		SE_MOUSE,	// evValue and evValue2 are reletive signed x / y moves
		SE_JOYSTICK_AXIS,	// evValue is an axis number and evValue2 is the current state (-127 to 127)
		SE_CONSOLE,	// evPtr is a char*
		SE_PACKET	// evPtr is a netadr_t followed by data bytes to evPtrLength
	}

	public struct sysEvent_t
	{
		int				evTime;
		sysEventType_t	evType;
		int				evValue, evValue2;
		int				evPtrLength;	// bytes of data pointed to by evPtr, for journaling
		void			*evPtr;			// this must be manually freed if not NULL
	}

	/* This is based on the Adaptive Huffman algorithm described in Sayood's Data
	 * Compression book.  The ranks are not actually stored, but implicitly defined
	 * by the location of a node within a doubly-linked list */

	public const int NYT HMAX;                   /* NYT = Not Yet Transmitted */
	public const int INTERNAL_NODE = (HMAX + 1);

	// Changed to class as it has references
	public class node_t
	{
		node_t left, right, parent; /* tree structure */ 
		node_t next, prev; /* doubly-linked list */
		node_t head; /* highest ranked node in block */
		int		weight;
		int		symbol;
	};

	public const int HMAX = 256; /* Maximum symbol */

	// Changed to class as it has references
	public class huff_t
	{
		int			blocNode;
		int			blocPtrs;

		node_t*		tree;
		node_t*		lhead;
		node_t*		ltail;
		node_t*		loc[HMAX+1];
		node_t**	freelist;

		node_t		nodeList[768];
		node_t*		nodePtrs[768];
	};

	public struct huffman_t
	{
		huff_t		compressor;
		huff_t		decompressor;
	}

	public static huffman_t clientHuffTables;

	public const int SV_ENCODE_START = 4;
	public const int SV_DECODE_START = 12;
	public const int CL_ENCODE_START = 12;
	public const int CL_DECODE_START = 4;
}