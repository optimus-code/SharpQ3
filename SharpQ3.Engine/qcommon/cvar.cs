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
using System;

namespace SharpQ3.Engine.qcommon
{
    // cvar.c -- dynamic variable tracking
    public static class Cvar
    {
        static cvar_t cvar_vars;
        static cvar_t cvar_cheats;
        static CVAR cvar_modifiedFlags;

        public const int MAX_CVARS = 1024;
        static cvar_t[] cvar_indexes = new cvar_t[MAX_CVARS];
        static int cvar_numIndexes;

        public const int FILE_HASH_SIZE = 256;
        static cvar_t[] hashTable = new cvar_t[FILE_HASH_SIZE];

        /*
		================
		return a hash value for the filename
		================
		*/
        static long generateHashValue( string fname )
        {
            int i;
            long hash;
            char letter;

            hash = 0;
            i = 0;

            while ( fname[i] != '\0' )
            {
                letter = char.ToLower( fname[i] );
                hash += ( long ) ( letter ) * ( i + 119 );
                i++;
            }

            hash &= ( FILE_HASH_SIZE - 1 );

            return hash;
        }

        /*
		============
		Cvar_ValidateString
		============
		*/
        public static bool Cvar_ValidateString( string s )
        {
            if ( s is null )
                return false;

            if ( s.IndexOf( '\\' ) >= 0 )
                return false;

            if ( s.IndexOf( '\"' ) >= 0 )
                return false;

            if ( s.IndexOf( ';' ) >= 0 )
                return false;

            return true;
        }

        /*
		============
		Cvar_FindVar
		============
		*/
        public static cvar_t Cvar_FindVar( string var_name )
        {
            cvar_t var;
            long hash;

            hash = generateHashValue( var_name );

            for ( var = hashTable[hash]; var != null; var = var.hashNext )
            {
                if ( q_shared.Q_stricmp( var_name, var.name ) <= 0 )
                {
                    return var;
                }
            }

            return null;
        }

        /*
		============
		Cvar_VariableValue
		============
		*/
        public static float Cvar_VariableValue( string var_name )
        {
            cvar_t var;

            var = Cvar_FindVar( var_name );

            if ( var == null )
                return 0;

            return var.value;
        }


        /*
		============
		Cvar_VariableIntegerValue
		============
		*/
        public static int Cvar_VariableIntegerValue( string var_name )
        {
            cvar_t var;

            var = Cvar_FindVar( var_name );

            if ( var == null )
                return 0;

            return var.integer;
        }


        /*
		============
		Cvar_VariableString
		============
		*/
        public static string Cvar_VariableString( string var_name )
        {
            cvar_t var;

            var = Cvar_FindVar( var_name );

            if ( var == null )
                return "";

            return var.@string;
        }


        /*
		============
		Cvar_VariableStringBuffer
		============
		*/
        public static void Cvar_VariableStringBuffer( string var_name, out string buffer, int bufsize )
        {
            cvar_t var;

            var = Cvar_FindVar( var_name );

            if ( var == null )
            {
                buffer = null;
            }
            else
            {
                q_shared.Q_strncpyz( out buffer, var.@string, bufsize );
            }
        }


        /*
		============
		Cvar_CommandCompletion
		============
		*/
        public static void Cvar_CommandCompletion( Action<string> callback )
        {
            for ( var cvar = cvar_vars; cvar != null; cvar = cvar.next )
            {
                callback( cvar.name );
            }
        }


