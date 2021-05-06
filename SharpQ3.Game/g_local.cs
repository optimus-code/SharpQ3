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

namespace SharpQ3.Game
{
	// g_local.h -- local definitions for game module
	public static class g_local
	{
		// the "gameversion" client command will print this plus compile date
		#define	GAMEVERSION	"baseq3"

		#define BODY_QUEUE_SIZE		8

		#define INFINITE			1000000

		#define	FRAMETIME			100					// msec
		#define	CARNAGE_REWARD_TIME	3000
		#define REWARD_SPRITE_TIME	2000

		#define	INTERMISSION_DELAY_TIME	1000
		#define	SP_INTERMISSION_DELAY_TIME	5000

		// gentity->flags
		#define	FL_GODMODE				0x00000010
		#define	FL_NOTARGET				0x00000020
		#define	FL_TEAMSLAVE			0x00000400	// not the first on the team
		#define FL_NO_KNOCKBACK			0x00000800
		#define FL_DROPPED_ITEM			0x00001000
		#define FL_NO_BOTS				0x00002000	// spawn point not for bot use
		#define FL_NO_HUMANS			0x00004000	// spawn point just for bots
		#define FL_FORCE_GESTURE		0x00008000	// force gesture on client

		// movers are things like doors, plats, buttons, etc
		typedef enum {
			MOVER_POS1,
			MOVER_POS2,
			MOVER_1TO2,
			MOVER_2TO1
		} moverState_t;

		#define SP_PODIUM_MODEL		"models/mapobjects/podium/podium4.md3"

		//============================================================================

		typedef struct gentity_s gentity_t;
		typedef struct gclient_s gclient_t;

		struct gentity_s {
			entityState_t	s;				// communicated by server to clients
			entityShared_t	r;				// shared by both the server system and game

			// DO NOT MODIFY ANYTHING ABOVE THIS, THE SERVER
			// EXPECTS THE FIELDS IN THAT ORDER!
			//================================

			struct gclient_s	*client;			// NULL if not a client

			bool	inuse;

			char		*classname;			// set in QuakeEd
			int			spawnflags;			// set in QuakeEd

			bool	neverFree;			// if true, FreeEntity will only unlink
											// bodyque uses this

			int			flags;				// FL_* variables

			char		*model;
			char		*model2;
			int			freetime;			// level.time when the object was freed
			
			int			eventTime;			// events will be cleared EVENT_VALID_MSEC after set
			bool	freeAfterEvent;
			bool	unlinkAfterEvent;

			bool	physicsObject;		// if true, it can be pushed by movers and fall off edges
											// all game items are physicsObjects, 
			float		physicsBounce;		// 1.0 = continuous bounce, 0.0 = no bounce
			int			clipmask;			// brushes with this content value will be collided against
											// when moving.  items and corpses do not collide against
											// players, for instance

			// movers
			moverState_t moverState;
			int			soundPos1;
			int			sound1to2;
			int			sound2to1;
			int			soundPos2;
			int			soundLoop;
			gentity_t	*parent;
			gentity_t	*nextTrain;
			gentity_t	*prevTrain;
			vec3_t		pos1, pos2;

			char		*message;

			int			timestamp;		// body queue sinking, etc

			float		angle;			// set in editor, -1 = up, -2 = down
			char		*target;
			char		*targetname;
			char		*team;
			char		*targetShaderName;
			char		*targetShaderNewName;
			gentity_t	*target_ent;

			float		speed;
			vec3_t		movedir;

			int			nextthink;
			void		(*think)(gentity_t *self);
			void		(*reached)(gentity_t *self);	// movers call this when hitting endpoint
			void		(*blocked)(gentity_t *self, gentity_t *other);
			void		(*touch)(gentity_t *self, gentity_t *other, trace_t *trace);
			void		(*use)(gentity_t *self, gentity_t *other, gentity_t *activator);
			void		(*pain)(gentity_t *self, gentity_t *attacker, int damage);
			void		(*die)(gentity_t *self, gentity_t *inflictor, gentity_t *attacker, int damage, int mod);

			int			pain_debounce_time;
			int			fly_sound_debounce_time;	// wind tunnel
			int			last_move_time;

			int			health;

			bool	takedamage;

			int			damage;
			int			splashDamage;	// quad will increase this without increasing radius
			int			splashRadius;
			int			methodOfDeath;
			int			splashMethodOfDeath;

			int			count;

