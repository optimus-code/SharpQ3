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

namespace SharpQ3.CGame
{
	public static class cg_local
	{
		// The entire cgame module is unloaded and reloaded on each level change,
		// so there is NO persistant data between levels on the client side.
		// If you absolutely need something stored, it can either be kept
		// by the server in the server stored userinfos, or stashed in a cvar.

		#define	POWERUP_BLINKS		5

		#define	POWERUP_BLINK_TIME	1000
		#define	FADE_TIME			200
		#define	PULSE_TIME			200
		#define	DAMAGE_DEFLECT_TIME	100
		#define	DAMAGE_RETURN_TIME	400
		#define DAMAGE_TIME			500
		#define	LAND_DEFLECT_TIME	150
		#define	LAND_RETURN_TIME	300
		#define	STEP_TIME			200
		#define	DUCK_TIME			100
		#define	PAIN_TWITCH_TIME	200
		#define	WEAPON_SELECT_TIME	1400
		#define	ITEM_SCALEUP_TIME	1000
		#define	ZOOM_TIME			150
		#define	ITEM_BLOB_TIME		200
		#define	MUZZLE_FLASH_TIME	20
		#define	SINK_TIME			1000		// time for fragments to sink into ground before going away
		#define	ATTACKER_HEAD_TIME	10000
		#define	REWARD_TIME			3000

		#define	PULSE_SCALE			1.5			// amount to scale up the icons when activating

		#define	MAX_STEP_CHANGE		32

		#define	MAX_VERTS_ON_POLY	10
		#define	MAX_MARK_POLYS		256

		#define STAT_MINUS			10	// num frame for '-' stats digit

		#define	ICON_SIZE			48
		#define	CHAR_WIDTH			32
		#define	CHAR_HEIGHT			48
		#define	TEXT_ICON_SPACE		4

		#define	TEAMCHAT_WIDTH		80
		#define TEAMCHAT_HEIGHT		8

		// very large characters
		#define	GIANT_WIDTH			32
		#define	GIANT_HEIGHT		48

		#define	NUM_CROSSHAIRS		10

		#define TEAM_OVERLAY_MAXNAME_WIDTH	12
		#define TEAM_OVERLAY_MAXLOCATION_WIDTH	16

		#define	DEFAULT_MODEL			"sarge"
		#define	DEFAULT_TEAM_MODEL		"sarge"
		#define	DEFAULT_TEAM_HEAD		"sarge"

		#define DEFAULT_REDTEAM_NAME		"Stroggs"
		#define DEFAULT_BLUETEAM_NAME		"Pagans"

		typedef enum {
			FOOTSTEP_NORMAL,
			FOOTSTEP_BOOT,
			FOOTSTEP_FLESH,
			FOOTSTEP_MECH,
			FOOTSTEP_ENERGY,
			FOOTSTEP_METAL,
			FOOTSTEP_SPLASH,

			FOOTSTEP_TOTAL
		} footstep_t;

		typedef enum {
			IMPACTSOUND_DEFAULT,
			IMPACTSOUND_METAL,
			IMPACTSOUND_FLESH
		} impactSound_t;

		//=================================================

		// player entities need to track more information
		// than any other type of entity.

		// note that not every player entity is a client entity,
		// because corpses after respawn are outside the normal
		// client numbering range

		// when changing animation, set animationTime to frameTime + lerping time
		// The current lerp will finish out, then it will lerp to the new animation
		typedef struct {
			int			oldFrame;
			int			oldFrameTime;		// time when ->oldFrame was exactly on

			int			frame;
			int			frameTime;			// time when ->frame will be exactly on

			float		backlerp;

			float		yawAngle;
			bool	yawing;
			float		pitchAngle;
			bool	pitching;

			int			animationNumber;	// may include ANIM_TOGGLEBIT
			animation_t	*animation;
			int			animationTime;		// time when the first frame of the animation will be exact
		} lerpFrame_t;


		typedef struct {
			lerpFrame_t		legs, torso, flag;
			int				painTime;
			int				painDirection;	// flip from 0 to 1
			int				lightningFiring;

			// railgun trail spawning
			vec3_t			railgunImpact;
			bool		railgunFlash;

			// machinegun spinning
			float			barrelAngle;
			int				barrelTime;
			bool		barrelSpinning;
		} playerEntity_t;

		//=================================================