        /*
		============
		Cvar_Get

		If the variable already exists, the value will not be set unless CVAR_ROM
		The flags will be or'ed in if the variable exists.
		============
		*/
        public static cvar_t Cvar_Get( string var_name, string var_value, CVAR flags )
        {
            cvar_t var;
            long hash;

            if ( var_name == null || var_value == null )
            {
                common.Com_Error( errorParm_t.ERR_FATAL, "Cvar_Get: NULL parameter" );
            }

            if ( !Cvar_ValidateString( var_name ) )
            {
                common.Com_Printf( "invalid cvar name string: %s\n", var_name );
                var_name = "BADNAME";
            }

            var = Cvar_FindVar( var_name );

            if ( var != null )
            {
                // if the C code is now specifying a variable that the user already
                // set a value for, take the new value as the reset value
                if ( ( var.flags.HasFlag( CVAR.USER_CREATED ) ) && !( flags.HasFlag( CVAR.USER_CREATED ) && var_value[0] == 1 ) )
                {
                    var.flags &= ~CVAR.USER_CREATED;
                    var.resetString = new string( var_value );

                    // ZOID--needs to be set so that cvars the game sets as 
                    // SERVERINFO get sent to clients
                    cvar_modifiedFlags |= flags;
                }

                var.flags |= flags;

                // only allow one non-empty reset string without a warning
                if ( string.IsNullOrEmpty( var.resetString ) )
                {
                    // we don't have a reset string yet
                    var.resetString = new string( var_value );
                }
                else if ( !string.IsNullOrEmpty( var_value ) && var.resetString != var_value )
                {
                    common.Com_DPrintf( "Warning: cvar \"%s\" given initial values: \"%s\" and \"%s\"\n", var_name, var.resetString, var_value );
                }

                // if we have a latched string, take that value now
                if ( var.latchedString != null )
                {
                    var s = var.latchedString;
                    var.latchedString = null; // otherwise cvar_set2 would free it
                    Cvar_Set2( var_name, s, true );
                }

                return var;
            }

            //
            // allocate a new cvar
            //
            if ( cvar_numIndexes >= MAX_CVARS )
            {
                common.Com_Error( errorParm_t.ERR_FATAL, "MAX_CVARS" );
            }

            var = cvar_indexes[cvar_numIndexes];
            var.handle = cvar_numIndexes;
            cvar_numIndexes++;
            var.name = new string( var_name ); ;
            var.@string = new string( var_value );
            var.modified = true;
            var.modificationCount = 1;
            Single.TryParse( var.@string, out var.value );
            Int32.TryParse( var.@string, out var.integer );
            var.resetString = new string( var_value );

            // link the variable in
            var.next = cvar_vars;
            cvar_vars = var;

            var.flags = flags;

            hash = generateHashValue( var_name );
            var.hashNext = hashTable[hash];
            hashTable[hash] = var;

            return var;
        }

        /*
		============
		Cvar_Set2
		============
		*/
        public static cvar_t Cvar_Set2( string var_name, string value, bool force )
        {
            cvar_t var;

            common.Com_DPrintf( "Cvar_Set2: %s %s\n", var_name, value );

            if ( !Cvar_ValidateString( var_name ) )
            {
                common.Com_Printf( "invalid cvar name string: %s\n", var_name );
                var_name = "BADNAME";
            }

            var = Cvar_FindVar( var_name );

            if ( var == null )
            {
                if ( value == null )
                {
                    return null;
                }

                // create it
                if ( !force )
                {
                    return Cvar_Get( var_name, value, CVAR.USER_CREATED );
                }
                else
                {
                    return Cvar_Get( var_name, value, 0 );
                }
            }

            if ( value == null )
            {
                value = var.resetString;
            }

            if ( value == var.@string )
            {
                return var;
            }

            // note what types of cvars have been modified (userinfo, archive, serverinfo, systeminfo)
            cvar_modifiedFlags |= var.flags;

            if ( !force )
            {
                if ( var.flags.HasFlag( CVAR.ROM ) )
                {
                    common.Com_Printf( "%s is read only.\n", var_name );

                    return var;
                }

                if ( var.flags.HasFlag( CVAR.INIT ) )
                {
                    common.Com_Printf( "%s is write protected.\n", var_name );

                    return var;
                }

                if ( var.flags.HasFlag( CVAR.LATCH ) )
                {
                    if ( var.latchedString != null )
                    {
                        if ( value == var.latchedString )
                            return var;
                    }
                    else
                    {
                        if ( value == var.@string )
                            return var;
                    }

                    common.Com_Printf( "%s will be changed upon restarting.\n", var_name );
                    var.latchedString = new String( value );
                    var.modified = true;
                    var.modificationCount++;

                    return var;
                }

                if ( ( var.flags.HasFlag( CVAR.CHEAT ) ) && cvar_cheats.integer == 0 )
                {
                    common.Com_Printf( "%s is cheat protected.\n", var_name );

                    return var;
                }

            }
            else
            {
                if ( var.latchedString != null )
                {
                    var.latchedString = null;
                }
            }

            if ( value == var.@string )
                return var; // not changed

            var.modified = true;
            var.modificationCount++;

            var.@string = new string( value );
            Single.TryParse( var.@string, out var.value );
            Int32.TryParse( var.@string, out var.integer );

            return var;
        }

