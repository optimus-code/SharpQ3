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
using System.IO;
using static SharpQ3.Engine.q_shared;

namespace SharpQ3.CGame
{
    // cg_syscalls.c -- this file is only included when building a dll
    public static class cg_syscalls
    {
        public delegate int syscall_delegate( cgameImport_t arg, object[] parameters );
        static syscall_delegate syscall;
        //static intptr_t (QDECL *syscall)( intptr_t arg, ... ) = (intptr_t (QDECL *)( intptr_t, ...))-1;

        static int syscall_f( cgameImport_t arg, params object[] parameters )
        {
            return syscall( arg, parameters );

        }
        //void dllEntry( intptr_t (QDECL  *syscallptr)( intptr_t arg,... ) ) {
        static void dllEntry( syscall_delegate syscallptr )
        {
            syscall = syscallptr;
        }

        static int PASSFLOAT( float x )
        {
            float floatTemp;
            floatTemp = x;
            return *( int* ) &floatTemp;
        }

        public static void trap_Print( string fmt )
        {
            syscall_f( cgameImport_t.CG_PRINT, fmt );
        }

        public static void trap_Error( string fmt )
        {
            syscall_f( cgameImport_t.CG_ERROR, fmt );
        }

        public static int trap_Milliseconds( )
        {
            return syscall_f( cgameImport_t.CG_MILLISECONDS );
        }

        static void trap_Cvar_Register( vmCvar_t vmCvar, string varName, string defaultValue, int flags )
        {
            syscall_f( cgameImport_t.CG_CVAR_REGISTER, vmCvar, varName, defaultValue, flags );
        }

        static void trap_Cvar_Update( vmCvar_t vmCvar )
        {
            syscall_f( cgameImport_t.CG_CVAR_UPDATE, vmCvar );
        }

        static void trap_Cvar_Set( string var_name, string value )
        {
            syscall_f( cgameImport_t.CG_CVAR_SET, var_name, value );
        }

        static void trap_Cvar_VariableStringBuffer( string var_name, string buffer, int bufsize )
        {
            syscall_f( cgameImport_t.CG_CVAR_VARIABLESTRINGBUFFER, var_name, buffer, bufsize );
        }

        static int trap_Argc( )
        {
            return syscall_f( cgameImport_t.CG_ARGC );
        }

        static void trap_Argv( int n, string buffer, int bufferLength )
        {
            syscall_f( cgameImport_t.CG_ARGV, n, buffer, bufferLength );
        }

        static void trap_Args( string buffer, int bufferLength )
        {
            syscall_f( cgameImport_t.CG_ARGS, buffer, bufferLength );
        }

        static int trap_FS_FOpenFile( string qpath, FileStream f, fsMode_t mode )
        {
            return syscall_f( cgameImport_t.CG_FS_FOPENFILE, qpath, f, mode );
        }

        static void trap_FS_Read( byte[] buffer, int len, FileStream f )
        {
            syscall_f( cgameImport_t.CG_FS_READ, buffer, len, f );
        }

        static void trap_FS_Write( byte[] buffer, int len, FileStream f )
        {
            syscall_f( cgameImport_t.CG_FS_WRITE, buffer, len, f );
        }

        static void trap_FS_FCloseFile( FileStream f )
        {
            syscall_f( cgameImport_t.CG_FS_FCLOSEFILE, f );
        }

        static int trap_FS_Seek( FileStream f, long offset, int origin )
        {
            return syscall_f( cgameImport_t.CG_FS_SEEK, f, offset, origin );
        }

        static void trap_SendConsoleCommand( string text )
        {
            syscall_f( cgameImport_t.CG_SENDCONSOLECOMMAND, text );
        }

        static void trap_AddCommand( string cmdName )
        {
            syscall_f( cgameImport_t.CG_ADDCOMMAND, cmdName );
        }

        static void trap_RemoveCommand( string cmdName )
        {
            syscall_f( cgameImport_t.CG_REMOVECOMMAND, cmdName );
        }

