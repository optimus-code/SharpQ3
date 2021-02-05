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

namespace SharpQ3.Ui
{
	public static class ui_local
	{
		#define UI_API_VERSION	4

		typedef void (*voidfunc_f)(void);


		//
		// ui_qmenu.c
		//

		#define RCOLUMN_OFFSET			( BIGCHAR_WIDTH )
		#define LCOLUMN_OFFSET			(-BIGCHAR_WIDTH )

		#define SLIDER_RANGE			10
		#define	MAX_EDIT_LINE			256

		#define MAX_MENUDEPTH			8
		#define MAX_MENUITEMS			64

		#define MTYPE_NULL				0
		#define MTYPE_SLIDER			1	
		#define MTYPE_ACTION			2
		#define MTYPE_SPINCONTROL		3
		#define MTYPE_FIELD				4
		#define MTYPE_RADIOBUTTON		5
		#define MTYPE_BITMAP			6	
		#define MTYPE_TEXT				7
		#define MTYPE_SCROLLLIST		8
		#define MTYPE_PTEXT				9
		#define MTYPE_BTEXT				10

		#define QMF_BLINK				0x00000001
		#define QMF_SMALLFONT			0x00000002
		#define QMF_LEFT_JUSTIFY		0x00000004
		#define QMF_CENTER_JUSTIFY		0x00000008
		#define QMF_RIGHT_JUSTIFY		0x00000010
		#define QMF_NUMBERSONLY			0x00000020	// edit field is only numbers
		#define QMF_HIGHLIGHT			0x00000040
		#define QMF_HIGHLIGHT_IF_FOCUS	0x00000080	// steady focus
		#define QMF_PULSEIFFOCUS		0x00000100	// pulse if focus
		#define QMF_HASMOUSEFOCUS		0x00000200
		#define QMF_NOONOFFTEXT			0x00000400
		#define QMF_MOUSEONLY			0x00000800	// only mouse input allowed
		#define QMF_HIDDEN				0x00001000	// skips drawing
		#define QMF_GRAYED				0x00002000	// grays and disables
		#define QMF_INACTIVE			0x00004000	// disables any input
		#define QMF_NODEFAULTINIT		0x00008000	// skip default initialization
		#define QMF_OWNERDRAW			0x00010000
		#define QMF_PULSE				0x00020000
		#define QMF_LOWERCASE			0x00040000	// edit field is all lower case
		#define QMF_UPPERCASE			0x00080000	// edit field is all upper case
		#define QMF_SILENT				0x00100000

		// callback notifications
		#define QM_GOTFOCUS				1
		#define QM_LOSTFOCUS			2
		#define QM_ACTIVATED			3

		typedef struct _tag_menuframework
		{
			int	cursor;
			int cursor_prev;

			int	nitems;
			void *items[MAX_MENUITEMS];

			void (*draw) (void);
			sfxHandle_t (*key) (int key);

			bool	wrapAround;
			bool	fullscreen;
			bool	showlogo;
		} menuframework_s;

		typedef struct
		{
			int type;
			const char *name;
			int	id;
			int x, y;
			int left;
			int	top;
			int	right;
			int	bottom;
			menuframework_s *parent;
			int menuPosition;
			unsigned flags;

			void (*callback)( void *self, int event );
			void (*statusbar)( void *self );
			void (*ownerdraw)( void *self );
		} menucommon_s;

		typedef struct {
			int		cursor;
			int		scroll;
			int		widthInChars;
			char	buffer[MAX_EDIT_LINE];
			int		maxchars;
		} mfield_t;

		typedef struct
		{
			menucommon_s	generic;
			mfield_t		field;
		} menufield_s;

		typedef struct 
		{
			menucommon_s generic;

			float minvalue;
			float maxvalue;
			float curvalue;

			float range;
		} menuslider_s;

		typedef struct
		{
			menucommon_s generic;

			int	oldvalue;
			int curvalue;
			int	numitems;
			int	top;
				
			const char **itemnames;

			int width;
			int height;
			int	columns;
			int	seperation;
		} menulist_s;

		typedef struct
		{
			menucommon_s generic;
		} menuaction_s;

		typedef struct
		{
			menucommon_s generic;
			int curvalue;
		} menuradiobutton_s;

		typedef struct
		{
			menucommon_s	generic;
			char*			focuspic;	
			char*			errorpic;
			qhandle_t		shader;
			qhandle_t		focusshader;
			int				width;
			int				height;
			float*			focuscolor;
		} menubitmap_s;

		typedef struct
		{
			menucommon_s	generic;
			char*			string;
			int				style;
			float*			color;
		} menutext_s;

		//
		// ui_servers2.c
		//
		#define MAX_FAVORITESERVERS 16

		//
		// ui_players.c
		//

		//FIXME ripped from cg_local.h
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
			// model info
			qhandle_t		legsModel;
			qhandle_t		legsSkin;
			lerpFrame_t		legs;

			qhandle_t		torsoModel;
			qhandle_t		torsoSkin;
			lerpFrame_t		torso;

			qhandle_t		headModel;
			qhandle_t		headSkin;

			animation_t		animations[MAX_ANIMATIONS];

			qhandle_t		weaponModel;
			qhandle_t		barrelModel;
			qhandle_t		flashModel;
			vec3_t			flashDlightColor;
			int				muzzleFlashTime;

			// currently in use drawing parms
			vec3_t			viewAngles;
			vec3_t			moveAngles;
			weapon_t		currentWeapon;
			int				legsAnim;
			int				torsoAnim;

			// animation vars
			weapon_t		weapon;
			weapon_t		lastWeapon;
			weapon_t		pendingWeapon;
			int				weaponTimer;
			int				pendingLegsAnim;
			int				torsoAnimationTimer;

			int				pendingTorsoAnim;
			int				legsAnimationTimer;

			bool		chat;
			bool		newModel;

			bool		barrelSpinning;
			float			barrelAngle;
			int				barrelTime;

			int				realWeapon;
		} playerInfo_t;

		//
		// ui_atoms.c
		//
		typedef struct {
			int					frametime;
			int					realtime;
			int					cursorx;
			int					cursory;
			int					menusp;
			menuframework_s*	activemenu;
			menuframework_s*	stack[MAX_MENUDEPTH];
			glconfig_t			glconfig;
			bool			debug;
			qhandle_t			whiteShader;
			qhandle_t			menuBackShader;
			qhandle_t			menuBackNoLogoShader;
			qhandle_t			charset;
			qhandle_t			charsetProp;
			qhandle_t			charsetPropGlow;
			qhandle_t			charsetPropB;
			qhandle_t			cursor;
			qhandle_t			rb_on;
			qhandle_t			rb_off;
			float				scale;
			float				bias;
			bool			firstdraw;
		} uiStatic_t;

		//
		// ui_gameinfo.c
		//
		typedef enum {
			AWARD_ACCURACY,
			AWARD_IMPRESSIVE,
			AWARD_EXCELLENT,
			AWARD_GAUNTLET,
			AWARD_FRAGS,
			AWARD_PERFECT
		} awardType_t;
	}
}
