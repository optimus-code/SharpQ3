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
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpQ3.Engine.qcommon
{
	/*****************************************************************************
	 * name:		files.c
	 *
	 * desc:		handle based filesystem for Quake III Arena 
	 *
	 * $Archive: /MissionPack/code/qcommon/files.c $
	 *
	 *****************************************************************************/
	public struct fileHandle_t
    {
		public int ID;
    }

	public class fileInPack_t
	{
		public string name;			// name of the file
		public ulong pos;			// file info position in zip
		public fileInPack_t next;   // next file in the hash
	}

	public class pack_t
	{
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = q_shared.MAX_OSPATH )]
		public byte[] pakFilename;    // c:\quake3\baseq3\pak0.pk3

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = q_shared.MAX_OSPATH )]
		public byte[] pakBasename;   // pak0

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = q_shared.MAX_OSPATH )]
		public byte[] pakGamename; // baseq3
		public unzFile handle;                     // handle to zip file
		public int checksum;                   // regular checksum
		public int pure_checksum;              // checksum for pure
		public int numfiles;                   // number of files in pk3
		public int referenced;                 // referenced file flags
		public int hashSize;                   // hash table size (power of 2)
		public fileInPack_t[] hashTable;                   // hash table
		public fileInPack_t buildBuffer;               // buffer with the filenames etc.
	}

	public class directory_t
	{
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = q_shared.MAX_OSPATH )]
		public byte[] path;       // c:\quake3

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = q_shared.MAX_OSPATH )]
		public byte[] gamedir; // baseq3
	}

	public class searchpath_t
	{
		public searchpath_t next;

		public pack_t pack;        // only one of pack / dir will be non NULL
		public directory_t dir;
	}

	public static class files
	{
		/*
		=============================================================================

		QUAKE3 FILESYSTEM

		All of Quake's data access is through a hierarchical file system, but the contents of 
		the file system can be transparently merged from several sources.

		A "qpath" is a reference to game file data.  MAX_ZPATH is 256 characters, which must include
		a terminating zero. "..", "\\", and ":" are explicitly illegal in qpaths to prevent any
		references outside the quake directory system.

		The "base path" is the path to the directory holding all the game directories and usually
		the executable.  It defaults to ".", but can be overridden with a "+set fs_basepath c:\quake3"
		command line to allow code debugging in a different directory.  Basepath cannot
		be modified at all after startup.  Any files that are created (demos, screenshots,
		etc) will be created reletive to the base path, so base path should usually be writable.

		The "cd path" is the path to an alternate hierarchy that will be searched if a file
		is not located in the base path.  A user can do a partial install that copies some
		data to a base path created on their hard drive and leave the rest on the cd.  Files
		are never writen to the cd path.  It defaults to a value set by the installer, like
		"e:\quake3", but it can be overridden with "+set ds_cdpath g:\quake3".

		If a user runs the game directly from a CD, the base path would be on the CD.  This
		should still function correctly, but all file writes will fail (harmlessly).

		The "home path" is the path used for all write access. On win32 systems we have "base path"
		== "home path", but on *nix systems the base installation is usually readonly, and
		"home path" points to ~/.q3a or similar

		The user can also install custom mods and content in "home path", so it should be searched
		along with "home path" and "cd path" for game content.


		The "base game" is the directory under the paths where data comes from by default, and
		can be either "baseq3" or "demoq3".

		The "current game" may be the same as the base game, or it may be the name of another
		directory under the paths that should be searched for files before looking in the base game.
		This is the basis for addons.

		Clients automatically set the game directory after receiving a gamestate from a server,
		so only servers need to worry about +set fs_game.

		No other directories outside of the base game and current game will ever be referenced by
		filesystem functions.

		To save disk space and speed loading, directory trees can be collapsed into zip files.
		The files use a ".pk3" extension to prevent users from unzipping them accidentally, but
		otherwise the are simply normal uncompressed zip files.  A game directory can have multiple
		zip files of the form "pak0.pk3", "pak1.pk3", etc.  Zip files are searched in decending order
		from the highest number to the lowest, and will always take precedence over the filesystem.
		This allows a pk3 distributed as a patch to override all existing data.

		Because we will have updated executables freely available online, there is no point to
		trying to restrict demo / oem versions of the game with code changes.  Demo / oem versions
		should be exactly the same executables as release versions, but with different data that
		automatically restricts where game media can come from to prevent add-ons from working.

		After the paths are initialized, quake will look for the product.txt file.  If not
		found and verified, the game will run in restricted mode.  In restricted mode, only 
		files contained in demoq3/pak0.pk3 will be available for loading, and only if the zip header is
		verified to not have been modified.  A single exception is made for q3config.cfg.  Files
		can still be written out in restricted mode, so screenshots and demos are allowed.
		Restricted mode can be tested by setting "+set fs_restrict 1" on the command line, even
		if there is a valid product.txt under the basepath or cdpath.

		If not running in restricted mode, and a file is not found in any local filesystem,
		an attempt will be made to download it and save it under the base path.

		If the "fs_copyfiles" cvar is set to 1, then every time a file is sourced from the cd
		path, it will be copied over to the base path.  This is a development aid to help build
		test releases and to copy working sets over slow network links.

		File search order: when FS_FOpenFileRead gets called it will go through the fs_searchpaths
		structure and stop on the first successful hit. fs_searchpaths is built with successive
		calls to FS_AddGameDirectory

		Additionaly, we search in several subdirectories:
		current game is the current mode
		base game is a variable to allow mods based on other mods
		(such as baseq3 + missionpack content combination in a mod for instance)
		BASEGAME is the hardcoded base game ("baseq3")

		e.g. the qpath "sound/newstuff/test.wav" would be searched for in the following places:

		home path + current game's zip files
		home path + current game's directory
		base path + current game's zip files
		base path + current game's directory
		cd path + current game's zip files
		cd path + current game's directory

		home path + base game's zip file
		home path + base game's directory
		base path + base game's zip file
		base path + base game's directory
		cd path + base game's zip file
		cd path + base game's directory

		home path + BASEGAME's zip file
		home path + BASEGAME's directory
		base path + BASEGAME's zip file
		base path + BASEGAME's directory
		cd path + BASEGAME's zip file
		cd path + BASEGAME's directory

		server download, to be written to home path + current game's directory


		The filesystem can be safely shutdown and reinitialized with different
		basedir / cddir / game combinations, but all other subsystems that rely on it
		(sound, video) must also be forced to restart.

		Because the same files are loaded by both the clip model (CM_) and renderer (TR_)
		subsystems, a simple single-file caching scheme is used.  The CM_ subsystems will
		load the file with a request to cache.  Only one file will be kept cached at a time,
		so any models that are going to be referenced by both subsystems should alternate
		between the CM_ load function and the ref load function.

		TODO: A qpath that starts with a leading slash will always refer to the base game, even if another
		game is currently active.  This allows character models, skins, and sounds to be downloaded
		to a common directory no matter which game is active.

		How to prevent downloading zip files?
		Pass pk3 file names in systeminfo, and download before FS_Restart()?

		Aborting a download disconnects the client from the server.

		How to mark files as downloadable?  Commercial add-ons won't be downloadable.

		Non-commercial downloads will want to download the entire zip file.
		the game would have to be reset to actually read the zip in

		Auto-update information

		Path separators

		Casing

		  separate server gamedir and client gamedir, so if the user starts
		  a local game after having connected to a network game, it won't stick
		  with the network game.

		  allow menu options for game selection?

		Read / write config to floppy option.

		Different version coexistance?

		When building a pak file, make sure a q3config.cfg isn't present in it,
		or configs will never get loaded from disk!

		  todo:

		  downloading (outside fs?)
		  game directory passing and restarting

		=============================================================================

		*/

		// if this is defined, the executable positively won't work with any paks other
		// than the demo pak, even if productid is present.  This is only used for our
		// last demo release to prevent the mac and linux users from using the demo
		// executable with the production windows pak before the mac/linux products
		// hit the shelves a little later
		// NOW defined in build files
		//#define PRE_RELEASE_TADEMO

		const int MAX_ZPATH = 256;
		const int MAX_SEARCH_PATHS = 4096;
		const int MAX_FILEHASH_SIZE = 1024;

		static	string		fs_gamedir;//[q_shared.MAX_OSPATH];	// this will be a single file name with no separators
		static	cvar_t		fs_debug;
		static	cvar_t		fs_homepath;
		static	cvar_t		fs_basepath;
		static	cvar_t		fs_basegame;
		static	cvar_t		fs_cdpath;
		static	cvar_t		fs_copyfiles;
		static	cvar_t		fs_gamedirvar;
		static	searchpath_t[]	fs_searchpaths;
		static	int			fs_readCount;			// total bytes read
		static	int			fs_loadCount;			// total files read
		static	int			fs_loadStack;			// total files in memory
		static	int			fs_packFiles;			// total number of files in packs

		static int fs_fakeChkSum;
		static int fs_checksumFeed;

		public struct qfile_gut
		{
			public FileStream o;
			public ZipArchive z;
			public Stream zf;
		}

		public struct qfile_ut
		{
			public qfile_gut file;
			public bool unique;
		}

		struct fileHandleData_t
		{
			public qfile_ut handleFiles;
			public bool handleSync;
			public int baseOffset;
			public int fileSize;
			public int zipFilePos;
			public bool zipFile;
			public bool streamed;
			public string name;//[MAX_ZPATH]; optimus-code TODO
		} ;

		static fileHandleData_t[] fsh = new fileHandleData_t[qcommon.MAX_FILE_HANDLES];

		// TTimo - https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=540
		// wether we did a reorder on the current search path when joining the server
		static bool fs_reordered;

		// never load anything from pk3 files that are not present at the server when pure
		static int		fs_numServerPaks;
		static int[]		fs_serverPaks = new int[MAX_SEARCH_PATHS];					// checksums
		static string[] fs_serverPakNames = new string[MAX_SEARCH_PATHS];				// pk3 names

		// only used for autodownload, to make sure the client has at least
		// all the pk3 files that are referenced at the server side
		static int		fs_numServerReferencedPaks;
		static int[]	fs_serverReferencedPaks = new int[MAX_SEARCH_PATHS];			// checksums
		static string[] fs_serverReferencedPakNames = new string[MAX_SEARCH_PATHS];     // pk3 names

		// last valid game folder used
		static string lastValidBase;//[q_shared.MAX_OSPATH];
		static string lastValidGame;//[q_shared.MAX_OSPATH];

		// productId: This file is copyright 1999 Id Software, and may not be duplicated except during a licensed installation of the full commercial version of Quake 3:Arena
		static byte[] fs_scrambledProductId = new byte[] 
		{
			220, 129, 255, 108, 244, 163, 171, 55, 133, 65, 199, 36, 140, 222, 53, 99, 65, 171, 175, 232, 236, 193, 210, 250, 169, 104, 231, 231, 21, 201, 170, 208, 135, 175, 130, 136, 85, 215, 71, 23, 96, 32, 96, 83, 44, 240, 219, 138, 184, 215, 73, 27, 196, 247, 55, 139, 148, 68, 78, 203, 213, 238, 139, 23, 45, 205, 118, 186, 236, 230, 231, 107, 212, 1, 10, 98, 30, 20, 116, 180, 216, 248, 166, 35, 45, 22, 215, 229, 35, 116, 250, 167, 117, 3, 57, 55, 201, 229, 218, 222, 128, 12, 141, 149, 32, 110, 168, 215, 184, 53, 31, 147, 62, 12, 138, 67, 132, 54, 125, 6, 221, 148, 140, 4, 21, 44, 198, 3, 126, 12, 100, 236, 61, 42, 44, 251, 15, 135, 14, 134, 89, 92, 177, 246, 152, 106, 124, 78, 118, 80, 28, 42
		};

		/*
		==============
		FS_Initialized
		==============
		*/

		public static bool FS_Initialized() {
			return (bool) (fs_searchpaths != null);
		}

		/*
		=================
		FS_PakIsPure
		=================
		*/
		static bool FS_PakIsPure( pack_t pack ) {
			int i;

			if ( fs_numServerPaks > 0 ) {
				for ( i = 0 ; i < fs_numServerPaks ; i++ ) {
					// FIXME: also use hashed file names
					// NOTE TTimo: a pk3 with same checksum but different name would be validated too
					//   I don't see this as allowing for any exploit, it would only happen if the client does manips of it's file names 'not a bug'
					if ( pack.checksum == fs_serverPaks[i] ) {
						return true;		// on the aproved list
					}
				}
				return false;	// not on the pure server pak list
			}
			return true;
		}


		/*
		=================
		FS_LoadStack
		return load stack
		=================
		*/
		static int FS_LoadStack()
		{
			return fs_loadStack;
		}
		                      
		/*
		================
		return a hash value for the filename
		================
		*/
		static long FS_HashFileName( string fname, int hashSize ) 
		{
			int		i;
			long	hash;
			char	letter;

			hash = 0;
			i = 0;
			while (fname[i] != '\0') {
				letter = Char.ToLower(fname[i]);
				if (letter =='.') break;				// don't include extension
				if (letter =='\\') letter = '/';		// damn path names
				if (letter == q_shared.PATH_SEP) letter = '/';		// damn path names
				hash+=(long)(letter)*(i+119);
				i++;
			}
			hash = (hash ^ (hash >> 10) ^ (hash >> 20));
			hash &= (hashSize-1);
			return hash;
		}

		static fileHandle_t	FS_HandleForFile() 
		{
			int		i;

			for ( i = 1 ; i < qcommon.MAX_FILE_HANDLES ; i++ ) 
			{
				if ( fsh[i].handleFiles.file.o == null ) 
				{
					return new fileHandle_t { ID = i };
				}
			}
			common.Com_Error( errorParm_t.ERR_DROP, "FS_HandleForFile: none free" );
			return new fileHandle_t { ID = 0 };
		}

		static FileStream FS_FileForHandle( fileHandle_t f ) 
		{
			if ( f.ID < 0 || f.ID > qcommon.MAX_FILE_HANDLES ) 
			{
				common.Com_Error( errorParm_t.ERR_DROP, "FS_FileForHandle: out of reange" );
			}
			if (fsh[f.ID].zipFile == true) 
			{
				common.Com_Error( errorParm_t.ERR_DROP, "FS_FileForHandle: can't get FILE on zip file" );
			}
			if (fsh[f.ID].handleFiles.file.o == null) 
			{
				common.Com_Error( errorParm_t.ERR_DROP, "FS_FileForHandle: NULL" );
			}
			
			return fsh[f.ID].handleFiles.file.o;
		}

		public static void FS_ForceFlush( fileHandle_t f ) 
		{
			var file = FS_FileForHandle(f);
			file?.Dispose( );
		}

		/*
		================
		FS_filelength

		If this is called on a non-unique FILE (from a pak file),
		it will return the size of the pak file, not the expected
		size of the file.
		================
		*/
		public static long FS_filelength( fileHandle_t f ) 
		{
			var h = FS_FileForHandle(f);			
			return h.Length;
		}

		/*
		====================
		FS_ReplaceSeparators

		Fix things up differently for win/unix/mac
		====================
		*/
		public static void FS_ReplaceSeparators( ref string path ) 
		{
			path = path.Replace( '/', q_shared.PATH_SEP ).Replace( '\\', q_shared.PATH_SEP );
		}

		/*
		===================
		FS_BuildOSPath

		Qpath may have either forward or backwards slashes
		===================
		*/
		public static string FS_BuildOSPath( string basePath, string game, string qpath ) 
		{
			string[] ospath = new string[2];
			int toggle = 0;
			
			toggle ^= 1;		// flip-flop to allow two returns without clash

			if( game == null || game[0] == 0 ) {
				game = fs_gamedir;
			}

			q_shared.Com_sprintf( out var temp, q_shared.MAX_OSPATH, "/%s/%s", game, qpath );
			FS_ReplaceSeparators( ref temp );	
			q_shared.Com_sprintf( out ospath[toggle], ospath[0].Length, "%s%s", basePath, temp );
			
			return ospath[toggle];
		}


		/*
		============
		FS_CreatePath

		Creates any directories needed to store the given filename
		============
		*/
		static bool FS_CreatePath (string OSPath) {
			string ofs;
			
			// make absolutely sure that it can't back up the path
			// FIXME: is c: allowed???
			if ( OSPath.Contains( ".." ) || OSPath.Contains( "::" ) ) 
			{
				common.Com_Printf( "WARNING: refusing to create relative path \"%s\"\n", OSPath );
				return true;
			}

			for (ofs = OSPath+1 ; *ofs ; ofs++) {
				if (*ofs == q_shared.PATH_SEP) {	
					// create the directory
					*ofs = 0;
					Sys_Mkdir (OSPath);
					*ofs = q_shared.PATH_SEP;
				}
			}
			return false;
		}

		/*
		=================
		FS_CopyFile

		Copy a fully specified file from one place to another
		=================
		*/
		static void FS_CopyFile( string fromOSPath, string toOSPath )
		{
			common.Com_Printf( "copy %s to %s\n", fromOSPath, toOSPath );

			if (fromOSPath.Contains("journal.dat") || fromOSPath.Contains("journaldata.dat")) 
			{
				common.Com_Printf( "Ignoring journal files\n");
				return;
			}

			try
			{
				File.Copy( fromOSPath, toOSPath, true );
			}
			catch ( IOException ex )
			{
				common.Com_Error( errorParm_t.ERR_FATAL, $"FS_CopyFile exception:\n{ex.ToString()}" );
			}			
		}

		/*
		===========
		FS_Remove

		===========
		*/
		static void FS_Remove( string osPath ) 
		{
			if ( File.Exists( osPath ) )
				File.Delete( osPath );
		}

		/*
		================
		FS_FileExists

		Tests if the file exists in the current gamedir, this DOES NOT
		search the paths.  This is to determine if opening a file to write
		(which always goes into the current gamedir) will cause any overwrites.
		NOTE TTimo: this goes with FS_FOpenFileWrite for opening the file afterwards
		================
		*/
		public static bool FS_FileExists( string file )
		{
			var testpath = FS_BuildOSPath( fs_homepath.@string, fs_gamedir, file );

			return File.Exists( testpath );
		}

		/*
		================
		FS_SV_FileExists

		Tests if the file exists 
		================
		*/
		public static bool FS_SV_FileExists( string file )
		{
			var testpath = FS_BuildOSPath( fs_homepath.@string, file, "");
			//testpath[strlen(testpath)-1] = '\0';

			return File.Exists( testpath );
		}


		/*
		===========
		FS_SV_FOpenFileWrite

		===========
		*/
		public static fileHandle_t FS_SV_FOpenFileWrite( string filename )
		{
			if ( fs_searchpaths == null )
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );

			var ospath = FS_BuildOSPath( fs_homepath.@string, filename, "" );
			//ospath[strlen(ospath)-1] = '\0';

			var f = FS_HandleForFile( );
			fsh[f.ID].zipFile = false;

			if ( fs_debug.integer == 1 )
				common.Com_Printf( "FS_SV_FOpenFileWrite: %s\n", ospath );

			if ( FS_CreatePath( ospath ) )
				return new fileHandle_t { ID = 0 };

			common.Com_DPrintf( "writing to: %s\n", ospath );
			fsh[f.ID].handleFiles.file.o = File.OpenWrite( ospath );

			q_shared.Q_strncpyz( out fsh[f.ID].name, filename, fsh[f.ID].name.Length );

			fsh[f.ID].handleSync = false;

			if ( fsh[f.ID].handleFiles.file.o == null ) 
				f = new fileHandle_t { ID = 0 };

			return f;
		}

		/*
		===========
		FS_SV_FOpenFileRead
		search for a file somewhere below the home path, base path or cd path
		we search in that order, matching FS_SV_FOpenFileRead order
		===========
		*/
		public static long FS_SV_FOpenFileRead( string filename, out fileHandle_t fp ) 
		{
			string ospath;

			if ( fs_searchpaths == null ) 
			{
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			var f = FS_HandleForFile();
			fsh[f.ID].zipFile = false;

			q_shared.Q_strncpyz( out fsh[f.ID].name, filename, fsh[f.ID].name.Length );

			// don't let sound stutter
			S_ClearSoundBuffer();

			// search homepath
			ospath = FS_BuildOSPath( fs_homepath.@string, filename, "" );
			// remove trailing slash
			//ospath[strlen(ospath)-1] = '\0';

			if ( fs_debug.integer == 1 ) {
				common.Com_Printf( "FS_SV_FOpenFileRead (fs_homepath): %s\n", ospath );
			}

			fsh[f.ID].handleFiles.file.o = File.OpenRead( ospath );
			fsh[f.ID].handleSync = false;
		  if (fsh[f.ID].handleFiles.file.o == null)
		  {
		    // NOTE TTimo on non *nix systems, fs_homepath == fs_basepath, might want to avoid
		    if (q_shared.Q_stricmp(fs_homepath.@string,fs_basepath.@string) == 1)
		    {
		      // search basepath
		      ospath = FS_BuildOSPath( fs_basepath.@string, filename, "" );
		      //ospath[strlen(ospath)-1] = '\0';

		      if ( fs_debug.integer == 1 )
		      {
		        common.Com_Printf( "FS_SV_FOpenFileRead (fs_basepath): %s\n", ospath );
		      }

		      fsh[f.ID].handleFiles.file.o = File.OpenRead( ospath );
		      fsh[f.ID].handleSync = false;

		      if ( fsh[f.ID].handleFiles.file.o == null )
		      {
		        f = new fileHandle_t();
		      }
		    }
		  }

			if (fsh[f.ID].handleFiles.file.o == null) 
			{
		    // search cd path
		    ospath = FS_BuildOSPath( fs_cdpath.@string, filename, "" );
		    //ospath[strlen(ospath)-1] = '\0';

		    if (fs_debug.integer == 1)
		    {
		      common.Com_Printf( "FS_SV_FOpenFileRead (fs_cdpath) : %s\n", ospath );
		    }

			  fsh[f.ID].handleFiles.file.o = File.OpenRead( ospath );
			  fsh[f.ID].handleSync = false;

			  if( fsh[f.ID].handleFiles.file.o == null )
				{
					f = new fileHandle_t( );
				}
		  }
		  
			fp = f;

			if (f.ID != 0) 
				return FS_filelength(f);

			return 0;
		}


		/*
		===========
		FS_SV_Rename

		===========
		*/
		public static void FS_SV_Rename( string from, string to ) 
		{
			string from_ospath, to_ospath;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			// don't let sound stutter
			S_ClearSoundBuffer();

			from_ospath = FS_BuildOSPath( fs_homepath.@string, from, "" );
			to_ospath = FS_BuildOSPath( fs_homepath.@string, to, "" );
			//from_ospath[strlen(from_ospath)-1] = '\0';
			//to_ospath[strlen(to_ospath)-1] = '\0';

			if ( fs_debug.integer == 1) {
				common.Com_Printf( "FS_SV_Rename: %s -. %s\n", from_ospath, to_ospath );
			}

			FS_CopyFile ( from_ospath, to_ospath );
			FS_Remove ( from_ospath );
		}



		/*
		===========
		FS_Rename

		===========
		*/
		public static void FS_Rename( string from, string to ) {
			string from_ospath, to_ospath;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			// don't let sound stutter
			S_ClearSoundBuffer();

			from_ospath = FS_BuildOSPath( fs_homepath.@string, fs_gamedir, from );
			to_ospath = FS_BuildOSPath( fs_homepath.@string, fs_gamedir, to );

			if ( fs_debug.integer == 1 ) {
				common.Com_Printf( "FS_Rename: %s -. %s\n", from_ospath, to_ospath );
			}

			FS_CopyFile ( from_ospath, to_ospath );
			FS_Remove ( from_ospath );
		}

		/*
		==============
		FS_FCloseFile

		If the FILE pointer is an open pak file, leave it open.

		For some reason, other dll's can't just cal fclose()
		on files returned by FS_FOpenFile...
		==============
		*/
		public static void FS_FCloseFile( fileHandle_t f ) 
		{
			if ( fs_searchpaths == null ) 
			{
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if (fsh[f.ID].streamed) 
			{
				Sys_EndStreamedFile(f.ID );
			}
			if (fsh[f.ID].zipFile == true) 
			{
				fsh[f.ID].handleFiles.file.zf?.Dispose( );

				if ( fsh[f.ID].handleFiles.unique ) 
					fsh[f.ID].handleFiles.file.z?.Dispose( );
				fsh[f.ID] = default;
				return;
			}

			// we didn't find it as a pak, so close it as a unique file
			fsh[f.ID].handleFiles.file.o?.Dispose( );
			fsh[f.ID] = default;
		}

		/*
		===========
		FS_FOpenFileWrite

		===========
		*/
		public static fileHandle_t FS_FOpenFileWrite( string filename ) 
		{
			string ospath;

			if ( fs_searchpaths == null ) 
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );

			var f = FS_HandleForFile();
			fsh[f.ID].zipFile = false;

			ospath = FS_BuildOSPath( fs_homepath.@string, fs_gamedir, filename );

			if ( fs_debug.integer == 1 ) {
				common.Com_Printf( "FS_FOpenFileWrite: %s\n", ospath );
			}

			if( FS_CreatePath( ospath ) )
				return default;

			// enabling the following line causes a recursive function call loop
			// when running with +set logfile 1 +set developer 1
			//Com_DPrintf( "writing to: %s\n", ospath );
			fsh[f.ID].handleFiles.file.o = File.OpenWrite( ospath );

			q_shared.Q_strncpyz( out fsh[f.ID].name, filename, fsh[f.ID].name.Length );

			fsh[f.ID].handleSync = false;

			if (fsh[f.ID].handleFiles.file.o == null)
				f = default;

			return f;
		}

		/*
		===========
		FS_FOpenFileAppend

		===========
		*/
		public static fileHandle_t FS_FOpenFileAppend( string filename ) 
		{
			string ospath;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			var f = FS_HandleForFile();
			fsh[f.ID].zipFile = false;

			q_shared.Q_strncpyz( out fsh[f.ID].name, filename, fsh[f.ID].name.Length );

			// don't let sound stutter
			S_ClearSoundBuffer();

			ospath = FS_BuildOSPath( fs_homepath.@string, fs_gamedir, filename );

			if ( fs_debug.integer == 1 ) {
				common.Com_Printf( "FS_FOpenFileAppend: %s\n", ospath );
			}

			if( FS_CreatePath( ospath ) ) {
				return default;
			}

			fsh[f.ID].handleFiles.file.o = File.Open( ospath, FileMode.Append );
			fsh[f.ID].handleSync = false;
			if (fsh[f.ID].handleFiles.file.o == null) {
				f = default;
			}
			return f;
		}

		/*
		===========
		FS_FilenameCompare

		Ignore case and seprator char distinctions
		===========
		*/
		public static bool FS_FilenameCompare( string s1, string s2 ) 
		{
			return s1.Replace( "\\", "/" ).Equals( s2.Replace( "\\", "/" ), StringComparison.InvariantCultureIgnoreCase );
		}

		/*
		===========
		FS_ShiftedStrStr
		===========
		*/
		static string FS_ShiftedStrStr( string @string, string substring, int shift )
		{
			var subStringBytes = Encoding.ASCII.GetBytes( substring );
			var buf = new StringBuilder( q_shared.MAX_STRING_TOKENS );

			for ( var i = 0; i < subStringBytes.Length; i++ )
				buf.Append( ( byte ) ( subStringBytes[i] + shift ) );

			buf.Append( ( Byte ) '\0' );

			var ci = @string.IndexOf( buf.ToString( ) );

			if ( ci >= 0 && ci < @string.Length )
				return @string.Substring( ci );

			return null;
		}

		/*
		===========
		FS_FOpenFileRead

		Finds the file in the search path.
		Returns filesize and an open FILE pointer.
		Used for streaming data out of either a
		separate file or a ZIP file.
		===========
		*/
		public static bool		com_fullyInitialized;

		public static bool FS_FOpenFileRead( string filename, out fileHandle_t file, bool uniqueFILE )
		{
			string			netpath;
			pack_t			pak;
			fileInPack_t	pakFile;
			directory_t		dir;
			long			hash;
			unz_s			zfi;
			FileStream		temp;
			int				l;
			string demoExt;//[16];

			hash = 0;
			file = default;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( file.ID == 0 ) {
				// just wants to see if file is there
				foreach ( var search in fs_searchpaths ) {
					//
					if ( search.pack != null ) {
						hash = FS_HashFileName(filename, search.pack.hashSize);
					}
					// is the element a pak file?
					if ( search.pack != null && search.pack.hashTable[hash] != null ) {
						// look through all the pak file elements
						pak = search.pack;
						pakFile = pak.hashTable[hash];
						do {
							// case and separator insensitive comparisons
							if ( !FS_FilenameCompare( pakFile.name, filename ) ) {
								// found it!
								return true;
							}
							pakFile = pakFile.next;
						} while(pakFile != null);
					} else if ( search.dir != null ) {
						dir = search.dir;
					
						netpath = FS_BuildOSPath( dir.path, dir.gamedir, filename );
                        try
                        {
							temp = File.OpenRead( netpath );
						}
                        catch ( IOException )
                        {
							continue;
						}

						if ( temp == null )
							continue;

						temp.Dispose();
						return true;
					}
				}
				return false;
			}

			if ( filename == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "FS_FOpenFileRead: NULL 'filename' parameter passed\n" );
			}

			common.Com_sprintf (demoExt, sizeof(demoExt), ".dm_%d",PROTOCOL_VERSION );
			// qpaths are not supposed to have a leading slash
			if ( filename[0] == '/' || filename[0] == '\\' ) {
				filename++;
			}

			// make absolutely sure that it can't back up the path.
			// The searchpaths do guarantee that something will always
			// be prepended, so we don't need to worry about "c:" or "//limbo" 
			if ( filename.Contains( ".." ) || filename.Contains( "::" ) ) {
				file = default;
				return false;
			}

			// make sure the q3key file is only readable by the quake3.exe at initialization
			// any other time the key should only be accessed in memory using the provided functions
			if( com_fullyInitialized && filename.Contains( "q3key" ) ) {
				file = default;
				return false;
			}

			//
			// search through the path, one element at a time
			//

			file = FS_HandleForFile();
			fsh[file.ID].handleFiles.unique = uniqueFILE;

			foreach ( var search in fs_searchpaths ) {
				//
				if ( search.pack != null ) {
					hash = FS_HashFileName(filename, search.pack.hashSize);
				}
				// is the element a pak file?
				if ( search.pack != null && search.pack.hashTable[hash] != null ) {
					// disregard if it doesn't match one of the allowed pure pak files
					if ( !FS_PakIsPure(search.pack) ) {
						continue;
					}

					// look through all the pak file elements
					pak = search.pack;
					pakFile = pak.hashTable[hash];
					do {
						// case and separator insensitive comparisons
						if ( !FS_FilenameCompare( pakFile.name, filename ) ) {
							// found it!

							// mark the pak as having been referenced and mark specifics on cgame and ui
							// shaders, txt, arena files  by themselves do not count as a reference as 
							// these are loaded from all pk3s 
							// from every pk3 file.. 
							l = filename.Length;
							if ( !(pak.referenced & FS_GENERAL_REF)) {
								if ( q_shared.Q_stricmp(filename + l - 7, ".shader") != 0 &&
									q_shared.Q_stricmp(filename + l - 4, ".txt") != 0 &&
									q_shared.Q_stricmp(filename + l - 4, ".cfg") != 0 &&
									q_shared.Q_stricmp(filename + l - 7, ".config") != 0 &&
									!filename.Contains("levelshots") &&
									q_shared.Q_stricmp(filename + l - 4, ".bot") != 0 &&
									q_shared.Q_stricmp(filename + l - 6, ".arena") != 0 &&
									q_shared.Q_stricmp(filename + l - 5, ".menu") != 0) {
									pak.referenced |= FS_GENERAL_REF;
								}
							}

							// qagame.qvm	- 13
							// dTZT`X!di`
							if (!(pak.referenced & FS_QAGAME_REF) && FS_ShiftedStrStr(filename, "dTZT`X!di`", 13)) {
								pak.referenced |= FS_QAGAME_REF;
							}
							// cgame.qvm	- 7
							// \`Zf^'jof
							if (!(pak.referenced & FS_CGAME_REF) && FS_ShiftedStrStr(filename , "\\`Zf^'jof", 7)) {
								pak.referenced |= FS_CGAME_REF;
							}
							// ui.qvm		- 5
							// pd)lqh
							if (!(pak.referenced & FS_UI_REF) && FS_ShiftedStrStr(filename , "pd)lqh", 5)) {
								pak.referenced |= FS_UI_REF;
							}

							if ( uniqueFILE ) {
								// open a new file on the pakfile
								fsh[file.ID].handleFiles.file.z = unzReOpen (pak.pakFilename, pak.handle);
								if (fsh[file.ID].handleFiles.file.z == null) {
									common.Com_Error (errorParm_t.ERR_FATAL, "Couldn't reopen %s", pak.pakFilename);
								}
							} else {
								fsh[file.ID].handleFiles.file.z = pak.handle;
							}
							q_shared.Q_strncpyz( out fsh[file.ID].name, filename, fsh[file.ID].name.Length );
							fsh[file.ID].zipFile = true;
							zfi = (unz_s *)fsh[file.ID].handleFiles.file.z;
							// in case the file was new
							temp = zfi.file;
							// set the file position in the zip file (also sets the current file info)
							unzSetCurrentFileInfoPosition(pak.handle, pakFile.pos);
							// copy the file info into the unzip structure
							Com_Memcpy( zfi, pak.handle, sizeof(unz_s) );
							// we copy this back into the structure
							zfi.file = temp;
							// open the file in the zip
							unzOpenCurrentFile( fsh[*file].handleFiles.file.z );
							fsh[*file].zipFilePos = pakFile.pos;

							if ( fs_debug.integer ) {
								common.Com_Printf( "FS_FOpenFileRead: %s (found in '%s')\n", 
									filename, pak.pakFilename );
							}
							return zfi.cur_file_info.uncompressed_size;
						}
						pakFile = pakFile.next;
					} while(pakFile != NULL);
				} else if ( search.dir ) {
					// check a file in the directory tree

					// if we are running restricted, the only files we
					// will allow to come from the directory are .cfg files
					l = (int)strlen( filename );
		      // FIXME TTimo I'm not sure about the fs_numServerPaks test
		      // if you are using FS_ReadFile to find out if a file exists,
		      //   this test can make the search fail although the file is in the directory
		      // I had the problem on https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=8
		      // turned out I used FS_FileExists instead
					if ( fs_numServerPaks ) {

						if ( Q_stricmp( filename + l - 4, ".cfg" )		// for config files
							&& Q_stricmp( filename + l - 5, ".menu" )	// menu files
							&& Q_stricmp( filename + l - 5, ".game" )	// menu files
							&& Q_stricmp( filename + l - (int)strlen(demoExt), demoExt )	// menu files
							&& Q_stricmp( filename + l - 4, ".dat" ) ) {	// for journal files
							continue;
						}
					}

					dir = search.dir;
					
					netpath = FS_BuildOSPath( dir.path, dir.gamedir, filename );
					fsh[*file].handleFiles.file.o = fopen (netpath, "rb");
					if ( !fsh[*file].handleFiles.file.o ) {
						continue;
					}

					if ( Q_stricmp( filename + l - 4, ".cfg" )		// for config files
						&& Q_stricmp( filename + l - 5, ".menu" )	// menu files
						&& Q_stricmp( filename + l - 5, ".game" )	// menu files
						&& Q_stricmp( filename + l - (int)strlen(demoExt), demoExt )	// menu files
						&& Q_stricmp( filename + l - 4, ".dat" ) ) {	// for journal files
						fs_fakeChkSum = random();
					}
		      
					Q_strncpyz( fsh[*file].name, filename, sizeof( fsh[*file].name ) );
					fsh[*file].zipFile = false;
					if ( fs_debug.integer ) {
						common.Com_Printf( "FS_FOpenFileRead: %s (found in '%s/%s')\n", filename,
							dir.path, dir.gamedir );
					}

					// if we are getting it from the cdpath, optionally copy it
					//  to the basepath
					if ( fs_copyfiles.integer && !Q_stricmp( dir.path, fs_cdpath.string ) ) {
						char	*copypath;

						copypath = FS_BuildOSPath( fs_basepath.string, dir.gamedir, filename );
						FS_CopyFile( netpath, copypath );
					}

					return FS_filelength (*file);
				}		
			}
			
			Com_DPrintf ("Can't find %s\n", filename);
			*file = 0;
			return -1;
		}


		/*
		=================
		FS_Read

		Properly handles partial reads
		=================
		*/
		public static int FS_Read2( byte[] buffer, int len, fileHandle_t f ) {
			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( f.ID == 0 ) {
				return 0;
			}
			if (fsh[f.ID].streamed) {
				int r;
				fsh[f.ID].streamed = false;
				r = Sys_StreamedRead( buffer, len, 1, f);
				fsh[f.ID].streamed = true;
				return r;
			} else {
				return FS_Read( buffer, len, f);
			}
		}

		public static int FS_Read( byte[] buffer, int len, fileHandle_t f ) {
			int		block, remaining;
			int		read;
			byte[]	buf;
			int		bufI = 0;
			int		tries;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( f.ID == 0 ) {
				return 0;
			}

			buf = buffer;
			fs_readCount += len;

			if (fsh[f.ID].zipFile == false) {
				remaining = len;
				tries = 0;
				while (remaining > 0) {
					block = remaining;
					var readCount = Math.Min( block, len - bufI );
					read = fsh[f.ID].handleFiles.file.o.Read( buf, bufI, readCount );
					if (read == 0) {
						// we might have been trying to read from a CD, which
						// sometimes returns a 0 read on windows
						if (tries == 0) {
							tries = 1;
						} else {
							return len-remaining;	//Com_Error (ERR_FATAL, "FS_Read: 0 bytes read");
						}
					}

					if (read == -1) {
						common.Com_Error (errorParm_t.ERR_FATAL, "FS_Read: -1 bytes read");
					}

					remaining -= read;
					bufI += read;
				}
				return len;
			} else {
				return fsh[f.ID].handleFiles.file.zf.Read( buffer, 0, len);
			}
		}

		/*
		=================
		FS_Write

		Properly handles partial writes
		=================
		*/
		public static int FS_Write( byte[] buffer, int len, fileHandle_t h ) {
			int		block, remaining;
			int		written;
			byte[]	buf;
			int		bufI = 0;
			int		tries;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( h.ID == 0 ) {
				return 0;
			}

			var f = FS_FileForHandle(h);
			buf = buffer;

			remaining = len;
			tries = 0;
			while (remaining > 0) {
				block = remaining;
				written = Math.Min( block, len - bufI );
				f.Write( buf, bufI, written );
				if (written == 0) {
					if (tries == 0) {
						tries = 1;
					} else {
						common.Com_Printf( "FS_Write: 0 bytes written\n" );
						return 0;
					}
				}

				if (written == -1) {
					common.Com_Printf( "FS_Write: -1 bytes written\n" );
					return 0;
				}

				remaining -= written;
				bufI += written;
			}
			if ( fsh[h.ID].handleSync ) {
				f.Flush( );
			}
			return len;
		}

		public static void FS_Printf( fileHandle_t h, string fmt, params object[] parameters ) 
		{
			var msg = qcommon.Q_vsnprintf (fmt, parameters );

			if ( msg.Length > qcommon.MAXPRINTMSG )
				msg = msg.Substring( 0, qcommon.MAXPRINTMSG );

			FS_Write(Encoding.ASCII.GetBytes( msg ), msg.Length, h);
		}

		/*
		=================
		FS_Seek

		=================
		*/
		public static long FS_Seek( fileHandle_t f, long offset, SeekOrigin origin ) {
			SeekOrigin		_origin;
			string foo;//[65536];

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
				return -1;
			}

			if (fsh[f.ID].streamed) {
				fsh[f.ID].streamed = false;
				Sys_StreamSeek( f, offset, origin );
				fsh[f.ID].streamed = true;
			}

			if (fsh[f.ID].zipFile == true) {
				if (offset == 0 && origin == SeekOrigin.Begin) {
					// set the file position in the zip file (also sets the current file info)
					unzSetCurrentFileInfoPosition(fsh[f.ID].handleFiles.file.z, fsh[f.ID].zipFilePos);
					return unzOpenCurrentFile(fsh[f.ID].handleFiles.file.z);
				} else if (offset<65536) {
					// set the file position in the zip file (also sets the current file info)
					unzSetCurrentFileInfoPosition(fsh[f.ID].handleFiles.file.z, fsh[f.ID].zipFilePos);
					unzOpenCurrentFile(fsh[f.ID].handleFiles.file.z);
					return FS_Read(foo, offset, f);
				} else {
					common.Com_Error( errorParm_t.ERR_FATAL, "ZIP FILE FSEEK NOT YET IMPLEMENTED\n" );
					return -1;
				}
			} else {
				var file = FS_FileForHandle(f);
				switch( origin ) {
				case SeekOrigin.Current:
					_origin = SeekOrigin.Current;
					break;
				case SeekOrigin.End:
					_origin = SeekOrigin.End;
					break;
				case SeekOrigin.Begin:
					_origin = SeekOrigin.Begin;
					break;
				default:
					_origin = SeekOrigin.Current;
					common.Com_Error( errorParm_t.ERR_FATAL, "Bad origin in FS_Seek\n" );
					break;
				}

				return file.Seek( offset, _origin );
			}
		}


		/*
		======================================================================================

		CONVENIENCE FUNCTIONS FOR ENTIRE FILES

		======================================================================================
		*/

		bool	FS_FileIsInPAK(string filename, out int pChecksum ) {
			pack_t			pak;
			fileInPack_t	pakFile;
			long			hash = 0;

			pChecksum = 0;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( filename == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "FS_FOpenFileRead: NULL 'filename' parameter passed\n" );
			}

			// qpaths are not supposed to have a leading slash
			if ( filename[0] == '/' || filename[0] == '\\' ) {
				filename = filename.Substring( 1 );
			}

			// make absolutely sure that it can't back up the path.
			// The searchpaths do guarantee that something will always
			// be prepended, so we don't need to worry about "c:" or "//limbo" 
			if ( filename.Contains( ".." ) || filename.Contains( "::" ) ) {
				return false;
			}

			//
			// search through the path, one element at a time
			//

			foreach ( var search in fs_searchpaths ) {
				//
				if (search.pack != null) {
					hash = FS_HashFileName(filename, search.pack.hashSize);
				}
				// is the element a pak file?
				if ( search.pack != null && search.pack.hashTable[hash] != null ) {
					// disregard if it doesn't match one of the allowed pure pak files
					if ( !FS_PakIsPure(search.pack) ) {
						continue;
					}

					// look through all the pak file elements
					pak = search.pack;
					pakFile = pak.hashTable[hash];
					do {
						// case and separator insensitive comparisons
						if ( !FS_FilenameCompare( pakFile.name, filename ) ) {							
							pChecksum = pak.pure_checksum;						
							return true;
						}
						pakFile = pakFile.next;
					} while(pakFile != null);
				}
			}
			return false;
		}

		/*
		============
		FS_ReadFile

		Filename are relative to the quake search path
		a null buffer will just return the file length without loading
		============
		*/
		public static int FS_ReadFile( string qpath, out byte[] buffer ) {
			FileStream	h;
			byte[]			buf;
			bool		isConfig;
			int				len;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( qpath == null || qpath.Length == 0 ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "FS_ReadFile with empty name\n" );
			}

			buf = null;	// quiet compiler warning

			// if this is a .cfg file and we are playing back a journal, read
			// it from the journal file
			if ( qpath.EndsWith( ".cfg" ) ) {
				isConfig = true;
				if ( com_journal != null && com_journal.integer == 2 ) {
					int		r;

					common.Com_DPrintf( "Loading %s from journal file.\n", qpath );
					r = FS_Read( &len, sizeof( len ), com_journalDataFile );
					if ( r != sizeof( len ) ) {
						if (buffer != null ) *buffer = null;
						return -1;
					}
					// if the file didn't exist when the journal was created
					if (len == 0) {
						if (buffer == null ) {
							return 1;			// hack for old journal files
						}
						*buffer = null;
						return -1;
					}
					if (buffer == null) {
						return len;
					}

					buf = (byte*) Hunk_AllocateTempMemory(len+1);
					*buffer = buf;

					r = FS_Read( buf, len, com_journalDataFile );
					if ( r != len ) {
						common.Com_Error( errorParm_t.ERR_FATAL, "Read from journalDataFile failed" );
					}

					fs_loadCount++;
					fs_loadStack++;

					// guarantee that it will have a trailing 0 for string operations
					buf[len] = 0;

					return len;
				}
			} else {
				isConfig = false;
			}

			// look for it in the filesystem or pack files
			len = FS_FOpenFileRead( qpath, &h, false );
			if ( h == 0 ) {
				if ( buffer ) {
					*buffer = null;
				}
				// if we are journalling and it is a config file, write a zero to the journal file
				if ( isConfig && com_journal && com_journal.integer == 1 ) {
					Com_DPrintf( "Writing zero for %s to journal file.\n", qpath );
					len = 0;
					FS_Write( &len, sizeof( len ), com_journalDataFile );
					FS_Flush( com_journalDataFile );
				}
				return -1;
			}
			
			if ( !buffer ) {
				if ( isConfig && com_journal && com_journal.integer == 1 ) {
					Com_DPrintf( "Writing len for %s to journal file.\n", qpath );
					FS_Write( &len, sizeof( len ), com_journalDataFile );
					FS_Flush( com_journalDataFile );
				}
				FS_FCloseFile( h);
				return len;
			}

			fs_loadCount++;
			fs_loadStack++;

			buf = (byte*) Hunk_AllocateTempMemory(len+1);
			*buffer = buf;

			FS_Read (buf, len, h);

			// guarantee that it will have a trailing 0 for string operations
			buf[len] = 0;
			FS_FCloseFile( h );

			// if we are journalling and it is a config file, write it to the journal file
			if ( isConfig && com_journal && com_journal.integer == 1 ) {
				Com_DPrintf( "Writing %s to journal file.\n", qpath );
				FS_Write( &len, sizeof( len ), com_journalDataFile );
				FS_Write( buf, len, com_journalDataFile );
				FS_Flush( com_journalDataFile );
			}
			return len;
		}

		/*
		=============
		FS_FreeFile
		=============
		*/
		void FS_FreeFile( void *buffer ) {
			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}
			if ( !buffer ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "FS_FreeFile( NULL )" );
			}
			fs_loadStack--;

			Hunk_FreeTempMemory( buffer );

			// if all of our temp files are free, clear all of our space
			if ( fs_loadStack == 0 ) {
				Hunk_ClearTempMemory();
			}
		}

		/*
		============
		FS_WriteFile

		Filename are reletive to the quake search path
		============
		*/
		public static void FS_WriteFile( string qpath, byte[] buffer, int size ) {
			fileHandle_t f;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( qpath == null || buffer == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "FS_WriteFile: NULL parameter" );
			}

			f = FS_FOpenFileWrite( qpath );
			if ( f.ID == 0 ) {
				common.Com_Printf( "Failed to open %s\n", qpath );
				return;
			}

			FS_Write( buffer, size, f );

			FS_FCloseFile( f );
		}



		/*
		==========================================================================

		ZIP FILE LOADING

		==========================================================================
		*/

		/*
		=================
		FS_LoadZipFile

		Creates a new pak_t in the search chain for the contents
		of a zip file.
		=================
		*/
		static pack_t FS_LoadZipFile( string zipfile, string basename )
		{
			fileInPack_t	buildBuffer;
			pack_t			pack;
			ZipArchive			uf;
			int				err;
			unz_global_info gi;
			char			filename_inzip[MAX_ZPATH];
			unz_file_info	file_info;
			int				i, len;
			long			hash;
			int				fs_numHeaderLongs;
			int				*fs_headerLongs;
			char			*namePtr;

			fs_numHeaderLongs = 0;

			uf = unzOpen(zipfile);
			err = unzGetGlobalInfo (uf,&gi);

			if (err != UNZ_OK)
				return NULL;

			fs_packFiles += gi.number_entry;

			len = 0;
			unzGoToFirstFile(uf);
			for (i = 0; i < gi.number_entry; i++)
			{
				err = unzGetCurrentFileInfo(uf, &file_info, filename_inzip, sizeof(filename_inzip), NULL, 0, NULL, 0);
				if (err != UNZ_OK) {
					break;
				}
				len += (int)strlen(filename_inzip) + 1;
				unzGoToNextFile(uf);
			}

			buildBuffer = (fileInPack_t*) Z_Malloc( (gi.number_entry * sizeof( fileInPack_t )) + len );
			namePtr = ((char *) buildBuffer) + gi.number_entry * sizeof( fileInPack_t );
			fs_headerLongs = (int*) Z_Malloc( gi.number_entry * sizeof(int) );

			// get the hash table size from the number of files in the zip
			// because lots of custom pk3 files have less than 32 or 64 files
			for (i = 1; i <= MAX_FILEHASH_SIZE; i <<= 1) {
				if (i > gi.number_entry) {
					break;
				}
			}

			pack = new pack_t( );
			pack.hashSize = i;
			pack.hashTable = (fileInPack_t **) (((char *) pack) + sizeof( pack_t ));
			for(i = 0; i < pack.hashSize; i++) {
				pack.hashTable[i] = null;
			}

			q_shared.Q_strncpyz( pack.pakFilename, zipfile, sizeof( pack.pakFilename ) );
			q_shared.Q_strncpyz( pack.pakBasename, basename, sizeof( pack.pakBasename ) );

			// strip .pk3 if needed
			if ( (int)strlen( pack.pakBasename ) > 4 && !q_shared.Q_stricmp( pack.pakBasename + (int)strlen( pack.pakBasename ) - 4, ".pk3" ) ) {
				pack.pakBasename[strlen( pack.pakBasename ) - 4] = 0;
			}

			pack.handle = uf;
			pack.numfiles = gi.number_entry;
			unzGoToFirstFile(uf);

			for (i = 0; i < gi.number_entry; i++)
			{
				err = unzGetCurrentFileInfo(uf, &file_info, filename_inzip, sizeof(filename_inzip), NULL, 0, NULL, 0);
				if (err != UNZ_OK) {
					break;
				}
				if (file_info.uncompressed_size > 0) {
					fs_headerLongs[fs_numHeaderLongs++] = LittleLong(file_info.crc);
				}
				q_shared.Q_strlwr( filename_inzip );
				hash = FS_HashFileName(filename_inzip, pack.hashSize);
				buildBuffer[i].name = namePtr;
				strcpy( buildBuffer[i].name, filename_inzip );
				namePtr += (int)strlen(filename_inzip) + 1;
				// store the file position in the zip
				unzGetCurrentFileInfoPosition(uf, &buildBuffer[i].pos);
				//
				buildBuffer[i].next = pack.hashTable[hash];
				pack.hashTable[hash] = &buildBuffer[i];
				unzGoToNextFile(uf);
			}

			pack.checksum = Com_BlockChecksum( fs_headerLongs, 4 * fs_numHeaderLongs );
			pack.pure_checksum = Com_BlockChecksumKey( fs_headerLongs, 4 * fs_numHeaderLongs, LittleLong(fs_checksumFeed) );
			pack.checksum = LittleLong( pack.checksum );
			pack.pure_checksum = LittleLong( pack.pure_checksum );

			Z_Free(fs_headerLongs);

			pack.buildBuffer = buildBuffer;
			return pack;
		}

		/*
		=================================================================================

		DIRECTORY SCANNING FUNCTIONS

		=================================================================================
		*/

		private const int MAX_FOUND_FILES = 0x1000;

		public static int FS_ReturnPath( string zname, out string zpath, out int depth ) {
			int len, at, newdep;

			newdep = 0;
			//zpath[0] = 0;
			len = 0;
			at = 0;

			while(zname[at] != 0)
			{
				if (zname[at]=='/' || zname[at]=='\\') {
					len = at;
					newdep++;
				}
				at++;
			}
			zpath = zname;
			//zpath[len] = 0;
			depth = newdep;

			return len;
		}

		/*
		==================
		FS_AddFileToList
		==================
		*/
		static int FS_AddFileToList( string name, char *list[MAX_FOUND_FILES], int nfiles ) {
			int		i;

			if ( nfiles == MAX_FOUND_FILES - 1 ) {
				return nfiles;
			}
			for ( i = 0 ; i < nfiles ; i++ ) {
				if ( !q_shared.Q_stricmp( name, list[i] ) ) {
					return nfiles;		// allready in list
				}
			}
			list[nfiles] = CopyString( name );
			nfiles++;

			return nfiles;
		}

		/*
		===============
		FS_ListFilteredFiles

		Returns a uniqued list of files that match the given criteria
		from all search paths
		===============
		*/
		public static string[] FS_ListFilteredFiles( string path, string extension, string filter, out int numfiles ) {
			int				nfiles;
			var list = new List<string>( MAX_FOUND_FILES );
			int				i;
			int				pathLength;
			int				extensionLength;
			int				length, pathDepth, temp;
			pack_t			pak;
			fileInPack_t	buildBuffer;
			string			zpath;//[MAX_ZPATH];

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( path == null ) {
				numfiles = 0;
				return null;
			}
			if ( extension == null ) {
				extension = "";
			}

			pathLength = path.Length;
			if ( path[pathLength-1] == '\\' || path[pathLength-1] == '/' ) {
				pathLength--;
			}
			extensionLength = extension.Length;
			nfiles = 0;
			FS_ReturnPath(path, out zpath, out pathDepth);

			//
			// search through the path, one element at a time, adding to list
			//
			foreach ( var search in fs_searchpaths ) {
				// is the element a pak file?
				if (search.pack != null) {

					//ZOID:  If we are pure, don't search for files on paks that
					// aren't on the pure list
					if ( !FS_PakIsPure(search.pack) ) {
						continue;
					}

					// look through all the pak file elements
					pak = search.pack;
					buildBuffer = pak.buildBuffer;
					for (i = 0; i < pak.numfiles; i++) {
						string name;
						int		zpathLen, depth;

						// check for directory match
						name = buildBuffer[i].name;
						//
						if (filter != null) {
							// case insensitive
							if (!common.Com_FilterPath( filter, name, false ))
								continue;
							// unique the match
							nfiles = FS_AddFileToList( name, list, nfiles );
						}
						else {

							zpathLen = FS_ReturnPath(name, zpath, &depth);

							if ( (depth-pathDepth)>2 || pathLength > zpathLen || q_shared.Q_stricmpn( name, path, pathLength ) ) {
								continue;
							}

							// check for extension match
							length = name.Length;
							if ( length < extensionLength ) {
								continue;
							}

							if ( q_shared.Q_stricmp( name + length - extensionLength, extension ) ) {
								continue;
							}
							// unique the match

							temp = pathLength;
							if (pathLength > 0) {
								temp++;		// include the '/'
							}
							nfiles = FS_AddFileToList( name + temp, list, nfiles );
						}
					}
				} else if (search.dir != null) { // scan for files in the filesystem
					string netpath;
					int numSysFiles;
					string[] sysFiles;
					string name;

					// don't scan directories for files if we are pure or restricted
					if ( fs_numServerPaks > 0 ) {
				        continue;
				    } else {
						netpath = FS_BuildOSPath( search.dir.path, search.dir.gamedir, path );
						sysFiles = Sys_ListFiles( netpath, extension, filter, &numSysFiles, false );
						for ( i = 0 ; i < numSysFiles ; i++ ) {
							// unique the match
							name = sysFiles[i];
							nfiles = FS_AddFileToList( name, list, nfiles );
						}
						Sys_FreeFileList( sysFiles );
					}
				}		
			}

			// return a copy of the list
			numfiles = nfiles;

			if ( nfiles == 0 ) {
				return null;
			}

			return list.ToArray();
		}

		/*
		=================
		FS_ListFiles
		=================
		*/
		char **FS_ListFiles( const char *path, const char *extension, int *numfiles ) {
			return FS_ListFilteredFiles( path, extension, NULL, numfiles );
		}

		/*
		=================
		FS_FreeFileList
		=================
		*/
		void FS_FreeFileList( char **list ) {
			int		i;

			if ( fs_searchpaths == null ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Filesystem call made without initialization\n" );
			}

			if ( !list ) {
				return;
			}

			for ( i = 0 ; list[i] ; i++ ) {
				Z_Free( list[i] );
			}

			Z_Free( list );
		}


		/*
		================
		FS_GetFileList
		================
		*/
		int	FS_GetFileList(  const char *path, const char *extension, char *listbuf, int bufsize ) {
			int		nFiles, i, nTotal, nLen;
			char **pFiles = NULL;

			*listbuf = 0;
			nFiles = 0;
			nTotal = 0;

			if (Q_stricmp(path, "$modlist") == 0) {
				return FS_GetModList(listbuf, bufsize);
			}

			pFiles = FS_ListFiles(path, extension, &nFiles);

			for (i =0; i < nFiles; i++) {
				nLen = (int)strlen(pFiles[i]) + 1;
				if (nTotal + nLen + 1 < bufsize) {
					strcpy(listbuf, pFiles[i]);
					listbuf += nLen;
					nTotal += nLen;
				}
				else {
					nFiles = i;
					break;
				}
			}

			FS_FreeFileList(pFiles);

			return nFiles;
		}

		/*
		=======================
		Sys_ConcatenateFileLists

		mkv: Naive implementation. Concatenates three lists into a
		     new list, and frees the old lists from the heap.
		bk001129 - from cvs1.17 (mkv)

		FIXME TTimo those two should move to common.c next to Sys_ListFiles
		=======================
		 */
		static unsigned int Sys_CountFileList(char **list)
		{
		  int i = 0;

		  if (list)
		  {
		    while (*list)
		    {
		      list++;
		      i++;
		    }
		  }
		  return i;
		}

		static char** Sys_ConcatenateFileLists( char **list0, char **list1, char **list2 )
		{
		  int totalLength = 0;
		  char** cat = NULL, **dst, **src;

		  totalLength += Sys_CountFileList(list0);
		  totalLength += Sys_CountFileList(list1);
		  totalLength += Sys_CountFileList(list2);

		  /* Create new list. */
		  dst = cat = (char**) Z_Malloc( ( totalLength + 1 ) * sizeof( char* ) );

		  /* Copy over lists. */
		  if (list0)
		  {
		    for (src = list0; *src; src++, dst++)
		      *dst = *src;
		  }
		  if (list1)
		  {
		    for (src = list1; *src; src++, dst++)
		      *dst = *src;
		  }
		  if (list2)
		  {
		    for (src = list2; *src; src++, dst++)
		      *dst = *src;
		  }

		  // Terminate the list
		  *dst = NULL;

		  // Free our old lists.
		  // NOTE: not freeing their content, it's been merged in dst and still being used
		  if (list0) Z_Free( list0 );
		  if (list1) Z_Free( list1 );
		  if (list2) Z_Free( list2 );

		  return cat;
		}

		/*
		================
		FS_GetModList

		Returns a list of mod directory names
		A mod directory is a peer to baseq3 with a pk3 in it
		The directories are searched in base path, cd path and home path
		================
		*/
		int	FS_GetModList( char *listbuf, int bufsize ) {
		  int		nMods, i, j, nTotal, nLen, nPaks, nPotential, nDescLen;
		  char **pFiles = NULL;
		  char **pPaks = NULL;
		  char *name, *path;
		  char descPath[MAX_OSPATH];
		  fileHandle_t descHandle;

		  int dummy;
		  char **pFiles0 = NULL;
		  char **pFiles1 = NULL;
		  char **pFiles2 = NULL;
		  bool bDrop = false;

		  *listbuf = 0;
		  nMods = nPotential = nTotal = 0;

		  pFiles0 = Sys_ListFiles( fs_homepath.string, NULL, NULL, &dummy, true );
		  pFiles1 = Sys_ListFiles( fs_basepath.string, NULL, NULL, &dummy, true );
		  pFiles2 = Sys_ListFiles( fs_cdpath.string, NULL, NULL, &dummy, true );
		  // we searched for mods in the three paths
		  // it is likely that we have duplicate names now, which we will cleanup below
		  pFiles = Sys_ConcatenateFileLists( pFiles0, pFiles1, pFiles2 );
		  nPotential = Sys_CountFileList(pFiles);

		  for ( i = 0 ; i < nPotential ; i++ ) {
		    name = pFiles[i];
		    // NOTE: cleaner would involve more changes
		    // ignore duplicate mod directories
		    if (i!=0) {
		      bDrop = false;
		      for(j=0; j<i; j++)
		      {
		        if (Q_stricmp(pFiles[j],name)==0) {
		          // this one can be dropped
		          bDrop = true;
		          break;
		        }
		      }
		    }
		    if (bDrop) {
		      continue;
		    }
		    // we drop "baseq3" "." and ".."
		    if (Q_stricmp(name, "baseq3") && Q_stricmpn(name, ".", 1)) {
		      // now we need to find some .pk3 files to validate the mod
		      // NOTE TTimo: (actually I'm not sure why .. what if it's a mod under developement with no .pk3?)
		      // we didn't keep the information when we merged the directory names, as to what OS Path it was found under
		      //   so it could be in base path, cd path or home path
		      //   we will try each three of them here (yes, it's a bit messy)
		      path = FS_BuildOSPath( fs_basepath.string, name, "" );
		      nPaks = 0;
		      pPaks = Sys_ListFiles(path, ".pk3", NULL, &nPaks, false); 
		      Sys_FreeFileList( pPaks ); // we only use Sys_ListFiles to check wether .pk3 files are present

		      /* Try on cd path */
		      if( nPaks <= 0 ) {
		        path = FS_BuildOSPath( fs_cdpath.string, name, "" );
		        nPaks = 0;
		        pPaks = Sys_ListFiles( path, ".pk3", NULL, &nPaks, false );
		        Sys_FreeFileList( pPaks );
		      }

		      /* try on home path */
		      if ( nPaks <= 0 )
		      {
		        path = FS_BuildOSPath( fs_homepath.string, name, "" );
		        nPaks = 0;
		        pPaks = Sys_ListFiles( path, ".pk3", NULL, &nPaks, false );
		        Sys_FreeFileList( pPaks );
		      }

		      if (nPaks > 0) {
		        nLen = (int)strlen(name) + 1;
		        // nLen is the length of the mod path
		        // we need to see if there is a description available
		        descPath[0] = '\0';
		        strcpy(descPath, name);
		        strcat(descPath, "/description.txt");
		        nDescLen = FS_SV_FOpenFileRead( descPath, &descHandle );
		        if ( nDescLen > 0 && descHandle) {
		          FILE *file;
		          file = FS_FileForHandle(descHandle);
		          Com_Memset( descPath, 0, sizeof( descPath ) );
		          nDescLen = (int)fread(descPath, 1, 48, file);
		          if (nDescLen >= 0) {
		            descPath[nDescLen] = '\0';
		          }
		          FS_FCloseFile(descHandle);
		        } else {
		          strcpy(descPath, name);
		        }
		        nDescLen = (int)strlen(descPath) + 1;

		        if (nTotal + nLen + 1 + nDescLen + 1 < bufsize) {
		          strcpy(listbuf, name);
		          listbuf += nLen;
		          strcpy(listbuf, descPath);
		          listbuf += nDescLen;
		          nTotal += nLen + nDescLen;
		          nMods++;
		        }
		        else {
		          break;
		        }
		      }
		    }
		  }
		  Sys_FreeFileList( pFiles );

		  return nMods;
		}




		//============================================================================

		/*
		================
		FS_Dir_f
		================
		*/
		void FS_Dir_f( void ) {
			char	*path;
			char	*extension;
			char	**dirnames;
			int		ndirs;
			int		i;

			if ( Cmd_Argc() < 2 || Cmd_Argc() > 3 ) {
				common.Com_Printf( "usage: dir <directory> [extension]\n" );
				return;
			}

			if ( Cmd_Argc() == 2 ) {
				path = Cmd_Argv( 1 );
				extension = "";
			} else {
				path = Cmd_Argv( 1 );
				extension = Cmd_Argv( 2 );
			}

			common.Com_Printf( "Directory of %s %s\n", path, extension );
			common.Com_Printf( "---------------\n" );

			dirnames = FS_ListFiles( path, extension, &ndirs );

			for ( i = 0; i < ndirs; i++ ) {
				common.Com_Printf( "%s\n", dirnames[i] );
			}
			FS_FreeFileList( dirnames );
		}

		/*
		===========
		FS_ConvertPath
		===========
		*/
		void FS_ConvertPath( char *s ) {
			while (*s) {
				if ( *s == '\\' || *s == ':' ) {
					*s = '/';
				}
				s++;
			}
		}

		/*
		===========
		FS_PathCmp

		Ignore case and seprator char distinctions
		===========
		*/
		int FS_PathCmp( const char *s1, const char *s2 ) {
			int		c1, c2;
			
			do {
				c1 = *s1++;
				c2 = *s2++;

				if (c1 >= 'a' && c1 <= 'z') {
					c1 -= ('a' - 'A');
				}
				if (c2 >= 'a' && c2 <= 'z') {
					c2 -= ('a' - 'A');
				}

				if ( c1 == '\\' || c1 == ':' ) {
					c1 = '/';
				}
				if ( c2 == '\\' || c2 == ':' ) {
					c2 = '/';
				}
				
				if (c1 < c2) {
					return -1;		// strings not equal
				}
				if (c1 > c2) {
					return 1;
				}
			} while (c1);
			
			return 0;		// strings are equal
		}

		/*
		================
		FS_SortFileList
		================
		*/
		void FS_SortFileList(char **filelist, int numfiles) {
			int i, j, k, numsortedfiles;
			char **sortedlist;

			sortedlist = (char**) Z_Malloc( ( numfiles + 1 ) * sizeof( *sortedlist ) );
			sortedlist[0] = NULL;
			numsortedfiles = 0;
			for (i = 0; i < numfiles; i++) {
				for (j = 0; j < numsortedfiles; j++) {
					if (FS_PathCmp(filelist[i], sortedlist[j]) < 0) {
						break;
					}
				}
				for (k = numsortedfiles; k > j; k--) {
					sortedlist[k] = sortedlist[k-1];
				}
				sortedlist[j] = filelist[i];
				numsortedfiles++;
			}
			Com_Memcpy(filelist, sortedlist, numfiles * sizeof( *filelist ) );
			Z_Free(sortedlist);
		}

		/*
		================
		FS_NewDir_f
		================
		*/
		void FS_NewDir_f( void ) {
			char	*filter;
			char	**dirnames;
			int		ndirs;
			int		i;

			if ( Cmd_Argc() < 2 ) {
				common.Com_Printf( "usage: fdir <filter>\n" );
				common.Com_Printf( "example: fdir *q3dm*.bsp\n");
				return;
			}

			filter = Cmd_Argv( 1 );

			common.Com_Printf( "---------------\n" );

			dirnames = FS_ListFilteredFiles( "", "", filter, &ndirs );

			FS_SortFileList(dirnames, ndirs);

			for ( i = 0; i < ndirs; i++ ) {
				FS_ConvertPath(dirnames[i]);
				common.Com_Printf( "%s\n", dirnames[i] );
			}
			common.Com_Printf( "%d files listed\n", ndirs );
			FS_FreeFileList( dirnames );
		}

		/*
		============
		FS_Path_f

		============
		*/
		static void FS_Path_f( ) {
			searchpath_t	*s;
			int				i;

			common.Com_Printf ("Current search path:\n");
			for (s = fs_searchpaths; s; s = s.next) {
				if (s.pack) {
					common.Com_Printf ("%s (%i files)\n", s.pack.pakFilename, s.pack.numfiles);
					if ( fs_numServerPaks ) {
						if ( !FS_PakIsPure(s.pack) ) {
							common.Com_Printf( "    not on the pure list\n" );
						} else {
							common.Com_Printf( "    on the pure list\n" );
						}
					}
				} else {
					common.Com_Printf ("%s/%s\n", s.dir.path, s.dir.gamedir );
				}
			}


			common.Com_Printf( "\n" );
			for ( i = 1 ; i < qcommon.MAX_FILE_HANDLES ; i++ ) {
				if ( fsh[i].handleFiles.file.o ) {
					common.Com_Printf( "handle %i: %s\n", i, fsh[i].name );
				}
			}
		}

		/*
		============
		FS_TouchFile_f

		The only purpose of this function is to allow game script files to copy
		arbitrary files furing an "fs_copyfiles 1" run.
		============
		*/
		void FS_TouchFile_f( void ) {
			fileHandle_t	f;

			if ( Cmd_Argc() != 2 ) {
				common.Com_Printf( "Usage: touchFile <file>\n" );
				return;
			}

			FS_FOpenFileRead( Cmd_Argv( 1 ), &f, false );
			if ( f ) {
				FS_FCloseFile( f );
			}
		}

		//===========================================================================


		static int QDECL paksort( const void *a, const void *b ) {
			char	*aa, *bb;

			aa = *(char **)a;
			bb = *(char **)b;

			return FS_PathCmp( aa, bb );
		}

		/*
		================
		FS_AddGameDirectory

		Sets fs_gamedir, adds the directory to the head of the path,
		then loads the zip headers
		================
		*/
		#define	MAX_PAKFILES	1024
		static void FS_AddGameDirectory( const char *path, const char *dir ) {
			searchpath_t	*sp;
			int				i;
			searchpath_t	*search;
			pack_t			*pak;
			char			*pakfile;
			int				numfiles;
			char			**pakfiles;
			char			*sorted[MAX_PAKFILES];

			// this fixes the case where fs_basepath is the same as fs_cdpath
			// which happens on full installs
			for ( sp = fs_searchpaths ; sp ; sp = sp.next ) {
				if ( sp.dir && !Q_stricmp(sp.dir.path, path) && !Q_stricmp(sp.dir.gamedir, dir)) {
					return;			// we've already got this one
				}
			}
			
			Q_strncpyz( fs_gamedir, dir, sizeof( fs_gamedir ) );

			//
			// add the directory to the search path
			//
			search = (searchpath_t*) Z_Malloc (sizeof(searchpath_t));
			search.dir = (directory_t*) Z_Malloc( sizeof( *search.dir ) );

			Q_strncpyz( search.dir.path, path, sizeof( search.dir.path ) );
			Q_strncpyz( search.dir.gamedir, dir, sizeof( search.dir.gamedir ) );
			search.next = fs_searchpaths;
			fs_searchpaths = search;

			// find all pak files in this directory
			pakfile = FS_BuildOSPath( path, dir, "" );
			pakfile[ (int)strlen(pakfile) - 1 ] = 0;	// strip the trailing slash

			pakfiles = Sys_ListFiles( pakfile, ".pk3", NULL, &numfiles, false );

			// sort them so that later alphabetic matches override
			// earlier ones.  This makes pak1.pk3 override pak0.pk3
			if ( numfiles > MAX_PAKFILES ) {
				numfiles = MAX_PAKFILES;
			}
			for ( i = 0 ; i < numfiles ; i++ ) {
				sorted[i] = pakfiles[i];
			}

			qsort( sorted, numfiles, sizeof(void*), paksort );

			for ( i = 0 ; i < numfiles ; i++ ) {
				pakfile = FS_BuildOSPath( path, dir, sorted[i] );
				if ( ( pak = FS_LoadZipFile( pakfile, sorted[i] ) ) == 0 )
					continue;
				// store the game name for downloading
				strcpy(pak.pakGamename, dir);

				search = (searchpath_t*) Z_Malloc (sizeof(searchpath_t));
				search.pack = pak;
				search.next = fs_searchpaths;
				fs_searchpaths = search;
			}

			// done
			Sys_FreeFileList( pakfiles );
		}

		/*
		================
		FS_idPak
		================
		*/
		bool FS_idPak( char *pak, char *base ) {
			int i;

			for (i = 0; i < NUM_ID_PAKS; i++) {
				if ( !FS_FilenameCompare(pak, va("%s/pak%d", base, i)) ) {
					break;
				}
			}
			if (i < NUM_ID_PAKS) {
				return true;
			}
			return false;
		}

		/*
		================
		FS_ComparePaks

		----------------
		dlstring == true

		Returns a list of pak files that we should download from the server. They all get stored
		in the current gamedir and an FS_Restart will be fired up after we download them all.

		The string is the format:

		@remotename@localname [repeat]

		static int		fs_numServerReferencedPaks;
		static int		fs_serverReferencedPaks[MAX_SEARCH_PATHS];
		static char		*fs_serverReferencedPakNames[MAX_SEARCH_PATHS];

		----------------
		dlstring == false

		we are not interested in a download string format, we want something human-readable
		(this is used for diagnostics while connecting to a pure server)

		================
		*/
		bool FS_ComparePaks( char *neededpaks, int len, bool dlstring ) {
			searchpath_t	*sp;
			bool havepak, badchecksum;
			int i;

			if ( !fs_numServerReferencedPaks ) {
				return false; // Server didn't send any pack information along
			}

			*neededpaks = 0;

			for ( i = 0 ; i < fs_numServerReferencedPaks ; i++ ) {
				// Ok, see if we have this pak file
				badchecksum = false;
				havepak = false;

				// never autodownload any of the id paks
				if ( FS_idPak(fs_serverReferencedPakNames[i], "baseq3") || FS_idPak(fs_serverReferencedPakNames[i], "missionpack") ) {
					continue;
				}

				for ( sp = fs_searchpaths ; sp ; sp = sp.next ) {
					if ( sp.pack && sp.pack.checksum == fs_serverReferencedPaks[i] ) {
						havepak = true; // This is it!
						break;
					}
				}

				if ( !havepak && fs_serverReferencedPakNames[i] && *fs_serverReferencedPakNames[i] ) { 
					// Don't got it

		      if (dlstring)
		      {
		        // Remote name
		        Q_strcat( neededpaks, len, "@");
		        Q_strcat( neededpaks, len, fs_serverReferencedPakNames[i] );
		        Q_strcat( neededpaks, len, ".pk3" );

		        // Local name
		        Q_strcat( neededpaks, len, "@");
		        // Do we have one with the same name?
		        if ( FS_SV_FileExists( va( "%s.pk3", fs_serverReferencedPakNames[i] ) ) )
		        {
		          char st[MAX_ZPATH];
		          // We already have one called this, we need to download it to another name
		          // Make something up with the checksum in it
		          Com_sprintf( st, sizeof( st ), "%s.%08x.pk3", fs_serverReferencedPakNames[i], fs_serverReferencedPaks[i] );
		          Q_strcat( neededpaks, len, st );
		        } else
		        {
		          Q_strcat( neededpaks, len, fs_serverReferencedPakNames[i] );
		          Q_strcat( neededpaks, len, ".pk3" );
		        }
		      }
		      else
		      {
		        Q_strcat( neededpaks, len, fs_serverReferencedPakNames[i] );
					  Q_strcat( neededpaks, len, ".pk3" );
		        // Do we have one with the same name?
		        if ( FS_SV_FileExists( va( "%s.pk3", fs_serverReferencedPakNames[i] ) ) )
		        {
		          Q_strcat( neededpaks, len, " (local file exists with wrong checksum)");
		        }
		        Q_strcat( neededpaks, len, "\n");
		      }
				}
			}

			if ( *neededpaks ) {
				return true;
			}

			return false; // We have them all
		}

		/*
		================
		FS_Shutdown

		Frees all resources and closes all files
		================
		*/
		void FS_Shutdown( bool closemfp ) {
			searchpath_t	*p, *next;
			int	i;

			for(i = 0; i < qcommon.MAX_FILE_HANDLES; i++) {
				if (fsh[i].fileSize) {
					FS_FCloseFile(i);
				}
			}

			// free everything
			for ( p = fs_searchpaths ; p ; p = next ) {
				next = p.next;

				if ( p.pack ) {
					unzClose(p.pack.handle);
					Z_Free( p.pack.buildBuffer );
					Z_Free( p.pack );
				}
				if ( p.dir ) {
					Z_Free( p.dir );
				}
				Z_Free( p );
			}

			// any FS_ calls will now be an error until reinitialized
			fs_searchpaths = NULL;

			Cmd_RemoveCommand( "path" );
			Cmd_RemoveCommand( "dir" );
			Cmd_RemoveCommand( "fdir" );
			Cmd_RemoveCommand( "touchFile" );
		}
		 
		/*
		================
		FS_ReorderPurePaks
		NOTE TTimo: the reordering that happens here is not reflected in the cvars (\cvarlist *pak*)
		  this can lead to misleading situations, see https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=540
		================
		*/
		static void FS_ReorderPurePaks()
		{
			searchpath_t *s;
			int i;
			searchpath_t **p_insert_index, // for linked list reordering
				**p_previous; // when doing the scan
			
			// only relevant when connected to pure server
			if ( !fs_numServerPaks )
				return;
			
			fs_reordered = false;
			
			p_insert_index = &fs_searchpaths; // we insert in order at the beginning of the list 
			for ( i = 0 ; i < fs_numServerPaks ; i++ ) {
				p_previous = p_insert_index; // track the pointer-to-current-item
				for (s = *p_insert_index; s; s = s.next) {
					// the part of the list before p_insert_index has been sorted already
					if (s.pack && fs_serverPaks[i] == s.pack.checksum) {
						fs_reordered = true;
						// move this element to the insert list
						*p_previous = s.next;
						s.next = *p_insert_index;
						*p_insert_index = s;
						// increment insert list
						p_insert_index = &s.next;
						break; // iterate to next server pack
					}
					p_previous = &s.next; 
				}
			}
		}

		/*
		================
		FS_Startup
		================
		*/
		static void FS_Startup( const char *gameName ) {
		        const char *homePath;
			cvar_t	*fs;

			common.Com_Printf( "----- FS_Startup -----\n" );

			fs_debug = Cvar_Get( "fs_debug", "0", 0 );
			fs_copyfiles = Cvar_Get( "fs_copyfiles", "0", CVAR_INIT );
			fs_cdpath = Cvar_Get ("fs_cdpath", Sys_DefaultCDPath(), CVAR_INIT );
			fs_basepath = Cvar_Get ("fs_basepath", Sys_DefaultInstallPath(), CVAR_INIT );
			fs_basegame = Cvar_Get ("fs_basegame", "", CVAR_INIT );
		  homePath = Sys_DefaultHomePath();
		  if (!homePath || !homePath[0]) {
				homePath = fs_basepath.string;
			}
			fs_homepath = Cvar_Get ("fs_homepath", homePath, CVAR_INIT );
			fs_gamedirvar = Cvar_Get ("fs_game", "", CVAR_INIT|CVAR_SYSTEMINFO );

			// add search path elements in reverse priority order
			if (fs_cdpath.string[0]) {
				FS_AddGameDirectory( fs_cdpath.string, gameName );
			}
			if (fs_basepath.string[0]) {
				FS_AddGameDirectory( fs_basepath.string, gameName );
			}
		  // fs_homepath is somewhat particular to *nix systems, only add if relevant
		  // NOTE: same filtering below for mods and basegame
			if (fs_basepath.string[0] && Q_stricmp(fs_homepath.string,fs_basepath.string)) {
				FS_AddGameDirectory ( fs_homepath.string, gameName );
			}
		        
			// check for additional base game so mods can be based upon other mods
			if ( fs_basegame.string[0] && !Q_stricmp( gameName, BASEGAME ) && Q_stricmp( fs_basegame.string, gameName ) ) {
				if (fs_cdpath.string[0]) {
					FS_AddGameDirectory(fs_cdpath.string, fs_basegame.string);
				}
				if (fs_basepath.string[0]) {
					FS_AddGameDirectory(fs_basepath.string, fs_basegame.string);
				}
				if (fs_homepath.string[0] && Q_stricmp(fs_homepath.string,fs_basepath.string)) {
					FS_AddGameDirectory(fs_homepath.string, fs_basegame.string);
				}
			}

			// check for additional game folder for mods
			if ( fs_gamedirvar.string[0] && !Q_stricmp( gameName, BASEGAME ) && Q_stricmp( fs_gamedirvar.string, gameName ) ) {
				if (fs_cdpath.string[0]) {
					FS_AddGameDirectory(fs_cdpath.string, fs_gamedirvar.string);
				}
				if (fs_basepath.string[0]) {
					FS_AddGameDirectory(fs_basepath.string, fs_gamedirvar.string);
				}
				if (fs_homepath.string[0] && Q_stricmp(fs_homepath.string,fs_basepath.string)) {
					FS_AddGameDirectory(fs_homepath.string, fs_gamedirvar.string);
				}
			}

			fs = Cvar_Get ("fs_game", "", CVAR_INIT|CVAR_SYSTEMINFO );

			// add our commands
			Cmd_AddCommand ("path", FS_Path_f);
			Cmd_AddCommand ("dir", FS_Dir_f );
			Cmd_AddCommand ("fdir", FS_NewDir_f );
			Cmd_AddCommand ("touchFile", FS_TouchFile_f );

			// https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=506
			// reorder the pure pk3 files according to server order
			FS_ReorderPurePaks();
			
			// print the current search paths
			FS_Path_f();

			fs_gamedirvar.modified = false; // We just loaded, it's not modified

			common.Com_Printf( "----------------------\n" );

			common.Com_Printf( "%d files in pk3 files\n", fs_packFiles );
		}

		/*
		=====================
		FS_GamePureChecksum

		Returns the checksum of the pk3 from which the server loaded the qagame.qvm
		=====================
		*/
		const char *FS_GamePureChecksum( void ) {
			static char	info[MAX_STRING_TOKENS];
			searchpath_t *search;

			info[0] = 0;

			for ( search = fs_searchpaths ; search ; search = search.next ) {
				// is the element a pak file?
				if ( search.pack ) {
					if (search.pack.referenced & FS_QAGAME_REF) {
						Com_sprintf(info, sizeof(info), "%d", search.pack.checksum);
					}
				}
			}

			return info;
		}

		/*
		=====================
		FS_LoadedPakChecksums

		Returns a space separated string containing the checksums of all loaded pk3 files.
		Servers with sv_pure set will get this string and pass it to clients.
		=====================
		*/
		const char *FS_LoadedPakChecksums( void ) {
			static char	info[BIG_INFO_STRING];
			searchpath_t	*search;

			info[0] = 0;

			for ( search = fs_searchpaths ; search ; search = search.next ) {
				// is the element a pak file? 
				if ( !search.pack ) {
					continue;
				}

				Q_strcat( info, sizeof( info ), va("%i ", search.pack.checksum ) );
			}

			return info;
		}

		/*
		=====================
		FS_LoadedPakNames

		Returns a space separated string containing the names of all loaded pk3 files.
		Servers with sv_pure set will get this string and pass it to clients.
		=====================
		*/
		const char *FS_LoadedPakNames( void ) {
			static char	info[BIG_INFO_STRING];
			searchpath_t	*search;

			info[0] = 0;

			for ( search = fs_searchpaths ; search ; search = search.next ) {
				// is the element a pak file?
				if ( !search.pack ) {
					continue;
				}

				if (*info) {
					Q_strcat(info, sizeof( info ), " " );
				}
				Q_strcat( info, sizeof( info ), search.pack.pakBasename );
			}

			return info;
		}

		/*
		=====================
		FS_LoadedPakPureChecksums

		Returns a space separated string containing the pure checksums of all loaded pk3 files.
		Servers with sv_pure use these checksums to compare with the checksums the clients send
		back to the server.
		=====================
		*/
		const char *FS_LoadedPakPureChecksums( void ) {
			static char	info[BIG_INFO_STRING];
			searchpath_t	*search;

			info[0] = 0;

			for ( search = fs_searchpaths ; search ; search = search.next ) {
				// is the element a pak file? 
				if ( !search.pack ) {
					continue;
				}

				Q_strcat( info, sizeof( info ), va("%i ", search.pack.pure_checksum ) );
			}

			return info;
		}

		/*
		=====================
		FS_ReferencedPakChecksums

		Returns a space separated string containing the checksums of all referenced pk3 files.
		The server will send this to the clients so they can check which files should be auto-downloaded. 
		=====================
		*/
		const char *FS_ReferencedPakChecksums( void ) {
			static char	info[BIG_INFO_STRING];
			searchpath_t *search;

			info[0] = 0;


			for ( search = fs_searchpaths ; search ; search = search.next ) {
				// is the element a pak file?
				if ( search.pack ) {
					if (search.pack.referenced || Q_stricmpn(search.pack.pakGamename, BASEGAME, (int)strlen(BASEGAME))) {
						Q_strcat( info, sizeof( info ), va("%i ", search.pack.checksum ) );
					}
				}
			}

			return info;
		}

		/*
		=====================
		FS_ReferencedPakPureChecksums

		Returns a space separated string containing the pure checksums of all referenced pk3 files.
		Servers with sv_pure set will get this string back from clients for pure validation 

		The string has a specific order, "cgame ui @ ref1 ref2 ref3 ..."
		=====================
		*/
		const char *FS_ReferencedPakPureChecksums( void ) {
			static char	info[BIG_INFO_STRING];
			searchpath_t	*search;
			int nFlags, numPaks, checksum;

			info[0] = 0;

			checksum = fs_checksumFeed;
			numPaks = 0;
			for (nFlags = FS_CGAME_REF; nFlags; nFlags = nFlags >> 1) {
				if (nFlags & FS_GENERAL_REF) {
					// add a delimter between must haves and general refs
					//Q_strcat(info, sizeof(info), "@ ");
					info[strlen(info)+1] = '\0';
					info[strlen(info)+2] = '\0';
					info[strlen(info)] = '@';
					info[strlen(info)] = ' ';
				}
				for ( search = fs_searchpaths ; search ; search = search.next ) {
					// is the element a pak file and has it been referenced based on flag?
					if ( search.pack && (search.pack.referenced & nFlags)) {
						Q_strcat( info, sizeof( info ), va("%i ", search.pack.pure_checksum ) );
						if (nFlags & (FS_CGAME_REF | FS_UI_REF)) {
							break;
						}
						checksum ^= search.pack.pure_checksum;
						numPaks++;
					}
				}
				if (fs_fakeChkSum != 0) {
					// only added if a non-pure file is referenced
					Q_strcat( info, sizeof( info ), va("%i ", fs_fakeChkSum ) );
				}
			}
			// last checksum is the encoded number of referenced pk3s
			checksum ^= numPaks;
			Q_strcat( info, sizeof( info ), va("%i ", checksum ) );

			return info;
		}

		/*
		=====================
		FS_ReferencedPakNames

		Returns a space separated string containing the names of all referenced pk3 files.
		The server will send this to the clients so they can check which files should be auto-downloaded. 
		=====================
		*/
		private static string FS_ReferencedPakNames( ) {
			string info = "";//[BIG_INFO_STRING];

			//info[0] = 0;

			// we want to return ALL pk3's from the fs_game path
			// and referenced one's from baseq3
			foreach ( var search in fs_searchpaths ) { 
				// is the element a pak file?
				if ( search.pack != null ) {
					if (info != null) {
						q_shared.Q_strcat(info, sizeof( info ), " " );
					}
					if (search.pack.referenced || Q_stricmpn(search.pack.pakGamename, BASEGAME, (int)strlen(BASEGAME))) {
						q_shared.Q_strcat( info, sizeof( info ), search.pack.pakGamename );
						q_shared.Q_strcat( info, sizeof( info ), "/" );
						q_shared.Q_strcat( info, sizeof( info ), search.pack.pakBasename );
					}
				}
			}

			return info;
		}

		/*
		=====================
		FS_ClearPakReferences
		=====================
		*/
		private static void FS_ClearPakReferences( int flags ) {
			if ( flags == 0 ) {
				flags = -1;
			}
			foreach ( var search in fs_searchpaths ) { 
				// is the element a pak file and has it been referenced?
				if ( search.pack ) {
					search.pack.referenced &= ~flags;
				}
			}
		}


		/*
		=====================
		FS_PureServerSetLoadedPaks

		If the string is empty, all data sources will be allowed.
		If not empty, only pk3 files that match one of the space
		separated checksums will be checked for files, with the
		exception of .cfg and .dat files.
		=====================
		*/
		private static void FS_PureServerSetLoadedPaks( string pakSums, string pakNames ) {
			int		i, c, d;

			Cmd_TokenizeString( pakSums );

			c = Cmd_Argc();
			if ( c > MAX_SEARCH_PATHS ) {
				c = MAX_SEARCH_PATHS;
			}

			fs_numServerPaks = c;

			for ( i = 0 ; i < c ; i++ ) {
				fs_serverPaks[i] = atoi( Cmd_Argv( i ) );
			}

			if (fs_numServerPaks) {
				common.Com_DPrintf( "Connected to a pure server.\n" );
			}
			else
			{
				if (fs_reordered)
				{
					// https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=540
					// force a restart to make sure the search order will be correct
					common.Com_DPrintf( "FS search reorder is required\n" );
					FS_Restart(fs_checksumFeed);
					return;
				}
			}

			for ( i = 0 ; i < c ; i++ ) {
				if (fs_serverPakNames[i]) {
					Z_Free(fs_serverPakNames[i]);
				}
				fs_serverPakNames[i] = null;
			}
			if ( pakNames != null && pakNames.Length > 0 ) {
				Cmd_TokenizeString( pakNames );

				d = Cmd_Argc();
				if ( d > MAX_SEARCH_PATHS ) {
					d = MAX_SEARCH_PATHS;
				}

				for ( i = 0 ; i < d ; i++ ) {
					fs_serverPakNames[i] = CopyString( Cmd_Argv( i ) );
				}
			}
		}

		/*
		=====================
		FS_PureServerSetReferencedPaks

		The checksums and names of the pk3 files referenced at the server
		are sent to the client and stored here. The client will use these
		checksums to see if any pk3 files need to be auto-downloaded. 
		=====================
		*/
		public static void FS_PureServerSetReferencedPaks( string pakSums, string pakNames ) {
			int		i, c, d;

			Cmd_TokenizeString( pakSums );

			c = Cmd_Argc();
			if ( c > MAX_SEARCH_PATHS ) {
				c = MAX_SEARCH_PATHS;
			}

			fs_numServerReferencedPaks = c;

			for ( i = 0 ; i < c ; i++ ) {
				fs_serverReferencedPaks[i] = atoi( Cmd_Argv( i ) );
			}

			for ( i = 0 ; i < c ; i++ ) {
				if (fs_serverReferencedPakNames[i]) {
					Z_Free(fs_serverReferencedPakNames[i]);
				}
				fs_serverReferencedPakNames[i] = null;
			}
			if ( pakNames != null && pakNames.Length > 0 ) {
				Cmd_TokenizeString( pakNames );

				d = Cmd_Argc();
				if ( d > MAX_SEARCH_PATHS ) {
					d = MAX_SEARCH_PATHS;
				}

				for ( i = 0 ; i < d ; i++ ) {
					fs_serverReferencedPakNames[i] = CopyString( Cmd_Argv( i ) );
				}
			}
		}

		/*
		================
		FS_InitFilesystem

		Called only at inital startup, not when the filesystem
		is resetting due to a game change
		================
		*/
		public static void FS_InitFilesystem( ) {
			// allow command line parms to override our defaults
			// we have to specially handle this, because normal command
			// line variable sets don't happen until after the filesystem
			// has already been initialized
			common.Com_StartupVariable( "fs_cdpath" );
			common.Com_StartupVariable( "fs_basepath" );
			common.Com_StartupVariable( "fs_homepath" );
			common.Com_StartupVariable( "fs_game" );
			common.Com_StartupVariable( "fs_copyfiles" );

			// try to start up normally
			FS_Startup( BASEGAME );

			// if we can't find default.cfg, assume that the paths are
			// busted and error out now, rather than getting an unreadable
			// graphics screen when the font fails to load
			if ( FS_ReadFile( "default.cfg", null ) <= 0 ) {
				common.Com_Error( errorParm_t.ERR_FATAL, "Couldn't load default.cfg" );
				// bk001208 - SafeMode see below, FIXME?
			}

			q_shared.Q_strncpyz(lastValidBase, fs_basepath.string, sizeof(lastValidBase));
			q_shared.Q_strncpyz(lastValidGame, fs_gamedirvar.string, sizeof(lastValidGame));

		  // bk001208 - SafeMode see below, FIXME?
		}


		/*
		================
		FS_Restart
		================
		*/
		void FS_Restart( int checksumFeed ) {

			// free anything we currently have loaded
			FS_Shutdown(false);

			// set the checksum feed
			fs_checksumFeed = checksumFeed;

			// clear pak references
			FS_ClearPakReferences(0);

			// try to start up normally
			FS_Startup( BASEGAME );

			// if we can't find default.cfg, assume that the paths are
			// busted and error out now, rather than getting an unreadable
			// graphics screen when the font fails to load
			if ( FS_ReadFile( "default.cfg", NULL ) <= 0 ) {
				// this might happen when connecting to a pure server not using BASEGAME/pak0.pk3
				// (for instance a TA demo server)
				if (lastValidBase[0]) {
					FS_PureServerSetLoadedPaks("", "");
					Cvar_Set("fs_basepath", lastValidBase);
					Cvar_Set("fs_gamedirvar", lastValidGame);
					lastValidBase[0] = '\0';
					lastValidGame[0] = '\0';
					FS_Restart(checksumFeed);
					common.Com_Error( errorParm_t.ERR_DROP, "Invalid game folder\n" );
					return;
				}
				common.Com_Error( errorParm_t.ERR_FATAL, "Couldn't load default.cfg" );
			}

			// bk010116 - new check before safeMode
			if ( Q_stricmp(fs_gamedirvar.@string, lastValidGame) ) {
				// skip the q3config.cfg if "safe" is on the command line
				if ( !common.Com_SafeMode() ) {
					Cbuf_AddText ("exec q3config.cfg\n");
				}
			}

			Q_strncpyz(lastValidBase, fs_basepath.string, sizeof(lastValidBase));
			Q_strncpyz(lastValidGame, fs_gamedirvar.string, sizeof(lastValidGame));

		}

		/*
		=================
		FS_ConditionalRestart
		restart if necessary
		=================
		*/
		public static bool FS_ConditionalRestart( int checksumFeed ) {
			if( fs_gamedirvar.modified || checksumFeed != fs_checksumFeed ) {
				FS_Restart( checksumFeed );
				return true;
			}
			return false;
		}

		/*
		========================================================================================

		Handle based file calls for virtual machines

		========================================================================================
		*/

		public static int FS_FOpenFileByMode( string qpath, fileHandle_t f, q_shared.fsMode_t mode ) {
			int		r = 0;
			bool	sync;

			sync = false;

			switch( mode ) 
			{
				case q_shared.fsMode_t.FS_READ:
					r = FS_FOpenFileRead( qpath, f, true );
					break;
				case q_shared.fsMode_t.FS_WRITE:
					f = FS_FOpenFileWrite( qpath );
					r = 0;
					if (f.ID == 0) {
						r = -1;
					}
					break;
				case q_shared.fsMode_t.FS_APPEND_SYNC:
					sync = true;
					break;
				case q_shared.fsMode_t.FS_APPEND:
					f = FS_FOpenFileAppend( qpath );
					r = 0;
					if (f.ID == 0) {
						r = -1;
					}
					break;
				default:
					common.Com_Error( errorParm_t.ERR_FATAL, "FSH_FOpenFile: bad mode" );
					return -1;
			}

			if (f.ID == 0) {
				return r;
			}

			if ( f.ID > 0 ) {
				if (fsh[f.ID].zipFile == true) {
					fsh[f.ID].baseOffset = unztell(fsh[f.ID].handleFiles.file.z);
				} else {
					fsh[f.ID].baseOffset = ftell(fsh[f.ID].handleFiles.file.o);
				}
				fsh[f.ID].fileSize = r;
				fsh[f.ID].streamed = false;

				if (mode == q_shared.fsMode_t.FS_READ ) {
					Sys_BeginStreamedFile( f, 0x4000 );
					fsh[f.ID].streamed = true;
				}
			}
			fsh[f.ID].handleSync = sync;

			return r;
		}

		int		FS_FTell( fileHandle_t f ) {
			int pos;
			if (fsh[f].zipFile == true) {
				pos = unztell(fsh[f].handleFiles.file.z);
			} else {
				pos = ftell(fsh[f].handleFiles.file.o);
			}
			return pos;
		}

		void	FS_Flush( fileHandle_t f ) {
			fflush(fsh[f].handleFiles.file.o);
		}
	}
}
