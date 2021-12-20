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

namespace SharpQ3.Engine.qcommon
{
	using System;

	// cmd.c -- Quake script command processing module
	public static class cmd
	{
		public const int MAX_CMD_BUFFER = 16384;
		public const int MAX_CMD_LINE = 1024;

		public struct cmd_t
		{
			public byte[]	data;
			public int maxsize;
			public int cursize;
		}

		private static int			cmd_wait;
		private static cmd_t cmd_text;
		private static byte cmd_text_buf[MAX_CMD_BUFFER];


		//=============================================================================

		/*
		============
		Cmd_Wait_f

		Causes execution of the remainder of the command buffer to be delayed until
		next frame.  This allows commands like:
		bind g "cmd use rocket ; +attack ; wait ; -attack ; cmd use blaster"
		============
		*/
		private static void Cmd_Wait_f( ) {
			if ( Cmd_Argc() == 2 ) {
				cmd_wait = atoi( Cmd_Argv( 1 ) );
			} else {
				cmd_wait = 1;
			}
		}


		/*
		=============================================================================

								COMMAND BUFFER

		=============================================================================
		*/

		/*
		============
		Cbuf_Init
		============
		*/
		private static void Cbuf_Init ()
		{
			cmd_text.data = cmd_text_buf;
			cmd_text.maxsize = MAX_CMD_BUFFER;
			cmd_text.cursize = 0;
		}

		/*
		============
		Cbuf_AddText

		Adds command text at the end of the buffer, does NOT add a final \n
		============
		*/
		public static void Cbuf_AddText( string text )
		{
			int		l;
			
			l = (int) text.Length;

			if (cmd_text.cursize + l >= cmd_text.maxsize)
			{
				com.Com_Printf ("Cbuf_AddText: overflow\n");
				return;
			}
			com.Com_Memcpy(&cmd_text.data[cmd_text.cursize], text, l);
			cmd_text.cursize += l;
		}


		/*
		============
		Cbuf_InsertText

		Adds command text immediately after the current command
		Adds a \n to the text
		============
		*/
		public static void Cbuf_InsertText( string text ) 
		{
			int		len;
			int		i;

			len = (int) text.Length + 1;
			if ( len + cmd_text.cursize > cmd_text.maxsize ) {
				com.Com_Printf( "Cbuf_InsertText overflowed\n" );
				return;
			}

			// move the existing command text
			for ( i = cmd_text.cursize - 1 ; i >= 0 ; i-- ) {
				cmd_text.data[ i + len ] = cmd_text.data[ i ];
			}

			// copy the new text in
			com/Com_Memcpy( cmd_text.data, text, len - 1 );

			// add a \n
			cmd_text.data[ len - 1 ] = (byte)'\n';

			cmd_text.cursize += len;
		}


		/*
		============
		Cbuf_ExecuteText
		============
		*/
		private static void Cbuf_ExecuteText (int exec_when, string text)
		{
			switch (exec_when)
			{
			case EXEC_NOW:
				if (text && (int) text.Length > 0) {
					Cmd_ExecuteString (text);
				} else {
					Cbuf_Execute();
				}
				break;
			case EXEC_INSERT:
				Cbuf_InsertText (text);
				break;
			case EXEC_APPEND:
				Cbuf_AddText (text);
				break;
			default:
				Com_Error (ERR_FATAL, "Cbuf_ExecuteText: bad exec_when");
			}
		}