		// centity_t have a direct corespondence with gentity_t in the game, but
		// only the entityState_t is directly communicated to the cgame
		typedef struct centity_s {
			entityState_t	currentState;	// from cg.frame
			entityState_t	nextState;		// from cg.nextFrame, if available
			bool		interpolate;	// true if next is valid to interpolate to
			bool		currentValid;	// true if cg.frame holds this entity

			int				muzzleFlashTime;	// move to playerEntity?
			int				previousEvent;
			int				teleportFlag;

			int				trailTime;		// so missile trails can handle dropped initial packets
			int				dustTrailTime;
			int				miscTime;

			int				snapShotTime;	// last time this entity was found in a snapshot

			playerEntity_t	pe;

			int				errorTime;		// decay the error from this time
			vec3_t			errorOrigin;
			vec3_t			errorAngles;
			
			bool		extrapolated;	// false if origin / angles is an interpolation
			vec3_t			rawOrigin;
			vec3_t			rawAngles;

			vec3_t			beamEnd;

			// exact interpolated position of entity on this frame
			vec3_t			lerpOrigin;
			vec3_t			lerpAngles;
		} centity_t;


		//======================================================================

		// local entities are created as a result of events or predicted actions,
		// and live independantly from all server transmitted entities

		typedef struct markPoly_s {
			struct markPoly_s	*prevMark, *nextMark;
			int			time;
			qhandle_t	markShader;
			bool	alphaFade;		// fade alpha instead of rgb
			float		color[4];
			poly_t		poly;
			polyVert_t	verts[MAX_VERTS_ON_POLY];
		} markPoly_t;


		typedef enum {
			LE_MARK,
			LE_EXPLOSION,
			LE_SPRITE_EXPLOSION,
			LE_FRAGMENT,
			LE_MOVE_SCALE_FADE,
			LE_FALL_SCALE_FADE,
			LE_FADE_RGB,
			LE_SCALE_FADE,
			LE_SCOREPLUM,
		} leType_t;

		typedef enum {
			LEF_PUFF_DONT_SCALE  = 0x0001,			// do not scale size over time
			LEF_TUMBLE			 = 0x0002,			// tumble over time, used for ejecting shells
			LEF_SOUND1			 = 0x0004,			// sound 1 for kamikaze
			LEF_SOUND2			 = 0x0008			// sound 2 for kamikaze
		} leFlag_t;

		typedef enum {
			LEMT_NONE,
			LEMT_BURN,
			LEMT_BLOOD
		} leMarkType_t;			// fragment local entities can leave marks on walls

		typedef enum {
			LEBS_NONE,
			LEBS_BLOOD,
			LEBS_BRASS
		} leBounceSoundType_t;	// fragment local entities can make sounds on impacts

		typedef struct localEntity_s {
			struct localEntity_s	*prev, *next;
			leType_t		leType;
			int				leFlags;

			int				startTime;
			int				endTime;
			int				fadeInTime;

			float			lifeRate;			// 1.0 / (endTime - startTime)

			trajectory_t	pos;
			trajectory_t	angles;

			float			bounceFactor;		// 0.0 = no bounce, 1.0 = perfect

			float			color[4];

			float			radius;

			float			light;
			vec3_t			lightColor;

			leMarkType_t		leMarkType;		// mark to leave on fragment impact
			leBounceSoundType_t	leBounceSoundType;

			refEntity_t		refEntity;		
		} localEntity_t;

		//======================================================================


		typedef struct {
			int				client;
			int				score;
			int				ping;
			int				time;
			int				scoreFlags;
			int				powerUps;
			int				accuracy;
			int				impressiveCount;
			int				excellentCount;
			int				guantletCount;
			int				defendCount;
			int				assistCount;
			int				captures;
			bool	perfect;
			int				team;
		} score_t;

		// each client has an associated clientInfo_t
		// that contains media references necessary to present the
		// client model and other color coded effects
		// this is regenerated each time a client's configstring changes,
		// usually as a result of a userinfo (name, model, etc) change
		#define	MAX_CUSTOM_SOUNDS	32