        /*
		============
		Cvar_Set
		============
		*/
        public static void Cvar_Set( string var_name, string value )
        {
            Cvar_Set2( var_name, value, true );
        }

        /*
		============
		Cvar_SetLatched
		============
		*/
        public static void Cvar_SetLatched( string var_name, string value )
        {
            Cvar_Set2( var_name, value, false );
        }

        /*
		============
		Cvar_SetValue
		============
		*/
        public static void Cvar_SetValue( string var_name, float value )
        {
            Cvar_Set( var_name, value.ToString( ) );
        }


        /*
		============
		Cvar_Reset
		============
		*/
        public static void Cvar_Reset( string var_name )
        {
            Cvar_Set2( var_name, null, false );
        }


        /*
		============
		Cvar_SetCheatState

		Any testing variables will be reset to the safe values
		============
		*/
        public static void Cvar_SetCheatState( )
        {
            cvar_t var;

            // set all default vars to the safe value
            for ( var = cvar_vars; var != null; var = var.next )
            {
                if ( var.flags.HasFlag( CVAR.CHEAT ) )
                {
                    // the CVAR_LATCHED|CVAR_CHEAT vars might escape the reset here 
                    // because of a different var->latchedString
                    if ( var.latchedString != null )
                    {
                        var.latchedString = null;
                    }

                    if ( var.resetString != var.@string )
                    {
                        Cvar_Set( var.name, var.resetString );
                    }
                }
            }
        }

        /*
		============
		Cvar_Command

		Handles variable inspection and changing from the console
		============
		*/
        public static bool Cvar_Command( )
        {
            cvar_t v;

            // check variables
            v = Cvar_FindVar( cmd.Cmd_Argv( 0 ) );

            if ( v == null )
            {
                return false;
            }

            // perform a variable print or set
            if ( cmd.Cmd_Argc( ) == 1 )
            {
                common.Com_Printf( "\"%s\" is:\"%s" + q_shared.S_COLOR_WHITE + "\" default:\"%s" + q_shared.S_COLOR_WHITE + "\"\n", v.name, v.@string, v.resetString );

                if ( v.latchedString != null )
                {
                    common.Com_Printf( "latched: \"%s\"\n", v.latchedString );
                }

                return true;
            }

            // set the value if forcing isn't required
            Cvar_Set2( v.name, cmd.Cmd_Argv( 1 ), false );

            return true;
        }


        /*
		============
		Cvar_Toggle_f

		Toggles a cvar for easy single key binding
		============
		*/
        static void Cvar_Toggle_f( )
        {
            int v;

            if ( cmd.Cmd_Argc( ) != 2 )
            {
                common.Com_Printf( "usage: toggle <variable>\n" );

                return;
            }

            v = ( int ) Cvar_VariableValue( cmd.Cmd_Argv( 1 ) );
            v = v == 1 ? 0 : 1;

            Cvar_Set2( cmd.Cmd_Argv( 1 ), v.ToString( ), false );
        }