		/*
		============
		Cbuf_Execute
		============
		*/
		private static void Cbuf_Execute ()
		{
			int		i;
			char	*text;
			char	line[MAX_CMD_LINE];
			int		quotes;

			while (cmd_text.cursize)
			{
				if ( cmd_wait )	{
					// skip out while text still remains in buffer, leaving it
					// for next frame
					cmd_wait--;
					break;
				}

				// find a \n or ; line break
				text = (char *)cmd_text.data;

				quotes = 0;
				for (i=0 ; i< cmd_text.cursize ; i++)
				{
					if (text[i] == '"')
						quotes++;
					if ( !(quotes&1) &&  text[i] == ';')
						break;	// don't break if inside a quoted string
					if (text[i] == '\n' || text[i] == '\r' )
						break;
				}

				if( i >= (MAX_CMD_LINE - 1)) {
					i = MAX_CMD_LINE - 1;
				}
						
				Com_Memcpy (line, text, i);
				line[i] = 0;
				
		// delete the text from the command buffer and move remaining commands down
		// this is necessary because commands (exec) can insert data at the
		// beginning of the text buffer

				if (i == cmd_text.cursize)
					cmd_text.cursize = 0;
				else
				{
					i++;
					cmd_text.cursize -= i;
					memmove (text, text+i, cmd_text.cursize);
				}

		// execute the command line

				Cmd_ExecuteString (line);		
			}
		}


		/*
		==============================================================================

								SCRIPT COMMANDS

		==============================================================================
		*/


		/*
		===============
		Cmd_Exec_f
		===============
		*/
		private static void Cmd_Exec_f( ) 
		{
			char	*f;
			int		len;
			char	filename[MAX_QPATH];

			if (Cmd_Argc () != 2) {
				Com_Printf ("exec <filename> : execute a script file\n");
				return;
			}

			Q_strncpyz( filename, Cmd_Argv(1), sizeof( filename ) );
			COM_DefaultExtension( filename, sizeof( filename ), ".cfg" ); 
			len = FS_ReadFile( filename, (void **)&f);
			if (!f) {
				Com_Printf ("couldn't exec %s\n",Cmd_Argv(1));
				return;
			}
			Com_Printf ("execing %s\n",Cmd_Argv(1));
			
			Cbuf_InsertText (f);

			FS_FreeFile (f);
		}


		/*
		===============
		Cmd_Vstr_f

		Inserts the current value of a variable as command text
		===============
		*/
		private static void Cmd_Vstr_f( ) {
			char	*v;

			if (Cmd_Argc () != 2) {
				Com_Printf ("vstr <variablename> : execute a variable command\n");
				return;
			}

			v = Cvar_VariableString( Cmd_Argv( 1 ) );
			Cbuf_InsertText( va("%s\n", v ) );
		}


		/*
		===============
		Cmd_Echo_f

		Just prints the rest of the line to the console
		===============
		*/
		private static void Cmd_Echo_f ()
		{
			int		i;
			
			for (i=1 ; i<Cmd_Argc() ; i++)
				Com_Printf ("%s ",Cmd_Argv(i));
			Com_Printf ("\n");
		}


		/*
		=============================================================================

							COMMAND EXECUTION

		=============================================================================
		*/

		public class cmd_function_t
		{
			public cmd_function_t next;
			public string name;
			public Action function;
		}


		private static	int			cmd_argc;
		private static string[] cmd_argv;//[MAX_STRING_TOKENS];       // points into cmd_tokenized
		private static string cmd_tokenized;//[BIG_INFO_STRING+MAX_STRING_TOKENS];   // will have 0 bytes inserted
		private static string cmd_cmd;//[BIG_INFO_STRING]; // the original command we received (no token processing)

		private static cmd_function_t cmd_functions;        // possible commands to execute

		/*
		============
		Cmd_Argc
		============
		*/
		public static int Cmd_Argc( ) {
			return cmd_argc;
		}

		/*
		============
		Cmd_Argv
		============
		*/
		public static string Cmd_Argv( int arg ) {
			if ( (unsigned)arg >= cmd_argc ) {
				return "";
			}
			return cmd_argv[arg];	
		}

		/*
		============
		Cmd_ArgvBuffer

		The interpreted versions use this because
		they can't have pointers returned to them
		============
		*/
		public static void Cmd_ArgvBuffer( int arg, string buffer, int bufferLength ) {
			Q_strncpyz( buffer, Cmd_Argv( arg ), bufferLength );
		}