		typedef struct {
			bool		infoValid;

			char			name[MAX_QPATH];
			team_t			team;

			int				botSkill;		// 0 = not bot, 1-5 = bot

			vec3_t			color1;
			vec3_t			color2;

			int				score;			// updated by score servercmds
			int				location;		// location index for team mode
			int				health;			// you only get this info about your teammates
			int				armor;
			int				curWeapon;

			int				handicap;
			int				wins, losses;	// in tourney mode

			int				teamTask;		// task in teamplay (offence/defence)
			bool		teamLeader;		// true when this is a team leader

			int				powerups;		// so can display quad/flag status

			int				medkitUsageTime;
			int				invulnerabilityStartTime;
			int				invulnerabilityStopTime;

			int				breathPuffTime;

			// when clientinfo is changed, the loading of models/skins/sounds
			// can be deferred until you are dead, to prevent hitches in
			// gameplay
			char			modelName[MAX_QPATH];
			char			skinName[MAX_QPATH];
			char			headModelName[MAX_QPATH];
			char			headSkinName[MAX_QPATH];
			char			redTeam[MAX_TEAMNAME];
			char			blueTeam[MAX_TEAMNAME];
			bool		deferred;

			bool		newAnims;		// true if using the new mission pack animations
			bool		fixedlegs;		// true if legs yaw is always the same as torso yaw
			bool		fixedtorso;		// true if torso never changes yaw

			vec3_t			headOffset;		// move head in icon views
			footstep_t		footsteps;
			gender_t		gender;			// from model

			qhandle_t		legsModel;
			qhandle_t		legsSkin;

			qhandle_t		torsoModel;
			qhandle_t		torsoSkin;

			qhandle_t		headModel;
			qhandle_t		headSkin;

			qhandle_t		modelIcon;

			animation_t		animations[MAX_TOTALANIMATIONS];

			sfxHandle_t		sounds[MAX_CUSTOM_SOUNDS];
		} clientInfo_t;


		// each WP_* weapon enum has an associated weaponInfo_t
		// that contains media references necessary to present the
		// weapon and its effects
		typedef struct weaponInfo_s {
			bool		registered;
			gitem_t			*item;

			qhandle_t		handsModel;			// the hands don't actually draw, they just position the weapon
			qhandle_t		weaponModel;
			qhandle_t		barrelModel;
			qhandle_t		flashModel;

			vec3_t			weaponMidpoint;		// so it will rotate centered instead of by tag

			float			flashDlight;
			vec3_t			flashDlightColor;
			sfxHandle_t		flashSound[4];		// fast firing weapons randomly choose

			qhandle_t		weaponIcon;
			qhandle_t		ammoIcon;

			qhandle_t		ammoModel;

			qhandle_t		missileModel;
			sfxHandle_t		missileSound;
			void			(*missileTrailFunc)( centity_t *, const struct weaponInfo_s *wi );
			float			missileDlight;
			vec3_t			missileDlightColor;
			int				missileRenderfx;

			void			(*ejectBrassFunc)( centity_t * );

			float			trailRadius;
			float			wiTrailTime;

			sfxHandle_t		readySound;
			sfxHandle_t		firingSound;
			bool		loopFireSound;
		} weaponInfo_t;


		// each IT_* item has an associated itemInfo_t
		// that constains media references necessary to present the
		// item and its effects
		typedef struct {
			bool		registered;
			qhandle_t		models[MAX_ITEM_MODELS];
			qhandle_t		icon;
		} itemInfo_t;


		typedef struct {
			int				itemNum;
		} powerupInfo_t;


		#define MAX_SKULLTRAIL		10

		typedef struct {
			vec3_t positions[MAX_SKULLTRAIL];
			int numpositions;
		} skulltrail_t;


		#define MAX_REWARDSTACK		10
		#define MAX_SOUNDBUFFER		20

		//======================================================================

		// all cg.stepTime, cg.duckTime, cg.landTime, etc are set to cg.time when the action
		// occurs, and they will have visible effects for #define STEP_TIME or whatever msec after

		#define MAX_PREDICTED_EVENTS	16
		 
