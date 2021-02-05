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

namespace SharpQ3.Engine.server
{
	// server.h
	public static class server
	{
		#define	PERS_SCORE				0		// !!! MUST NOT CHANGE, SERVER AND
												// GAME BOTH REFERENCE !!!

		#define	MAX_ENT_CLUSTERS	16

		typedef struct svEntity_s {
			struct worldSector_s *worldSector;
			struct svEntity_s *nextEntityInWorldSector;
			
			entityState_t	baseline;		// for delta compression of initial sighting
			int			numClusters;		// if -1, use headnode instead
			int			clusternums[MAX_ENT_CLUSTERS];
			int			lastCluster;		// if all the clusters don't fit in clusternums
			int			areanum, areanum2;
			int			snapshotCounter;	// used to prevent double adding from portal views
		} svEntity_t;

		typedef enum {
			SS_DEAD,			// no map loaded
			SS_LOADING,			// spawning level entities
			SS_GAME				// actively running
		} serverState_t;

		typedef struct {
			serverState_t	state;
			bool		restarting;			// if true, send configstring changes during SS_LOADING
			int				serverId;			// changes each server start
			int				restartedServerId;	// serverId before a map_restart
			int				checksumFeed;		// the feed key that we use to compute the pure checksum strings
			// https://zerowing.idsoftware.com/bugzilla/show_bug.cgi?id=475
			// the serverId associated with the current checksumFeed (always <= serverId)
			int       checksumFeedServerId;	
			int				snapshotCounter;	// incremented for each snapshot built
			int				timeResidual;		// <= 1000 / sv_frame->value
			int				nextFrameTime;		// when time > nextFrameTime, process world
			struct cmodel_s	*models[MAX_MODELS];
			char			*configstrings[MAX_CONFIGSTRINGS];
			svEntity_t		svEntities[MAX_GENTITIES];

			char			*entityParsePoint;	// used during game VM init

			// the game virtual machine will update these on init and changes
			sharedEntity_t	*gentities;
			int				gentitySize;
			int				num_entities;		// current number, <= MAX_GENTITIES

			playerState_t	*gameClients;
			int				gameClientSize;		// will be > sizeof(playerState_t) due to game private data

			int				restartTime;
		} server_t;





		typedef struct {
			int				areabytes;
			byte			areabits[MAX_MAP_AREA_BYTES];		// portalarea visibility bits
			playerState_t	ps;
			int				num_entities;
			int				first_entity;		// into the circular sv_packet_entities[]
												// the entities MUST be in increasing state number
												// order, otherwise the delta compression will fail
			int				messageSent;		// time the message was transmitted
			int				messageAcked;		// time the message was acked
			int				messageSize;		// used to rate drop packets
		} clientSnapshot_t;

		typedef enum {
			CS_FREE,		// can be reused for a new connection
			CS_ZOMBIE,		// client has been disconnected, but don't reuse
							// connection for a couple seconds
			CS_CONNECTED,	// has been assigned to a client_t, but no gamestate yet
			CS_PRIMED,		// gamestate has been sent, but client hasn't sent a usercmd
			CS_ACTIVE		// client is fully in game
		} clientState_t;

		typedef struct netchan_buffer_s {
			msg_t           msg;
			byte            msgBuffer[MAX_MSGLEN];
			struct netchan_buffer_s *next;
		} netchan_buffer_t;