			gentity_t	*chain;
			gentity_t	*enemy;
			gentity_t	*activator;
			gentity_t	*teamchain;		// next entity in team
			gentity_t	*teammaster;	// master of the team

			int			watertype;
			int			waterlevel;

			int			noise_index;

			// timing variables
			float		wait;
			float		random;

			gitem_t		*item;			// for bonus items
		};


		typedef enum {
			CON_DISCONNECTED,
			CON_CONNECTING,
			CON_CONNECTED
		} clientConnected_t;

		typedef enum {
			SPECTATOR_NOT,
			SPECTATOR_FREE,
			SPECTATOR_FOLLOW,
			SPECTATOR_SCOREBOARD
		} spectatorState_t;

		typedef enum {
			TEAM_BEGIN,		// Beginning a team game, spawn at base
			TEAM_ACTIVE		// Now actively playing
		} playerTeamStateState_t;

		typedef struct {
			playerTeamStateState_t	state;

			int			location;

			int			captures;
			int			basedefense;
			int			carrierdefense;
			int			flagrecovery;
			int			fragcarrier;
			int			assists;

			float		lasthurtcarrier;
			float		lastreturnedflag;
			float		flagsince;
			float		lastfraggedcarrier;
		} playerTeamState_t;

		// the auto following clients don't follow a specific client
		// number, but instead follow the first two active players
		#define	FOLLOW_ACTIVE1	-1
		#define	FOLLOW_ACTIVE2	-2

		// client data that stays across multiple levels or tournament restarts
		// this is achieved by writing all the data to cvar strings at game shutdown
		// time and reading them back at connection time.  Anything added here
		// MUST be dealt with in G_InitSessionData() / G_ReadSessionData() / G_WriteSessionData()
		typedef struct {
			team_t		sessionTeam;
			int			spectatorTime;		// for determining next-in-line to play
			spectatorState_t	spectatorState;
			int			spectatorClient;	// for chasecam and follow mode
			int			wins, losses;		// tournament stats
			bool	teamLeader;			// true when this client is a team leader
		} clientSession_t;

		//
		#define MAX_NETNAME			36
		#define	MAX_VOTE_COUNT		3

		// client data that stays across multiple respawns, but is cleared
		// on each level change or team change at ClientBegin()
		typedef struct {
			clientConnected_t	connected;	
			usercmd_t	cmd;				// we would lose angles if not persistant
			bool	localClient;		// true if "ip" info key is "localhost"
			bool	initialSpawn;		// the first spawn should be at a cool location
			bool	predictItemPickup;	// based on cg_predictItems userinfo
			bool	pmoveFixed;			//
			char		netname[MAX_NETNAME];
			int			maxHealth;			// for handicapping
			int			enterTime;			// level.time the client entered the game
			playerTeamState_t teamState;	// status in teamplay games
			int			voteCount;			// to prevent people from constantly calling votes
			int			teamVoteCount;		// to prevent people from constantly calling votes
			bool	teamInfo;			// send team overlay updates?
		} clientPersistant_t;


		// this structure is cleared on each ClientSpawn(),
		// except for 'client->pers' and 'client->sess'
		struct gclient_s {
			// ps MUST be the first element, because the server expects it
			playerState_t	ps;				// communicated by server to clients

			// the rest of the structure is private to game
			clientPersistant_t	pers;
			clientSession_t		sess;

			bool	readyToExit;		// wishes to leave the intermission

			bool	noclip;

			int			lastCmdTime;		// level.time of last usercmd_t, for EF_CONNECTION
											// we can't just use pers.lastCommand.time, because
											// of the g_sycronousclients case
			int			buttons;
			int			oldbuttons;
			int			latched_buttons;

			vec3_t		oldOrigin;

			// sum up damage over an entire frame, so
			// shotgun blasts give a single big kick
			int			damage_armor;		// damage absorbed by armor
			int			damage_blood;		// damage taken out of health
			int			damage_knockback;	// impact damage
			vec3_t		damage_from;		// origin for vector calculation
			bool	damage_fromWorld;	// if true, don't use the damage_from vector

			int			accurateCount;		// for "impressive" reward sound

			int			accuracy_shots;		// total number of shots
			int			accuracy_hits;		// total number of hits

			//
			int			lastkilled_client;	// last client that this client killed
			int			lasthurt_client;	// last client that damaged this client
			int			lasthurt_mod;		// type of damage the client did

