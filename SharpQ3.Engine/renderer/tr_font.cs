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
namespace SharpQ3.Engine.renderer
{
	// tr_font.c
	public static class tr_font
	{
		// The font system uses FreeType 2.x to render TrueType fonts for use within the game.
		// As of this writing ( Nov, 2000 ) Team Arena uses these fonts for all of the ui and 
		// about 90% of the cgame presentation. A few areas of the CGAME were left uses the old 
		// fonts since the code is shared with standard Q3A.
		//
		// If you include this font rendering code in a commercial product you MUST include the
		// following somewhere with your product, see www.freetype.org for specifics or changes.
		// The Freetype code also uses some hinting techniques that MIGHT infringe on patents 
		// held by apple so be aware of that also.
		//
		// As of Q3A 1.25+ and Team Arena, we are shipping the game with the font rendering code
		// disabled. This removes any potential patent issues and it keeps us from having to 
		// distribute an actual TrueTrype font which is 1. expensive to do and 2. seems to require
		// an act of god to accomplish. 
		//
		// What we did was pre-render the fonts using FreeType ( which is why we leave the FreeType
		// credit in the credits ) and then saved off the glyph data and then hand touched up the 
		// font bitmaps so they scale a bit better in GL.
		//
		// There are limitations in the way fonts are saved and reloaded in that it is based on 
		// point size and not name. So if you pre-render Helvetica in 18 point and Impact in 18 point
		// you will end up with a single 18 point data file and image set. Typically you will want to 
		// choose 3 sizes to best approximate the scaling you will be doing in the ui scripting system
		// 
		// In the UI Scripting code, a scale of 1.0 is equal to a 48 point font. In Team Arena, we
		// use three or four scales, most of them exactly equaling the specific rendered size. We 
		// rendered three sizes in Team Arena, 12, 16, and 20. 
		//
		// To generate new font data you need to go through the following steps.
		// 1. delete the fontImage_x_xx.tga files and fontImage_xx.dat files from the fonts path.
		// 2. in a ui script, specificy a font, smallFont, and bigFont keyword with font name and 
		//    point size. the original TrueType fonts must exist in fonts at this point.
		// 3. run the game, you should see things normally.
		// 4. Exit the game and there will be three dat files and at least three tga files. The 
		//    tga's are in 256x256 pages so if it takes three images to render a 24 point font you 
		//    will end up with fontImage_0_24.tga through fontImage_2_24.tga
		// 5. You will need to flip the tga's in Photoshop as the tga output code writes them upside
		//    down.
		// 6. In future runs of the game, the system looks for these images and data files when a s
		//    specific point sized font is rendered and loads them for use. 
		// 7. Because of the original beta nature of the FreeType code you will probably want to hand
		//    touch the font bitmaps.
		// 
		// Currently a define in the project turns on or off the FreeType code which is currently 
		// defined out. To pre-render new fonts you need enable the define ( BUILD_FREETYPE ) and 
		// uncheck the exclude from build check box in the FreeType2 area of the Renderer project. 

		#define MAX_FONTS 6
		static int registeredFontCount = 0;
		static fontInfo_t registeredFont[MAX_FONTS];

		static int fdOffset;
		static byte	*fdFile;

		int readInt() {
			int i = fdFile[fdOffset]+(fdFile[fdOffset+1]<<8)+(fdFile[fdOffset+2]<<16)+(fdFile[fdOffset+3]<<24);
			fdOffset += 4;
			return i;
		}

		typedef union {
			byte	fred[4];
			float	ffred;
		} poor;

		float readFloat() {
			poor	me;

			me.fred[0] = fdFile[fdOffset+0];
			me.fred[1] = fdFile[fdOffset+1];
			me.fred[2] = fdFile[fdOffset+2];
			me.fred[3] = fdFile[fdOffset+3];

			fdOffset += 4;
			return me.ffred;
		}

		void RE_RegisterFont(const char *fontName, int pointSize, fontInfo_t *font) {
		  void *faceData;
			int i, len;
		  char name[1024];
			float dpi = 72;											//
			float glyphScale =  72.0f / dpi; 		// change the scale to be relative to 1 based on 72 dpi ( so dpi of 144 means a scale of .5 )

			if (pointSize <= 0) {
				pointSize = 12;
			}
			// we also need to adjust the scale based on point size relative to 48 points as the ui scaling is based on a 48 point font
			glyphScale *= 48.0f / pointSize;

			// make sure the render thread is stopped
			R_SyncRenderThread();

		  if (registeredFontCount >= MAX_FONTS) {
		    ri.Printf(PRINT_ALL, "RE_RegisterFont: Too many fonts registered already.\n");
		    return;
		  }

			Com_sprintf(name, sizeof(name), "fonts/fontImage_%i.dat",pointSize);
			for (i = 0; i < registeredFontCount; i++) {
				if (Q_stricmp(name, registeredFont[i].name) == 0) {
					Com_Memcpy(font, &registeredFont[i], sizeof(fontInfo_t));
					return;
				}
			}

			len = ri.FS_ReadFile(name, NULL);
			if (len == sizeof(fontInfo_t)) {
				ri.FS_ReadFile(name, &faceData);
				fdOffset = 0;
				fdFile = (byte*) faceData;
				for(i=0; i<GLYPHS_PER_FONT; i++) {
					font->glyphs[i].height		= readInt();
					font->glyphs[i].top			= readInt();
					font->glyphs[i].bottom		= readInt();
					font->glyphs[i].pitch		= readInt();
					font->glyphs[i].xSkip		= readInt();
					font->glyphs[i].imageWidth	= readInt();
					font->glyphs[i].imageHeight = readInt();
					font->glyphs[i].s			= readFloat();
					font->glyphs[i].t			= readFloat();
					font->glyphs[i].s2			= readFloat();
					font->glyphs[i].t2			= readFloat();
					font->glyphs[i].glyph		= readInt();
					Com_Memcpy(font->glyphs[i].shaderName, &fdFile[fdOffset], 32);
					fdOffset += 32;
				}
				font->glyphScale = readFloat();
				Com_Memcpy(font->name, &fdFile[fdOffset], MAX_QPATH);

		//		Com_Memcpy(font, faceData, sizeof(fontInfo_t));
				Q_strncpyz(font->name, name, sizeof(font->name));
				for (i = GLYPH_START; i < GLYPH_END; i++) {
					font->glyphs[i].glyph = RE_RegisterShaderNoMip(font->glyphs[i].shaderName);
				}
			  Com_Memcpy(&registeredFont[registeredFontCount++], font, sizeof(fontInfo_t));
				return;
			}

		    ri.Printf(PRINT_ALL, "RE_RegisterFont: FreeType code not available\n");
		}



		void R_InitFreeType() {
		  registeredFontCount = 0;
		}


		void R_DoneFreeType() {
			registeredFontCount = 0;
		}
	}
}