		typedef struct client_s {
			clientState_t	state;
			char			userinfo[MAX_INFO_STRING];		// name, etc

			char			reliableCommands[MAX_RELIABLE_COMMANDS][MAX_STRING_CHARS];
			int				reliableSequence;		// last added reliable message, not necesarily sent or acknowledged yet
			int				reliableAcknowledge;	// last acknowledged reliable message
			int				reliableSent;			// last sent reliable message, not necesarily acknowledged yet
			int				messageAcknowledge;

			int				gamestateMessageNum;	// netchan->outgoingSequence of gamestate
			int				challenge;

			usercmd_t		lastUsercmd;
			int				lastMessageNum;		// for delta compression
			int				lastClientCommand;	// reliable client message sequence
			char			lastClientCommandString[MAX_STRING_CHARS];
			sharedEntity_t	*gentity;			// SV_GentityNum(clientnum)
			char			name[MAX_NAME_LENGTH];			// extracted from userinfo, high bits masked

			// downloading
			char			downloadName[MAX_QPATH]; // if not empty string, we are downloading
			fileHandle_t	download;			// file being downloaded
 			int				downloadSize;		// total bytes (can't use EOF because of paks)
 			int				downloadCount;		// bytes sent
			int				downloadClientBlock;	// last block we sent to the client, awaiting ack
			int				downloadCurrentBlock;	// current block number
			int				downloadXmitBlock;	// last block we xmited
			unsigned char	*downloadBlocks[MAX_DOWNLOAD_WINDOW];	// the buffers for the download blocks
			int				downloadBlockSize[MAX_DOWNLOAD_WINDOW];
			bool		downloadEOF;		// We have sent the EOF block
			int				downloadSendTime;	// time we last got an ack from the client

			int				deltaMessage;		// frame last client usercmd message
			int				nextReliableTime;	// svs.time when another reliable command will be allowed
			int				lastPacketTime;		// svs.time when packet was last received
			int				lastConnectTime;	// svs.time when connection started
			int				nextSnapshotTime;	// send another snapshot when svs.time >= nextSnapshotTime
			bool		rateDelayed;		// true if nextSnapshotTime was set based on rate instead of snapshotMsec
			int				timeoutCount;		// must timeout a few frames in a row so debugging doesn't break
			clientSnapshot_t	frames[PACKET_BACKUP];	// updates can be delta'd from here
			int				ping;
			int				rate;				// bytes / second
			int				snapshotMsec;		// requests a snapshot every snapshotMsec unless rate choked
			int				pureAuthentic;
			bool  gotCP; // TTimo - additional flag to distinguish between a bad pure checksum, and no cp command at all
			netchan_t		netchan;
			// TTimo
			// queuing outgoing fragmented messages to send them properly, without udp packet bursts
			// in case large fragmented messages are stacking up
			// buffer them into this queue, and hand them out to netchan as needed
			netchan_buffer_t *netchan_start_queue;
			netchan_buffer_t **netchan_end_queue;
		} client_t;

		//=============================================================================


		// MAX_CHALLENGES is made large to prevent a denial
		// of service attack that could cycle all of them
		// out before legitimate users connected
		#define	MAX_CHALLENGES	1024

		#define	AUTHORIZE_TIMEOUT	5000

		typedef struct {
			netadr_t	adr;
			int			challenge;
			int			time;				// time the last packet was sent to the autherize server
			int			pingTime;			// time the challenge response was sent to client
			int			firstTime;			// time the adr was first used, for authorize timeout checks
			bool	connected;
		} challenge_t;


		#define	MAX_MASTERS	8				// max recipients for heartbeat packets


		// this structure will be cleared only when the game dll changes
		typedef struct {
			bool	initialized;				// sv_init has completed

			int			time;						// will be strictly increasing across level changes

			int			snapFlagServerBit;			// ^= SNAPFLAG_SERVERCOUNT every SV_SpawnServer()

			client_t	*clients;					// [sv_maxclients->integer];
			int			numSnapshotEntities;		// sv_maxclients->integer*PACKET_BACKUP*MAX_PACKET_ENTITIES
			int			nextSnapshotEntities;		// next snapshotEntities to use
			entityState_t	*snapshotEntities;		// [numSnapshotEntities]
			int			nextHeartbeatTime;
			challenge_t	challenges[MAX_CHALLENGES];	// to prevent invalid IPs from connecting
			netadr_t	redirectAddress;			// for rcon return messages

			netadr_t	authorizeAddress;			// for rcon return messages
		} serverStatic_t;

		#define	MAX_MASTER_SERVERS	5
	}
}