		typedef struct {
			int			clientFrame;		// incremented each frame

			int			clientNum;
			
			bool	demoPlayback;
			bool	levelShot;			// taking a level menu screenshot
			int			deferredPlayerLoading;
			bool	loading;			// don't defer players at initial startup
			bool	intermissionStarted;	// don't play voice rewards, because game will end shortly

			// there are only one or two snapshot_t that are relevent at a time
			int			latestSnapshotNum;	// the number of snapshots the client system has received
			int			latestSnapshotTime;	// the time from latestSnapshotNum, so we don't need to read the snapshot yet

			snapshot_t	*snap;				// cg.snap->serverTime <= cg.time
			snapshot_t	*nextSnap;			// cg.nextSnap->serverTime > cg.time, or NULL
			snapshot_t	activeSnapshots[2];

			float		frameInterpolation;	// (float)( cg.time - cg.frame->serverTime ) / (cg.nextFrame->serverTime - cg.frame->serverTime)

			bool	thisFrameTeleport;
			bool	nextFrameTeleport;

			int			frametime;		// cg.time - cg.oldTime

			int			time;			// this is the time value that the client
										// is rendering at.
			int			oldTime;		// time at last frame, used for missile trails and prediction checking

			int			physicsTime;	// either cg.snap->time or cg.nextSnap->time

			int			timelimitWarnings;	// 5 min, 1 min, overtime
			int			fraglimitWarnings;

			bool	mapRestart;			// set on a map restart to set back the weapon

			bool	renderingThirdPerson;		// during deaths, chasecams, etc

			// prediction state
			bool	hyperspace;				// true if prediction has hit a trigger_teleport
			playerState_t	predictedPlayerState;
			centity_t		predictedPlayerEntity;
			bool	validPPS;				// clear until the first call to CG_PredictPlayerState
			int			predictedErrorTime;
			vec3_t		predictedError;

			int			eventSequence;
			int			predictableEvents[MAX_PREDICTED_EVENTS];

			float		stepChange;				// for stair up smoothing
			int			stepTime;

			float		duckChange;				// for duck viewheight smoothing
			int			duckTime;

			float		landChange;				// for landing hard
			int			landTime;

			// input state sent to server
			int			weaponSelect;

			// auto rotating items
			vec3_t		autoAngles;
			vec3_t		autoAxis[3];
			vec3_t		autoAnglesFast;
			vec3_t		autoAxisFast[3];

			// view rendering
			refdef_t	refdef;
			vec3_t		refdefViewAngles;		// will be converted to refdef.viewaxis

			// zoom key
			bool	zoomed;
			int			zoomTime;
			float		zoomSensitivity;

			// information screen text during loading
			char		infoScreenText[MAX_STRING_CHARS];

			// scoreboard
			int			scoresRequestTime;
			int			numScores;
			int			selectedScore;
			int			teamScores[2];
			score_t		scores[MAX_CLIENTS];
			bool	showScores;
			bool	scoreBoardShowing;
			int			scoreFadeTime;
			char		killerName[MAX_NAME_LENGTH];
			char			spectatorList[MAX_STRING_CHARS];		// list of names
			int				spectatorLen;												// length of list
			float			spectatorWidth;											// width in device units
			int				spectatorTime;											// next time to offset
			int				spectatorPaintX;										// current paint x
			int				spectatorPaintX2;										// current paint x
			int				spectatorOffset;										// current offset from start
			int				spectatorPaintLen; 									// current offset from start

			// skull trails
			skulltrail_t	skulltrails[MAX_CLIENTS];

			// centerprinting
			int			centerPrintTime;
			int			centerPrintCharWidth;
			int			centerPrintY;
			char		centerPrint[1024];
			int			centerPrintLines;

			// low ammo warning state
			int			lowAmmoWarning;		// 1 = low, 2 = empty

			// kill timers for carnage reward
			int			lastKillTime;

			// crosshair client ID
			int			crosshairClientNum;
			int			crosshairClientTime;

			// powerup active flashing
			int			powerupActive;
			int			powerupTime;

			// attacking player
			int			attackerTime;
			int			voiceTime;

			// reward medals
			int			rewardStack;
			int			rewardTime;
			int			rewardCount[MAX_REWARDSTACK];
			qhandle_t	rewardShader[MAX_REWARDSTACK];
			qhandle_t	rewardSound[MAX_REWARDSTACK];

			// sound buffer mainly for announcer sounds
			int			soundBufferIn;
			int			soundBufferOut;
			int			soundTime;
			qhandle_t	soundBuffer[MAX_SOUNDBUFFER];

			// for voice chat buffer
			int			voiceChatTime;
			int			voiceChatBufferIn;
			int			voiceChatBufferOut;

			// warmup countdown
			int			warmup;
			int			warmupCount;

			//==========================

			int			itemPickup;
			int			itemPickupTime;
			int			itemPickupBlendTime;	// the pulse around the crosshair is timed seperately

			int			weaponSelectTime;
			int			weaponAnimation;
			int			weaponAnimationTime;

			// blend blobs
			float		damageTime;
			float		damageX, damageY, damageValue;

			// status bar head
			float		headYaw;
			float		headEndPitch;
			float		headEndYaw;
			int			headEndTime;
			float		headStartPitch;
			float		headStartYaw;
			int			headStartTime;

			// view movement
			float		v_dmg_time;
			float		v_dmg_pitch;
			float		v_dmg_roll;

			vec3_t		kick_angles;	// weapon kicks
			vec3_t		kick_origin;

			// temp working variables for player view
			float		bobfracsin;
			int			bobcycle;
			float		xyspeed;
			int     nextOrbitTime;

			//bool cameraMode;		// if rendering from a loaded camera


			// development tool
			refEntity_t		testModelEntity;
			char			testModelName[MAX_QPATH];
			bool		testGun;

		} cg_t;