        static void trap_SendClientCommand( string s )
        {
            syscall_f( cgameImport_t.CG_SENDCLIENTCOMMAND, s );
        }

        static void trap_UpdateScreen( )
        {
            syscall_f( cgameImport_t.CG_UPDATESCREEN );
        }

        static void trap_CM_LoadMap( string mapname )
        {
            syscall_f( cgameImport_t.CG_CM_LOADMAP, mapname );
        }

        static int trap_CM_NumInlineModels( )
        {
            return syscall_f( cgameImport_t.CG_CM_NUMINLINEMODELS );
        }

        static clipHandle_t trap_CM_InlineModel( int index )
        {
            return syscall_f( cgameImport_t.CG_CM_INLINEMODEL, index );
        }

        static clipHandle_t trap_CM_TempBoxModel( const vec3_t mins, const vec3_t maxs ) {
			return syscall_f( cgameImport_t.CG_CM_TEMPBOXMODEL, mins, maxs );
    }

    static clipHandle_t trap_CM_TempCapsuleModel( const vec3_t mins, const vec3_t maxs )
    {
        return syscall_f( cgameImport_t.CG_CM_TEMPCAPSULEMODEL, mins, maxs );
    }

    static int trap_CM_PointContents( const vec3_t p, clipHandle_t model )
    {
        return syscall_f( cgameImport_t.CG_CM_POINTCONTENTS, p, model );
    }

    static int trap_CM_TransformedPointContents( const vec3_t p, clipHandle_t model, const vec3_t origin, const vec3_t angles )
    {
        return syscall_f( cgameImport_t.CG_CM_TRANSFORMEDPOINTCONTENTS, p, model, origin, angles );
    }

    static void trap_CM_BoxTrace( trace_t* results, const vec3_t start, const vec3_t end,
								  const vec3_t mins, const vec3_t maxs,
                              clipHandle_t model, int brushmask )
    {
        syscall_f( cgameImport_t.CG_CM_BOXTRACE, results, start, end, mins, maxs, model, brushmask );
    }

    static void trap_CM_CapsuleTrace( trace_t* results, const vec3_t start, const vec3_t end,
								  const vec3_t mins, const vec3_t maxs,
                              clipHandle_t model, int brushmask )
    {
        syscall_f( cgameImport_t.CG_CM_CAPSULETRACE, results, start, end, mins, maxs, model, brushmask );
    }

    static void trap_CM_TransformedBoxTrace( trace_t* results, const vec3_t start, const vec3_t end,
								  const vec3_t mins, const vec3_t maxs,
                              clipHandle_t model, int brushmask,
								  const vec3_t origin, const vec3_t angles )
    {
        syscall_f( cgameImport_t.CG_CM_TRANSFORMEDBOXTRACE, results, start, end, mins, maxs, model, brushmask, origin, angles );
    }

    static void trap_CM_TransformedCapsuleTrace( trace_t* results, const vec3_t start, const vec3_t end,
								  const vec3_t mins, const vec3_t maxs,
                              clipHandle_t model, int brushmask,
								  const vec3_t origin, const vec3_t angles )
    {
        syscall_f( cgameImport_t.CG_CM_TRANSFORMEDCAPSULETRACE, results, start, end, mins, maxs, model, brushmask, origin, angles );
    }

    static int trap_CM_MarkFragments( int numPoints, const vec3_t* points,
						const vec3_t projection,
                    int maxPoints, vec3_t pointBuffer,
                    int maxFragments, markFragment_t* fragmentBuffer )
    {
        return syscall_f( cgameImport_t.CG_CM_MARKFRAGMENTS, numPoints, points, projection, maxPoints, pointBuffer, maxFragments, fragmentBuffer );
    }

    static void trap_S_StartSound( vec3_t origin, int entityNum, int entchannel, sfxHandle_t sfx )
    {
        syscall_f( cgameImport_t.CG_S_STARTSOUND, origin, entityNum, entchannel, sfx );
    }