		/*
		============
		Cmd_Args

		Returns a single string containing argv(1) to argv(argc()-1)
		============
		*/
		public static string Cmd_Args( ) {
			static	char		cmd_args[MAX_STRING_CHARS];
			int		i;

			cmd_args[0] = 0;
			for ( i = 1 ; i < cmd_argc ; i++ ) {
				strcat( cmd_args, cmd_argv[i] );
				if ( i != cmd_argc-1 ) {
					strcat( cmd_args, " " );
				}
			}

			return cmd_args;
		}

		/*
		============
		Cmd_Args

		Returns a single string containing argv(arg) to argv(argc()-1)
		============
		*/
		private static string Cmd_ArgsFrom( int arg ) {
			static	char		cmd_args[BIG_INFO_STRING];
			int		i;

			cmd_args[0] = 0;
			if (arg < 0)
				arg = 0;
			for ( i = arg ; i < cmd_argc ; i++ ) {
				strcat( cmd_args, cmd_argv[i] );
				if ( i != cmd_argc-1 ) {
					strcat( cmd_args, " " );
				}
			}

			return cmd_args;
		}

		/*
		============
		Cmd_ArgsBuffer

		The interpreted versions use this because
		they can't have pointers returned to them
		============
		*/
		private static void Cmd_ArgsBuffer( string buffer, int bufferLength ) {
			Q_strncpyz( buffer, Cmd_Args(), bufferLength );
		}

		/*
		============
		Cmd_Cmd

		Retrieve the unmodified command string
		For rcon use when you want to transmit without altering quoting
		https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=543
		============
		*/
		private static string Cmd_Cmd()
		{
			return cmd_cmd;
		}

		/*
		============
		Cmd_TokenizeString

		Parses the given string into command line tokens.
		The text is copied to a seperate buffer and 0 characters
		are inserted in the apropriate place, The argv array
		will point into this temporary buffer.
		============
		*/
		public static void Cmd_TokenizeString( string text_in ) {
			const char	*text;
			char	*textOut;

			// clear previous args
			cmd_argc = 0;

			if ( !text_in ) {
				return;
			}
			
			Q_strncpyz( cmd_cmd, text_in, sizeof(cmd_cmd) );

			text = text_in;
			textOut = cmd_tokenized;

			while ( 1 ) {
				if ( cmd_argc == MAX_STRING_TOKENS ) {
					return;			// this is usually something malicious
				}

				while ( 1 ) {
					// skip whitespace
					while ( *text && *text <= ' ' ) {
						text++;
					}
					if ( !*text ) {
						return;			// all tokens parsed
					}

					// skip // comments
					if ( text[0] == '/' && text[1] == '/' ) {
						return;			// all tokens parsed
					}

					// skip /* */ comments
					if ( text[0] == '/' && text[1] =='*' ) {
						while ( *text && ( text[0] != '*' || text[1] != '/' ) ) {
							text++;
						}
						if ( !*text ) {
							return;		// all tokens parsed
						}
						text += 2;
					} else {
						break;			// we are ready to parse a token
					}
				}

				// handle quoted strings
		    // NOTE TTimo this doesn't handle \" escaping
				if ( *text == '"' ) {
					cmd_argv[cmd_argc] = textOut;
					cmd_argc++;
					text++;
					while ( *text && *text != '"' ) {
						*textOut++ = *text++;
					}
					*textOut++ = 0;
					if ( !*text ) {
						return;		// all tokens parsed
					}
					text++;
					continue;
				}

				// regular token
				cmd_argv[cmd_argc] = textOut;
				cmd_argc++;

				// skip until whitespace, quote, or command
				while ( *text > ' ' ) {
					if ( text[0] == '"' ) {
						break;
					}

					if ( text[0] == '/' && text[1] == '/' ) {
						break;
					}

					// skip /* */ comments
					if ( text[0] == '/' && text[1] =='*' ) {
						break;
					}

					*textOut++ = *text++;
				}

				*textOut++ = 0;

				if ( !*text ) {
					return;		// all tokens parsed
				}
			}
			
		}


