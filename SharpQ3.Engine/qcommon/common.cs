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

using SprintfNET;
using System;
using System.IO;
using System.Text;
using SharpQ3.Engine.client;
using static SharpQ3.Engine.q_shared;

namespace SharpQ3.Engine.qcommon
{
	// common.c -- misc functions used in client and server
	public static class common
	{
	private static int[] demo_protocols = new []{ 66, 67, 68, 0 };

	private const int MAX_NUM_ARGVS = 50;

	private const int MIN_DEDICATED_COMHUNKMEGS = 1;
	private const int MIN_COMHUNKMEGS = 56;
	private const string DEF_COMHUNKMEGS = "56";
	private const string DEF_COMZONEMEGS = "16";

	private static int com_argc;
	private static string com_argv;//[MAX_NUM_ARGVS+1];

	private static jmp_buf abortframe;      // an ERR_DROP occured, exit the entire frame


	private static FileStream debuglogfile;
	private static fileHandle_t logfile;
	public static int com_journalFile;            // events are written here
	public static int com_journalDataFile;        // config files are written here

	public static cvar_t com_viewlog;
	public static cvar_t com_speeds;
	public static cvar_t com_developer;
	public static cvar_t com_dedicated;
	public static cvar_t com_timescale;
	public static cvar_t com_fixedtime;
	public static cvar_t com_dropsim;     // 0.0 to 1.0, simulated packet drops
	public static cvar_t com_journal;
	public static cvar_t com_maxfps;
	public static cvar_t com_timedemo;
	public static cvar_t com_sv_running;
	public static cvar_t com_cl_running;
	public static cvar_t com_logfile;     // 1 = buffer log, 2 = flush after each print
	public static cvar_t com_showtrace;
	public static cvar_t com_version;
	public static cvar_t com_blood;
	public static cvar_t com_buildScript; // for automated data building scripts
	public static cvar_t com_introPlayed;
	public static cvar_t cl_paused;
	private static cvar_t sv_paused;
	public static cvar_t com_cameraMode;

	// com_speeds times
	private static int time_game;
	private static int time_frontend;       // renderer frontend time
	private static int time_backend;        // renderer backend time

	private static int com_frameTime;
	private static int com_frameMsec;
	private static int com_frameNumber;

	private static bool com_errorEntered;
	private static bool com_fullyInitialized;

	private static string com_errorMessage;//[MAXPRINTMSG];

	//============================================================================

	private static string rd_buffer;
	private static int	rd_buffersize;
	delegate void rd_flushDelegate( string buffer );
	private static rd_flushDelegate rd_flush;

	private static void Com_BeginRedirect (string buffer, int buffersize, rd_flushDelegate flush)
	{
		if (buffer == null || buffersize <= 0 || flush == null)
			return;
		rd_buffer = buffer;
		rd_buffersize = buffersize;
		rd_flush = flush;

		//*rd_buffer = 0;
	}

	private static void Com_EndRedirect ()
	{
		if ( rd_flush != null ) 
		{
			rd_flush(rd_buffer);
		}

		rd_buffer = null;
		rd_buffersize = 0;
		rd_flush = null;
	}

	private static bool opening_qconsole = false;

	/*
	=============
	Com_Printf

	Both client and server can use this, and it will output
	to the apropriate place.

	A raw string should NEVER be passed as fmt, because of "%f" type crashers.
	=============
	*/
	public static void Com_Printf( string fmt, params object[] args ) 
	{
		var msg = StringFormatter.PrintF( fmt, args );
		
		if ( rd_buffer != null ) 
		{
			if ((msg.Length + rd_buffer.Length) > (rd_buffersize - 1)) 
			{
				rd_flush(rd_buffer);
				//*rd_buffer = 0;
			}
			q_shared.Q_strcat(ref rd_buffer, rd_buffersize, msg);
		// TTimo nooo .. that would defeat the purpose
			//rd_flush(rd_buffer);			
			//*rd_buffer = 0;
			return;
		}

		// echo to console if we're not a dedicated server
		if ( com_dedicated != null && com_dedicated.integer == 0 ) 
		{
			cl_console.CL_ConsolePrint( msg );
		}

		// echo to dedicated console and early console
		Sys_Print( msg );

		// logfile
		if ( com_logfile != null && com_logfile.integer == 1 ) 
		{
		// TTimo: only open the qconsole.log if the filesystem is in an initialized state
		//   also, avoid recursing in the qconsole.log opening (i.e. if fs_debug is on)
			if ( logfile.ID == 0 && files.FS_Initialized() && !opening_qconsole) 
			{
				opening_qconsole = true;

				var newtime = DateTimeOffset.Now.ToUnixTimeSeconds();

				logfile = files.FS_FOpenFileWrite( "qconsole.log" );
				Com_Printf( "logfile opened on %s\n", DateTime.Now.ToString( "ddd MMM dd hh:mm:ss yyyy" ) );
				if ( com_logfile.integer > 1 ) 
				{
					// force it to not buffer so we get valid
					// data even if we are crashing
					files.FS_ForceFlush(logfile);
				}

				opening_qconsole = false;
			}
			if ( logfile.ID > 0 && files.FS_Initialized()) 
			{
				files.FS_Write(Encoding.ASCII.GetBytes( msg ), msg.Length, logfile);
			}
		}
	}


	/*
	================
	Com_DPrintf

	A Com_Printf that only shows up if the "developer" cvar is set
	================
	*/
	public static void Com_DPrintf( string fmt, params object[] args )
	{		
		if ( com_developer == null || com_developer.integer != 1 ) {
			return;			// don't confuse non-developers with techie stuff...
		}

		var msg = StringFormatter.PrintF( fmt, args );			
		Com_Printf ("%s", msg);
	}

	private static int	lastErrorTime;
	private static int	errorCount;
	/*
	=============
	Com_Error

	Both client and server can use this, and it will
	do the apropriate things.
	=============
	*/
	public static void Com_Error( errorParm_t code, string fmt, params object[] args )
	{
		int			currentTime;

		// when we are running automated scripts, make sure we
		// know if anything failed
		if ( com_buildScript != null && com_buildScript.integer == 1) 
		{
			code = errorParm_t.ERR_FATAL;
		}

		// make sure we can get at our local stuff
		files.FS_PureServerSetLoadedPaks( "", "" );

		// if we are getting a solid stream of ERR_DROP, do an ERR_FATAL
		currentTime = Sys_Milliseconds();
		if ( currentTime - lastErrorTime < 100 ) {
			if ( ++errorCount > 3 ) {
				code = errorParm_t.ERR_FATAL;
			}
		} else {
			errorCount = 0;
		}
		lastErrorTime = currentTime;

		if ( com_errorEntered ) {
			Sys_Error( "recursive error after: %s", com_errorMessage );
		}
		com_errorEntered = true;

		com_errorMessage = StringFormatter.PrintF( fmt, args );

		if ( code != errorParm_t.ERR_DISCONNECT ) {
			Cvar.Cvar_Set("com_errorMessage", com_errorMessage);
		}

		if ( code == errorParm_t.ERR_SERVERDISCONNECT ) {
			CL_Disconnect( true );
			CL_FlushMemory( );
			com_errorEntered = false;
			longjmp (abortframe, -1);
		} else if ( code == errorParm_t.ERR_DROP || code == errorParm_t.ERR_DISCONNECT ) {
			Com_Printf ("********************\nERROR: %s\n********************\n", com_errorMessage);
			SV_Shutdown (va("Server crashed: %s\n",  com_errorMessage));
			CL_Disconnect( true );
			CL_FlushMemory( );
			com_errorEntered = false;
			longjmp (abortframe, -1);
		} else {
			CL_Shutdown ();
			SV_Shutdown (va("Server fatal crashed: %s\n", com_errorMessage));
		}

		common.Com_Shutdown ();

		Sys_Error ("%s", com_errorMessage);
	}


	/*
	=============
	Com_Quit_f

	Both client and server can use this, and it will
	do the apropriate things.
	=============
	*/
	private static void Com_Quit_f( ) 
	{
		// don't try to shutdown if we are in a recursive error
		if ( !com_errorEntered )
		{
			SV_Shutdown ("Server quit\n");
			CL_Shutdown ();
			Com_Shutdown ();
			files.FS_Shutdown(true);
		}
		Sys_Quit ();
	}



	/*
	============================================================================

	COMMAND LINE FUNCTIONS

	+ characters seperate the commandLine string into multiple console
	command lines.

	All of these are valid:

	quake3 +set test blah +map test
	quake3 set test blah+map test
	quake3 set test blah + map test

	============================================================================
	*/

	private const int MAX_CONSOLE_LINES = 32;
	private static int		com_numConsoleLines;
	private static char[][] com_consoleLines = new char[MAX_CONSOLE_LINES][];

	/*
	==================
	Com_ParseCommandLine

	Break it up into multiple console lines
	==================
	*/
	private static void Com_ParseCommandLine( ref string commandLine ) 
	{
		bool inq = false;
		com_consoleLines[0] = commandLine.ToCharArray();
		com_numConsoleLines = 1;

		var commandLineIndex = 0;

		while ( commandLineIndex < commandLine.Length - 1 ) 
		{
			var character = commandLine[commandLineIndex];

			if ( character == '"' ) 
			{
				inq = !inq;
			}
			// look for a + seperating character
			// if commandLine came from a file, we might have real line seperators
			if ( ( character == '+' && !inq) || character == '\n'  || character == '\r' ) 
			{
				if ( com_numConsoleLines == MAX_CONSOLE_LINES ) 
				{
					return;
				}
				com_consoleLines[com_numConsoleLines] = commandLine.Substring( commandLineIndex + 1 ).ToCharArray();
				com_numConsoleLines++;

				if ( commandLineIndex > 0 )
					commandLine = commandLine.Substring( commandLineIndex - 1 );
			}
			commandLineIndex++;
		}
	}