    static void trap_S_StartLocalSound( sfxHandle_t sfx, int channelNum )
    {
        syscall_f( cgameImport_t.CG_S_STARTLOCALSOUND, sfx, channelNum );
    }

    static void trap_S_ClearLoopingSounds( bool killall )
    {
        syscall_f( cgameImport_t.CG_S_CLEARLOOPINGSOUNDS, killall );
    }

    static void trap_S_AddLoopingSound( int entityNum, const vec3_t origin, const vec3_t velocity, sfxHandle_t sfx )
    {
        syscall_f( cgameImport_t.CG_S_ADDLOOPINGSOUND, entityNum, origin, velocity, sfx );
    }

    static void trap_S_AddRealLoopingSound( int entityNum, const vec3_t origin, const vec3_t velocity, sfxHandle_t sfx )
    {
        syscall_f( cgameImport_t.CG_S_ADDREALLOOPINGSOUND, entityNum, origin, velocity, sfx );
    }

    static void trap_S_StopLoopingSound( int entityNum )
    {
        syscall_f( cgameImport_t.CG_S_STOPLOOPINGSOUND, entityNum );
    }

    static void trap_S_UpdateEntityPosition( int entityNum, const vec3_t origin )
    {
        syscall_f( CG_S_UPDATEENTITYPOSITION, entityNum, origin );
    }

    static void trap_S_Respatialize( int entityNum, const vec3_t origin, vec3_t axis[3], int inwater )
    {
        syscall_f( CG_S_RESPATIALIZE, entityNum, origin, axis, inwater );
    }

    static sfxHandle_t trap_S_RegisterSound( string sample, bool compressed )
    {
        return syscall_f( CG_S_REGISTERSOUND, sample, compressed );
    }

    static void trap_S_StartBackgroundTrack( string intro, string loop )
    {
        syscall_f( CG_S_STARTBACKGROUNDTRACK, intro, loop );
    }

    static void trap_R_LoadWorldMap( string mapname )
    {
        syscall_f( CG_R_LOADWORLDMAP, mapname );
    }

    static qhandle_t trap_R_RegisterModel( string name )
    {
        return syscall_f( CG_R_REGISTERMODEL, name );
    }

    static qhandle_t trap_R_RegisterSkin( string name )
    {
        return syscall_f( CG_R_REGISTERSKIN, name );
    }

    static qhandle_t trap_R_RegisterShader( string name )
    {
        return syscall_f( CG_R_REGISTERSHADER, name );
    }

    static qhandle_t trap_R_RegisterShaderNoMip( string name )
    {
        return syscall_f( CG_R_REGISTERSHADERNOMIP, name );
    }

    static void trap_R_RegisterFont( string fontName, int pointSize, fontInfo_t* font )
    {
        syscall_f( CG_R_REGISTERFONT, fontName, pointSize, font );
    }

    static void trap_R_ClearScene( void )
    {
        syscall_f( CG_R_CLEARSCENE );
    }

    static void trap_R_AddRefEntityToScene( const refEntity_t* re )
    {
        syscall_f( CG_R_ADDREFENTITYTOSCENE, re );
    }

    static void trap_R_AddPolyToScene( qhandle_t hShader, int numVerts, const polyVert_t* verts )
    {
        syscall_f( CG_R_ADDPOLYTOSCENE, hShader, numVerts, verts );
    }

    static void trap_R_AddPolysToScene( qhandle_t hShader, int numVerts, const polyVert_t* verts, int num )
    {
        syscall_f( CG_R_ADDPOLYSTOSCENE, hShader, numVerts, verts, num );
    }

    static int trap_R_LightForPoint( vec3_t point, vec3_t ambientLight, vec3_t directedLight, vec3_t lightDir )
    {
        return syscall_f( CG_R_LIGHTFORPOINT, point, ambientLight, directedLight, lightDir );
    }