		/*
		============
		Cmd_AddCommand
		============
		*/
		private static void Cmd_AddCommand( string cmd_name, Action function ) {
			cmd_function_t	*cmd;
			
			// fail if the command already exists
			for ( cmd = cmd_functions ; cmd ; cmd=cmd->next ) {
				if ( !strcmp( cmd_name, cmd->name ) ) {
					// allow completion-only commands to be silently doubled
					if ( function != NULL ) {
						Com_Printf ("Cmd_AddCommand: %s already defined\n", cmd_name);
					}
					return;
				}
			}

			// use a small malloc to avoid zone fragmentation
			cmd = (cmd_function_t*) S_Malloc(sizeof(cmd_function_t));
			cmd->name = CopyString( cmd_name );
			cmd->function = function;
			cmd->next = cmd_functions;
			cmd_functions = cmd;
		}

		/*
		============
		Cmd_RemoveCommand
		============
		*/
		private static void Cmd_RemoveCommand( string cmd_name ) {
			cmd_function_t	*cmd, **back;

			back = &cmd_functions;
			while( 1 ) {
				cmd = *back;
				if ( !cmd ) {
					// command wasn't active
					return;
				}
				if ( !strcmp( cmd_name, cmd->name ) ) {
					*back = cmd->next;
					if (cmd->name) {
						Z_Free(cmd->name);
					}
					Z_Free (cmd);
					return;
				}
				back = &cmd->next;
			}
		}


		/*
		============
		Cmd_CommandCompletion
		============
		*/
		private static void Cmd_CommandCompletion( void(*callback)(const char *s) ) {
			cmd_function_t	*cmd;
			
			for (cmd=cmd_functions ; cmd ; cmd=cmd->next) {
				callback( cmd->name );
			}
		}


		/*
		============
		Cmd_ExecuteString

		A complete command line has been parsed, so try to execute it
		============
		*/
		private static void	Cmd_ExecuteString( string text ) {	
			cmd_function_t	*cmd, **prev;

			// execute the command line
			Cmd_TokenizeString( text );		
			if ( !Cmd_Argc() ) {
				return;		// no tokens
			}

			// check registered command functions	
			for ( prev = &cmd_functions ; *prev ; prev = &cmd->next ) {
				cmd = *prev;
				if ( !Q_stricmp( cmd_argv[0],cmd->name ) ) {
					// rearrange the links so that the command will be
					// near the head of the list next time it is used
					*prev = cmd->next;
					cmd->next = cmd_functions;
					cmd_functions = cmd;

					// perform the action
					if ( !cmd->function ) {
						// let the cgame or game handle it
						break;
					} else {
						cmd->function ();
					}
					return;
				}
			}
			
			// check cvars
			if ( Cvar_Command() ) {
				return;
			}

			// check client game commands
			if ( com_cl_running && com_cl_running->integer && CL_GameCommand() ) {
				return;
			}

			// check server game commands
			if ( com_sv_running && com_sv_running->integer && SV_GameCommand() ) {
				return;
			}

			// check ui commands
			if ( com_cl_running && com_cl_running->integer && UI_GameCommand() ) {
				return;
			}

			// send it as a server command if we are connected
			// this will usually result in a chat message
			CL_ForwardCommandToServer ( text );
		}

		/*
		============
		Cmd_List_f
		============
		*/
		private static void Cmd_List_f ()
		{
			cmd_function_t	*cmd;
			int				i;
			char			*match;

			if ( Cmd_Argc() > 1 ) {
				match = Cmd_Argv( 1 );
			} else {
				match = NULL;
			}

			i = 0;
			for (cmd=cmd_functions ; cmd ; cmd=cmd->next) {
				if (match && !Com_Filter(match, cmd->name, false)) continue;

				Com_Printf ("%s\n", cmd->name);
				i++;
			}
			Com_Printf ("%i commands\n", i);
		}

		/*
		============
		Cmd_Init
		============
		*/
		private static void Cmd_Init () {
			Cmd_AddCommand ("cmdlist",Cmd_List_f);
			Cmd_AddCommand ("exec",Cmd_Exec_f);
			Cmd_AddCommand ("vstr",Cmd_Vstr_f);
			Cmd_AddCommand ("echo",Cmd_Echo_f);
			Cmd_AddCommand ("wait", Cmd_Wait_f);
		}
	}
}
