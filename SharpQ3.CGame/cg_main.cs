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
using System.Runtime.InteropServices;

namespace SharpQ3.CGame
{
	// cg_main.c -- initialization and primary entry point for cgame
	public static class cg_main
	{
		static int forceModelModificationCount = -1;

		/*
		================
		vmMain

		This is the only way control passes into the module.
		This must be the very first function compiled into the .q3vm file
		================
		*/
		static int vmMain( int command, int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9, int arg10, int arg11  ) 
		{
			switch ( ( cgameExport_t ) command ) 
			{
				case cgameExport_t.CG_INIT:
					CG_Init( arg0, arg1, arg2 );
					return 0;
				case cgameExport_t.CG_SHUTDOWN:
					CG_Shutdown();
					return 0;
				case cgameExport_t.CG_CONSOLE_COMMAND:
					return CG_ConsoleCommand();
				case cgameExport_t.CG_DRAW_ACTIVE_FRAME:
					CG_DrawActiveFrame( arg0, arg1, arg2 );
					return 0;
				case cgameExport_t.CG_CROSSHAIR_PLAYER:
					return CG_CrosshairPlayer();
				case cgameExport_t.CG_LAST_ATTACKER:
					return CG_LastAttacker();
				case cgameExport_t.CG_KEY_EVENT:
					CG_KeyEvent(arg0, arg1 == 1);
					return 0;
				case cgameExport_t.CG_MOUSE_EVENT:
					CG_MouseEvent(arg0, arg1);
					return 0;
				case cgameExport_t.CG_EVENT_HANDLING:
					CG_EventHandling(arg0);
					return 0;
				default:
					CG_Error( "vmMain: unknown command %i", command );
					break;
			}
			return -1;
		}


		static cg_t				cg;
		static cgs_t				cgs;
		static centity_t			cg_entities[MAX_GENTITIES];
		static weaponInfo_t		cg_weapons[MAX_WEAPONS];
		static itemInfo_t			cg_items[MAX_ITEMS];


		static vmCvar_t cg_railTrailTime;
		static vmCvar_t cg_centertime;
		static vmCvar_t cg_runpitch;
		static vmCvar_t cg_runroll;
		static vmCvar_t cg_bobup;
		static vmCvar_t cg_bobpitch;
		static vmCvar_t cg_bobroll;
		static vmCvar_t cg_swingSpeed;
		static vmCvar_t cg_shadows;
		static vmCvar_t cg_gibs;
		static vmCvar_t cg_drawTimer;
		static vmCvar_t cg_drawFPS;
		static vmCvar_t cg_drawSnapshot;
		static vmCvar_t cg_draw3dIcons;
		static vmCvar_t cg_drawIcons;
		static vmCvar_t cg_drawAmmoWarning;
		static vmCvar_t cg_drawCrosshair;
		static vmCvar_t cg_drawCrosshairNames;
		static vmCvar_t cg_drawRewards;
		static vmCvar_t cg_crosshairSize;
		static vmCvar_t cg_crosshairX;
		static vmCvar_t cg_crosshairY;
		static vmCvar_t cg_crosshairHealth;
		static vmCvar_t cg_draw2D;
		static vmCvar_t cg_drawStatus;
		static vmCvar_t cg_animSpeed;
		static vmCvar_t cg_debugAnim;
		static vmCvar_t cg_debugPosition;
		static vmCvar_t cg_debugEvents;
		static vmCvar_t cg_errorDecay;
		static vmCvar_t cg_nopredict;
		static vmCvar_t cg_noPlayerAnims;
		static vmCvar_t cg_showmiss;
		static vmCvar_t cg_footsteps;
		static vmCvar_t cg_addMarks;
		static vmCvar_t cg_brassTime;
		static vmCvar_t cg_viewsize;
		static vmCvar_t cg_drawGun;
		static vmCvar_t cg_gun_frame;
		static vmCvar_t cg_gun_x;
		static vmCvar_t cg_gun_y;
		static vmCvar_t cg_gun_z;
		static vmCvar_t cg_tracerChance;
		static vmCvar_t cg_tracerWidth;
		static vmCvar_t cg_tracerLength;
		static vmCvar_t cg_autoswitch;
		static vmCvar_t cg_ignore;
		static vmCvar_t cg_simpleItems;
		static vmCvar_t cg_fov;
		static vmCvar_t cg_zoomFov;
		static vmCvar_t cg_thirdPerson;
		static vmCvar_t cg_thirdPersonRange;
		static vmCvar_t cg_thirdPersonAngle;
		static vmCvar_t cg_stereoSeparation;
		static vmCvar_t cg_lagometer;
		static vmCvar_t cg_drawAttacker;
		static vmCvar_t cg_synchronousClients;
		static vmCvar_t cg_teamChatTime;
		static vmCvar_t cg_teamChatHeight;
		static vmCvar_t cg_stats;
		static vmCvar_t cg_buildScript;
		static vmCvar_t cg_forceModel;
		static vmCvar_t cg_paused;
		static vmCvar_t cg_blood;
		static vmCvar_t cg_predictItems;
		static vmCvar_t cg_deferPlayers;
		static vmCvar_t cg_drawTeamOverlay;
		static vmCvar_t cg_teamOverlayUserinfo;
		static vmCvar_t cg_drawFriend;
		static vmCvar_t cg_teamChatsOnly;
		static vmCvar_t cg_noVoiceChats;
		static vmCvar_t cg_noVoiceText;
		static vmCvar_t cg_hudFiles;
		static vmCvar_t cg_scorePlum;
		static vmCvar_t cg_smoothClients;
		static vmCvar_t pmove_fixed;
		//static vmCvar_t	cg_pmove_fixed;
		static vmCvar_t pmove_msec;
		static vmCvar_t cg_pmove_msec;
		static vmCvar_t cg_cameraMode;
		static vmCvar_t cg_cameraOrbit;
		static vmCvar_t cg_cameraOrbitDelay;
		static vmCvar_t cg_timescaleFadeEnd;
		static vmCvar_t cg_timescaleFadeSpeed;
		static vmCvar_t cg_timescale;
		static vmCvar_t cg_smallFont;
		static vmCvar_t cg_bigFont;
		static vmCvar_t cg_noTaunt;
		static vmCvar_t cg_noProjectileTrail;
		static vmCvar_t cg_oldRail;
		static vmCvar_t cg_oldRocket;
		static vmCvar_t cg_oldPlasma;
		static vmCvar_t	cg_trueLightning;