    static void trap_R_AddLightToScene( const vec3_t org, float intensity, float r, float g, float b )
    {
        syscall_f( CG_R_ADDLIGHTTOSCENE, org, PASSFLOAT( intensity ), PASSFLOAT( r ), PASSFLOAT( g ), PASSFLOAT( b ) );
    }

    static void trap_R_AddAdditiveLightToScene( const vec3_t org, float intensity, float r, float g, float b )
    {
        syscall_f( CG_R_ADDADDITIVELIGHTTOSCENE, org, PASSFLOAT( intensity ), PASSFLOAT( r ), PASSFLOAT( g ), PASSFLOAT( b ) );
    }

    static void trap_R_RenderScene( const refdef_t* fd )
    {
        syscall_f( CG_R_RENDERSCENE, fd );
    }

    static void trap_R_SetColor( const float* rgba )
    {
        syscall_f( CG_R_SETCOLOR, rgba );
    }

    static void trap_R_DrawStretchPic( float x, float y, float w, float h,
                                   float s1, float t1, float s2, float t2, qhandle_t hShader )
    {
        syscall_f( CG_R_DRAWSTRETCHPIC, PASSFLOAT( x ), PASSFLOAT( y ), PASSFLOAT( w ), PASSFLOAT( h ), PASSFLOAT( s1 ), PASSFLOAT( t1 ), PASSFLOAT( s2 ), PASSFLOAT( t2 ), hShader );
    }

    static void trap_R_ModelBounds( clipHandle_t model, vec3_t mins, vec3_t maxs )
    {
        syscall_f( CG_R_MODELBOUNDS, model, mins, maxs );
    }

    static int trap_R_LerpTag( orientation_t* tag, clipHandle_t mod, int startFrame, int endFrame,
                           float frac, string tagName )
    {
        return syscall_f( CG_R_LERPTAG, tag, mod, startFrame, endFrame, PASSFLOAT( frac ), tagName );
    }

    static void trap_R_RemapShader( string oldShader, string newShader, string timeOffset )
    {
        syscall_f( CG_R_REMAP_SHADER, oldShader, newShader, timeOffset );
    }

    static void trap_GetGlconfig( glconfig_t* glconfig )
    {
        syscall_f( CG_GETGLCONFIG, glconfig );
    }

    static void trap_GetGameState( gameState_t* gamestate )
    {
        syscall_f( CG_GETGAMESTATE, gamestate );
    }

    static void trap_GetCurrentSnapshotNumber( int* snapshotNumber, int* serverTime )
    {
        syscall_f( CG_GETCURRENTSNAPSHOTNUMBER, snapshotNumber, serverTime );
    }

    static bool trap_GetSnapshot( int snapshotNumber, snapshot_t* snapshot )
    {
        return syscall_f( CG_GETSNAPSHOT, snapshotNumber, snapshot );
    }

    static bool trap_GetServerCommand( int serverCommandNumber )
    {
        return syscall_f( CG_GETSERVERCOMMAND, serverCommandNumber );
    }

    static int trap_GetCurrentCmdNumber( void )
    {
        return syscall_f( CG_GETCURRENTCMDNUMBER );
    }

    static bool trap_GetUserCmd( int cmdNumber, usercmd_t* ucmd )
    {
        return syscall_f( CG_GETUSERCMD, cmdNumber, ucmd );
    }

    static void trap_SetUserCmdValue( int stateValue, float sensitivityScale )
    {
        syscall_f( CG_SETUSERCMDVALUE, stateValue, PASSFLOAT( sensitivityScale ) );
    }

    static void testPrintInt( string string, int i )
    {
        syscall_f( CG_TESTPRINTINT, string, i );
    }

    static void testPrintFloat( string string, float f )
    {
        syscall_f( CG_TESTPRINTFLOAT, string, PASSFLOAT( f ) );
    }