	/*
	===================
	Com_SafeMode

	Check for "safe" on the command line, which will
	skip loading of q3config.cfg
	===================
	*/
	public static bool Com_SafeMode( ) 
	{
		int		i;

		for ( i = 0 ; i < com_numConsoleLines ; i++ ) {
			cmd.Cmd_TokenizeString( com_consoleLines[i].ToString() );
			if ( q_shared.Q_stricmp( cmd.Cmd_Argv(0), "safe" ) <= 0
				|| q_shared.Q_stricmp( cmd.Cmd_Argv(0), "cvar_restart" ) <= 0 ) {
				com_consoleLines[i][0] = (char)0;
				return true;
			}
		}
		return false;
	}


	/*
	===============
	Com_StartupVariable

	Searches for command line parameters that are set commands.
	If match is not NULL, only that cvar will be looked for.
	That is necessary because cddir and basedir need to be set
	before the filesystem is started, but all other sets shouls
	be after execing the config and default.
	===============
	*/
	public static void Com_StartupVariable( string match ) {
		int		i;
		string s;
		cvar_t	cv;

		for (i=0 ; i < com_numConsoleLines ; i++) {
			cmd.Cmd_TokenizeString( com_consoleLines[i].ToString() );
			if ( !cmd.Cmd_Argv(0).Contains( "set" ) ) {
				continue;
			}

			s = cmd.Cmd_Argv(1);
			if ( match == null || !s.Contains( match ) ) {
				Cvar.Cvar_Set( s, cmd.Cmd_Argv(2) );
				cv = Cvar.Cvar_Get( s, "", CVAR.NONE );
				cv.flags |= CVAR.USER_CREATED;
	//			com_consoleLines[i] = 0;
			}
		}
	}


	/*
	=================
	Com_AddStartupCommands

	Adds command line parameters as script statements
	Commands are seperated by + signs

	Returns true if any late commands were added, which
	will keep the demoloop from immediately starting
	=================
	*/
	private static bool Com_AddStartupCommands( ) 
	{
		int		i;
		bool	added;

		added = false;
		// quote every token, so args with semicolons can work
		for (i=0 ; i < com_numConsoleLines ; i++) {
			if ( com_consoleLines[i] == null || com_consoleLines[i][0] == 0 ) {
				continue;
			}

			// set commands won't override menu startup
			if ( q_shared.Q_stricmpn( com_consoleLines[i].ToString(), "set", 3 ) > 0 )
				added = true;
			
			cmd.Cbuf_AddText( com_consoleLines[i].ToString() );
			cmd.Cbuf_AddText( "\n" );
		}

		return added;
	}

	//============================================================================

	private static void Info_Print( string s )
	{
		string	key;
		string value;
		string o;
		int		l;
		var sI = 0;

		if (s[0] == '\\')
			sI++;

		while ( sI < s.Length )
		{
			o = key;
			while ( sI < s.Length && s[sI] != '\\' )
				o += s[sI++];

			l = o.Length - key.Length;
			if (l < 20)
			{
				Com_Memset (o, ' ', 20-l);
				key[20] = 0;
			}
			else
				*o = 0;
			Com_Printf ("%s", key);

			if ( sI >= s.Length )
			{
				Com_Printf ("MISSING VALUE\n");
				return;
			}

			o = value;
			s++;
			while ( sI < s.Length && s[sI] != '\\' )
				o += s[sI++];
			*o = 0;

			if ( sI < s.Length )
				sI++;
			Com_Printf ("%s\n", value);
		}
	}

	/*
	============
	Com_StringContains
	============
	*/
	private static string Com_StringContains( string str1, string str2, bool casesensitive) 
	{
		if ( str1.Contains( str2, casesensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase ) )
			return str1;
		else
			return null;
	}

	/*
	============
	Com_Filter
	============
	*/
	public static bool Com_Filter(string filter, string name, bool casesensitive)
	{
		StringBuilder buf = new StringBuilder( q_shared.MAX_TOKEN_CHARS );
		string ptr;
		int i;
		bool found;
		int nameI = 0;
		int filterI = 0;

		while( filterI < filter.Length ) {
			if ( filter[filterI] == '*') {
				filterI++;
				for (i = 0; filterI < filter.Length; i++) {
					if ( filter[filterI] == '*' || filter[filterI] == '?') break;
					buf[i] = filter[filterI];
					filterI++;
				}
				buf[i] = '\0';
				if ( buf.Length > 0) {
					ptr = Com_StringContains(name, buf.ToString(), casesensitive);
					if ( ptr == null ) return false;
					name = ptr + (int)buf.Length;
				}
			}
			else if ( filter[filterI] == '?') {
				filterI++;
				nameI++;
			}
			else if ( filter[filterI] == '[' && filter[filterI + 1] == '[') {
				filterI++;
			}
			else if ( filter[filterI] == '[') {
				filterI++;
				found = false;
				while(filterI < filter.Length && !found) {
					if ( filter[filterI] == ']' && filter[filterI + 1] != ']') break;
					if ( filter[filterI + 1] == '-' && filterI + 2 < filter.Length && ( filter[filterI + 2] != ']' || filter[filterI + 3] == ']')) {
						if (casesensitive) {
							if ( name[nameI] >= filter[filterI] && name[nameI] <= filter[filterI + 2] ) found = true;
						}
						else {
							if ( Char.ToUpper( name[nameI] ) >= Char.ToUpper( filter[filterI] ) &&
								Char.ToUpper( name[nameI] ) <= Char.ToUpper( filter[filterI + 2] ) ) found = true;
						}
						filter += 3;
					}
					else {
						if (casesensitive) {
							if ( filter[filterI] == name[nameI] ) found = true;
						}
						else {
							if (Char.ToUpper( filter[filterI] ) == Char.ToUpper( name[nameI] ) ) found = true;
						}
						filterI++;
					}
				}
				if (!found) return false;
				while(filterI < filter.Length) {
					if ( filter[filterI] == ']' && filter[filterI + 1] != ']') break;
					filterI++;
				}
				filterI++;
				nameI++;
			}
			else {
				if (casesensitive) {
					if ( filter[filterI] != name[nameI]) return false;
				}
				else {
					if ( Char.ToUpper( filter[filterI] ) != Char.ToUpper( name[nameI] ) ) return false;
				}
				filterI++;
				nameI++;
			}
		}
		return true;
	}

	/*
	============
	Com_FilterPath
	============
	*/
	public static bool Com_FilterPath(string filter, string name, bool casesensitive)
	{
		int i;
		StringBuilder new_filter = new StringBuilder( q_shared.MAX_QPATH );
		StringBuilder new_name = new StringBuilder( q_shared.MAX_QPATH );

		for (i = 0; i < q_shared.MAX_QPATH -1 && i < filter.Length; i++) {
			if ( filter[i] == '\\' || filter[i] == ':' ) {
				new_filter[i] = '/';
			}
			else {
				new_filter[i] = filter[i];
			}
		}
		new_filter[i] = '\0';
		for (i = 0; i < q_shared.MAX_QPATH-1 && i < name.Length; i++) {
			if ( name[i] == '\\' || name[i] == ':' ) {
				new_name[i] = '/';
			}
			else {
				new_name[i] = name[i];
			}
		}
		new_name[i] = '\0';
		return Com_Filter(new_filter.ToString(), new_name.ToString(), casesensitive);
	}

	/*
	============
	Com_HashKey
	============
	*/
	private static int Com_HashKey( string str, int maxlen)
	{
		int hash, i;

		hash = 0;
		for (i = 0; i < maxlen && str[i] != '\0'; i++) {
			hash += str[i] * (119 + i);
		}
		hash = (hash ^ (hash >> 10) ^ (hash >> 20));
		return hash;
	}

	/*
	================
	Com_RealTime
	================
	*/
	public static long Com_RealTime() {
		return DateTime.Now.Ticks;
	}


	/*
	==============================================================================

							ZONE MEMORY ALLOCATION

	There is never any space between memblocks, and there will never be two
	contiguous free memblocks.

	The rover can be left pointing at a non-empty block

	The zone calls are pretty much only used for small strings and structures,
	all big things are allocated on the hunk.
	==============================================================================
	*/

	public const int ZONEID = 0x1d4a11;
	public const int MINFRAGMENT = 64;

	public struct zonedebug_t {
		string label;
		string file;
		int line;
		int allocSize;
	}

	public class memblock_t {
		int		size;           // including the header and possibly tiny fragments
		int     tag;            // a tag of 0 is a free block
		memblock_t next;
		memblock_t prev;
		int     id;        		// should be ZONEID
	}

	public class memzone_t {
		int		size;			// total bytes malloced, including header
		int		used;			// total bytes used
		memblock_t	blocklist;	// start / end cap for linked list
		memblock_t	rover;
	} ;

	// main zone for all "dynamic" memory allocation
	private static memzone_t	mainzone;
	// we also have a small zone for small allocations that would only
	// fragment the main zone (think of cvar and cmd strings)
	private static memzone_t	smallzone;

	/*
	========================
	Z_ClearZone
	========================
	*/
	private static void Z_ClearZone( memzone_t zone, int size ) {
		memblock_t	block;
	
		// set the entire zone to one free block

		zone.blocklist.next = zone.blocklist.prev = block =
			(memblock_t *)( (byte *)zone + sizeof(memzone_t) );
		zone.blocklist.tag = 1;	// in use block
		zone.blocklist.id = 0;
		zone.blocklist.size = 0;
		zone.rover = block;
		zone.size = size;
		zone.used = 0;
	
		block.prev = block.next = &zone.blocklist;
		block.tag = 0;			// free block
		block.id = ZONEID;
		block.size = size - sizeof(memzone_t);
	}

	/*
	========================
	Z_AvailableZoneMemory
	========================
	*/
	private static int Z_AvailableZoneMemory( memzone_t zone ) {
		return zone.size - zone.used;
	}

	/*
	========================
	Z_AvailableMemory
	========================
	*/
	private static int Z_AvailableMemory( ) {
		return Z_AvailableZoneMemory( mainzone );
	}

	///*
	/// We we leave it to GC and Dispose for C#
	//========================
	//Z_Free
	//========================
	//*/
	//private static void Z_Free( void *ptr ) {
	//	memblock_t	*block, *other;
	//	memzone_t *zone;
	