		public struct cvarTable_t
		{
			vmCvar_t	vmCvar;
			string		cvarName;
			string		defaultString;
			CVAR		cvarFlags;

			public cvarTable_t( vmCvar_t vmCvar, string cvarName, string defaultString, CVAR cvarFlags )
            {
				this.vmCvar = vmCvar;
				this.cvarName = cvarName;
				this.defaultString = defaultString;
				this.cvarFlags = cvarFlags;
			}
		};

		static cvarTable_t[] cvarTable = new []{ // bk001129
			new cvarTable_t( cg_ignore, "cg_ignore", "0", 0 ),	// used for debugging
			new cvarTable_t( cg_autoswitch, "cg_autoswitch", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_drawGun, "cg_drawGun", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_zoomFov, "cg_zoomfov", "22.5", CVAR.ARCHIVE ),
			new cvarTable_t( cg_fov, "cg_fov", "90", CVAR.ARCHIVE ),
			new cvarTable_t( cg_viewsize, "cg_viewsize", "100", CVAR.ARCHIVE ),
			new cvarTable_t( cg_stereoSeparation, "cg_stereoSeparation", "0.4", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_shadows, "cg_shadows", "1", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_gibs, "cg_gibs", "1", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_draw2D, "cg_draw2D", "1", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_drawStatus, "cg_drawStatus", "1", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_drawTimer, "cg_drawTimer", "0", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_drawFPS, "cg_drawFPS", "0", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_drawSnapshot, "cg_drawSnapshot", "0", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_draw3dIcons, "cg_draw3dIcons", "1", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_drawIcons, "cg_drawIcons", "1", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_drawAmmoWarning, "cg_drawAmmoWarning", "1", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_drawAttacker, "cg_drawAttacker", "1", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_drawCrosshair, "cg_drawCrosshair", "4", CVAR.ARCHIVE ),
			new cvarTable_t( cg_drawCrosshairNames, "cg_drawCrosshairNames", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_drawRewards, "cg_drawRewards", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_crosshairSize, "cg_crosshairSize", "24", CVAR.ARCHIVE ),
			new cvarTable_t( cg_crosshairHealth, "cg_crosshairHealth", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_crosshairX, "cg_crosshairX", "0", CVAR.ARCHIVE ),
			new cvarTable_t( cg_crosshairY, "cg_crosshairY", "0", CVAR.ARCHIVE ),
			new cvarTable_t( cg_brassTime, "cg_brassTime", "2500", CVAR.ARCHIVE ),
			new cvarTable_t( cg_simpleItems, "cg_simpleItems", "0", CVAR.ARCHIVE ),
			new cvarTable_t( cg_addMarks, "cg_marks", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_lagometer, "cg_lagometer", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_railTrailTime, "cg_railTrailTime", "400", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_gun_x, "cg_gunX", "0", CVAR.CHEAT ),
			new cvarTable_t( cg_gun_y, "cg_gunY", "0", CVAR.CHEAT ),
			new cvarTable_t( cg_gun_z, "cg_gunZ", "0", CVAR.CHEAT ),
			new cvarTable_t( cg_centertime, "cg_centertime", "3", CVAR.CHEAT ),
			new cvarTable_t( cg_runpitch, "cg_runpitch", "0.002", CVAR.ARCHIVE),
			new cvarTable_t( cg_runroll, "cg_runroll", "0.005", CVAR.ARCHIVE ),
			new cvarTable_t( cg_bobup , "cg_bobup", "0.005", CVAR.CHEAT ),
			new cvarTable_t( cg_bobpitch, "cg_bobpitch", "0.002", CVAR.ARCHIVE ),
			new cvarTable_t( cg_bobroll, "cg_bobroll", "0.002", CVAR.ARCHIVE ),
			new cvarTable_t( cg_swingSpeed, "cg_swingSpeed", "0.3", CVAR.CHEAT ),
			new cvarTable_t( cg_animSpeed, "cg_animspeed", "1", CVAR.CHEAT ),
			new cvarTable_t( cg_debugAnim, "cg_debuganim", "0", CVAR.CHEAT ),
			new cvarTable_t( cg_debugPosition, "cg_debugposition", "0", CVAR.CHEAT ),
			new cvarTable_t( cg_debugEvents, "cg_debugevents", "0", CVAR.CHEAT ),
			new cvarTable_t( cg_errorDecay, "cg_errordecay", "100", 0 ),
			new cvarTable_t( cg_nopredict, "cg_nopredict", "0", 0 ),
			new cvarTable_t( cg_noPlayerAnims, "cg_noplayeranims", "0", CVAR.CHEAT ),
			new cvarTable_t( cg_showmiss, "cg_showmiss", "0", 0 ),
			new cvarTable_t( cg_footsteps, "cg_footsteps", "1", CVAR.CHEAT ),
			new cvarTable_t( cg_tracerChance, "cg_tracerchance", "0.4", CVAR.CHEAT ),
			new cvarTable_t( cg_tracerWidth, "cg_tracerwidth", "1", CVAR.CHEAT ),
			new cvarTable_t( cg_tracerLength, "cg_tracerlength", "100", CVAR.CHEAT ),
			new cvarTable_t( cg_thirdPersonRange, "cg_thirdPersonRange", "40", CVAR.CHEAT ),
			new cvarTable_t( cg_thirdPersonAngle, "cg_thirdPersonAngle", "0", CVAR.CHEAT ),
			new cvarTable_t( cg_thirdPerson, "cg_thirdPerson", "0", 0 ),
			new cvarTable_t( cg_teamChatTime, "cg_teamChatTime", "3000", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_teamChatHeight, "cg_teamChatHeight", "0", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_forceModel, "cg_forceModel", "0", CVAR.ARCHIVE  ),
			new cvarTable_t( cg_predictItems, "cg_predictItems", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_deferPlayers, "cg_deferPlayers", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_drawTeamOverlay, "cg_drawTeamOverlay", "0", CVAR.ARCHIVE ),
			new cvarTable_t( cg_teamOverlayUserinfo, "teamoverlay", "0", CVAR.ROM | CVAR.USERINFO ),
			new cvarTable_t( cg_stats, "cg_stats", "0", 0 ),
			new cvarTable_t( cg_drawFriend, "cg_drawFriend", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_teamChatsOnly, "cg_teamChatsOnly", "0", CVAR.ARCHIVE ),
			new cvarTable_t( cg_noVoiceChats, "cg_noVoiceChats", "0", CVAR.ARCHIVE ),
			new cvarTable_t( cg_noVoiceText, "cg_noVoiceText", "0", CVAR.ARCHIVE ),
			// the following variables are created in other parts of the system,
			// but we also reference them here
			new cvarTable_t( cg_buildScript, "com_buildScript", "0", 0 ),	// force loading of all possible data amd error on failures
			new cvarTable_t( cg_paused, "cl_paused", "0", CVAR.ROM ),
			new cvarTable_t( cg_blood, "com_blood", "1", CVAR.ARCHIVE ),
			new cvarTable_t( cg_synchronousClients, "g_synchronousClients", "0", 0 ),	// communicated by systeminfo
			new cvarTable_t( cg_cameraOrbit, "cg_cameraOrbit", "0", CVAR.CHEAT),
			new cvarTable_t( cg_cameraOrbitDelay, "cg_cameraOrbitDelay", "50", CVAR.ARCHIVE),
			new cvarTable_t( cg_timescaleFadeEnd, "cg_timescaleFadeEnd", "1", 0),
			new cvarTable_t( cg_timescaleFadeSpeed, "cg_timescaleFadeSpeed", "0", 0),
			new cvarTable_t( cg_timescale, "timescale", "1", 0),
			new cvarTable_t( cg_scorePlum, "cg_scorePlums", "1", CVAR.USERINFO | CVAR.ARCHIVE),
			new cvarTable_t( cg_smoothClients, "cg_smoothClients", "0", CVAR.USERINFO | CVAR.ARCHIVE),
			new cvarTable_t( cg_cameraMode, "com_cameraMode", "0", CVAR.CHEAT),

			new cvarTable_t( pmove_fixed, "pmove_fixed", "0", 0),
			new cvarTable_t( pmove_msec, "pmove_msec", "8", 0),
			new cvarTable_t( cg_noTaunt, "cg_noTaunt", "0", CVAR.ARCHIVE),
			new cvarTable_t( cg_noProjectileTrail, "cg_noProjectileTrail", "0", CVAR.ARCHIVE),
			new cvarTable_t( cg_smallFont, "ui_smallFont", "0.25", CVAR.ARCHIVE),
			new cvarTable_t( cg_bigFont, "ui_bigFont", "0.4", CVAR.ARCHIVE),
			new cvarTable_t( cg_oldRail, "cg_oldRail", "1", CVAR.ARCHIVE),
			new cvarTable_t( cg_oldRocket, "cg_oldRocket", "1", CVAR.ARCHIVE),
			new cvarTable_t( cg_oldPlasma, "cg_oldPlasma", "1", CVAR.ARCHIVE),
			new cvarTable_t( cg_trueLightning, "cg_trueLightning", "0.0", CVAR.ARCHIVE)
		//	new cvarTable_t( cg_pmove_fixed, "cg_pmove_fixed", "0", CVAR.USERINFO | CVAR.ARCHIVE )
		};