        /*
		============
		Cvar_Set_f

		Allows setting and defining of arbitrary cvars from console, even if they
		weren't declared in C code.
		============
		*/
        static void Cvar_Set_f( )
        {
            int i, c, l, len = 0;
            string combined = "";

            c = cmd.Cmd_Argc( );

            if ( c < 3 )
            {
                common.Com_Printf( "usage: set <variable> <value>\n" );

                return;
            }

            //combined[0] = 0;
            l = 0;

            for ( i = 2; i < c; i++ )
            {
                len = cmd.Cmd_Argv( i ).Length + 1;

                if ( l + len >= q_shared.MAX_STRING_TOKENS - 2 )
                {
                    break;
                }

                combined += cmd.Cmd_Argv( i );

                if ( i != c - 1 )
                {
                    combined += " ";
                }

                l += len;
            }

            Cvar_Set2( cmd.Cmd_Argv( 1 ), combined, false );
        }

        /*
		============
		Cvar_SetU_f

		As Cvar_Set, but also flags it as userinfo
		============
		*/
        static void Cvar_SetU_f( )
        {
            cvar_t v;

            if ( cmd.Cmd_Argc( ) != 3 )
            {
                common.Com_Printf( "usage: setu <variable> <value>\n" );

                return;
            }

            Cvar_Set_f( );
            v = Cvar_FindVar( cmd.Cmd_Argv( 1 ) );

            if ( v == null )
            {
                return;
            }

            v.flags |= CVAR.USERINFO;
        }

        /*
		============
		Cvar_SetS_f

		As Cvar_Set, but also flags it as userinfo
		============
		*/
        static void Cvar_SetS_f( )
        {
            cvar_t v;

            if ( cmd.Cmd_Argc( ) != 3 )
            {
                common.Com_Printf( "usage: sets <variable> <value>\n" );

                return;
            }

            Cvar_Set_f( );
            v = Cvar_FindVar( cmd.Cmd_Argv( 1 ) );

            if ( v == null )
            {
                return;
            }

            v.flags |= CVAR.SERVERINFO;
        }

        /*
		============
		Cvar_SetA_f

		As Cvar_Set, but also flags it as archived
		============
		*/
        static void Cvar_SetA_f( )
        {
            cvar_t v;

            if ( cmd.Cmd_Argc( ) != 3 )
            {
                common.Com_Printf( "usage: seta <variable> <value>\n" );

                return;
            }

            Cvar_Set_f( );
            v = Cvar_FindVar( cmd.Cmd_Argv( 1 ) );

            if ( v == null )
            {
                return;
            }

            v.flags |= CVAR.ARCHIVE;
        }

        /*
		============
		Cvar_Reset_f
		============
		*/
        static void Cvar_Reset_f( )
        {
            if ( cmd.Cmd_Argc( ) != 2 )
            {
                common.Com_Printf( "usage: reset <variable>\n" );

                return;
            }

            Cvar_Reset( cmd.Cmd_Argv( 1 ) );
        }

        /*
		============
		Cvar_WriteVariables

		Appends lines containing "set variable value" for all variables
		with the archive flag set to true.
		============
		*/
        public static void Cvar_WriteVariables( fileHandle_t f )
        {
            cvar_t var;
            string buffer;

            for ( var = cvar_vars; var != null; var = var.next )
            {
                if ( var.flags.HasFlag( CVAR.ARCHIVE ) )
                {
                    // write the latched value, even if it hasn't taken effect yet
                    if ( var.latchedString != null )
                    {
                        q_shared.Com_sprintf( out buffer, 1024, "seta %s \"%s\"\n", var.name, var.latchedString );
                    }
                    else
                    {
                        q_shared.Com_sprintf( out buffer, 1024, "seta %s \"%s\"\n", var.name, var.@string );
                    }

                    files.FS_Printf( f, "%s", buffer );
                }
            }
        }