	//	if (!ptr) {
	//		Com_Error( errorParm_t.ERR_DROP, "Z_Free: NULL pointer" );
	//	}

	//	block = (memblock_t *) ( (byte *)ptr - sizeof(memblock_t));
	//	if (block.id != ZONEID) {
	//		Com_Error( errorParm_t.ERR_FATAL, "Z_Free: freed a pointer without ZONEID" );
	//	}
	//	if (block.tag == 0) {
	//		Com_Error( errorParm_t.ERR_FATAL, "Z_Free: freed a freed pointer" );
	//	}
	//	// if static memory
	//	if (block.tag == TAG_STATIC) {
	//		return;
	//	}

	//	// check the memory trash tester
	//	if ( *(int *)((byte *)block + block.size - 4 ) != ZONEID ) {
	//		Com_Error( errorParm_t.ERR_FATAL, "Z_Free: memory block wrote past end" );
	//	}

	//	if (block.tag == TAG_SMALL) {
	//		zone = smallzone;
	//	}
	//	else {
	//		zone = mainzone;
	//	}

	//	zone.used -= block.size;
	//	// set the block to something that should cause problems
	//	// if it is referenced...
	//	Com_Memset( ptr, 0xaa, block.size - sizeof( *block ) );

	//	block.tag = 0;		// mark as free
	
	//	other = block.prev;
	//	if (!other.tag) {
	//		// merge with previous free block
	//		other.size += block.size;
	//		other.next = block.next;
	//		other.next.prev = other;
	//		if (block == zone.rover) {
	//			zone.rover = other;
	//		}
	//		block = other;
	//	}

	//	zone.rover = block;

	//	other = block.next;
	//	if ( !other.tag ) {
	//		// merge the next free block onto the end
	//		block.size += other.size;
	//		block.next = other.next;
	//		block.next.prev = block;
	//		if (other == zone.rover) {
	//			zone.rover = block;
	//		}
	//	}
	//}


	///*
	//================
	//Z_FreeTags
	//================
	//*/
	//private static void Z_FreeTags( int tag ) {
	//	int			count;
	//	memzone_t	*zone;

	//	if ( tag == TAG_SMALL ) {
	//		zone = smallzone;
	//	}
	//	else {
	//		zone = mainzone;
	//	}
	//	count = 0;
	//	// use the rover as our pointer, because
	//	// Z_Free automatically adjusts it
	//	zone.rover = zone.blocklist.next;
	//	do {
	//		if ( zone.rover.tag == tag ) {
	//			count++;
	//			Z_Free( (void *)(zone.rover + 1) );
	//			continue;
	//		}
	//		zone.rover = zone.rover.next;
	//	} while ( zone.rover != &zone.blocklist );
	//}


	///*
	//================
	//Z_TagMalloc
	//================
	//*/
	//private static void *Z_TagMalloc( int size, int tag ) {
	//	int		extra, allocSize;
	//	memblock_t	*start, *rover, *newBlock, *base;
	//	memzone_t *zone;

	//	if (!tag) {
	//		Com_Error( errorParm_t.ERR_FATAL, "Z_TagMalloc: tried to use a 0 tag" );
	//	}

	//	if ( tag == TAG_SMALL ) {
	//		zone = smallzone;
	//	}
	//	else {
	//		zone = mainzone;
	//	}

	//	allocSize = size;
	//	//
	//	// scan through the block list looking for the first free block
	//	// of sufficient size
	//	//
	//	size += sizeof(memblock_t);	// account for size of block header
	//	size += 4;					// space for memory trash tester
	//	size = (size + 3) & ~3;		// align to 32 bit boundary
	
	//	base = rover = zone.rover;
	//	start = base.prev;
	
	//	do {
	//		if (rover == start)	{
	//			// scaned all the way around the list
	//			Com_Error( errorParm_t.ERR_FATAL, "Z_Malloc: failed on allocation of %i bytes from the %s zone",
	//								size, zone == smallzone ? "small" : "main");
	//			return NULL;
	//		}
	//		if (rover.tag) {
	//			base = rover = rover.next;
	//		} else {
	//			rover = rover.next;
	//		}
	//	} while (base.tag || base.size < size);
	
	//	//
	//	// found a block big enough
	//	//
	//	extra = base.size - size;
	//	if (extra > MINFRAGMENT) {
	//		// there will be a free fragment after the allocated block
	//		newBlock = (memblock_t *)((byte *)base + size);
	//		newBlock.size = extra;
	//		newBlock.tag = 0;			// free block
	//		newBlock.prev = base;
	//		newBlock.id = ZONEID;
	//		newBlock.next = base.next;
	//		newBlock.next.prev = newBlock;
	//		base.next = newBlock;
	//		base.size = size;
	//	}
	
	//	base.tag = tag;			// no longer a free block
	
	//	zone.rover = base.next;	// next allocation will start looking here
	//	zone.used += base.size;	//
	
	//	base.id = ZONEID;

	//	// marker for memory trash testing
	//	*(int *)((byte *)base + base.size - 4) = ZONEID;

	//	return (void *) ((byte *)base + sizeof(memblock_t));
	//}

	///*
	//========================
	//Z_Malloc
	//========================
	//*/
	//private static void *Z_Malloc( int size ) {
	//	void	*buf;
	
	//  //Z_CheckHeap ();	// DEBUG

	//	buf = Z_TagMalloc( size, TAG_GENERAL );
	//	Com_Memset( buf, 0, size );

	//	return buf;
	//}

	//private static void *S_Malloc( int size ) {
	//	return Z_TagMalloc( size, TAG_SMALL );
	//}

	///*
	//========================
	//Z_CheckHeap
	//========================
	//*/
	//private static void Z_CheckHeap( void ) {
	//	memblock_t	*block;
	
	//	for (block = mainzone.blocklist.next ; ; block = block.next) {
	//		if (block.next == &mainzone.blocklist) {
	//			break;			// all blocks have been hit
	//		}
	//		if ( (byte *)block + block.size != (byte *)block.next)
	//			Com_Error( errorParm_t.ERR_FATAL, "Z_CheckHeap: block size does not touch the next block\n" );
	//		if ( block.next.prev != block) {
	//			Com_Error( errorParm_t.ERR_FATAL, "Z_CheckHeap: next block doesn't have proper back link\n" );
	//		}
	//		if ( !block.tag && !block.next.tag ) {
	//			Com_Error( errorParm_t.ERR_FATAL, "Z_CheckHeap: two consecutive free blocks\n" );
	//		}
	//	}
	//}

	///*
	//========================
	//Z_LogZoneHeap
	//========================
	//*/
	//private static void Z_LogZoneHeap( memzone_t *zone, char *name ) {
	//	memblock_t	*block;
	//	char		buf[4096];
	//	int size, allocSize, numBlocks;

	//	if (!logfile || !files.FS_Initialized())
	//		return;
	//	size = allocSize = numBlocks = 0;
	//	Com_sprintf(buf, sizeof(buf), "\r\n================\r\n%s log\r\n================\r\n", name);
	//	files.FS_Write(buf, (int)strlen(buf), logfile);
	//	for (block = zone.blocklist.next ; block.next != &zone.blocklist; block = block.next) {
	//		if (block.tag) {
	//			size += block.size;
	//			numBlocks++;
	//		}
	//	}
	//	allocSize = numBlocks * sizeof(memblock_t); // + 32 bit alignment
	//	Com_sprintf(buf, sizeof(buf), "%d %s memory in %d blocks\r\n", size, name, numBlocks);
	//	files.FS_Write(buf, (int)strlen(buf), logfile);
	//	Com_sprintf(buf, sizeof(buf), "%d %s memory overhead\r\n", size - allocSize, name);
	//	files.FS_Write(buf, (int)strlen(buf), logfile);
	//}

	///*
	//========================
	//Z_LogHeap
	//========================
	//*/
	//private static void Z_LogHeap( void ) {
	//	Z_LogZoneHeap( mainzone, "MAIN" );
	//	Z_LogZoneHeap( smallzone, "SMALL" );
	//}

	//// static mem blocks to reduce a lot of small zone overhead
	//typedef struct memstatic_s {
	//	memblock_t b;
	//	byte mem[2];
	//} memstatic_t;