		static int cvarTableSize = Marshal.SizeOf( typeof( cvarTable_t ) ) * cvarTable.Length;// sizeof( cvarTable ) / sizeof( cvarTable[0] );

		/*
		=================
		CG_RegisterCvars
		=================
		*/
		static void CG_RegisterCvars( ) 
		{
			int			i;
			cvarTable_t	*cv;
			char		var[MAX_TOKEN_CHARS];

			for ( i = 0, cv = cvarTable ; i < cvarTableSize ; i++, cv++ ) {
				cg_syscalls.trap_Cvar_Register( cv->vmCvar, cv->cvarName,
					cv->defaultString, cv->cvarFlags );
			}

			// see if we are also running the server on this machine
			cg_syscalls.trap_Cvar_VariableStringBuffer( "sv_running", var, sizeof( var ) );
			cgs.localServer = atoi( var );

			forceModelModificationCount = cg_forceModel.modificationCount;

			cg_syscalls.trap_Cvar_Register(NULL, "model", DEFAULT_MODEL, CVAR_USERINFO | CVAR_ARCHIVE );
			cg_syscalls.trap_Cvar_Register(NULL, "headmodel", DEFAULT_MODEL, CVAR_USERINFO | CVAR_ARCHIVE );
			cg_syscalls.trap_Cvar_Register(NULL, "team_model", DEFAULT_TEAM_MODEL, CVAR_USERINFO | CVAR_ARCHIVE );
			cg_syscalls.trap_Cvar_Register(NULL, "team_headmodel", DEFAULT_TEAM_HEAD, CVAR_USERINFO | CVAR_ARCHIVE );
		}

		/*																																			
		===================
		CG_ForceModelChange
		===================
		*/
		static void CG_ForceModelChange( void ) {
			int		i;

			for (i=0 ; i<MAX_CLIENTS ; i++) {
				const char		*clientInfo;

				clientInfo = CG_ConfigString( CS_PLAYERS+i );
				if ( !clientInfo[0] ) {
					continue;
				}
				CG_NewClientInfo( i );
			}
		}

		/*
		=================
		CG_UpdateCvars
		=================
		*/
		void CG_UpdateCvars( void ) {
			int			i;
			cvarTable_t	*cv;

			for ( i = 0, cv = cvarTable ; i < cvarTableSize ; i++, cv++ ) {
				trap_Cvar_Update( cv->vmCvar );
			}

			// check for modications here

			// If team overlay is on, ask for updates from the server.  If its off,
			// let the server know so we don't receive it
			if ( drawTeamOverlayModificationCount != cg_drawTeamOverlay.modificationCount ) {
				drawTeamOverlayModificationCount = cg_drawTeamOverlay.modificationCount;

				if ( cg_drawTeamOverlay.integer > 0 ) {
					cg_syscalls.trap_Cvar_Set( "teamoverlay", "1" );
				} else {
					cg_syscalls.trap_Cvar_Set( "teamoverlay", "0" );
				}
				// FIXME E3 HACK
				cg_syscalls.trap_Cvar_Set( "teamoverlay", "1" );
			}

			// if force model changed
			if ( forceModelModificationCount != cg_forceModel.modificationCount ) {
				forceModelModificationCount = cg_forceModel.modificationCount;
				CG_ForceModelChange();
			}
		}

		static int CG_CrosshairPlayer( ) {
			if ( cg.time > ( cg.crosshairClientTime + 1000 ) ) {
				return -1;
			}
			return cg.crosshairClientNum;
		}