			// timers
			int			respawnTime;		// can respawn when time > this, force after g_forcerespwan
			int			inactivityTime;		// kick players when time > this
			bool	inactivityWarning;	// true if the five seoond warning has been given
			int			rewardTime;			// clear the EF_AWARD_IMPRESSIVE, etc when time > this

			int			airOutTime;

			int			lastKillTime;		// for multiple kill rewards

			bool	fireHeld;			// used for hook
			gentity_t	*hook;				// grapple hook if out

			int			switchTeamTime;		// time the player switched teams

			// timeResidual is used to handle events that happen every second
			// like health / armor countdowns and regeneration
			int			timeResidual;

			char		*areabits;
		};


		//
		// this structure is cleared as each map is entered
		//
		#define	MAX_SPAWN_VARS			64
		#define	MAX_SPAWN_VARS_CHARS	4096

		typedef struct {
			struct gclient_s	*clients;		// [maxclients]

			struct gentity_s	*gentities;
			int			gentitySize;
			int			num_entities;		// current number, <= MAX_GENTITIES

			int			warmupTime;			// restart match at this time

			fileHandle_t	logFile;

			// store latched cvars here that we want to get at often
			int			maxclients;

			int			framenum;
			int			time;					// in msec
			int			previousTime;			// so movers can back up when blocked

			int			startTime;				// level.time the map was started

			int			teamScores[TEAM_NUM_TEAMS];
			int			lastTeamLocationTime;		// last time of client team location update

			bool	newSession;				// don't use any old session data, because
												// we changed gametype

			bool	restarted;				// waiting for a map_restart to fire

			int			numConnectedClients;
			int			numNonSpectatorClients;	// includes connecting clients
			int			numPlayingClients;		// connected, non-spectators
			int			sortedClients[MAX_CLIENTS];		// sorted by score
			int			follow1, follow2;		// clientNums for auto-follow spectators

			int			snd_fry;				// sound index for standing in lava

			int			warmupModificationCount;	// for detecting if g_warmup is changed

			// voting state
			char		voteString[MAX_STRING_CHARS];
			char		voteDisplayString[MAX_STRING_CHARS];
			int			voteTime;				// level.time vote was called
			int			voteExecuteTime;		// time the vote is executed
			int			voteYes;
			int			voteNo;
			int			numVotingClients;		// set by CalculateRanks

			// team voting state
			char		teamVoteString[2][MAX_STRING_CHARS];
			int			teamVoteTime[2];		// level.time vote was called
			int			teamVoteYes[2];
			int			teamVoteNo[2];
			int			numteamVotingClients[2];// set by CalculateRanks

			// spawn variables
			bool	spawning;				// the G_Spawn*() functions are valid
			int			numSpawnVars;
			char		*spawnVars[MAX_SPAWN_VARS][2];	// key / value pairs
			int			numSpawnVarChars;
			char		spawnVarChars[MAX_SPAWN_VARS_CHARS];

			// intermission state
			int			intermissionQueued;		// intermission was qualified, but
												// wait INTERMISSION_DELAY_TIME before
												// actually going there so the last
												// frag can be watched.  Disable future
												// kills during this delay
			int			intermissiontime;		// time the intermission was started
			char		*changemap;
			bool	readyToExit;			// at least one client wants to exit
			int			exitTime;
			vec3_t		intermission_origin;	// also used for spectator spawns
			vec3_t		intermission_angle;

			bool	locationLinked;			// target_locations get linked
			gentity_t	*locationHead;			// head of the location list
			int			bodyQueIndex;			// dead bodies
			gentity_t	*bodyQue[BODY_QUEUE_SIZE];
		} level_locals_t;

		// damage flags
		#define DAMAGE_RADIUS				0x00000001	// damage was indirect
		#define DAMAGE_NO_ARMOR				0x00000002	// armour does not protect from this damage
		#define DAMAGE_NO_KNOCKBACK			0x00000004	// do not affect velocity, just view angles
		#define DAMAGE_NO_PROTECTION		0x00000008  // armor, shields, invulnerability, and godmode have no effect

		// ai_main.c
		#define MAX_FILEPATH			144

		//bot settings
		typedef struct bot_settings_s
		{
			char characterfile[MAX_FILEPATH];
			float skill;
			char team[MAX_FILEPATH];
		} bot_settings_t;

		#define	FOFS(x) ((int)(intptr_t)&(((gentity_t *)0)->x))
	}
}
