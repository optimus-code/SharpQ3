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
	/*****************************************************************************
	 * name:		be_ai_chat.h
	 *
	 * desc:		char AI
	 *
	 * $Archive: /source/code/botlib/be_ai_chat.h $
	 *
	 *****************************************************************************/
	public static class be_ai_chat
	{
		#define MAX_MESSAGE_SIZE		256
		#define MAX_CHATTYPE_NAME		32
		#define MAX_MATCHVARIABLES		8

		#define CHAT_GENDERLESS			0
		#define CHAT_GENDERFEMALE		1
		#define CHAT_GENDERMALE			2

		#define CHAT_ALL					0
		#define CHAT_TEAM					1
		#define CHAT_TELL					2

		//a console message
		typedef struct bot_consolemessage_s
		{
			int handle;
			float time;									//message time
			int type;									//message type
			char message[MAX_MESSAGE_SIZE];				//message
			struct bot_consolemessage_s *prev, *next;	//prev and next in list
		} bot_consolemessage_t;

		//match variable
		typedef struct bot_matchvariable_s
		{
			char offset;
			int length;
		} bot_matchvariable_t;
		//returned to AI when a match is found
		typedef struct bot_match_s
		{
			char string[MAX_MESSAGE_SIZE];
			int type;
			int subtype;
			bot_matchvariable_t variables[MAX_MATCHVARIABLES];
		} bot_match_t;
	}
}