		static int CG_LastAttacker( ) {
			if ( !cg.attackerTime ) {
				return -1;
			}
			return cg.snap->ps.persistant[PERS_ATTACKER];
		}

		public static void CG_Printf( string msg, params object[] parameters )
		{
			var text = SprintfNET.StringFormatter.PrintF( msg, parameters );			

			cg_syscalls.trap_Print( text );
		}

		public static void CG_Error( string msg, params object[] parameters ) 
		{
			var text = SprintfNET.StringFormatter.PrintF( msg, parameters );
		
			cg_syscalls.trap_Error( text );
		}

		// this is only here so the functions in q_shared.c and bg_*.c can link (FIXME)

		public static void Com_Error( int level, string msg, params object[] parameters )
		{
			CG_Error( "%s", text);
		}

		public static void Com_Printf( string msg, params object[] parameters )
		{ 
			var text = SprintfNET.StringFormatter.PrintF( msg, parameters );

			CG_Printf ("%s", text);
		}

		/*
		================
		CG_Argv
		================
		*/
		const char *CG_Argv( int arg ) {
			static char	buffer[MAX_STRING_CHARS];

			trap_Argv( arg, buffer, sizeof( buffer ) );

			return buffer;
		}


		//========================================================================

		/*
		=================
		CG_RegisterItemSounds

		The server says this item is used on this level
		=================
		*/
		static void CG_RegisterItemSounds( int itemNum ) {
			gitem_t			*item;
			char			data[MAX_QPATH];
			char			*s, *start;
			int				len;

			item = &bg_itemlist[ itemNum ];

			if( item->pickup_sound ) {
				trap_S_RegisterSound( item->pickup_sound, false );
			}

			// parse the space seperated precache string for other media
			s = item->sounds;
			if (!s || !s[0])
				return;

			while (*s) {
				start = s;
				while (*s && *s != ' ') {
					s++;
				}

				len = s-start;
				if (len >= MAX_QPATH || len < 5) {
					CG_Error( "PrecacheItem: %s has bad precache string", 
						item->classname);
					return;
				}
				memcpy (data, start, len);
				data[len] = 0;
				if ( *s ) {
					s++;
				}

				if ( !strcmp(data+len-3, "wav" )) {
					trap_S_RegisterSound( data, false );
				}
			}
		}