    static int trap_MemoryRemaining( void )
    {
        return syscall_f( CG_MEMORY_REMAINING );
    }

    static bool trap_Key_IsDown( int keynum )
    {
        return syscall_f( CG_KEY_ISDOWN, keynum );
    }

    static int trap_Key_GetCatcher( void )
    {
        return syscall_f( CG_KEY_GETCATCHER );
    }

    static void trap_Key_SetCatcher( int catcher )
    {
        syscall_f( CG_KEY_SETCATCHER, catcher );
    }

    static int trap_Key_GetKey( string binding )
    {
        return syscall_f( CG_KEY_GETKEY, binding );
    }

    static int trap_PC_AddGlobalDefine( string define )
    {
        return syscall_f( CG_PC_ADD_GLOBAL_DEFINE, define );
    }

    static int trap_PC_LoadSource( string filename )
    {
        return syscall_f( CG_PC_LOAD_SOURCE, filename );
    }

    static int trap_PC_FreeSource( int handle )
    {
        return syscall_f( CG_PC_FREE_SOURCE, handle );
    }

    static int trap_PC_ReadToken( int handle, pc_token_t* pc_token )
    {
        return syscall_f( CG_PC_READ_TOKEN, handle, pc_token );
    }

    static int trap_PC_SourceFileAndLine( int handle, string filename, int* line )
    {
        return syscall_f( CG_PC_SOURCE_FILE_AND_LINE, handle, filename, line );
    }

    static void trap_S_StopBackgroundTrack( void )
    {
        syscall_f( CG_S_STOPBACKGROUNDTRACK );
    }

    static int trap_RealTime( qtime_t* qtime )
    {
        return syscall_f( CG_REAL_TIME, qtime );
    }

    static void trap_SnapVector( float* v )
    {
        syscall_f( CG_SNAPVECTOR, v );
    }

    // this returns a handle.  arg0 is the name in the format "idlogo.roq", set arg1 to NULL, alteredstates to false (do not alter gamestate)
    static int trap_CIN_PlayCinematic( string arg0, int xpos, int ypos, int width, int height, int bits )
    {
        return syscall_f( CG_CIN_PLAYCINEMATIC, arg0, xpos, ypos, width, height, bits );
    }

    // stops playing the cinematic and ends it.  should always return FMV_EOF
    // cinematics must be stopped in reverse order of when they are started
    static e_status trap_CIN_StopCinematic( int handle )
    {
        return syscall_f( CG_CIN_STOPCINEMATIC, handle );
    }


    // will run a frame of the cinematic but will not draw it.  Will return FMV_EOF if the end of the cinematic has been reached.
    static e_status trap_CIN_RunCinematic( int handle )
    {
        return syscall_f( CG_CIN_RUNCINEMATIC, handle );
    }


    // draws the current frame
    static void trap_CIN_DrawCinematic( int handle )
    {
        syscall_f( CG_CIN_DRAWCINEMATIC, handle );
    }


    // allows you to resize the animation dynamically
    static void trap_CIN_SetExtents( int handle, int x, int y, int w, int h )
    {
        syscall_f( CG_CIN_SETEXTENTS, handle, x, y, w, h );
    }

    /*
    static bool trap_loadCamera( string name ) {
        return syscall_f( CG_LOADCAMERA, name );
    }

    static void trap_startCamera(int time) {
        syscall_f(CG_STARTCAMERA, time);
    }

    static bool trap_getCameraInfo( int time, vec3_t *origin, vec3_t *angles) {
        return syscall_f( CG_GETCAMERAINFO, time, origin, angles );
    }
    */

    static bool trap_GetEntityToken( string buffer, int bufferSize )
    {
        return syscall_f( CG_GET_ENTITY_TOKEN, buffer, bufferSize );
    }

    static bool trap_R_inPVS( const vec3_t p1, const vec3_t p2 )
    {
        return syscall_f( CG_R_INPVS, p1, p2 );
    }
}
}