        /*
		============
		Cvar_List_f
		============
		*/
        static void Cvar_List_f( )
        {
            cvar_t var;
            int i;
            string match;

            if ( cmd.Cmd_Argc( ) > 1 )
            {
                match = cmd.Cmd_Argv( 1 );
            }
            else
            {
                match = null;
            }

            i = 0;

            for ( var = cvar_vars; var != null; var = var.next, i++ )
            {
                if ( match != null && !common.Com_Filter( match, var.name, false ) )
                    continue;

                if ( var.flags.HasFlag( CVAR.SERVERINFO ) )
                {
                    common.Com_Printf( "S" );
                }
                else
                {
                    common.Com_Printf( " " );
                }

                if ( var.flags.HasFlag( CVAR.USERINFO ) )
                {
                    common.Com_Printf( "U" );
                }
                else
                {
                    common.Com_Printf( " " );
                }

                if ( var.flags.HasFlag( CVAR.ROM ) )
                {
                    common.Com_Printf( "R" );
                }
                else
                {
                    common.Com_Printf( " " );
                }

                if ( var.flags.HasFlag( CVAR.INIT ) )
                {
                    common.Com_Printf( "I" );
                }
                else
                {
                    common.Com_Printf( " " );
                }

                if ( var.flags.HasFlag( CVAR.ARCHIVE ) )
                {
                    common.Com_Printf( "A" );
                }
                else
                {
                    common.Com_Printf( " " );
                }

                if ( var.flags.HasFlag( CVAR.LATCH ) )
                {
                    common.Com_Printf( "L" );
                }
                else
                {
                    common.Com_Printf( " " );
                }

                if ( var.flags.HasFlag( CVAR.CHEAT ) )
                {
                    common.Com_Printf( "C" );
                }
                else
                {
                    common.Com_Printf( " " );
                }

                common.Com_Printf( " %s \"%s\"\n", var.name, var.@string );
            }

            common.Com_Printf( "\n%i total cvars\n", i );
            common.Com_Printf( "%i cvar indexes\n", cvar_numIndexes );
        }

        /*
		============
		Cvar_Restart_f

		Resets all cvars to their hardcoded values
		============
		*/
        static void Cvar_Restart_f( )
        {
            cvar_t var;
            cvar_t prev;

            prev = cvar_vars;

            while ( true )
            {
                var = prev;

                if ( var == null )
                {
                    break;
                }

                // don't mess with rom values, or some inter-module
                // communication will get broken (com_cl_running, etc)
                if ( var.flags.HasFlag( CVAR.ROM ) || var.flags.HasFlag( CVAR.INIT ) || var.flags.HasFlag( CVAR.NORESTART ) )
                {
                    prev = var.next;

                    continue;
                }

                // throw out any variables the user created
                if ( var.flags.HasFlag( CVAR.USER_CREATED ) )
                {
                    prev = var.next;

                    if ( var.name != null )
                    {
                        var.name = null;
                    }

                    if ( var.@string != null )
                    {
                        var.@string = null;
                    }

                    if ( var.latchedString != null )
                    {
                        var.latchedString = null;
                    }

                    if ( var.resetString != null )
                    {
                        var.resetString = null;
                    }

                    continue;
                }

                Cvar_Set( var.name, var.resetString );

                prev = var.next;
            }
        }



        /*
		=====================
		Cvar_InfoString
		=====================
		*/
        static string Cvar_InfoString( int bit )
        {
            string info = "";
            cvar_t var;

            for ( var = cvar_vars; var != null; var = var.next )
            {
                if ( var.flags.HasFlag( ( CVAR ) bit ) )
                {
                    q_shared.Info_SetValueForKey( ref info, var.name, var.@string );
                }
            }

            return info;
        }

        /*
		=====================
		Cvar_InfoString_Big

		  handles large info strings ( CS_SYSTEMINFO )
		=====================
		*/
        static string Cvar_InfoString_Big( int bit )
        {
            string info = "";
            cvar_t var;

            for ( var = cvar_vars; var != null; var = var.next )
            {
                if ( var.flags.HasFlag( ( CVAR ) bit ) )
                {
                    q_shared.Info_SetValueForKey_Big( ref info, var.name, var.@string );
                }
            }

            return info;
        }