		/*
		=================
		CG_RegisterSounds

		called during a precache command
		=================
		*/
		static void CG_RegisterSounds( void ) {
			int		i;
			char	items[MAX_ITEMS+1];
			char	name[MAX_QPATH];
			const char	*soundName;

			cgs.media.oneMinuteSound = trap_S_RegisterSound( "sound/feedback/1_minute.wav", true );
			cgs.media.fiveMinuteSound = trap_S_RegisterSound( "sound/feedback/5_minute.wav", true );
			cgs.media.suddenDeathSound = trap_S_RegisterSound( "sound/feedback/sudden_death.wav", true );
			cgs.media.oneFragSound = trap_S_RegisterSound( "sound/feedback/1_frag.wav", true );
			cgs.media.twoFragSound = trap_S_RegisterSound( "sound/feedback/2_frags.wav", true );
			cgs.media.threeFragSound = trap_S_RegisterSound( "sound/feedback/3_frags.wav", true );
			cgs.media.count3Sound = trap_S_RegisterSound( "sound/feedback/three.wav", true );
			cgs.media.count2Sound = trap_S_RegisterSound( "sound/feedback/two.wav", true );
			cgs.media.count1Sound = trap_S_RegisterSound( "sound/feedback/one.wav", true );
			cgs.media.countFightSound = trap_S_RegisterSound( "sound/feedback/fight.wav", true );
			cgs.media.countPrepareSound = trap_S_RegisterSound( "sound/feedback/prepare.wav", true );

			if ( cgs.gametype >= GT_TEAM || cg_buildScript.integer ) {

				cgs.media.captureAwardSound = trap_S_RegisterSound( "sound/teamplay/flagcapture_yourteam.wav", true );
				cgs.media.redLeadsSound = trap_S_RegisterSound( "sound/feedback/redleads.wav", true );
				cgs.media.blueLeadsSound = trap_S_RegisterSound( "sound/feedback/blueleads.wav", true );
				cgs.media.teamsTiedSound = trap_S_RegisterSound( "sound/feedback/teamstied.wav", true );
				cgs.media.hitTeamSound = trap_S_RegisterSound( "sound/feedback/hit_teammate.wav", true );

				cgs.media.redScoredSound = trap_S_RegisterSound( "sound/teamplay/voc_red_scores.wav", true );
				cgs.media.blueScoredSound = trap_S_RegisterSound( "sound/teamplay/voc_blue_scores.wav", true );

				cgs.media.captureYourTeamSound = trap_S_RegisterSound( "sound/teamplay/flagcapture_yourteam.wav", true );
				cgs.media.captureOpponentSound = trap_S_RegisterSound( "sound/teamplay/flagcapture_opponent.wav", true );

				cgs.media.returnYourTeamSound = trap_S_RegisterSound( "sound/teamplay/flagreturn_yourteam.wav", true );
				cgs.media.returnOpponentSound = trap_S_RegisterSound( "sound/teamplay/flagreturn_opponent.wav", true );

				cgs.media.takenYourTeamSound = trap_S_RegisterSound( "sound/teamplay/flagtaken_yourteam.wav", true );
				cgs.media.takenOpponentSound = trap_S_RegisterSound( "sound/teamplay/flagtaken_opponent.wav", true );

				if ( cgs.gametype == GT_CTF || cg_buildScript.integer ) {
					cgs.media.redFlagReturnedSound = trap_S_RegisterSound( "sound/teamplay/voc_red_returned.wav", true );
					cgs.media.blueFlagReturnedSound = trap_S_RegisterSound( "sound/teamplay/voc_blue_returned.wav", true );
					cgs.media.enemyTookYourFlagSound = trap_S_RegisterSound( "sound/teamplay/voc_enemy_flag.wav", true );
					cgs.media.yourTeamTookEnemyFlagSound = trap_S_RegisterSound( "sound/teamplay/voc_team_flag.wav", true );
				}

				cgs.media.youHaveFlagSound = trap_S_RegisterSound( "sound/teamplay/voc_you_flag.wav", true );
				cgs.media.holyShitSound = trap_S_RegisterSound("sound/feedback/voc_holyshit.wav", true);
				cgs.media.neutralFlagReturnedSound = trap_S_RegisterSound( "sound/teamplay/flagreturn_opponent.wav", true );
				cgs.media.yourTeamTookTheFlagSound = trap_S_RegisterSound( "sound/teamplay/voc_team_1flag.wav", true );
				cgs.media.enemyTookTheFlagSound = trap_S_RegisterSound( "sound/teamplay/voc_enemy_1flag.wav", true );
			}

			cgs.media.tracerSound = trap_S_RegisterSound( "sound/weapons/machinegun/buletby1.wav", false );
			cgs.media.selectSound = trap_S_RegisterSound( "sound/weapons/change.wav", false );
			cgs.media.wearOffSound = trap_S_RegisterSound( "sound/items/wearoff.wav", false );
			cgs.media.useNothingSound = trap_S_RegisterSound( "sound/items/use_nothing.wav", false );
			cgs.media.gibSound = trap_S_RegisterSound( "sound/player/gibsplt1.wav", false );
			cgs.media.gibBounce1Sound = trap_S_RegisterSound( "sound/player/gibimp1.wav", false );
			cgs.media.gibBounce2Sound = trap_S_RegisterSound( "sound/player/gibimp2.wav", false );
			cgs.media.gibBounce3Sound = trap_S_RegisterSound( "sound/player/gibimp3.wav", false );

			cgs.media.teleInSound = trap_S_RegisterSound( "sound/world/telein.wav", false );
			cgs.media.teleOutSound = trap_S_RegisterSound( "sound/world/teleout.wav", false );
			cgs.media.respawnSound = trap_S_RegisterSound( "sound/items/respawn1.wav", false );

			cgs.media.noAmmoSound = trap_S_RegisterSound( "sound/weapons/noammo.wav", false );

			cgs.media.talkSound = trap_S_RegisterSound( "sound/player/talk.wav", false );
			cgs.media.landSound = trap_S_RegisterSound( "sound/player/land1.wav", false);

			cgs.media.hitSound = trap_S_RegisterSound( "sound/feedback/hit.wav", false );

			cgs.media.impressiveSound = trap_S_RegisterSound( "sound/feedback/impressive.wav", true );
			cgs.media.excellentSound = trap_S_RegisterSound( "sound/feedback/excellent.wav", true );
			cgs.media.deniedSound = trap_S_RegisterSound( "sound/feedback/denied.wav", true );
			cgs.media.humiliationSound = trap_S_RegisterSound( "sound/feedback/humiliation.wav", true );
			cgs.media.assistSound = trap_S_RegisterSound( "sound/feedback/assist.wav", true );
			cgs.media.defendSound = trap_S_RegisterSound( "sound/feedback/defense.wav", true );

			cgs.media.takenLeadSound = trap_S_RegisterSound( "sound/feedback/takenlead.wav", true);
			cgs.media.tiedLeadSound = trap_S_RegisterSound( "sound/feedback/tiedlead.wav", true);
			cgs.media.lostLeadSound = trap_S_RegisterSound( "sound/feedback/lostlead.wav", true);

			cgs.media.watrInSound = trap_S_RegisterSound( "sound/player/watr_in.wav", false);
			cgs.media.watrOutSound = trap_S_RegisterSound( "sound/player/watr_out.wav", false);
			cgs.media.watrUnSound = trap_S_RegisterSound( "sound/player/watr_un.wav", false);

			cgs.media.jumpPadSound = trap_S_RegisterSound ("sound/world/jumppad.wav", false );

			for (i=0 ; i<4 ; i++) {
				Com_sprintf (name, sizeof(name), "sound/player/footsteps/step%i.wav", i+1);
				cgs.media.footsteps[FOOTSTEP_NORMAL][i] = trap_S_RegisterSound (name, false);

				Com_sprintf (name, sizeof(name), "sound/player/footsteps/boot%i.wav", i+1);
				cgs.media.footsteps[FOOTSTEP_BOOT][i] = trap_S_RegisterSound (name, false);

				Com_sprintf (name, sizeof(name), "sound/player/footsteps/flesh%i.wav", i+1);
				cgs.media.footsteps[FOOTSTEP_FLESH][i] = trap_S_RegisterSound (name, false);

				Com_sprintf (name, sizeof(name), "sound/player/footsteps/mech%i.wav", i+1);
				cgs.media.footsteps[FOOTSTEP_MECH][i] = trap_S_RegisterSound (name, false);

				Com_sprintf (name, sizeof(name), "sound/player/footsteps/energy%i.wav", i+1);
				cgs.media.footsteps[FOOTSTEP_ENERGY][i] = trap_S_RegisterSound (name, false);

				Com_sprintf (name, sizeof(name), "sound/player/footsteps/splash%i.wav", i+1);
				cgs.media.footsteps[FOOTSTEP_SPLASH][i] = trap_S_RegisterSound (name, false);

				Com_sprintf (name, sizeof(name), "sound/player/footsteps/clank%i.wav", i+1);
				cgs.media.footsteps[FOOTSTEP_METAL][i] = trap_S_RegisterSound (name, false);
			}

			// only register the items that the server says we need
			strcpy( items, CG_ConfigString( CS_ITEMS ) );

			for ( i = 1 ; i < bg_numItems ; i++ ) {
		//		if ( items[ i ] == '1' || cg_buildScript.integer ) {
					CG_RegisterItemSounds( i );
		//		}
			}

			for ( i = 1 ; i < MAX_SOUNDS ; i++ ) {
				soundName = CG_ConfigString( CS_SOUNDS+i );
				if ( !soundName[0] ) {
					break;
				}
				if ( soundName[0] == '*' ) {
					continue;	// custom sound
				}
				cgs.gameSounds[i] = trap_S_RegisterSound( soundName, false );
			}

			// FIXME: only needed with item
			cgs.media.flightSound = trap_S_RegisterSound( "sound/items/flight.wav", false );
			cgs.media.medkitSound = trap_S_RegisterSound ("sound/items/use_medkit.wav", false);
			cgs.media.quadSound = trap_S_RegisterSound("sound/items/damage3.wav", false);
			cgs.media.sfx_ric1 = trap_S_RegisterSound ("sound/weapons/machinegun/ric1.wav", false);
			cgs.media.sfx_ric2 = trap_S_RegisterSound ("sound/weapons/machinegun/ric2.wav", false);
			cgs.media.sfx_ric3 = trap_S_RegisterSound ("sound/weapons/machinegun/ric3.wav", false);
			cgs.media.sfx_railg = trap_S_RegisterSound ("sound/weapons/railgun/railgf1a.wav", false);
			cgs.media.sfx_rockexp = trap_S_RegisterSound ("sound/weapons/rocket/rocklx1a.wav", false);
			cgs.media.sfx_plasmaexp = trap_S_RegisterSound ("sound/weapons/plasma/plasmx1a.wav", false);

			cgs.media.regenSound = trap_S_RegisterSound("sound/items/regen.wav", false);
			cgs.media.protectSound = trap_S_RegisterSound("sound/items/protect3.wav", false);
			cgs.media.n_healthSound = trap_S_RegisterSound("sound/items/n_health.wav", false );
			cgs.media.hgrenb1aSound = trap_S_RegisterSound("sound/weapons/grenade/hgrenb1a.wav", false);
			cgs.media.hgrenb2aSound = trap_S_RegisterSound("sound/weapons/grenade/hgrenb2a.wav", false);
		}