		// all of the model, shader, and sound references that are
		// loaded at gamestate time are stored in cgMedia_t
		// Other media that can be tied to clients, weapons, or items are
		// stored in the clientInfo_t, itemInfo_t, weaponInfo_t, and powerupInfo_t
		typedef struct {
			qhandle_t	charsetShader;
			qhandle_t	charsetProp;
			qhandle_t	charsetPropGlow;
			qhandle_t	charsetPropB;
			qhandle_t	whiteShader;

			qhandle_t	redCubeModel;
			qhandle_t	blueCubeModel;
			qhandle_t	redCubeIcon;
			qhandle_t	blueCubeIcon;
			qhandle_t	redFlagModel;
			qhandle_t	blueFlagModel;
			qhandle_t	neutralFlagModel;
			qhandle_t	redFlagShader[3];
			qhandle_t	blueFlagShader[3];
			qhandle_t	flagShader[4];

			qhandle_t	flagPoleModel;
			qhandle_t	flagFlapModel;

			qhandle_t	redFlagFlapSkin;
			qhandle_t	blueFlagFlapSkin;
			qhandle_t	neutralFlagFlapSkin;

			qhandle_t	redFlagBaseModel;
			qhandle_t	blueFlagBaseModel;
			qhandle_t	neutralFlagBaseModel;

			qhandle_t	armorModel;
			qhandle_t	armorIcon;

			qhandle_t	teamStatusBar;

			qhandle_t	deferShader;

			// gib explosions
			qhandle_t	gibAbdomen;
			qhandle_t	gibArm;
			qhandle_t	gibChest;
			qhandle_t	gibFist;
			qhandle_t	gibFoot;
			qhandle_t	gibForearm;
			qhandle_t	gibIntestine;
			qhandle_t	gibLeg;
			qhandle_t	gibSkull;
			qhandle_t	gibBrain;

			qhandle_t	smoke2;

			qhandle_t	machinegunBrassModel;
			qhandle_t	shotgunBrassModel;

			qhandle_t	railRingsShader;
			qhandle_t	railCoreShader;

			qhandle_t	lightningShader;

			qhandle_t	friendShader;

			qhandle_t	balloonShader;
			qhandle_t	connectionShader;

			qhandle_t	selectShader;
			qhandle_t	viewBloodShader;
			qhandle_t	tracerShader;
			qhandle_t	crosshairShader[NUM_CROSSHAIRS];
			qhandle_t	lagometerShader;
			qhandle_t	backTileShader;
			qhandle_t	noammoShader;

			qhandle_t	smokePuffShader;
			qhandle_t	smokePuffRageProShader;
			qhandle_t	shotgunSmokePuffShader;
			qhandle_t	plasmaBallShader;
			qhandle_t	waterBubbleShader;
			qhandle_t	bloodTrailShader;

			qhandle_t	numberShaders[11];

			qhandle_t	shadowMarkShader;

			qhandle_t	botSkillShaders[5];

			// wall mark shaders
			qhandle_t	wakeMarkShader;
			qhandle_t	bloodMarkShader;
			qhandle_t	bulletMarkShader;
			qhandle_t	burnMarkShader;
			qhandle_t	holeMarkShader;
			qhandle_t	energyMarkShader;

			// powerup shaders
			qhandle_t	quadShader;
			qhandle_t	redQuadShader;
			qhandle_t	quadWeaponShader;
			qhandle_t	invisShader;
			qhandle_t	regenShader;
			qhandle_t	battleSuitShader;
			qhandle_t	battleWeaponShader;
			qhandle_t	hastePuffShader;
			qhandle_t	redKamikazeShader;
			qhandle_t	blueKamikazeShader;

			// weapon effect models
			qhandle_t	bulletFlashModel;
			qhandle_t	ringFlashModel;
			qhandle_t	dishFlashModel;
			qhandle_t	lightningExplosionModel;

			// weapon effect shaders
			qhandle_t	railExplosionShader;
			qhandle_t	plasmaExplosionShader;
			qhandle_t	bulletExplosionShader;
			qhandle_t	rocketExplosionShader;
			qhandle_t	grenadeExplosionShader;
			qhandle_t	bfgExplosionShader;
			qhandle_t	bloodExplosionShader;

			// special effects models
			qhandle_t	teleportEffectModel;
			qhandle_t	teleportEffectShader;

			qhandle_t	invulnerabilityPowerupModel;

			// scoreboard headers
			qhandle_t	scoreboardName;
			qhandle_t	scoreboardPing;
			qhandle_t	scoreboardScore;
			qhandle_t	scoreboardTime;

			// medals shown during gameplay
			qhandle_t	medalImpressive;
			qhandle_t	medalExcellent;
			qhandle_t	medalGauntlet;
			qhandle_t	medalDefend;
			qhandle_t	medalAssist;
			qhandle_t	medalCapture;

			// sounds
			sfxHandle_t	quadSound;
			sfxHandle_t	tracerSound;
			sfxHandle_t	selectSound;
			sfxHandle_t	useNothingSound;
			sfxHandle_t	wearOffSound;
			sfxHandle_t	footsteps[FOOTSTEP_TOTAL][4];
			sfxHandle_t	sfx_lghit1;
			sfxHandle_t	sfx_lghit2;
			sfxHandle_t	sfx_lghit3;
			sfxHandle_t	sfx_ric1;
			sfxHandle_t	sfx_ric2;
			sfxHandle_t	sfx_ric3;
			sfxHandle_t	sfx_railg;
			sfxHandle_t	sfx_rockexp;
			sfxHandle_t	sfx_plasmaexp;

			sfxHandle_t	gibSound;
			sfxHandle_t	gibBounce1Sound;
			sfxHandle_t	gibBounce2Sound;
			sfxHandle_t	gibBounce3Sound;
			sfxHandle_t	teleInSound;
			sfxHandle_t	teleOutSound;
			sfxHandle_t	noAmmoSound;
			sfxHandle_t	respawnSound;
			sfxHandle_t talkSound;
			sfxHandle_t landSound;
			sfxHandle_t fallSound;
			sfxHandle_t jumpPadSound;

			sfxHandle_t oneMinuteSound;
			sfxHandle_t fiveMinuteSound;
			sfxHandle_t suddenDeathSound;

			sfxHandle_t threeFragSound;
			sfxHandle_t twoFragSound;
			sfxHandle_t oneFragSound;

			sfxHandle_t hitSound;
			sfxHandle_t hitSoundHighArmor;
			sfxHandle_t hitSoundLowArmor;
			sfxHandle_t hitTeamSound;
			sfxHandle_t impressiveSound;
			sfxHandle_t excellentSound;
			sfxHandle_t deniedSound;
			sfxHandle_t humiliationSound;
			sfxHandle_t assistSound;
			sfxHandle_t defendSound;
			sfxHandle_t firstImpressiveSound;
			sfxHandle_t firstExcellentSound;
			sfxHandle_t firstHumiliationSound;

			sfxHandle_t takenLeadSound;
			sfxHandle_t tiedLeadSound;
			sfxHandle_t lostLeadSound;

			sfxHandle_t voteNow;
			sfxHandle_t votePassed;
			sfxHandle_t voteFailed;

			sfxHandle_t watrInSound;
			sfxHandle_t watrOutSound;
			sfxHandle_t watrUnSound;

			sfxHandle_t flightSound;
			sfxHandle_t medkitSound;

			sfxHandle_t weaponHoverSound;

			// teamplay sounds
			sfxHandle_t captureAwardSound;
			sfxHandle_t redScoredSound;
			sfxHandle_t blueScoredSound;
			sfxHandle_t redLeadsSound;
			sfxHandle_t blueLeadsSound;
			sfxHandle_t teamsTiedSound;

			sfxHandle_t	captureYourTeamSound;
			sfxHandle_t	captureOpponentSound;
			sfxHandle_t	returnYourTeamSound;
			sfxHandle_t	returnOpponentSound;
			sfxHandle_t	takenYourTeamSound;
			sfxHandle_t	takenOpponentSound;

			sfxHandle_t redFlagReturnedSound;
			sfxHandle_t blueFlagReturnedSound;
			sfxHandle_t neutralFlagReturnedSound;
			sfxHandle_t	enemyTookYourFlagSound;
			sfxHandle_t	enemyTookTheFlagSound;
			sfxHandle_t yourTeamTookEnemyFlagSound;
			sfxHandle_t yourTeamTookTheFlagSound;
			sfxHandle_t	youHaveFlagSound;
			sfxHandle_t yourBaseIsUnderAttackSound;
			sfxHandle_t holyShitSound;

			// tournament sounds
			sfxHandle_t	count3Sound;
			sfxHandle_t	count2Sound;
			sfxHandle_t	count1Sound;
			sfxHandle_t	countFightSound;
			sfxHandle_t	countPrepareSound;

			qhandle_t cursor;
			qhandle_t selectCursor;
			qhandle_t sizeCursor;

			sfxHandle_t	regenSound;
			sfxHandle_t	protectSound;
			sfxHandle_t	n_healthSound;
			sfxHandle_t	hgrenb1aSound;
			sfxHandle_t	hgrenb2aSound;
			sfxHandle_t	wstbimplSound;
			sfxHandle_t	wstbimpmSound;
			sfxHandle_t	wstbimpdSound;
			sfxHandle_t	wstbactvSound;

		} cgMedia_t;


