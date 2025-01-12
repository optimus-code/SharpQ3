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

namespace SharpQ3.Engine.platform
{
	// win_local.h: Win32-specific Quake3 header file
	public static class win_local
	{
		#define	DIRECTSOUND_VERSION	0x0300

		void	IN_MouseEvent (int mstate);

		void Sys_QueEvent( int time, sysEventType_t type, int value, int value2, int ptrLength, void *ptr );

		void	Sys_CreateConsole( void );
		void	Sys_DestroyConsole( void );

		char	*Sys_ConsoleInput (void);

		bool	Sys_GetPacket ( netadr_t *net_from, msg_t *net_message );

		// Input subsystem

		void	IN_Init (void);
		void	IN_Shutdown (void);

		void	IN_Activate (bool active);
		void	IN_Frame (void);

		// window procedure
		LONG WINAPI MainWndProc (
		    HWND    hWnd,
		    UINT    uMsg,
		    WPARAM  wParam,
		    LPARAM  lParam);

		void Conbuf_AppendText( const char *msg );

		void SNDDMA_Activate( void );
		int  SNDDMA_InitDS ();

		typedef struct
		{
			
			HINSTANCE		reflib_library;		// Handle to refresh DLL 
			bool		reflib_active;

			HWND			hWnd; // main window, refers to one of the hWnd_XXX listed below

		    HWND            hWnd_opengl;
		    HWND            hWnd_vulkan;
			HWND			hWnd_dx;

			HINSTANCE		hInstance;
			bool		activeApp;
			bool		isMinimized;
			OSVERSIONINFO	osversion;

			// when we get a windows message, we store the time off so keyboard processing
			// can know the exact time of an event
			unsigned		sysMsgTime;
		} WinVars_t;

		extern WinVars_t	g_wv;
	}
}