		//===================================================================================


		/*
		=================
		CG_RegisterGraphics

		This function may execute for a couple of minutes with a slow disk.
		=================
		*/
		static void CG_RegisterGraphics( void ) {
			int			i;
			char		items[MAX_ITEMS+1];
			static char		*sb_nums[11] = {
				"gfx/2d/numbers/zero_32b",
				"gfx/2d/numbers/one_32b",
				"gfx/2d/numbers/two_32b",
				"gfx/2d/numbers/three_32b",
				"gfx/2d/numbers/four_32b",
				"gfx/2d/numbers/five_32b",
				"gfx/2d/numbers/six_32b",
				"gfx/2d/numbers/seven_32b",
				"gfx/2d/numbers/eight_32b",
				"gfx/2d/numbers/nine_32b",
				"gfx/2d/numbers/minus_32b",
			};

			// clear any references to old media
			memset( &cg.refdef, 0, sizeof( cg.refdef ) );
			trap_R_ClearScene();

			CG_LoadingString( cgs.mapname );

			trap_R_LoadWorldMap( cgs.mapname );

			// precache status bar pics
			CG_LoadingString( "game media" );

			for ( i=0 ; i<11 ; i++) {
				cgs.media.numberShaders[i] = trap_R_RegisterShader( sb_nums[i] );
			}

			cgs.media.botSkillShaders[0] = trap_R_RegisterShader( "menu/art/skill1.tga" );
			cgs.media.botSkillShaders[1] = trap_R_RegisterShader( "menu/art/skill2.tga" );
			cgs.media.botSkillShaders[2] = trap_R_RegisterShader( "menu/art/skill3.tga" );
			cgs.media.botSkillShaders[3] = trap_R_RegisterShader( "menu/art/skill4.tga" );
			cgs.media.botSkillShaders[4] = trap_R_RegisterShader( "menu/art/skill5.tga" );

			cgs.media.viewBloodShader = trap_R_RegisterShader( "viewBloodBlend" );

			cgs.media.deferShader = trap_R_RegisterShaderNoMip( "gfx/2d/defer.tga" );

			cgs.media.scoreboardName = trap_R_RegisterShaderNoMip( "menu/tab/name.tga" );
			cgs.media.scoreboardPing = trap_R_RegisterShaderNoMip( "menu/tab/ping.tga" );
			cgs.media.scoreboardScore = trap_R_RegisterShaderNoMip( "menu/tab/score.tga" );
			cgs.media.scoreboardTime = trap_R_RegisterShaderNoMip( "menu/tab/time.tga" );

			cgs.media.smokePuffShader = trap_R_RegisterShader( "smokePuff" );
			cgs.media.smokePuffRageProShader = trap_R_RegisterShader( "smokePuffRagePro" );
			cgs.media.shotgunSmokePuffShader = trap_R_RegisterShader( "shotgunSmokePuff" );
			cgs.media.plasmaBallShader = trap_R_RegisterShader( "sprites/plasma1" );
			cgs.media.bloodTrailShader = trap_R_RegisterShader( "bloodTrail" );
			cgs.media.lagometerShader = trap_R_RegisterShader("lagometer" );
			cgs.media.connectionShader = trap_R_RegisterShader( "disconnected" );

			cgs.media.waterBubbleShader = trap_R_RegisterShader( "waterBubble" );

			cgs.media.tracerShader = trap_R_RegisterShader( "gfx/misc/tracer" );
			cgs.media.selectShader = trap_R_RegisterShader( "gfx/2d/select" );

			for ( i = 0 ; i < NUM_CROSSHAIRS ; i++ ) {
				cgs.media.crosshairShader[i] = trap_R_RegisterShader( va("gfx/2d/crosshair%c", 'a'+i) );
			}

			cgs.media.backTileShader = trap_R_RegisterShader( "gfx/2d/backtile" );
			cgs.media.noammoShader = trap_R_RegisterShader( "icons/noammo" );

			// powerup shaders
			cgs.media.quadShader = trap_R_RegisterShader("powerups/quad" );
			cgs.media.quadWeaponShader = trap_R_RegisterShader("powerups/quadWeapon" );
			cgs.media.battleSuitShader = trap_R_RegisterShader("powerups/battleSuit" );
			cgs.media.battleWeaponShader = trap_R_RegisterShader("powerups/battleWeapon" );
			cgs.media.invisShader = trap_R_RegisterShader("powerups/invisibility" );
			cgs.media.regenShader = trap_R_RegisterShader("powerups/regen" );
			cgs.media.hastePuffShader = trap_R_RegisterShader("hasteSmokePuff" );

			if ( cgs.gametype == GT_CTF || cg_buildScript.integer ) {
				cgs.media.redCubeModel = trap_R_RegisterModel( "models/powerups/orb/r_orb.md3" );
				cgs.media.blueCubeModel = trap_R_RegisterModel( "models/powerups/orb/b_orb.md3" );
				cgs.media.redCubeIcon = trap_R_RegisterShader( "icons/skull_red" );
				cgs.media.blueCubeIcon = trap_R_RegisterShader( "icons/skull_blue" );
			}

			if ( cgs.gametype == GT_CTF || cg_buildScript.integer ) {
				cgs.media.redFlagModel = trap_R_RegisterModel( "models/flags/r_flag.md3" );
				cgs.media.blueFlagModel = trap_R_RegisterModel( "models/flags/b_flag.md3" );
				cgs.media.redFlagShader[0] = trap_R_RegisterShaderNoMip( "icons/iconf_red1" );
				cgs.media.redFlagShader[1] = trap_R_RegisterShaderNoMip( "icons/iconf_red2" );
				cgs.media.redFlagShader[2] = trap_R_RegisterShaderNoMip( "icons/iconf_red3" );
				cgs.media.blueFlagShader[0] = trap_R_RegisterShaderNoMip( "icons/iconf_blu1" );
				cgs.media.blueFlagShader[1] = trap_R_RegisterShaderNoMip( "icons/iconf_blu2" );
				cgs.media.blueFlagShader[2] = trap_R_RegisterShaderNoMip( "icons/iconf_blu3" );
			}

			if ( cgs.gametype >= GT_TEAM || cg_buildScript.integer ) {
				cgs.media.friendShader = trap_R_RegisterShader( "sprites/foe" );
				cgs.media.redQuadShader = trap_R_RegisterShader("powerups/blueflag" );
				cgs.media.teamStatusBar = trap_R_RegisterShader( "gfx/2d/colorbar.tga" );
			}

			cgs.media.armorModel = trap_R_RegisterModel( "models/powerups/armor/armor_yel.md3" );
			cgs.media.armorIcon  = trap_R_RegisterShaderNoMip( "icons/iconr_yellow" );

			cgs.media.machinegunBrassModel = trap_R_RegisterModel( "models/weapons2/shells/m_shell.md3" );
			cgs.media.shotgunBrassModel = trap_R_RegisterModel( "models/weapons2/shells/s_shell.md3" );

			cgs.media.gibAbdomen = trap_R_RegisterModel( "models/gibs/abdomen.md3" );
			cgs.media.gibArm = trap_R_RegisterModel( "models/gibs/arm.md3" );
			cgs.media.gibChest = trap_R_RegisterModel( "models/gibs/chest.md3" );
			cgs.media.gibFist = trap_R_RegisterModel( "models/gibs/fist.md3" );
			cgs.media.gibFoot = trap_R_RegisterModel( "models/gibs/foot.md3" );
			cgs.media.gibForearm = trap_R_RegisterModel( "models/gibs/forearm.md3" );
			cgs.media.gibIntestine = trap_R_RegisterModel( "models/gibs/intestine.md3" );
			cgs.media.gibLeg = trap_R_RegisterModel( "models/gibs/leg.md3" );
			cgs.media.gibSkull = trap_R_RegisterModel( "models/gibs/skull.md3" );
			cgs.media.gibBrain = trap_R_RegisterModel( "models/gibs/brain.md3" );

			cgs.media.smoke2 = trap_R_RegisterModel( "models/weapons2/shells/s_shell.md3" );

			cgs.media.balloonShader = trap_R_RegisterShader( "sprites/balloon3" );

			cgs.media.bloodExplosionShader = trap_R_RegisterShader( "bloodExplosion" );

			cgs.media.bulletFlashModel = trap_R_RegisterModel("models/weaphits/bullet.md3");
			cgs.media.ringFlashModel = trap_R_RegisterModel("models/weaphits/ring02.md3");
			cgs.media.dishFlashModel = trap_R_RegisterModel("models/weaphits/boom01.md3");
			cgs.media.teleportEffectModel = trap_R_RegisterModel( "models/misc/telep.md3" );
			cgs.media.teleportEffectShader = trap_R_RegisterShader( "teleportEffect" );

			cgs.media.invulnerabilityPowerupModel = trap_R_RegisterModel( "models/powerups/shield/shield.md3" );
			cgs.media.medalImpressive = trap_R_RegisterShaderNoMip( "medal_impressive" );
			cgs.media.medalExcellent = trap_R_RegisterShaderNoMip( "medal_excellent" );
			cgs.media.medalGauntlet = trap_R_RegisterShaderNoMip( "medal_gauntlet" );
			cgs.media.medalDefend = trap_R_RegisterShaderNoMip( "medal_defend" );
			cgs.media.medalAssist = trap_R_RegisterShaderNoMip( "medal_assist" );
			cgs.media.medalCapture = trap_R_RegisterShaderNoMip( "medal_capture" );


			memset( cg_items, 0, sizeof( cg_items ) );
			memset( cg_weapons, 0, sizeof( cg_weapons ) );

			// only register the items that the server says we need
			strcpy( items, CG_ConfigString( CS_ITEMS) );

			for ( i = 1 ; i < bg_numItems ; i++ ) {
				if ( items[ i ] == '1' || cg_buildScript.integer ) {
					CG_LoadingItem( i );
					CG_RegisterItemVisuals( i );
				}
			}

			// wall marks
			cgs.media.bulletMarkShader = trap_R_RegisterShader( "gfx/damage/bullet_mrk" );
			cgs.media.burnMarkShader = trap_R_RegisterShader( "gfx/damage/burn_med_mrk" );
			cgs.media.holeMarkShader = trap_R_RegisterShader( "gfx/damage/hole_lg_mrk" );
			cgs.media.energyMarkShader = trap_R_RegisterShader( "gfx/damage/plasma_mrk" );
			cgs.media.shadowMarkShader = trap_R_RegisterShader( "markShadow" );
			cgs.media.wakeMarkShader = trap_R_RegisterShader( "wake" );
			cgs.media.bloodMarkShader = trap_R_RegisterShader( "bloodMark" );

			// register the inline models
			cgs.numInlineModels = trap_CM_NumInlineModels();
			for ( i = 1 ; i < cgs.numInlineModels ; i++ ) {
				char	name[10];
				vec3_t			mins, maxs;
				int				j;

				Com_sprintf( name, sizeof(name), "*%i", i );
				cgs.inlineDrawModel[i] = trap_R_RegisterModel( name );
				trap_R_ModelBounds( cgs.inlineDrawModel[i], mins, maxs );
				for ( j = 0 ; j < 3 ; j++ ) {
					cgs.inlineModelMidpoints[i][j] = mins[j] + 0.5 * ( maxs[j] - mins[j] );
				}
			}

			// register all the server specified models
			for (i=1 ; i<MAX_MODELS ; i++) {
				const char		*modelName;

				modelName = CG_ConfigString( CS_MODELS+i );
				if ( !modelName[0] ) {
					break;
				}
				cgs.gameModels[i] = trap_R_RegisterModel( modelName );
			}

			CG_ClearParticles ();
		/*
			for (i=1; i<MAX_PARTICLES_AREAS; i++)
			{
				{
					int rval;

					rval = CG_NewParticleArea ( CS_PARTICLES + i);
					if (!rval)
						break;
				}
			}
		*/
		}



