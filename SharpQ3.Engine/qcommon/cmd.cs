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
    using System.Text;
    using static SharpQ3.Engine.q_shared;

    // cmd.c -- Quake script command processing module
    public static class cmd
    {
        public const int MAX_CMD_BUFFER = 16384;
        public const int MAX_CMD_LINE = 1024;

        public struct cmd_t
        {
            public StringBuilder data;
            public int maxsize;
            public int cursize;
        }

        private static int cmd_wait;
        private static cmd_t cmd_text;
        private static StringBuilder cmd_text_buf = new StringBuilder( MAX_CMD_BUFFER );

        public static Func<bool> OnCLGameCommand;
        public static Func<bool> OnSVGameCommand;
        public static Func<bool> OnUIGameCommand;
        public static Action<string> OnCLForwardCommandToServer;

        //=============================================================================

        /*
		============
		Cmd_Wait_f

		Causes execution of the remainder of the command buffer to be delayed until
		next frame.  This allows commands like:
		bind g "cmd use rocket ; +attack ; wait ; -attack ; cmd use blaster"
		============
		*/
        private static void Cmd_Wait_f( )
        {
            if ( Cmd_Argc( ) == 2 )
                int.TryParse( Cmd_Argv( 1 ), out cmd_wait );
            else
                cmd_wait = 1;
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
        public static void Cbuf_Init( )
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
            var l = text.Length;

            if ( cmd_text.cursize + l >= cmd_text.maxsize )
            {
                common.Com_Printf( "Cbuf_AddText: overflow\n" );
                return;
            }
            cmd_text.data.Append( text );
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
            int len;
            int i;

            len = ( int ) text.Length + 1;
            if ( len + cmd_text.cursize > cmd_text.maxsize )
            {
                common.Com_Printf( "Cbuf_InsertText overflowed\n" );
                return;
            }

            // copy the new text in
            cmd_text.data.Insert( 0, text + '\n' );
            cmd_text.cursize += len;
        }


        /*
		============
		Cbuf_ExecuteText
		============
		*/
        public static void Cbuf_ExecuteText( cbufExec_t exec_when, string text )
        {
            switch ( exec_when )
            {
                case cbufExec_t.EXEC_NOW:
                    if ( text?.Length > 0 )
                        Cmd_ExecuteString( text );
                    else
                        Cbuf_Execute( );
                    break;

                case cbufExec_t.EXEC_INSERT:
                    Cbuf_InsertText( text );
                    break;

                case cbufExec_t.EXEC_APPEND:
                    Cbuf_AddText( text );
                    break;

                default:
                    common.Com_Error( errorParm_t.ERR_FATAL, "Cbuf_ExecuteText: bad exec_when" );
                    break;
            }
        }

        /*
		============
		Cbuf_Execute
		============
		*/
        public static void Cbuf_Execute( )
        {
            int i;
            StringBuilder text;
            StringBuilder line = new StringBuilder( MAX_CMD_LINE );
            int quotes;

            while ( cmd_text.cursize > 0 )
            {
                if ( cmd_wait > 0 )
                {
                    // skip out while text still remains in buffer, leaving it
                    // for next frame
                    cmd_wait--;
                    break;
                }

                // find a \n or ; line break
                text = cmd_text.data;

                quotes = 0;
                for ( i = 0; i < cmd_text.cursize; i++ )
                {
                    if ( text[i] == '"' )
                        quotes++;
                    if ( ( quotes & 1 ) == 0 && text[i] == ';' )
                        break;  // don't break if inside a quoted string
                    if ( text[i] == '\n' || text[i] == '\r' )
                        break;
                }

                if ( i >= ( MAX_CMD_LINE - 1 ) )
                {
                    i = MAX_CMD_LINE - 1;
                }

                line.Insert( 0, text.ToString( ), i );

                // delete the text from the command buffer and move remaining commands down
                // this is necessary because commands (exec) can insert data at the
                // beginning of the text buffer

                if ( i == cmd_text.cursize )
                    cmd_text.cursize = 0;
                else
                {
                    i++;
                    cmd_text.cursize -= i;

                    text = text.Remove( 0, cmd_text.cursize );
                    //memmove (text, text+i, cmd_text.cursize);
                }

                // execute the command line
                Cmd_ExecuteString( line.ToString( ) );
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
            if ( Cmd_Argc( ) != 2 )
            {
                common.Com_Printf( "exec <filename> : execute a script file\n" );
                return;
            }

            Q_strncpyz( out var filename, Cmd_Argv( 1 ), MAX_QPATH );
            COM_DefaultExtension( ref filename, MAX_QPATH, ".cfg" );
            var len = files.FS_ReadFile( filename, out var f, out var h );
            if ( f == null )
            {
                common.Com_Printf( "couldn't exec %s\n", Cmd_Argv( 1 ) );
                return;
            }
            common.Com_Printf( "execing %s\n", Cmd_Argv( 1 ) );

            Cbuf_InsertText( Encoding.ASCII.GetString( f ) );

            files.FS_FreeFile( h );
        }


        /*
		===============
		Cmd_Vstr_f

		Inserts the current value of a variable as command text
		===============
		*/
        private static void Cmd_Vstr_f( )
        {
            if ( Cmd_Argc( ) != 2 )
            {
                common.Com_Printf( "vstr <variablename> : execute a variable command\n" );
                return;
            }

            var v = Cvar.Cvar_VariableString( Cmd_Argv( 1 ) );
            Cbuf_InsertText( $"{v}\n" );
        }


        /*
		===============
		Cmd_Echo_f

		Just prints the rest of the line to the console
		===============
		*/
        private static void Cmd_Echo_f( )
        {
            for ( var i = 1; i < Cmd_Argc( ); i++ )
                common.Com_Printf( "%s ", Cmd_Argv( i ) );
            common.Com_Printf( "\n" );
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


        private static int cmd_argc;
        private static string[] cmd_argv;//[MAX_STRING_TOKENS];       // points into cmd_tokenized
        private static string cmd_tokenized;//[BIG_INFO_STRING+MAX_STRING_TOKENS];   // will have 0 bytes inserted
        private static string cmd_cmd;//[BIG_INFO_STRING]; // the original command we received (no token processing)

        private static cmd_function_t cmd_functions;        // possible commands to execute

        /*
		============
		Cmd_Argc
		============
		*/
        public static int Cmd_Argc( )
        {
            return cmd_argc;
        }

        /*
		============
		Cmd_Argv
		============
		*/
        public static string Cmd_Argv( int arg )
        {
            if ( ( uint ) arg >= cmd_argc )
            {
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
        public static void Cmd_ArgvBuffer( int arg, out string buffer, int bufferLength )
        {
            Q_strncpyz( out buffer, Cmd_Argv( arg ), bufferLength );
        }


        /*
		============
		Cmd_Args

		Returns a single string containing argv(1) to argv(argc()-1)
		============
		*/
        public static string Cmd_Args( )
        {
            var cmd_args = new StringBuilder( MAX_STRING_CHARS );

            for ( var i = 1; i < cmd_argc; i++ )
            {
                cmd_args.Append( cmd_argv[i] );
                if ( i != cmd_argc - 1 )
                {
                    cmd_args.Append( " " );
                }
            }

            return cmd_args.ToString( );
        }

        /*
		============
		Cmd_Args

		Returns a single string containing argv(arg) to argv(argc()-1)
		============
		*/
        private static string Cmd_ArgsFrom( int arg )
        {

            var cmd_args = new StringBuilder( MAX_STRING_CHARS );

            if ( arg < 0 )
                arg = 0;
            for ( var i = arg; i < cmd_argc; i++ )
            {
                cmd_args.Append( cmd_argv[i] );
                if ( i != cmd_argc - 1 )
                {
                    cmd_args.Append( " " );
                }
            }

            return cmd_args.ToString( );
        }

        /*
		============
		Cmd_ArgsBuffer

		The interpreted versions use this because
		they can't have pointers returned to them
		============
		*/
        private static void Cmd_ArgsBuffer( out string buffer, int bufferLength )
        {
            Q_strncpyz( out buffer, Cmd_Args( ), bufferLength );
        }

        /*
		============
		Cmd_Cmd

		Retrieve the unmodified command string
		For rcon use when you want to transmit without altering quoting
		https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=543
		============
		*/
        private static string Cmd_Cmd( )
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
        public static void Cmd_TokenizeString( string text_in )
        {
            char[] text;
            char[] textOut;
            int tI = 0, tO = 0;

            // clear previous args
            cmd_argc = 0;

            if ( text_in == null )
                return;

            Q_strncpyz( out cmd_cmd, text_in, cmd_cmd.Length );

            text = text_in.ToCharArray( );
            textOut = cmd_tokenized.ToCharArray( );

            while ( true )
            {
                if ( cmd_argc == MAX_STRING_TOKENS )
                {
                    return;         // this is usually something malicious
                }

                while ( true )
                {
                    // skip whitespace
                    while ( tI < text.Length && text[tI] <= ' ' )
                    {
                        tI++;
                    }
                    if ( tI >= text.Length )
                        return;         // all tokens parsed

                    // skip // comments
                    if ( text[tI] == '/' && text[tI + 1] == '/' )
                    {
                        return;         // all tokens parsed
                    }

                    // skip /* */ comments
                    if ( text[tI] == '/' && text[tI + 1] == '*' )
                    {
                        while ( tI < text.Length && ( text[tI] != '*' || text[tI + 1] != '/' ) )
                        {
                            tI++;
                        }
                        if ( tI >= text.Length )
                        {
                            return;     // all tokens parsed
                        }
                        tI += 2;
                    }
                    else
                    {
                        break;          // we are ready to parse a token
                    }
                }

                // handle quoted strings
                // NOTE TTimo this doesn't handle \" escaping
                if ( text[tI] == '"' )
                {
                    cmd_argv[cmd_argc] = textOut.ToString( );
                    cmd_argc++;
                    tI++;
                    while ( tI < text.Length && text[tI] != '"' )
                    {
                        textOut[tO++] = text[tI++];
                    }
                    textOut[tO++] = ( char ) 0;
                    if ( tI >= text.Length )
                        return;     // all tokens parsed

                    tI++;
                    continue;
                }

                // regular token
                cmd_argv[cmd_argc] = textOut.ToString( );
                cmd_argc++;

                // skip until whitespace, quote, or command
                while ( text[tI] > ' ' )
                {
                    if ( text[tI] == '"' )
                    {
                        break;
                    }

                    if ( text[tI] == '/' && text[tI + 1] == '/' )
                    {
                        break;
                    }

                    // skip /* */ comments
                    if ( text[0] == '/' && text[tI + 1] == '*' )
                    {
                        break;
                    }

                    textOut[tO++] = text[tI++];
                }

                textOut[tO++] = ( char ) 0;

                if ( tI >= text.Length )
                    return;     // all tokens parsed
            }
        }


        /*
		============
		Cmd_AddCommand
		============
		*/
        public static void Cmd_AddCommand( string cmd_name, Action function )
        {
            cmd_function_t cmd;

            // fail if the command already exists
            for ( cmd = cmd_functions; cmd != null; cmd = cmd.next )
            {
                if ( cmd_name == cmd.name )
                {
                    // allow completion-only commands to be silently doubled
                    if ( function != null )
                    {
                        common.Com_Printf( "Cmd_AddCommand: %s already defined\n", cmd_name );
                    }
                    return;
                }
            }

            // use a small malloc to avoid zone fragmentation
            cmd = new cmd_function_t( );
            cmd.name = new string( cmd_name );
            cmd.function = function;
            cmd.next = cmd_functions;
            cmd_functions = cmd;
        }

        /*
		============
		Cmd_RemoveCommand
		============
		*/
        public static void Cmd_RemoveCommand( string cmd_name )
        {
            var back = cmd_functions;
            cmd_function_t forward = null;
            while ( true )
            {
                var cmd = back;

                // command wasn't active
                if ( cmd == null )
                    return;

                if ( cmd_name == cmd.name )
                {
                    forward = back;
                    back = cmd.next;
                    // Wipe the reference as we don't have to worry about Z_Free
                    if ( forward != null )
                        forward.next = null;
                    return;
                }
                forward = back;
                back = cmd.next;
            }
        }


        /*
		============
		Cmd_CommandCompletion
		============
		*/
        private static void Cmd_CommandCompletion( Action<string> callback )
        {
            cmd_function_t cmd;

            for ( cmd = cmd_functions; cmd != null; cmd = cmd.next )
            {
                callback( cmd.name );
            }
        }


        /*
		============
		Cmd_ExecuteString

		A complete command line has been parsed, so try to execute it
		============
		*/
        private static void Cmd_ExecuteString( string text )
        {
            cmd_function_t cmd, prev;

            // execute the command line
            Cmd_TokenizeString( text );

            if ( Cmd_Argc( ) == 0 )
                return;     // no tokens

            // check registered command functions	
            for ( prev = cmd_functions; prev != null; prev = cmd.next )
            {
                cmd = prev;
                if ( Q_stricmp( cmd_argv[0], cmd.name ) <= 0 )
                {
                    // rearrange the links so that the command will be
                    // near the head of the list next time it is used
                    prev = cmd.next;
                    cmd.next = cmd_functions;
                    cmd_functions = cmd;

                    // perform the action
                    if ( cmd.function == null ) // let the cgame or game handle it
                        break;
                    else
                        cmd.function( );

                    return;
                }
            }

            // check cvars
            if ( Cvar.Cvar_Command( ) )
                return;

            // check client game commands
            if ( common.com_cl_running != null && common.com_cl_running.integer == 1 && OnCLGameCommand?.Invoke( ) == true )
                return;

            // check server game commands
            if ( common.com_sv_running != null && common.com_sv_running.integer == 1 && OnSVGameCommand?.Invoke( ) == true )
                return;

            // check ui commands
            if ( common.com_cl_running != null && common.com_cl_running.integer == 1 && OnUIGameCommand?.Invoke( ) == true )
                return;

            // send it as a server command if we are connected
            // this will usually result in a chat message
            OnCLForwardCommandToServer?.Invoke( text );
        }

        /*
		============
		Cmd_List_f
		============
		*/
        private static void Cmd_List_f( )
        {
            cmd_function_t cmd;
            int i;
            string match;

            if ( Cmd_Argc( ) > 1 )
                match = Cmd_Argv( 1 );
            else
                match = null;

            i = 0;
            for ( cmd = cmd_functions; cmd != null; cmd = cmd.next )
            {
                if ( match != null && !common.Com_Filter( match, cmd.name, false ) ) continue;

                common.Com_Printf( "%s\n", cmd.name );
                i++;
            }
            common.Com_Printf( "%i commands\n", i );
        }

        /*
		============
		Cmd_Init
		============
		*/
        public static void Cmd_Init( )
        {
            Cmd_AddCommand( "cmdlist", Cmd_List_f );
            Cmd_AddCommand( "exec", Cmd_Exec_f );
            Cmd_AddCommand( "vstr", Cmd_Vstr_f );
            Cmd_AddCommand( "echo", Cmd_Echo_f );
            Cmd_AddCommand( "wait", Cmd_Wait_f );
        }
    }
}