	//// bk001204 - initializer brackets
	//memstatic_t emptystring =
	//	{ {(sizeof(memblock_t)+2 + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'\0', '\0'} };
	//memstatic_t numberstring[] = {
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'0', '\0'} },
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'1', '\0'} },
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'2', '\0'} },
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'3', '\0'} },
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'4', '\0'} },
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'5', '\0'} },
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'6', '\0'} },
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'7', '\0'} },
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'8', '\0'} }, 
	//	{ {(sizeof(memstatic_t) + 3) & ~3, TAG_STATIC, NULL, NULL, ZONEID}, {'9', '\0'} }
	//};

	/*
	========================
	CopyString

	 NOTE:	never write over the memory CopyString returns because
			memory from a memstatic_t might be returned
	========================
	*/
	public static string CopyString( string input ) 
	{
		return new string( input );
	}

	/*
	==============================================================================

	Goals:
		reproducable without history effects -- no out of memory errors on weird map to map changes
		allow restarting of the client without fragmentation
		minimize total pages in use at run time
		minimize total pages needed during load time

	  Single block of memory with stack allocators coming from both ends towards the middle.

	  One side is designated the temporary memory allocator.

	  Temporary memory can be allocated and freed in any order.

	  A highwater mark is kept of the most in use at any time.

	  When there is no temporary memory allocated, the permanent and temp sides
	  can be switched, allowing the already touched temp memory to be used for
	  permanent storage.

	  Temp memory must never be allocated on two ends at once, or fragmentation
	  could occur.

	  If we have any in-use temp memory, additional temp allocations must come from
	  that side.

	  If not, we can choose to make either side the new temp side and push future
	  permanent allocations to the other side.  Permanent allocations should be
	  kept on the side that has the current greatest wasted highwater mark.

	==============================================================================
	*/


	public const long	HUNK_MAGIC = 0x89537892;
	public const long HUNK_FREE_MAGIC = 0x89537893;

	public struct hunkHeader_t
	{
		public int magic;
		public int size;
	}

	public struct hunkUsed_t
	{
		public int		mark;
		public int		permanent;
		public int		temp;
		public int		tempHighwater;
	} ;

	public class hunkblock_t
	{
		int size;
		byte printed;
		hunkblock_t next;
		string label;
		string file;
		int line;
	}

	static	hunkblock_t hunkblocks;

	static	hunkUsed_t	hunk_low, hunk_high;
	static	hunkUsed_t	hunk_permanent, hunk_temp;

	static	byte[]	s_hunkData = null;
	static	int		s_hunkTotal;

	static	int		s_zoneTotal;
	static	int		s_smallZoneTotal;


	/*
	=================
	Com_Meminfo_f
	=================
	*/
	private static void Com_Meminfo_f( ) 
	{
		memblock_t	block;
		int			zoneBytes, zoneBlocks;
		int			smallZoneBytes, smallZoneBlocks;
		int			botlibBytes, rendererBytes;
		int			unused;

		zoneBytes = 0;
		botlibBytes = 0;
		rendererBytes = 0;
		zoneBlocks = 0;
		for (block = mainzone.blocklist.next ; ; block = block.next) {
			if ( cmd.Cmd_Argc() != 1 ) {
				Com_Printf ("block:%p    size:%7i    tag:%3i\n",
					block, block.size, block.tag);
			}
			if ( block.tag ) {
				zoneBytes += block.size;
				zoneBlocks++;
				if ( block.tag == TAG_BOTLIB ) {
					botlibBytes += block.size;
				} else if ( block.tag == TAG_RENDERER ) {
					rendererBytes += block.size;
				}
			}

			if (block.next == &mainzone.blocklist) {
				break;			// all blocks have been hit	
			}
			if ( (byte *)block + block.size != (byte *)block.next) {
				Com_Printf ("ERROR: block size does not touch the next block\n");
			}
			if ( block.next.prev != block) {
				Com_Printf ("ERROR: next block doesn't have proper back link\n");
			}
			if ( !block.tag && !block.next.tag ) {
				Com_Printf ("ERROR: two consecutive free blocks\n");
			}
		}

		smallZoneBytes = 0;
		smallZoneBlocks = 0;
		for (block = smallzone.blocklist.next ; ; block = block.next) {
			if ( block.tag ) {
				smallZoneBytes += block.size;
				smallZoneBlocks++;
			}

			if (block.next == &smallzone.blocklist) {
				break;			// all blocks have been hit	
			}
		}

		Com_Printf( "%8i bytes total hunk\n", s_hunkTotal );
		Com_Printf( "%8i bytes total zone\n", s_zoneTotal );
		Com_Printf( "\n" );
		Com_Printf( "%8i low mark\n", hunk_low.mark );
		Com_Printf( "%8i low permanent\n", hunk_low.permanent );
		if ( hunk_low.temp != hunk_low.permanent ) {
			Com_Printf( "%8i low temp\n", hunk_low.temp );
		}
		Com_Printf( "%8i low tempHighwater\n", hunk_low.tempHighwater );
		Com_Printf( "\n" );
		Com_Printf( "%8i high mark\n", hunk_high.mark );
		Com_Printf( "%8i high permanent\n", hunk_high.permanent );
		if ( hunk_high.temp != hunk_high.permanent ) {
			Com_Printf( "%8i high temp\n", hunk_high.temp );
		}
		Com_Printf( "%8i high tempHighwater\n", hunk_high.tempHighwater );
		Com_Printf( "\n" );
		Com_Printf( "%8i total hunk in use\n", hunk_low.permanent + hunk_high.permanent );
		unused = 0;
		if ( hunk_low.tempHighwater > hunk_low.permanent ) {
			unused += hunk_low.tempHighwater - hunk_low.permanent;
		}
		if ( hunk_high.tempHighwater > hunk_high.permanent ) {
			unused += hunk_high.tempHighwater - hunk_high.permanent;
		}
		Com_Printf( "%8i unused highwater\n", unused );
		Com_Printf( "\n" );
		Com_Printf( "%8i bytes in %i zone blocks\n", zoneBytes, zoneBlocks	);
		Com_Printf( "        %8i bytes in dynamic botlib\n", botlibBytes );
		Com_Printf( "        %8i bytes in dynamic renderer\n", rendererBytes );
		Com_Printf( "        %8i bytes in dynamic other\n", zoneBytes - ( botlibBytes + rendererBytes ) );
		Com_Printf( "        %8i bytes in small Zone memory\n", smallZoneBytes );
	}

	/*
	===============
	Com_TouchMemory

	Touch all known used data to make sure it is paged in
	===============
	*/
	private static void Com_TouchMemory( ) 
	{
		int		start, end;
		int		i, j;
		int		sum;
		memblock_t	*block;

		Z_CheckHeap();

		start = Sys_Milliseconds();

		sum = 0;

		j = hunk_low.permanent >> 2;
		for ( i = 0 ; i < j ; i+=64 ) {			// only need to touch each page
			sum += ((int *)s_hunkData)[i];
		}

		i = ( s_hunkTotal - hunk_high.permanent ) >> 2;
		j = hunk_high.permanent >> 2;
		for (  ; i < j ; i+=64 ) {			// only need to touch each page
			sum += ((int *)s_hunkData)[i];
		}

		for (block = mainzone.blocklist.next ; ; block = block.next) {
			if ( block.tag ) {
				j = block.size >> 2;
				for ( i = 0 ; i < j ; i+=64 ) {				// only need to touch each page
					sum += ((int *)block)[i];
				}
			}
			if ( block.next == &mainzone.blocklist ) {
				break;			// all blocks have been hit	
			}
		}

		end = Sys_Milliseconds();

		Com_Printf( "Com_TouchMemory: %i msec\n", end - start );
	}



	/*
	=================
	Com_InitZoneMemory
	=================
	*/
	private static void Com_InitSmallZoneMemory( ) 
	{
		s_smallZoneTotal = 512 * 1024;
		// bk001205 - was malloc
		smallzone = (memzone_t*) calloc( s_smallZoneTotal, 1 );
		if ( !smallzone ) {
			Com_Error( errorParm_t.ERR_FATAL, "Small zone data failed to allocate %1.1f megs", (float)s_smallZoneTotal / (1024*1024) );
		}
		Z_ClearZone( smallzone, s_smallZoneTotal );
	
		return;
	}

	private static void Com_InitZoneMemory( ) 
	{
		cvar_t	*cv;
		// allocate the random block zone
		cv = Cvar.Cvar_Get( "com_zoneMegs", DEF_COMZONEMEGS, CVAR_LATCH | CVAR_ARCHIVE );

		if ( cv.integer < 20 ) {
			s_zoneTotal = 1024 * 1024 * 16;
		} else {
			s_zoneTotal = cv.integer * 1024 * 1024;
		}

		// bk001205 - was malloc
		mainzone = (memzone_t*) calloc( s_zoneTotal, 1 );
		if ( !mainzone ) {
			Com_Error( errorParm_t.ERR_FATAL, "Zone data failed to allocate %i megs", s_zoneTotal / (1024*1024) );
		}
		Z_ClearZone( mainzone, s_zoneTotal );

	}

	/*
	=================
	Hunk_Log
	=================
	*/
	private static void Hunk_Log() 
	{
		hunkblock_t	*block;
		char		buf[4096];
		int size, numBlocks;

		if (!logfile || !files.FS_Initialized())
			return;
		size = 0;
		numBlocks = 0;
		Com_sprintf(buf, sizeof(buf), "\r\n================\r\nHunk log\r\n================\r\n");
		files.FS_Write(buf, (int)strlen(buf), logfile);
		for (block = hunkblocks ; block; block = block.next) {
			size += block.size;
			numBlocks++;
		}
		Com_sprintf(buf, sizeof(buf), "%d Hunk memory\r\n", size);
		files.FS_Write(buf, (int)strlen(buf), logfile);
		Com_sprintf(buf, sizeof(buf), "%d hunk blocks\r\n", numBlocks);
		files.FS_Write(buf, (int)strlen(buf), logfile);
	}

	/*
	=================
	Hunk_SmallLog
	=================
	*/
	private static void Hunk_SmallLog( ) 
	{
		hunkblock_t	*block, *block2;
		char		buf[4096];
		int size, locsize, numBlocks;

		if (!logfile || !files.FS_Initialized())
			return;
		for (block = hunkblocks ; block; block = block.next) {
			block.printed = false;
		}
		size = 0;
		numBlocks = 0;
		Com_sprintf(buf, sizeof(buf), "\r\n================\r\nHunk Small log\r\n================\r\n");
		files.FS_Write(buf, (int)strlen(buf), logfile);
		for (block = hunkblocks; block; block = block.next) {
			if (block.printed) {
				continue;
			}
			locsize = block.size;
			for (block2 = block.next; block2; block2 = block2.next) {
				if (block.line != block2.line) {
					continue;
				}
				if (Q_stricmp(block.file, block2.file)) {
					continue;
				}
				size += block2.size;
				locsize += block2.size;
				block2.printed = true;
			}
			size += block.size;
			numBlocks++;
		}
		Com_sprintf(buf, sizeof(buf), "%d Hunk memory\r\n", size);
		files.FS_Write(buf, (int)strlen(buf), logfile);
		Com_sprintf(buf, sizeof(buf), "%d hunk blocks\r\n", numBlocks);
		files.FS_Write(buf, (int)strlen(buf), logfile);
	}

	/*
	=================
	Com_InitZoneMemory
	=================
	*/
	private static void Com_InitHunkMemory( ) 
	{
		cvar_t	*cv;
		int nMinAlloc;
		char *pMsg = NULL;

		// make sure the file system has allocated and "not" freed any temp blocks
		// this allows the config and product id files ( journal files too ) to be loaded
		// by the file system without redunant routines in the file system utilizing different 
		// memory systems
		if (files.FS_LoadStack() != 0) {
			Com_Error( errorParm_t.ERR_FATAL, "Hunk initialization failed. File system load stack not zero");
		}

		// allocate the stack based hunk allocator
		cv = Cvar.Cvar_Get( "com_hunkMegs", DEF_COMHUNKMEGS, CVAR_LATCH | CVAR_ARCHIVE );

		// if we are not dedicated min allocation is 56, otherwise min is 1
		if (com_dedicated && com_dedicated.integer) {
			nMinAlloc = MIN_DEDICATED_COMHUNKMEGS;
			pMsg = "Minimum com_hunkMegs for a dedicated server is %i, allocating %i megs.\n";
		}
		else {
			nMinAlloc = MIN_COMHUNKMEGS;
			pMsg = "Minimum com_hunkMegs is %i, allocating %i megs.\n";
		}

		if ( cv.integer < nMinAlloc ) {
			s_hunkTotal = 1024 * 1024 * nMinAlloc;
			Com_Printf(pMsg, nMinAlloc, s_hunkTotal / (1024 * 1024));
		} else {
			s_hunkTotal = cv.integer * 1024 * 1024;
		}


		// bk001205 - was malloc
		s_hunkData = (byte*) calloc( s_hunkTotal + 31, 1 );
		if ( !s_hunkData ) {
			Com_Error( errorParm_t.ERR_FATAL, "Hunk data failed to allocate %i megs", s_hunkTotal / (1024*1024) );
		}
		// cacheline align
		s_hunkData = (byte *) ( ( (intptr_t)s_hunkData + 31 ) & ~31 );
		Hunk_Clear();

		cmd.Cmd_AddCommand( "meminfo", Com_Meminfo_f );
	}

	/*
	====================
	Hunk_MemoryRemaining
	====================
	*/
	private static int	Hunk_MemoryRemaining( ) 
	{
		int		low, high;

		low = hunk_low.permanent > hunk_low.temp ? hunk_low.permanent : hunk_low.temp;
		high = hunk_high.permanent > hunk_high.temp ? hunk_high.permanent : hunk_high.temp;

		return s_hunkTotal - ( low + high );
	}

	/*
	===================
	Hunk_SetMark

	The server calls this after the level and game VM have been loaded
	===================
	*/
	private static void Hunk_SetMark( ) 
	{
		hunk_low.mark = hunk_low.permanent;
		hunk_high.mark = hunk_high.permanent;
	}

	/*
	=================
	Hunk_ClearToMark

	The client calls this before starting a vid_restart or snd_restart
	=================
	*/
	private static void Hunk_ClearToMark( ) 
	{
		hunk_low.permanent = hunk_low.temp = hunk_low.mark;
		hunk_high.permanent = hunk_high.temp = hunk_high.mark;
	}

	/*
	=================
	Hunk_CheckMark
	=================
	*/
	private static bool Hunk_CheckMark( )
	{
		if( hunk_low.mark || hunk_high.mark ) {
			return true;
		}
		return false;
	}

	/*
	=================
	Hunk_Clear

	The server calls this before shutting down or loading a new map
	=================
	*/
	private static void Hunk_Clear( ) 
	{

	#if DEDICATED
		SV_ShutdownGameProgs();
	#else
		CL_ShutdownCGame();
		CL_ShutdownUI();
		SV_ShutdownGameProgs();
		CIN_CloseAllVideos();
	#endif
		hunk_low.mark = 0;
		hunk_low.permanent = 0;
		hunk_low.temp = 0;
		hunk_low.tempHighwater = 0;

		hunk_high.mark = 0;
		hunk_high.permanent = 0;
		hunk_high.temp = 0;
		hunk_high.tempHighwater = 0;

		hunk_permanent = &hunk_low;
		hunk_temp = &hunk_high;

		Com_Printf( "Hunk_Clear: reset the hunk ok\n" );
		VM_Clear();
	}

	private static void Hunk_SwapBanks( )
	{
		hunkUsed_t	*swap;

		// can't swap banks if there is any temp already allocated
		if ( hunk_temp.temp != hunk_temp.permanent ) {
			return;
		}

		// if we have a larger highwater mark on this side, start making
		// our permanent allocations here and use the other side for temp
		if ( hunk_temp.tempHighwater - hunk_temp.permanent >
			hunk_permanent.tempHighwater - hunk_permanent.permanent ) {
			swap = hunk_temp;
			hunk_temp = hunk_permanent;
			hunk_permanent = swap;
		}
	}

	/*
	=================
	Hunk_Alloc

	Allocate permanent (until the hunk is cleared) memory
	=================
	*/
	private static void *Hunk_Alloc( int size, ha_pref preference ) 
	{
		void	*buf;

		if ( s_hunkData == NULL)
		{
			Com_Error( errorParm_t.ERR_FATAL, "Hunk_Alloc: Hunk memory system not initialized" );
		}

		// can't do preference if there is any temp allocated
		if (preference == h_dontcare || hunk_temp.temp != hunk_temp.permanent) {
			Hunk_SwapBanks();
		} else {
			if (preference == h_low && hunk_permanent != &hunk_low) {
				Hunk_SwapBanks();
			} else if (preference == h_high && hunk_permanent != &hunk_high) {
				Hunk_SwapBanks();
			}
		}

		// round to cacheline
		size = (size+31)&~31;

		if ( hunk_low.temp + hunk_high.temp + size > s_hunkTotal ) {
			Com_Error( errorParm_t.ERR_DROP, "Hunk_Alloc failed on %i", size );
		}

		if ( hunk_permanent == &hunk_low ) {
			buf = (void *)(s_hunkData + hunk_permanent.permanent);
			hunk_permanent.permanent += size;
		} else {
			hunk_permanent.permanent += size;
			buf = (void *)(s_hunkData + s_hunkTotal - hunk_permanent.permanent );
		}

		hunk_permanent.temp = hunk_permanent.permanent;

		Com_Memset( buf, 0, size );

		return buf;
	}

	/*
	=================
	Hunk_AllocateTempMemory

	This is used by the file loading system.
	Multiple files can be loaded in temporary memory.
	When the files-in-use count reaches zero, all temp memory will be deleted
	=================
	*/
	private static void *Hunk_AllocateTempMemory( int size )
	{
		void		*buf;
		hunkHeader_t	*hdr;

		// return a Z_Malloc'd block if the hunk has not been initialized
		// this allows the config and product id files ( journal files too ) to be loaded
		// by the file system without redunant routines in the file system utilizing different 
		// memory systems
		if ( s_hunkData == NULL )
		{
			return Z_Malloc(size);
		}

		Hunk_SwapBanks();

		size = ( (size+3)&~3 ) + sizeof( hunkHeader_t );

		if ( hunk_temp.temp + hunk_permanent.permanent + size > s_hunkTotal ) {
			Com_Error( errorParm_t.ERR_DROP, "Hunk_AllocateTempMemory: failed on %i", size );
		}

		if ( hunk_temp == &hunk_low ) {
			buf = (void *)(s_hunkData + hunk_temp.temp);
			hunk_temp.temp += size;
		} else {
			hunk_temp.temp += size;
			buf = (void *)(s_hunkData + s_hunkTotal - hunk_temp.temp );
		}

		if ( hunk_temp.temp > hunk_temp.tempHighwater ) {
			hunk_temp.tempHighwater = hunk_temp.temp;
		}

		hdr = (hunkHeader_t *)buf;
		buf = (void *)(hdr+1);

		hdr.magic = HUNK_MAGIC;
		hdr.size = size;

		// don't bother clearing, because we are going to load a file over it
		return buf;
	}


	/*
	==================
	Hunk_FreeTempMemory
	==================
	*/
	private static void Hunk_FreeTempMemory( void *buf )
	{
		hunkHeader_t	*hdr;

		  // free with Z_Free if the hunk has not been initialized
		  // this allows the config and product id files ( journal files too ) to be loaded
		  // by the file system without redunant routines in the file system utilizing different 
		  // memory systems
		if ( s_hunkData == NULL )
		{
			Z_Free(buf);
			return;
		}


		hdr = ( (hunkHeader_t *)buf ) - 1;
		if ( hdr.magic != HUNK_MAGIC ) {
			Com_Error( errorParm_t.ERR_FATAL, "Hunk_FreeTempMemory: bad magic" );
		}

		hdr.magic = HUNK_FREE_MAGIC;

		// this only works if the files are freed in stack order,
		// otherwise the memory will stay around until Hunk_ClearTempMemory
		if ( hunk_temp == &hunk_low ) {
			if ( hdr == (void *)(s_hunkData + hunk_temp.temp - hdr.size ) ) {
				hunk_temp.temp -= hdr.size;
			} else {
				Com_Printf( "Hunk_FreeTempMemory: not the final block\n" );
			}
		} else {
			if ( hdr == (void *)(s_hunkData + s_hunkTotal - hunk_temp.temp ) ) {
				hunk_temp.temp -= hdr.size;
			} else {
				Com_Printf( "Hunk_FreeTempMemory: not the final block\n" );
			}
		}
	}


	/*
	=================
	Hunk_ClearTempMemory

	The temp space is no longer needed.  If we have left more
	touched but unused memory on this side, have future
	permanent allocs use this side.
	=================
	*/
	private static void Hunk_ClearTempMemory( ) 
	{
		if ( s_hunkData != null ) 
		{
			hunk_temp.temp = hunk_temp.permanent;
		}
	}

	/*
	=================
	Hunk_Trash
	=================
	*/
	private static void Hunk_Trash( ) 
	{
		int length, i, rnd;
		char *buf, value;

		return;

		if ( s_hunkData == NULL )
			return;

		Cvar.Cvar_Set("com_jp", "1");
		Hunk_SwapBanks();

		if ( hunk_permanent == &hunk_low ) {
			buf = (char*) (void *)(s_hunkData + hunk_permanent.permanent);
		} else {
			buf = (char*) (void *)(s_hunkData + s_hunkTotal - hunk_permanent.permanent );
		}
		length = hunk_permanent.permanent;

		if (length > 0x7FFFF) {
			//randomly trash data within buf
			rnd = random() * (length - 0x7FFFF);
			value = 31;
			for (i = 0; i < 0x7FFFF; i++) {
				value *= 109;
				buf[rnd+i] ^= value;
			}
		}
	}

	/*
	===================================================================

	EVENTS AND JOURNALING

	In addition to these events, .cfg files are also copied to the
	journaled file
	===================================================================
	*/

	// bk001129 - here we go again: upped from 64
	// FIXME TTimo blunt upping from 256 to 1024
	// https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=5
	static ContextStaticAttribute int MAX_PUSHED_EVENTS = 1024;
	// bk001129 - init, also static
	static int com_pushedEventsHead = 0;
	static int com_pushedEventsTail = 0;
	// bk001129 - static
	static sysEvent_t	com_pushedEvents[MAX_PUSHED_EVENTS];

	/*
	=================
	Com_InitJournaling
	=================
	*/
	private static void Com_InitJournaling( ) 
	{
		Com_StartupVariable( "journal" );
		com_journal = Cvar.Cvar_Get ("journal", "0", CVAR_INIT);
		if ( !com_journal.integer ) {
			return;
		}

		if ( com_journal.integer == 1 ) {
			Com_Printf( "Journaling events\n");
			com_journalFile = files.FS_FOpenFileWrite( "journal.dat" );
			com_journalDataFile = files.FS_FOpenFileWrite( "journaldata.dat" );
		} else if ( com_journal.integer == 2 ) {
			Com_Printf( "Replaying journaled events\n");
			files.FS_FOpenFileRead( "journal.dat", &com_journalFile, true );
			files.FS_FOpenFileRead( "journaldata.dat", &com_journalDataFile, true );
		}

		if ( !com_journalFile || !com_journalDataFile ) {
			Cvar.Cvar_Set( "com_journal", "0" );
			com_journalFile = 0;
			com_journalDataFile = 0;
			Com_Printf( "Couldn't open journal files\n" );
		}
	}

	/*
	=================
	Com_GetRealEvent
	=================
	*/
	private static sysEvent_t Com_GetRealEvent( ) 
	{
		int			r;
		sysEvent_t	ev;

		// either get an event from the system or the journal file
		if ( com_journal.integer == 2 ) {
			r = files.FS_Read( &ev, sizeof(ev), com_journalFile );
			if ( r != sizeof(ev) ) {
				Com_Error( errorParm_t.ERR_FATAL, "Error reading from journal file" );
			}
			if ( ev.evPtrLength ) {
				ev.evPtr = Z_Malloc( ev.evPtrLength );
				r = files.FS_Read( ev.evPtr, ev.evPtrLength, com_journalFile );
				if ( r != ev.evPtrLength ) {
					Com_Error( errorParm_t.ERR_FATAL, "Error reading from journal file" );
				}
			}
		} else {
			ev = Sys_GetEvent();

			// write the journal value out if needed
			if ( com_journal.integer == 1 ) {
				r = files.FS_Write( &ev, sizeof(ev), com_journalFile );
				if ( r != sizeof(ev) ) {
					Com_Error( errorParm_t.ERR_FATAL, "Error writing to journal file" );
				}
				if ( ev.evPtrLength ) {
					r = files.FS_Write( ev.evPtr, ev.evPtrLength, com_journalFile );
					if ( r != ev.evPtrLength ) {
						Com_Error( errorParm_t.ERR_FATAL, "Error writing to journal file" );
					}
				}
			}
		}

		return ev;
	}


	/*
	=================
	Com_InitPushEvent
	=================
	*/
	// bk001129 - added
	private static void Com_InitPushEvent( ) 
	{
	  // clear the static buffer array
	  // this requires SE_NONE to be accepted as a valid but NOP event
	  memset( com_pushedEvents, 0, sizeof(com_pushedEvents) );
	  // reset counters while we are at it
	  // beware: GetEvent might still return an SE_NONE from the buffer
	  com_pushedEventsHead = 0;
	  com_pushedEventsTail = 0;
	}


	/*
	=================
	Com_PushEvent
	=================
	*/
	private static void Com_PushEvent( sysEvent_t *event ) {
		sysEvent_t		*ev;
		static int printedWarning = 0; // bk001129 - init, bk001204 - explicit int

		ev = &com_pushedEvents[ com_pushedEventsHead & (MAX_PUSHED_EVENTS-1) ];

		if ( com_pushedEventsHead - com_pushedEventsTail >= MAX_PUSHED_EVENTS ) {

			// don't print the warning constantly, or it can give time for more...
			if ( !printedWarning ) {
				printedWarning = true;
				Com_Printf( "WARNING: Com_PushEvent overflow\n" );
			}

			if ( ev.evPtr ) {
				Z_Free( ev.evPtr );
			}
			com_pushedEventsTail++;
		} else {
			printedWarning = false;
		}

		*ev = *event;
		com_pushedEventsHead++;
	}

	/*
	=================
	Com_GetEvent
	=================
	*/
	sysEvent_t	Com_GetEvent( void ) {
		if ( com_pushedEventsHead > com_pushedEventsTail ) {
			com_pushedEventsTail++;
			return com_pushedEvents[ (com_pushedEventsTail-1) & (MAX_PUSHED_EVENTS-1) ];
		}
		return Com_GetRealEvent();
	}

	/*
	=================
	Com_RunAndTimeServerPacket
	=================
	*/
	private static void Com_RunAndTimeServerPacket( netadr_t *evFrom, msg_t *buf ) {
		int		t1, t2, msec;

		t1 = 0;

		if ( com_speeds.integer ) {
			t1 = Sys_Milliseconds ();
		}

		SV_PacketEvent( *evFrom, buf );

		if ( com_speeds.integer ) {
			t2 = Sys_Milliseconds ();
			msec = t2 - t1;
			if ( com_speeds.integer == 3 ) {
				Com_Printf( "SV_PacketEvent time: %i\n", msec );
			}
		}
	}

	/*
	=================
	Com_EventLoop

	Returns last event time
	=================
	*/
	public static int Com_EventLoop( ) {
		sysEvent_t	ev;
		netadr_t	evFrom;
		byte		bufData[MAX_MSGLEN];
		msg_t		buf;

		MSG_Init( &buf, bufData, sizeof( bufData ) );

		while ( 1 ) {
			ev = Com_GetEvent();

			// if no more events are available
			if ( ev.evType == SE_NONE ) {
				// manually send packet events for the loopback channel
				while ( NET_GetLoopPacket( NS_CLIENT, &evFrom, &buf ) ) {
					CL_PacketEvent( evFrom, &buf );
				}

				while ( NET_GetLoopPacket( NS_SERVER, &evFrom, &buf ) ) {
					// if the server just shut down, flush the events
					if ( com_sv_running.integer ) {
						Com_RunAndTimeServerPacket( &evFrom, &buf );
					}
				}

				return ev.evTime;
			}


			switch ( ev.evType ) {
			default:
			  // bk001129 - was ev.evTime
				Com_Error( errorParm_t.ERR_FATAL, "Com_EventLoop: bad event type %i", ev.evType );
				break;
			case SE_NONE:
				break;
			case SE_KEY:
				CL_KeyEvent( ev.evValue, (bool) ev.evValue2, ev.evTime );
				break;
			case SE_CHAR:
				CL_CharEvent( ev.evValue );
				break;
			case SE_MOUSE:
				CL_MouseEvent( ev.evValue, ev.evValue2, ev.evTime );
				break;
			case SE_JOYSTICK_AXIS:
				CL_JoystickEvent( ev.evValue, ev.evValue2, ev.evTime );
				break;
			case SE_CONSOLE:
				Cbuf_AddText( (char *)ev.evPtr );
				Cbuf_AddText( "\n" );
				break;
			case SE_PACKET:
				// this cvar allows simulation of connections that
				// drop a lot of packets.  Note that loopback connections
				// don't go through here at all.
				if ( com_dropsim.value > 0 ) {
					static int seed;

					if ( Q_random( &seed ) < com_dropsim.value ) {
						break;		// drop this packet
					}
				}

				evFrom = *(netadr_t *)ev.evPtr;
				buf.cursize = ev.evPtrLength - sizeof( evFrom );

				// we must copy the contents of the message out, because
				// the event buffers are only large enough to hold the
				// exact payload, but channel messages need to be large
				// enough to hold fragment reassembly
				if ( (unsigned)buf.cursize > buf.maxsize ) {
					Com_Printf("Com_EventLoop: oversize packet\n");
					continue;
				}
				Com_Memcpy( buf.data, (byte *)((netadr_t *)ev.evPtr + 1), buf.cursize );
				if ( com_sv_running.integer ) {
					Com_RunAndTimeServerPacket( &evFrom, &buf );
				} else {
					CL_PacketEvent( evFrom, &buf );
				}
				break;
			}

			// free any block data
			if ( ev.evPtr ) {
				Z_Free( ev.evPtr );
			}
		}

		return 0;	// never reached
	}

	/*
	================
	Com_Milliseconds

	Can be used for profiling, but will be journaled accurately
	================
	*/
	private static int Com_Milliseconds () {
		sysEvent_t	ev;

		// get events and push them until we get a null event with the current time
		do {

			ev = Com_GetRealEvent();
			if ( ev.evType != SE_NONE ) {
				Com_PushEvent( &ev );
			}
		} while ( ev.evType != SE_NONE );
	
		return ev.evTime;
	}

	//============================================================================

	/*
	=============
	Com_Error_f

	Just throw a fatal error to
	test error shutdown procedures
	=============
	*/
	public static void Com_Error_f () 
	{
		if ( cmd.Cmd_Argc() > 1 ) 
			Com_Error( errorParm_t.ERR_DROP, "Testing drop error" );
		else 
			Com_Error( errorParm_t.ERR_FATAL, "Testing fatal error" );
	}


	/*
	=============
	Com_Freeze_f

	Just freeze in place for a given number of seconds to test
	error recovery
	=============
	*/
	private static void Com_Freeze_f () {
		float	s;
		int		start, now;

		if ( cmd.Cmd_Argc() != 2 ) {
			Com_Printf( "freeze <seconds>\n" );
			return;
		}

		float.TryParse( cmd.Cmd_Argv(1), out s );

		start = Com_Milliseconds();

		while ( true ) {
			now = Com_Milliseconds();
			if ( ( now - start ) * 0.001 > s ) {
				break;
			}
		}
	}

	/*
	=================
	Com_Crash_f

	A way to force a bus error for development reasons
	=================
	*/
	private static void Com_Crash_f( ) {
		//* ( int * ) 0 = 0x12345678;
	}


	/*
	=================
	Com_Init
	=================
	*/
	public static void Com_Init( string commandLine ) 
	{
		string s;

		Com_Printf( "%s %s %s\n", q_shared.Q3_VERSION, q_shared.CPUSTRING, DateTime.Now );

		if ( setjmp (abortframe) ) {
			Sys_Error ("Error during initialization");
		}

		// bk001129 - do this before anything else decides to push events
		Com_InitPushEvent();

		Com_InitSmallZoneMemory();
		Cvar.Cvar_Init ();

		// prepare enough of the subsystems to handle
		// cvar and command buffer management
		Com_ParseCommandLine( ref commandLine );

	//	Swap_Init ();
		cmd.Cbuf_Init ();

		Com_InitZoneMemory();
		cmd.Cmd_Init ();

		// override anything from the config files with command line args
		Com_StartupVariable( null );

		// get the developer cvar set as early as possible
		Com_StartupVariable( "developer" );

		// done early so bind command exists
		CL_InitKeyCommands();

		files.FS_InitFilesystem ();

		Com_InitJournaling();

		cmd.Cbuf_AddText ("exec default.cfg\n");

		// skip the q3config.cfg if "safe" is on the command line
		if ( !Com_SafeMode() ) {
			cmd.Cbuf_AddText ("exec q3config.cfg\n");
		}

		cmd.Cbuf_AddText ("exec autoexec.cfg\n");

		cmd.Cbuf_Execute ();

		// override anything from the config files with command line args
		Com_StartupVariable( null );

	  // get dedicated here for proper hunk megs initialization
	#if DEDICATED
		com_dedicated = Cvar.Cvar_Get ("dedicated", "1", CVAR.ROM);
	#else
		com_dedicated = Cvar.Cvar_Get ("dedicated", "0", CVAR.LATCH);
	#endif
		// allocate the stack based hunk allocator
		Com_InitHunkMemory();

		// if any archived cvars are modified after this, we will trigger a writing
		// of the config file
		cvar_modifiedFlags &= ~CVAR.ARCHIVE;

		//
		// init commands and vars
		//
		com_maxfps = Cvar.Cvar_Get ("com_maxfps", "85", CVAR.ARCHIVE);
		com_blood = Cvar.Cvar_Get ("com_blood", "1", CVAR.ARCHIVE);

		com_developer = Cvar.Cvar_Get ("developer", "0", CVAR.TEMP );
		com_logfile = Cvar.Cvar_Get ("logfile", "0", CVAR.TEMP );

		com_timescale = Cvar.Cvar_Get ("timescale", "1", CVAR.CHEAT | CVAR.SYSTEMINFO );
		com_fixedtime = Cvar.Cvar_Get ("fixedtime", "0", CVAR.CHEAT);
		com_showtrace = Cvar.Cvar_Get ("com_showtrace", "0", CVAR.CHEAT);
		com_dropsim = Cvar.Cvar_Get ("com_dropsim", "0", CVAR.CHEAT);
		com_viewlog = Cvar.Cvar_Get( "viewlog", "0", CVAR.CHEAT );
		com_speeds = Cvar.Cvar_Get ("com_speeds", "0", 0);
		com_timedemo = Cvar.Cvar_Get ("timedemo", "0", CVAR.CHEAT);
		com_cameraMode = Cvar.Cvar_Get ("com_cameraMode", "0", CVAR.CHEAT);

		cl_paused = Cvar.Cvar_Get ("cl_paused", "0", CVAR.ROM);
		sv_paused = Cvar.Cvar_Get ("sv_paused", "0", CVAR.ROM);
		com_sv_running = Cvar.Cvar_Get ("sv_running", "0", CVAR.ROM);
		com_cl_running = Cvar.Cvar_Get ("cl_running", "0", CVAR.ROM);
		com_buildScript = Cvar.Cvar_Get( "com_buildScript", "0", 0 );

		com_introPlayed = Cvar.Cvar_Get( "com_introplayed", "0", CVAR.ARCHIVE);

		if ( com_dedicated.integer == 1 ) {
			if ( com_viewlog.integer == 0 ) {
				Cvar.Cvar_Set( "viewlog", "1" );
			}
		}

		if ( com_developer?.integer == 1 ) {
			cmd.Cmd_AddCommand ("error", Com_Error_f);
			cmd.Cmd_AddCommand ("crash", Com_Crash_f );
			cmd.Cmd_AddCommand ("freeze", Com_Freeze_f);
		}
		cmd.Cmd_AddCommand ("quit", Com_Quit_f);
		cmd.Cmd_AddCommand ("changeVectors", MSG_ReportChangeVectors_f );
		cmd.Cmd_AddCommand ("writeconfig", Com_WriteConfig_f );

		s = va("%s %s %s", Q3_VERSION, CPUSTRING, __DATE__ );
		com_version = Cvar.Cvar_Get ("version", s, CVAR.ROM | CVAR.SERVERINFO );

		Sys_Init();
		Netchan_Init( Com_Milliseconds() & 0xffff );	// pick a port value that should be nice and random
		VM_Init();
		SV_Init();

		com_dedicated.modified = false;
		if ( com_dedicated.integer == 0 ) {
			CL_Init();
			Sys_ShowConsole( com_viewlog.integer, false );
		}

		// set com_frameTime so that if a map is started on the
		// command line it will still be able to count on com_frameTime
		// being random enough for a serverid
		com_frameTime = Com_Milliseconds();

		// add + commands from command line
		if ( !Com_AddStartupCommands() ) {
			// if the user didn't give any commands, run default action
			if ( com_dedicated.integer == 0 ) {
				cmd.Cbuf_AddText ("cinematic idlogo.RoQ\n");
				if( com_introPlayed.integer == 0 ) {
					Cvar.Cvar_Set( com_introPlayed.name, "1" );
					Cvar.Cvar_Set( "nextmap", "cinematic intro.RoQ" );
				}
			}
		}

		// start in full screen ui mode
		Cvar.Cvar_Set("r_uiFullScreen", "1");

		CL_StartHunkUsers();

		// make sure single player is off by default
		Cvar.Cvar_Set("ui_singlePlayerActive", "0");

		com_fullyInitialized = true;
		Com_Printf ("--- Common Initialization Complete ---\n");	
	}

	//==================================================================

	private static void Com_WriteConfigToFile( string filename )
	{
		fileHandle_t	f;

		f = files.FS_FOpenFileWrite( filename );
		if ( f.ID == 0 ) {
			Com_Printf ("Couldn't write %s.\n", filename );
			return;
		}

		files.FS_Printf (f, "// generated by quake, do not modify\n");
		Key_WriteBindings (f);
		Cvar.Cvar_WriteVariables (f);
		files.FS_FCloseFile( f );
	}


	/*
	===============
	Com_WriteConfiguration

	Writes key bindings and archived cvars to config file if modified
	===============
	*/
	private static void Com_WriteConfiguration( ) 
	{
		// if we are quiting without fully initializing, make sure
		// we don't write out anything
		if ( !com_fullyInitialized ) {
			return;
		}

		if ( !(cvar_modifiedFlags & CVAR.ARCHIVE ) ) {
			return;
		}
		cvar_modifiedFlags &= ~CVAR.ARCHIVE;

		Com_WriteConfigToFile( "q3config.cfg" );
	}


	/*
	===============
	Com_WriteConfig_f

	Write the config file to a specific name
	===============
	*/
	private static void Com_WriteConfig_f( ) 
	{
		if ( cmd.Cmd_Argc() != 2 ) {
			Com_Printf( "Usage: writeconfig <filename>\n" );
			return;
		}

		Q_strncpyz( out var filename, cmd.Cmd_Argv(1), q_shared.MAX_QPATH );
		COM_DefaultExtension( filename, q_shared.MAX_QPATH, ".cfg" );
		Com_Printf( "Writing %s.\n", filename );
		Com_WriteConfigToFile( filename );
	}

	/*
	================
	Com_ModifyMsec
	================
	*/
	private static int Com_ModifyMsec( ref int msec ) 
	{
		int		clampTime;

		//
		// modify time for debugging values
		//
		if ( com_fixedtime.integer == 1 ) {
			msec = com_fixedtime.integer;
		} else if ( com_timescale.value == 1 ) {
			msec *= com_timescale.value;
		} else if (com_cameraMode.integer == 1 ) {
			msec *= com_timescale.value;
		}
	
		// don't let it scale below 1 msec
		if ( msec < 1 && com_timescale.value == 1 ) {
			msec = 1;
		}

		if ( com_dedicated.integer == 1 ) {
			// dedicated servers don't want to clamp for a much longer
			// period, because it would mess up all the client's views
			// of time.
			if ( msec > 500 ) {
				Com_Printf( "Hitch warning: %i msec frame time\n", msec );
			}
			clampTime = 5000;
		} else 
		if ( com_sv_running.integer == 0 ) {
			// clients of remote servers do not want to clamp time, because
			// it would skew their view of the server's time temporarily
			clampTime = 5000;
		} else {
			// for local single player gaming
			// we may want to clamp the time to prevent players from
			// flying off edges when something hitches.
			clampTime = 200;
		}

		if ( msec > clampTime ) {
			msec = clampTime;
		}

		return msec;
	}

	/*
	=================
	Com_Frame
	=================
	*/
	public static void Com_Frame( ) 
	{

		int		msec, minMsec;
		static int	lastTime;
		int key;
 
		int		timeBeforeFirstEvents;
		int           timeBeforeServer;
		int           timeBeforeEvents;
		int           timeBeforeClient;
		int           timeAfter;
  




		if ( setjmp (abortframe) ) {
			return;			// an ERR_DROP was thrown
		}

		// bk001204 - init to zero.
		//  also:  might be clobbered by `longjmp' or `vfork'
		timeBeforeFirstEvents =0;
		timeBeforeServer =0;
		timeBeforeEvents =0;
		timeBeforeClient = 0;
		timeAfter = 0;


		// old net chan encryption key
		key = (int)(0x87243987);

		// write config file if anything changed
		Com_WriteConfiguration(); 

		// if "viewlog" has been modified, show or hide the log console
		if ( com_viewlog.modified ) {
			if ( com_dedicated.value == 0 ) {
				Sys_ShowConsole( com_viewlog.integer, false );
			}
			com_viewlog.modified = false;
		}

		//
		// main event loop
		//
		if ( com_speeds.integer == 1 ) {
			timeBeforeFirstEvents = Sys_Milliseconds ();
		}

		// we may want to spin here if things are going too fast
		if ( com_dedicated.integer == 0 && com_maxfps.integer > 0 && com_timedemo.integer == 0 ) {
			minMsec = 1000 / com_maxfps.integer;
		} else {
			minMsec = 1;
		}
		do {
			com_frameTime = Com_EventLoop();
			if ( lastTime > com_frameTime ) {
				lastTime = com_frameTime;		// possible on first frame
			}
			msec = com_frameTime - lastTime;
		} while ( msec < minMsec );
		Cbuf_Execute ();

		lastTime = com_frameTime;

		// mess with msec if needed
		com_frameMsec = msec;
		msec = Com_ModifyMsec( msec );

		//
		// server side
		//
		if ( com_speeds.integer == 1 ) {
			timeBeforeServer = Sys_Milliseconds ();
		}

		SV_Frame( msec );

		// if "dedicated" has been modified, start up
		// or shut down the client system.
		// Do this after the server may have started,
		// but before the client tries to auto-connect
		if ( com_dedicated.modified ) {
			// get the latched value
			Cvar.Cvar_Get( "dedicated", "0", 0 );
			com_dedicated.modified = false;
			if ( com_dedicated.integer == 0 ) {
				CL_Init();
				Sys_ShowConsole( com_viewlog.integer, false );
			} else {
				CL_Shutdown();
				Sys_ShowConsole( 1, true );
			}
		}

		//
		// client system
		//
		if ( com_dedicated.integer == 0 ) {
			//
			// run event loop a second time to get server to client packets
			// without a frame of latency
			//
			if ( com_speeds.integer == 1 ) {
				timeBeforeEvents = Sys_Milliseconds ();
			}
			Com_EventLoop();
			Cbuf_Execute ();


			//
			// client side
			//
			if ( com_speeds.integer == 1 ) {
				timeBeforeClient = Sys_Milliseconds ();
			}

			CL_Frame( msec );

			if ( com_speeds.integer == 1 ) {
				timeAfter = Sys_Milliseconds ();
			}
		}

		//
		// report timing information
		//
		if ( com_speeds.integer == 1 ) {
			int			all, sv, ev, cl;

			all = timeAfter - timeBeforeServer;
			sv = timeBeforeEvents - timeBeforeServer;
			ev = timeBeforeServer - timeBeforeFirstEvents + timeBeforeClient - timeBeforeEvents;
			cl = timeAfter - timeBeforeClient;
			sv -= time_game;
			cl -= time_frontend + time_backend;

			Com_Printf ("frame:%i all:%3i sv:%3i ev:%3i cl:%3i gm:%3i rf:%3i bk:%3i\n", 
						 com_frameNumber, all, sv, ev, cl, time_game, time_frontend, time_backend );
		}	

		//
		// trace optimization tracking
		//
		if ( com_showtrace.integer == 1 ) {
	
			extern	int c_traces, c_brush_traces, c_patch_traces;
			extern	int	c_pointcontents;

			Com_Printf ("%4i traces  (%ib %ip) %4i points\n", c_traces,
				c_brush_traces, c_patch_traces, c_pointcontents);
			c_traces = 0;
			c_brush_traces = 0;
			c_patch_traces = 0;
			c_pointcontents = 0;
		}

		// old net chan encryption key
		key = ( int ) ( lastTime * 0x87243987 );

		com_frameNumber++;
	}

	/*
	=================
	Com_Shutdown
	=================
	*/
	public static void Com_Shutdown () 
	{
		if (logfile > 0) {
			files.FS_FCloseFile ( new fileHandle_t { ID = logfile } );
			logfile = 0;
		}

		if ( com_journalFile > 0 ) {
			files.FS_FCloseFile( new fileHandle_t { ID = com_journalFile } );
			com_journalFile = 0;
		}

	}

	public static void Com_Memcpy (Array dest, Array src, int count)
	{
		Buffer.BlockCopy( src, 0, dest, 0, count );
	}

	public static void Com_Memset (Array dest, int val, int count)
	{
		var values = new int[count];
		Array.Fill( values, val );
		dest = values;
	}

	//------------------------------------------------------------------------


	/*
	=====================
	Q_acos

	the msvc acos doesn't always return a value between -PI and PI:

	int i;
	i = 1065353246;
	acos(*(float*) &i) == -1.#IND0

		This should go in q_math but it is too late to add new traps
		to game and ui
	=====================
	*/
	public static float Q_acos(float c) {
		float angle;

		angle = MathF.Acos(c);

		if (angle > M_PI) {
			return (float)M_PI;
		}
		if (angle < -M_PI) {
			return (float)M_PI;
		}
		return angle;
	}

	/*
	===========================================
	command line completion
	===========================================
	*/

	/*
	==================
	Field_Clear
	==================
	*/
	private static void Field_Clear( field_t *edit ) {
	  memset(edit.buffer, 0, MAX_EDIT_LINE);
		edit.cursor = 0;
		edit.scroll = 0;
	}

	private static const char *completionString;
	private static char shortestMatch[MAX_TOKEN_CHARS];
	private static int	matchCount;
	// field we are working on, passed to Field_CompleteCommand (&g_consoleCommand for instance)
	private static field_t *completionField;

	/*
	===============
	FindMatches

	===============
	*/
	private static void FindMatches( const char *s ) {
		int		i;

		if ( Q_stricmpn( s, completionString, (int)strlen( completionString ) ) ) {
			return;
		}
		matchCount++;
		if ( matchCount == 1 ) {
			Q_strncpyz( shortestMatch, s, sizeof( shortestMatch ) );
			return;
		}

		// cut shortestMatch to the amount common with s
		for ( i = 0 ; s[i] ; i++ ) {
			if ( tolower(shortestMatch[i]) != tolower(s[i]) ) {
				shortestMatch[i] = 0;
			}
		}
	}

	/*
	===============
	PrintMatches

	===============
	*/
	private static void PrintMatches( const char *s ) {
		if ( !Q_stricmpn( s, shortestMatch, (int)strlen( shortestMatch ) ) ) {
			Com_Printf( "    %s\n", s );
		}
	}

	private static void keyConcatArgs( void ) {
		int		i;
		char	*arg;

		for ( i = 1 ; i < cmd.Cmd_Argc() ; i++ ) {
			q_shared.Q_strcat( completionField.buffer, sizeof( completionField.buffer ), " " );
			arg = cmd.Cmd_Argv( i );
			while (*arg) {
				if (*arg == ' ') {
					q_shared.Q_strcat( completionField.buffer, sizeof( completionField.buffer ),  "\"");
					break;
				}
				arg++;
			}
			q_shared.Q_strcat( completionField.buffer, sizeof( completionField.buffer ),  cmd.Cmd_Argv( i ) );
			if (*arg == ' ') {
				q_shared.Q_strcat( completionField.buffer, sizeof( completionField.buffer ),  "\"");
			}
		}
	}

	private static void ConcatRemaining( const char *src, const char *start ) {
		char *str;

		str = (char*) strstr(src, start);
		if (!str) {
			keyConcatArgs();
			return;
		}

		str += (int)strlen(start);
		q_shared.Q_strcat( completionField.buffer, sizeof( completionField.buffer ), str);
	}

	/*
	===============
	Field_CompleteCommand

	perform Tab expansion
	NOTE TTimo this was originally client code only
	  moved to common code when writing tty console for *nix dedicated server
	===============
	*/
	private static void Field_CompleteCommand( field_t *field ) {
		field_t		temp;

		completionField = field;

		// only look at the first token for completion purposes
		cmd.Cmd_TokenizeString( completionField.buffer );

		completionString = cmd.Cmd_Argv(0);
		if ( completionString[0] == '\\' || completionString[0] == '/' ) {
			completionString++;
		}
		matchCount = 0;
		shortestMatch[0] = 0;

		if ( (int)strlen( completionString ) == 0 ) {
			return;
		}

		cmd.Cmd_CommandCompletion( FindMatches );
		Cvar.Cvar_CommandCompletion( FindMatches );

		if ( matchCount == 0 ) {
			return;	// no matches
		}

		Com_Memcpy(&temp, completionField, sizeof(field_t));

		if ( matchCount == 1 ) {
			Com_sprintf( completionField.buffer, sizeof( completionField.buffer ), "\\%s", shortestMatch );
			if ( cmd.Cmd_Argc() == 1 ) {
				q_shared.Q_strcat( completionField.buffer, sizeof( completionField.buffer ), " " );
			} else {
				ConcatRemaining( temp.buffer, completionString );
			}
			completionField.cursor = (int)strlen( completionField.buffer );
			return;
		}

		// multiple matches, complete to shortest
		Com_sprintf( completionField.buffer, sizeof( completionField.buffer ), "\\%s", shortestMatch );
		completionField.cursor = (int)strlen( completionField.buffer );
		ConcatRemaining( temp.buffer, completionString );

		Com_Printf( "]%s\n", completionField.buffer );

		// run through again, printing matches
		cmd.Cmd_CommandCompletion( PrintMatches );
		Cvar.Cvar_CommandCompletion( PrintMatches );
	}
}