		/*																																			
		=======================
		CG_BuildSpectatorString

		=======================
		*/
		void CG_BuildSpectatorString() {
			int i;
			cg.spectatorList[0] = 0;
			for (i = 0; i < MAX_CLIENTS; i++) {
				if (cgs.clientinfo[i].infoValid && cgs.clientinfo[i].team == TEAM_SPECTATOR ) {
					Q_strcat(cg.spectatorList, sizeof(cg.spectatorList), va("%s     ", cgs.clientinfo[i].name));
				}
			}
			i = (int)strlen(cg.spectatorList);
			if (i != cg.spectatorLen) {
				cg.spectatorLen = i;
				cg.spectatorWidth = -1;
			}
		}


		/*																																			
		===================
		CG_RegisterClients
		===================
		*/
		static void CG_RegisterClients( void ) {
			int		i;

			CG_LoadingClient(cg.clientNum);
			CG_NewClientInfo(cg.clientNum);

			for (i=0 ; i<MAX_CLIENTS ; i++) {
				const char		*clientInfo;

				if (cg.clientNum == i) {
					continue;
				}

				clientInfo = CG_ConfigString( CS_PLAYERS+i );
				if ( !clientInfo[0]) {
					continue;
				}
				CG_LoadingClient( i );
				CG_NewClientInfo( i );
			}
			CG_BuildSpectatorString();
		}

