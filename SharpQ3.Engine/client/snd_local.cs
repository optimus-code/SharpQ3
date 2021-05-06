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
// snd_local.h -- private sound definations

namespace SharpQ3.Engine.client
{
	public static class snd_local
	{
		#define	PAINTBUFFER_SIZE		4096					// this is in samples

		#define SND_CHUNK_SIZE			1024					// samples
		#define SND_CHUNK_SIZE_FLOAT	(SND_CHUNK_SIZE/2)		// floats
		#define SND_CHUNK_SIZE_BYTE		(SND_CHUNK_SIZE*2)		// floats

		typedef struct {
			int			left;	// the final values will be clamped to +/- 0x00ffff00 and shifted down
			int			right;
		} portable_samplepair_t;

		typedef struct adpcm_state {
		    short	sample;		/* Previous output value */
		    char	index;		/* Index into stepsize table */
		} adpcm_state_t;

		typedef	struct sndBuffer_s {
			short					sndChunk[SND_CHUNK_SIZE];
			struct sndBuffer_s		*next;
		    int						size;
			adpcm_state_t			adpcm;
		} sndBuffer;

		typedef struct sfx_s {
			sndBuffer		*soundData;
			bool		defaultSound;			// couldn't be loaded, so use buzz
			bool		inMemory;				// not in Memory
			bool		soundCompressed;		// not in Memory
			int				soundCompressionMethod;	
			int 			soundLength;
			char 			soundName[MAX_QPATH];
			int				lastTimeUsed;
			struct sfx_s	*next;
		} sfx_t;

		typedef struct {
			int			channels;
			int			samples;				// mono samples in buffer
			int			submission_chunk;		// don't mix less than this #
			int			samplebits;
			int			speed;
			byte		*buffer;
		} dma_t;

		#define START_SAMPLE_IMMEDIATE	0x7fffffff

		typedef struct loopSound_s {
			vec3_t		origin;
			vec3_t		velocity;
			sfx_t		*sfx;
			int			mergeFrame;
			bool	active;
			bool	kill;
			bool	doppler;
			float		dopplerScale;
			float		oldDopplerScale;
			int			framenum;
		} loopSound_t;

		typedef struct
		{
			int			allocTime;
			int			startSample;	// START_SAMPLE_IMMEDIATE = set immediately on next mix
			int			entnum;			// to allow overriding a specific sound
			int			entchannel;		// to allow overriding a specific sound
			int			leftvol;		// 0-255 volume after spatialization
			int			rightvol;		// 0-255 volume after spatialization
			int			master_vol;		// 0-255 volume before spatialization
			float		dopplerScale;
			float		oldDopplerScale;
			vec3_t		origin;			// only use if fixed_origin is set
			bool	fixed_origin;	// use origin instead of fetching entnum's origin
			sfx_t		*thesfx;		// sfx structure
			bool	doppler;
		} channel_t;


		#define	WAV_FORMAT_PCM		1


		typedef struct {
			int			format;
			int			rate;
			int			width;
			int			channels;
			int			samples;
			int			dataofs;		// chunk starts this many bytes from file start
		} wavinfo_t;


		/*
		====================================================================

		  SYSTEM SPECIFIC FUNCTIONS

		====================================================================
		*/

		#define	MAX_CHANNELS			96

		#define	MAX_RAW_SAMPLES	16384

		// wavelet function

		#define SENTINEL_MULAW_ZERO_RUN 127
		#define SENTINEL_MULAW_FOUR_BIT_RUN 126

		#define	NXStream byte
	}
}