		// The client game static (cgs) structure hold everything
		// loaded or calculated from the gamestate.  It will NOT
		// be cleared when a tournement restart is done, allowing
		// all clients to begin playing instantly
		typedef struct {
			gameState_t		gameState;			// gamestate from server
			glconfig_t		glconfig;			// rendering configuration
			float			screenXScale;		// derived from glconfig
			float			screenYScale;
			float			screenXBias;

			int				serverCommandSequence;	// reliable command stream counter
			int				processedSnapshotNum;// the number of snapshots cgame has requested

			bool		localServer;		// detected on startup by checking sv_running

			// parsed from serverinfo
			gametype_t		gametype;
			int				dmflags;
			int				teamflags;
			int				fraglimit;
			int				capturelimit;
			int				timelimit;
			int				maxclients;
			char			mapname[MAX_QPATH];
			char			redTeam[MAX_QPATH];
			char			blueTeam[MAX_QPATH];

			int				voteTime;
			int				voteYes;
			int				voteNo;
			bool		voteModified;			// beep whenever changed
			char			voteString[MAX_STRING_TOKENS];

			int				teamVoteTime[2];
			int				teamVoteYes[2];
			int				teamVoteNo[2];
			bool		teamVoteModified[2];	// beep whenever changed
			char			teamVoteString[2][MAX_STRING_TOKENS];

			int				levelStartTime;

			int				scores1, scores2;		// from configstrings
			int				redflag, blueflag;		// flag status from configstrings
			int				flagStatus;

			bool  newHud;

			//
			// locally derived information from gamestate
			//
			qhandle_t		gameModels[MAX_MODELS];
			sfxHandle_t		gameSounds[MAX_SOUNDS];

			int				numInlineModels;
			qhandle_t		inlineDrawModel[MAX_MODELS];
			vec3_t			inlineModelMidpoints[MAX_MODELS];

			clientInfo_t	clientinfo[MAX_CLIENTS];

			// teamchat width is *3 because of embedded color codes
			char			teamChatMsgs[TEAMCHAT_HEIGHT][TEAMCHAT_WIDTH*3+1];
			int				teamChatMsgTimes[TEAMCHAT_HEIGHT];
			int				teamChatPos;
			int				teamLastChatPos;

			int cursorX;
			int cursorY;
			bool eventHandling;
			bool mouseCaptured;
			bool sizingHud;
			void *capturedItem;
			qhandle_t activeCursor;

			// orders
			int currentOrder;
			bool orderPending;
			int orderTime;
			int currentVoiceClient;
			int acceptOrderTime;
			int acceptTask;
			int acceptLeader;
			char acceptVoice[MAX_NAME_LENGTH];

			// media
			cgMedia_t		media;

		} cgs_t;


		typedef enum {
		  SYSTEM_PRINT,
		  CHAT_PRINT,
		  TEAMCHAT_PRINT
		} q3print_t; // bk001201 - warning: useless keyword or type name in empty declaration
	}
}