		//===========================================================================

		/*
		=================
		CG_ConfigString
		=================
		*/
		const char *CG_ConfigString( int index ) {
			if ( index < 0 || index >= MAX_CONFIGSTRINGS ) {
				CG_Error( "CG_ConfigString: bad index: %i", index );
			}
			return cgs.gameState.stringData + cgs.gameState.stringOffsets[ index ];
		}

		//==================================================================

		/*
		======================
		CG_StartMusic

		======================
		*/
		void CG_StartMusic( void ) {
			char	*s;
			char	parm1[MAX_QPATH], parm2[MAX_QPATH];

			// start the background music
			s = (char *)CG_ConfigString( CS_MUSIC );
			Q_strncpyz( parm1, COM_Parse( &s ), sizeof( parm1 ) );
			Q_strncpyz( parm2, COM_Parse( &s ), sizeof( parm2 ) );

			trap_S_StartBackgroundTrack( parm1, parm2 );
		}

		/*
		=================
		CG_Init

		Called after every level change or subsystem restart
		Will perform callbacks to make the loading info screen update.
		=================
		*/
		static void CG_Init( int serverMessageNum, int serverCommandSequence, int clientNum ) {
			const char	*s;

			// clear everything
			memset( &cgs, 0, sizeof( cgs ) );
			memset( &cg, 0, sizeof( cg ) );
			memset( cg_entities, 0, sizeof(cg_entities) );
			memset( cg_weapons, 0, sizeof(cg_weapons) );
			memset( cg_items, 0, sizeof(cg_items) );

			cg.clientNum = clientNum;

			cgs.processedSnapshotNum = serverMessageNum;
			cgs.serverCommandSequence = serverCommandSequence;

			// load a few needed things before we do any screen updates
			cgs.media.charsetShader		= trap_R_RegisterShader( "gfx/2d/bigchars" );
			cgs.media.whiteShader		= trap_R_RegisterShader( "white" );
			cgs.media.charsetProp		= trap_R_RegisterShaderNoMip( "menu/art/font1_prop.tga" );
			cgs.media.charsetPropGlow	= trap_R_RegisterShaderNoMip( "menu/art/font1_prop_glo.tga" );
			cgs.media.charsetPropB		= trap_R_RegisterShaderNoMip( "menu/art/font2_prop.tga" );

			CG_RegisterCvars();

			CG_InitConsoleCommands();

			cg.weaponSelect = WP_MACHINEGUN;

			cgs.redflag = cgs.blueflag = -1; // For compatibily, default to unset for
			cgs.flagStatus = -1;
			// old servers

			// get the rendering configuration from the client system
			trap_GetGlconfig( &cgs.glconfig );
			cgs.screenXScale = cgs.glconfig.vidWidth / 640.0;
			cgs.screenYScale = cgs.glconfig.vidHeight / 480.0;

			// get the gamestate from the client system
			trap_GetGameState( &cgs.gameState );

			// check version
			s = CG_ConfigString( CS_GAME_VERSION );
			if ( strcmp( s, GAME_VERSION ) ) {
				CG_Error( "Client/Server game mismatch: %s/%s", GAME_VERSION, s );
			}

			s = CG_ConfigString( CS_LEVEL_START_TIME );
			cgs.levelStartTime = atoi( s );

			CG_ParseServerinfo();

			// load the new map
			CG_LoadingString( "collision map" );

			trap_CM_LoadMap( cgs.mapname );

			cg.loading = true;		// force players to load instead of defer

			CG_LoadingString( "sounds" );

			CG_RegisterSounds();

			CG_LoadingString( "graphics" );

			CG_RegisterGraphics();

			CG_LoadingString( "clients" );

			CG_RegisterClients();		// if low on memory, some clients will be deferred

			cg.loading = false;	// future players will be deferred

			CG_InitLocalEntities();

			CG_InitMarkPolys();

			// remove the last loading update
			cg.infoScreenText[0] = 0;

			// Make sure we have update values (scores)
			CG_SetConfigValues();

			CG_StartMusic();

			CG_LoadingString( "" );

			CG_ShaderStateChanged();

			trap_S_ClearLoopingSounds( true );
		}

		/*
		=================
		CG_Shutdown

		Called before every level change or subsystem restart
		=================
		*/
		static void CG_Shutdown( ) 
		{
			// some mods may need to do cleanup work here,
			// like closing files or archiving session data
		}


		/*
		==================
		CG_EventHandling
		==================
		 type 0 - no event handling
		      1 - team menu
		      2 - hud editor

		*/
		static void CG_EventHandling(int type) 
		{
		}



		void CG_KeyEvent(int key, bool down) {
		}

		static void CG_MouseEvent(int x, int y)
		{
		}
	}
}