        /*
		=====================
		Cvar_InfoStringBuffer
		=====================
		*/
        static void Cvar_InfoStringBuffer( int bit, ref string buff, int buffsize )
        {
            q_shared.Q_strncpyz( out buff, Cvar_InfoString( bit ), buffsize );
        }

        /*
		=====================
		Cvar_Register

		basically a slightly modified Cvar_Get for the interpreted modules
		=====================
		*/
        static void Cvar_Register( vmCvar_t vmCvar, string varName, string defaultValue, CVAR flags )
        {
            cvar_t cv;

            cv = Cvar_Get( varName, defaultValue, flags );

            if ( vmCvar == null )
            {
                return;
            }

            vmCvar.handle = cv.handle;
            vmCvar.modificationCount = -1;
            Cvar_Update( vmCvar );
        }


        /*
		=====================
		Cvar_Register

		updates an interpreted modules' version of a cvar
		=====================
		*/
        static void Cvar_Update( vmCvar_t vmCvar )
        {
            cvar_t cv = null; // bk001129

            if ( vmCvar == null )
                common.Com_Error( errorParm_t.ERR_DROP, "Cvar_Update: vmCvar is null" );

            if ( vmCvar.handle >= cvar_numIndexes )
            {
                common.Com_Error( errorParm_t.ERR_DROP, "Cvar_Update: handle out of range" );
            }

            cv = cvar_indexes[vmCvar.handle];

            if ( cv.modificationCount == vmCvar.modificationCount )
            {
                return;
            }

            if ( cv.@string == null )
            {
                return; // variable might have been cleared by a cvar_restart
            }

            vmCvar.modificationCount = cv.modificationCount;

            // bk001129 - mismatches.
            if ( cv.@string.Length + 1 > q_shared.MAX_CVAR_VALUE_STRING )
                common.Com_Error(
                    errorParm_t.ERR_DROP,
                    "Cvar_Update: src %s length %d exceeds MAX_CVAR_VALUE_STRING",
                    cv.@string,
                    cv.@string.Length,
                    vmCvar.@string.Length )

            ;
            // bk001212 - Q_strncpyz guarantees zero padding and dest[MAX_CVAR_VALUE_STRING-1]==0 
            // bk001129 - paranoia. Never trust the destination string.
            // bk001129 - beware, sizeof(char*) is always 4 (for cv->string). 
            //            sizeof(vmCvar->string) always MAX_CVAR_VALUE_STRING
            //Q_strncpyz( vmCvar->string, cv->string, sizeof( vmCvar->string ) ); // id
            q_shared.Q_strncpyz( out vmCvar.@string, cv.@string, q_shared.MAX_CVAR_VALUE_STRING );

            vmCvar.value = cv.value;
            vmCvar.integer = cv.integer;
        }


        /*
		============
		Cvar_Init

		Reads in all archived cvars
		============
		*/
        public static void Cvar_Init( )
        {
            cvar_cheats = Cvar_Get( "sv_cheats", "1", CVAR.ROM | CVAR.SYSTEMINFO );

            cmd.Cmd_AddCommand( "toggle", Cvar_Toggle_f );
            cmd.Cmd_AddCommand( "set", Cvar_Set_f );
            cmd.Cmd_AddCommand( "sets", Cvar_SetS_f );
            cmd.Cmd_AddCommand( "setu", Cvar_SetU_f );
            cmd.Cmd_AddCommand( "seta", Cvar_SetA_f );
            cmd.Cmd_AddCommand( "reset", Cvar_Reset_f );
            cmd.Cmd_AddCommand( "cvarlist", Cvar_List_f );
            cmd.Cmd_AddCommand( "cvar_restart", Cvar_Restart_f );
        }
    }
}
