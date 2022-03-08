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

using System;

namespace SharpQ3.Engine
{
    // q_math.c -- stateless support routines that are included in each code module
    public static class q_math
    {
        public readonly static vec3_t vec3_origin = new vec3_t { x = 0, y = 0, z = 0 };
        public readonly static vec3_t[] axisDefault = new vec3_t[3] { new vec3_t { x = 1, y = 0, z = 0 }, new vec3_t { x = 0, y = 1, z = 0 }, new vec3_t { x = 0, y = 0, z = 1 } };

        public readonly static vec4_t colorBlack = new vec4_t { x = 0, y = 0, z = 0, w = 1 };
        public readonly static vec4_t colorRed = new vec4_t { x = 1, y = 0, z = 0, w = 1 };
        public readonly static vec4_t colorGreen = new vec4_t { x = 0, y = 1, z = 0, w = 1 };
        public readonly static vec4_t colorBlue = new vec4_t { x = 0, y = 0, z = 1, w = 1 };
        public readonly static vec4_t colorYellow = new vec4_t { x = 1, y = 1, z = 0, w = 1 };
        public readonly static vec4_t colorMagenta = new vec4_t { x = 1, y = 0, z = 1, w = 1 };
        public readonly static vec4_t colorCyan = new vec4_t { x = 0, y = 1, z = 1, w = 1 };
        public readonly static vec4_t colorWhite = new vec4_t { x = 1, y = 1, z = 1, w = 1 };
        public readonly static vec4_t colorLtGrey = new vec4_t { x = 0.75f, y = 0.75f, z = 0.75f, w = 1 };
        public readonly static vec4_t colorMdGrey = new vec4_t { x = 0.5f, y = 0.5f, z = 0.5f, w = 1 };
        public readonly static vec4_t colorDkGrey = new vec4_t { x = 0.25f, y = 0.25f, z = 0.25f, w = 1 };

        static vec4_t[] g_color_table = new vec4_t[8]
        {
            new vec4_t{x=0.0f, y=0.0f, z=0.0f, w=1.0f},
            new vec4_t{x=1.0f, y=0.0f, z=0.0f, w=1.0f},
            new vec4_t{x=0.0f, y=1.0f, z=0.0f, w=1.0f},
            new vec4_t{x=1.0f, y=1.0f, z=0.0f, w=1.0f},
            new vec4_t{x=0.0f, y=0.0f, z=1.0f, w=1.0f},
            new vec4_t{x=0.0f, y=1.0f, z=1.0f, w=1.0f},
            new vec4_t{x=1.0f, y=0.0f, z=1.0f, w=1.0f},
            new vec4_t{x=1.0f, y=1.0f, z=1.0f, w=1.0f},
        };


        static vec3_t[] bytedirs/*[NUMVERTEXNORMALS]*/ = new vec3_t[]
        {
            new vec3_t(-0.525731f, 0.000000f, 0.850651f), new vec3_t(-0.442863f, 0.238856f, 0.864188f),
            new vec3_t(-0.295242f, 0.000000f, 0.955423f), new vec3_t(-0.309017f, 0.500000f, 0.809017f),
            new vec3_t(-0.162460f, 0.262866f, 0.951056f), new vec3_t(0.000000f, 0.000000f, 1.000000f),
            new vec3_t(0.000000f, 0.850651f, 0.525731f), new vec3_t(-0.147621f, 0.716567f, 0.681718f),
            new vec3_t(0.147621f, 0.716567f, 0.681718f), new vec3_t(0.000000f, 0.525731f, 0.850651f),
            new vec3_t(0.309017f, 0.500000f, 0.809017f), new vec3_t(0.525731f, 0.000000f, 0.850651f),
            new vec3_t(0.295242f, 0.000000f, 0.955423f), new vec3_t(0.442863f, 0.238856f, 0.864188f),
            new vec3_t(0.162460f, 0.262866f, 0.951056f), new vec3_t(-0.681718f, 0.147621f, 0.716567f),
            new vec3_t(-0.809017f, 0.309017f, 0.500000f),new vec3_t(-0.587785f, 0.425325f, 0.688191f),
            new vec3_t(-0.850651f, 0.525731f, 0.000000f),new vec3_t(-0.864188f, 0.442863f, 0.238856f),
            new vec3_t(-0.716567f, 0.681718f, 0.147621f),new vec3_t(-0.688191f, 0.587785f, 0.425325f),
            new vec3_t(-0.500000f, 0.809017f, 0.309017f), new vec3_t(-0.238856f, 0.864188f, 0.442863f),
            new vec3_t(-0.425325f, 0.688191f, 0.587785f), new vec3_t(-0.716567f, 0.681718f, -0.147621f),
            new vec3_t(-0.500000f, 0.809017f, -0.309017f), new vec3_t(-0.525731f, 0.850651f, 0.000000f),
            new vec3_t(0.000000f, 0.850651f, -0.525731f), new vec3_t(-0.238856f, 0.864188f, -0.442863f),
            new vec3_t(0.000000f, 0.955423f, -0.295242f), new vec3_t(-0.262866f, 0.951056f, -0.162460f),
            new vec3_t(0.000000f, 1.000000f, 0.000000f), new vec3_t(0.000000f, 0.955423f, 0.295242f),
            new vec3_t(-0.262866f, 0.951056f, 0.162460f), new vec3_t(0.238856f, 0.864188f, 0.442863f),
            new vec3_t(0.262866f, 0.951056f, 0.162460f), new vec3_t(0.500000f, 0.809017f, 0.309017f),
            new vec3_t(0.238856f, 0.864188f, -0.442863f),new vec3_t(0.262866f, 0.951056f, -0.162460f),
            new vec3_t(0.500000f, 0.809017f, -0.309017f),new vec3_t(0.850651f, 0.525731f, 0.000000f),
            new vec3_t(0.716567f, 0.681718f, 0.147621f), new vec3_t(0.716567f, 0.681718f, -0.147621f),
            new vec3_t(0.525731f, 0.850651f, 0.000000f), new vec3_t(0.425325f, 0.688191f, 0.587785f),
            new vec3_t(0.864188f, 0.442863f, 0.238856f), new vec3_t(0.688191f, 0.587785f, 0.425325f),
            new vec3_t(0.809017f, 0.309017f, 0.500000f), new vec3_t(0.681718f, 0.147621f, 0.716567f),
            new vec3_t(0.587785f, 0.425325f, 0.688191f), new vec3_t(0.955423f, 0.295242f, 0.000000f),
            new vec3_t(1.000000f, 0.000000f, 0.000000f), new vec3_t(0.951056f, 0.162460f, 0.262866f),
            new vec3_t(0.850651f, -0.525731f, 0.000000f),new vec3_t(0.955423f, -0.295242f, 0.000000f),
            new vec3_t(0.864188f, -0.442863f, 0.238856f), new vec3_t(0.951056f, -0.162460f, 0.262866f),
            new vec3_t(0.809017f, -0.309017f, 0.500000f), new vec3_t(0.681718f, -0.147621f, 0.716567f),
            new vec3_t(0.850651f, 0.000000f, 0.525731f), new vec3_t(0.864188f, 0.442863f, -0.238856f),
            new vec3_t(0.809017f, 0.309017f, -0.500000f), new vec3_t(0.951056f, 0.162460f, -0.262866f),
            new vec3_t(0.525731f, 0.000000f, -0.850651f), new vec3_t(0.681718f, 0.147621f, -0.716567f),
            new vec3_t(0.681718f, -0.147621f, -0.716567f),new vec3_t(0.850651f, 0.000000f, -0.525731f),
            new vec3_t(0.809017f, -0.309017f, -0.500000f), new vec3_t(0.864188f, -0.442863f, -0.238856f),
            new vec3_t(0.951056f, -0.162460f, -0.262866f), new vec3_t(0.147621f, 0.716567f, -0.681718f),
            new vec3_t(0.309017f, 0.500000f, -0.809017f), new vec3_t(0.425325f, 0.688191f, -0.587785f),
            new vec3_t(0.442863f, 0.238856f, -0.864188f), new vec3_t(0.587785f, 0.425325f, -0.688191f),
            new vec3_t(0.688191f, 0.587785f, -0.425325f), new vec3_t(-0.147621f, 0.716567f, -0.681718f),
            new vec3_t(-0.309017f, 0.500000f, -0.809017f), new vec3_t(0.000000f, 0.525731f, -0.850651f),
            new vec3_t(-0.525731f, 0.000000f, -0.850651f), new vec3_t(-0.442863f, 0.238856f, -0.864188f),
            new vec3_t(-0.295242f, 0.000000f, -0.955423f), new vec3_t(-0.162460f, 0.262866f, -0.951056f),
            new vec3_t(0.000000f, 0.000000f, -1.000000f), new vec3_t(0.295242f, 0.000000f, -0.955423f),
            new vec3_t(0.162460f, 0.262866f, -0.951056f), new vec3_t(-0.442863f, -0.238856f, -0.864188f),
            new vec3_t(-0.309017f, -0.500000f, -0.809017f), new vec3_t(-0.162460f, -0.262866f, -0.951056f),
            new vec3_t(0.000000f, -0.850651f, -0.525731f), new vec3_t(-0.147621f, -0.716567f, -0.681718f),
            new vec3_t(0.147621f, -0.716567f, -0.681718f), new vec3_t(0.000000f, -0.525731f, -0.850651f),
            new vec3_t(0.309017f, -0.500000f, -0.809017f), new vec3_t(0.442863f, -0.238856f, -0.864188f),
            new vec3_t(0.162460f, -0.262866f, -0.951056f), new vec3_t(0.238856f, -0.864188f, -0.442863f),
            new vec3_t(0.500000f, -0.809017f, -0.309017f), new vec3_t(0.425325f, -0.688191f, -0.587785f),
            new vec3_t(0.716567f, -0.681718f, -0.147621f), new vec3_t(0.688191f, -0.587785f, -0.425325f),
            new vec3_t(0.587785f, -0.425325f, -0.688191f), new vec3_t(0.000000f, -0.955423f, -0.295242f),
            new vec3_t(0.000000f, -1.000000f, 0.000000f), new vec3_t(0.262866f, -0.951056f, -0.162460f),
            new vec3_t(0.000000f, -0.850651f, 0.525731f), new vec3_t(0.000000f, -0.955423f, 0.295242f),
            new vec3_t(0.238856f, -0.864188f, 0.442863f), new vec3_t(0.262866f, -0.951056f, 0.162460f),
            new vec3_t(0.500000f, -0.809017f, 0.309017f), new vec3_t(0.716567f, -0.681718f, 0.147621f),
            new vec3_t(0.525731f, -0.850651f, 0.000000f), new vec3_t(-0.238856f, -0.864188f, -0.442863f),
            new vec3_t(-0.500000f, -0.809017f, -0.309017f), new vec3_t(-0.262866f, -0.951056f, -0.162460f),
            new vec3_t(-0.850651f, -0.525731f, 0.000000f), new vec3_t(-0.716567f, -0.681718f, -0.147621f),
            new vec3_t(-0.716567f, -0.681718f, 0.147621f), new vec3_t(-0.525731f, -0.850651f, 0.000000f),
            new vec3_t(-0.500000f, -0.809017f, 0.309017f), new vec3_t(-0.238856f, -0.864188f, 0.442863f),
            new vec3_t(-0.262866f, -0.951056f, 0.162460f), new vec3_t(-0.864188f, -0.442863f, 0.238856f),
            new vec3_t(-0.809017f, -0.309017f, 0.500000f), new vec3_t(-0.688191f, -0.587785f, 0.425325f),
            new vec3_t(-0.681718f, -0.147621f, 0.716567f), new vec3_t(-0.442863f, -0.238856f, 0.864188f),
            new vec3_t(-0.587785f, -0.425325f, 0.688191f), new vec3_t(-0.309017f, -0.500000f, 0.809017f),
            new vec3_t(-0.147621f, -0.716567f, 0.681718f), new vec3_t(-0.425325f, -0.688191f, 0.587785f),
            new vec3_t(-0.162460f, -0.262866f, 0.951056f), new vec3_t(0.442863f, -0.238856f, 0.864188f),
            new vec3_t(0.162460f, -0.262866f, 0.951056f), new vec3_t(0.309017f, -0.500000f, 0.809017f),
            new vec3_t(0.147621f, -0.716567f, 0.681718f), new vec3_t(0.000000f, -0.525731f, 0.850651f),
            new vec3_t(0.425325f, -0.688191f, 0.587785f), new vec3_t(0.587785f, -0.425325f, 0.688191f),
            new vec3_t(0.688191f, -0.587785f, 0.425325f), new vec3_t(-0.955423f, 0.295242f, 0.000000f),
            new vec3_t(-0.951056f, 0.162460f, 0.262866f), new vec3_t(-1.000000f, 0.000000f, 0.000000f),
            new vec3_t(-0.850651f, 0.000000f, 0.525731f), new vec3_t(-0.955423f, -0.295242f, 0.000000f),
            new vec3_t(-0.951056f, -0.162460f, 0.262866f), new vec3_t(-0.864188f, 0.442863f, -0.238856f),
            new vec3_t(-0.951056f, 0.162460f, -0.262866f), new vec3_t(-0.809017f, 0.309017f, -0.500000f),
            new vec3_t(-0.864188f, -0.442863f, -0.238856f), new vec3_t(-0.951056f, -0.162460f, -0.262866f),
            new vec3_t(-0.809017f, -0.309017f, -0.500000f), new vec3_t(-0.681718f, 0.147621f, -0.716567f),
            new vec3_t(-0.681718f, -0.147621f, -0.716567f), new vec3_t(-0.850651f, 0.000000f, -0.525731f),
            new vec3_t(-0.688191f, 0.587785f, -0.425325f), new vec3_t(-0.587785f, 0.425325f, -0.688191f),
            new vec3_t(-0.425325f, 0.688191f, -0.587785f), new vec3_t(-0.425325f, -0.688191f, -0.587785f),
            new vec3_t(-0.587785f, -0.425325f, -0.688191f), new vec3_t(-0.688191f, -0.587785f, -0.425325f)
        };


        //==============================================================

        public static int Q_rand( int seed )
        {
            var random = new Random( seed );
            return random.Next( );
        }

        public static float Q_random( int seed )
        {
            var random = new Random( seed );
            return ( float ) random.NextDouble( );
        }

        public static float Q_crandom( int seed )
        {
            return 2.0f * ( Q_random( seed ) - 0.5f );
        }

        public static int VectorCompare( vec3_t v1, vec3_t v2 )
        {
            if ( v1.x != v2.x || v1.y != v2.y || v1.z != v2.z )
            {
                return 0;
            }
            return 1;
        }

        public static float VectorLength( vec3_t v )
        {
            return ( float ) Math.Sqrt( v.x * v.x + v.y * v.y + v.z * v.z );
        }

        public static float VectorLengthSquared( vec3_t v )
        {
            return ( v.x * v.x + v.y * v.y + v.z * v.z );
        }

        public static float Distance( vec3_t p1, vec3_t p2 )
        {
            q_shared.VectorSubtract( p2, p1, out vec3_t v );
            return q_shared.VectorLength( v );
        }

        public static float DistanceSquared( vec3_t p1, vec3_t p2 )
        {
            q_shared.VectorSubtract( p2, p1, out vec3_t v );
            return v.x * v.x + v.y * v.y + v.z * v.z;
        }

        // fast vector normalize routine that does not check to make sure
        // that length != 0, nor does it return length, uses rsqrt approximation
        public static void VectorNormalizeFast( ref vec3_t v )
        {
            float ilength;

            ilength = Q_rsqrt( q_shared.DotProduct( v, v ) );

            v.x *= ilength;
            v.y *= ilength;
            v.z *= ilength;
        }

        public static void VectorInverse( ref vec3_t v )
        {
            v.x = -v.x;
            v.y = -v.y;
            v.z = -v.z;
        }

        public static void CrossProduct( vec3_t v1, vec3_t v2, out vec3_t cross )
        {
            cross = new vec3_t( );
            cross.x = v1.y * v2.z - v1.z * v2.y;
            cross.y = v1.z * v2.x - v1.x * v2.z;
            cross.z = v1.x * v2.y - v1.y * v2.x;
        }

        //=======================================================

        public static sbyte ClampChar( int i )
        {
            if ( i < -128 )
            {
                return -128;
            }
            if ( i > 127 )
            {
                return 127;
            }
            return ( sbyte ) i;
        }

        public static short ClampShort( int i )
        {
            if ( i < -32768 )
            {
                return -32768;
            }
            if ( i > 0x7fff )
            {
                return 0x7fff;
            }
            return ( short ) i;
        }


        // this isn't a real cheap function to call!
        public static int DirToByte( vec3_t dir )
        {
            int i, best;
            float d, bestd;

            if ( dir == vec3_t.zero )
            {
                return 0;
            }

            bestd = 0;
            best = 0;
            for ( i = 0; i < q_shared.NUMVERTEXNORMALS; i++ )
            {
                d = q_shared.DotProduct( dir, bytedirs[i] );
                if ( d > bestd )
                {
                    bestd = d;
                    best = i;
                }
            }

            return best;
        }

        public static void ByteToDir( int b, out vec3_t dir )
        {
            if ( b < 0 || b >= q_shared.NUMVERTEXNORMALS )
            {
                q_shared.VectorCopy( vec3_origin, out dir );
                return;
            }
            q_shared.VectorCopy( bytedirs[b], out dir );
        }


        public static uint ColorBytes3( float r, float g, float b )
        {
            return BitConverter.ToUInt32( new byte[] { ( byte ) ( r * 255 ), ( byte ) ( g * 255 ), ( byte ) ( b * 255 ) } );
        }

        public static uint ColorBytes4( float r, float g, float b, float a )
        {
            return BitConverter.ToUInt32( new byte[] { ( byte ) ( r * 255 ), ( byte ) ( g * 255 ), ( byte ) ( b * 255 ), ( byte ) ( a * 255 ) } );
        }

        public static float NormalizeColor( vec3_t @in, out vec3_t @out )
        {
            var max = @in.x;
            @out = new vec3_t( );
            if ( @in.y > max )
            {
                max = @in.y;
            }
            if ( @in.z > max )
            {
                max = @in.z;
            }

            if ( max != 0f )
            {
                @out.x = @in.x / max;
                @out.y = @in.y / max;
                @out.z = @in.z / max;
            }
            return max;
        }


        /*
		=====================
		PlaneFromPoints

		Returns false if the triangle is degenrate.
		The normal will point out of the clock for clockwise ordered points
		=====================
		*/
        public static bool PlaneFromPoints( out vec4_t plane, vec3_t a, vec3_t b, vec3_t c )
        {
            vec3_t d1, d2;
            plane = new vec4_t( );
            var planeXYZ = new vec3_t( );
            q_shared.VectorSubtract( b, a, out d1 );
            q_shared.VectorSubtract( c, a, out d2 );
            q_shared.CrossProduct( d2, d1, out planeXYZ );
            if ( VectorNormalize( ref planeXYZ ) == 0 )
            {
                return false;
            }

            plane.xyz = planeXYZ;
            plane.w = q_shared.DotProduct( a, plane.xyz );
            return true;
        }

        /*
		===============
		RotatePointAroundVector

		This is not implemented very well...
		===============
		*/
        public static void RotatePointAroundVector( vec3_t dst, vec3_t dir, vec3_t point,
                                     float degrees )
        {
            float[][] m = new float[3][];
            float[][] im = new float[3][];
            float[][] zrot = new float[3][];
            float[][] tmpmat = new float[3][];
            float[][] rot = new float[3][];
            int i;
            vec3_t vr, vup, vf;
            float rad;

            vf = new vec3_t( );
            vf[0] = dir[0];
            vf[1] = dir[1];
            vf[2] = dir[2];

            PerpendicularVector( out vr, dir );
            q_shared.CrossProduct( vr, vf, out vup );

            m[0] = new float[3];
            m[1] = new float[3];
            m[2] = new float[3];

            m[0][0] = vr[0];
            m[1][0] = vr[1];
            m[2][0] = vr[2];

            m[0][1] = vup[0];
            m[1][1] = vup[1];
            m[2][1] = vup[2];

            m[0][2] = vf[0];
            m[1][2] = vf[1];
            m[2][2] = vf[2];

            m.CopyTo( im, m.Length );
            im[0][1] = m[1][0];
            im[0][2] = m[2][0];
            im[1][0] = m[0][1];
            im[1][2] = m[2][1];
            im[2][0] = m[0][2];
            im[2][1] = m[1][2];

            zrot[0] = new float[3];
            zrot[1] = new float[3];
            zrot[2] = new float[3];

            zrot[0][0] = zrot[1][1] = zrot[2][2] = 1.0F;

            rad = q_shared.DEG2RAD( degrees );
            zrot[0][0] = MathF.Cos( rad );
            zrot[0][1] = MathF.Sin( rad );
            zrot[1][0] = -MathF.Sin( rad );
            zrot[1][1] = MathF.Cos( rad );

            MatrixMultiply( m, zrot, out tmpmat );
            MatrixMultiply( tmpmat, im, out rot );

            for ( i = 0; i < 3; i++ )
            {
                dst[i] = rot[i][0] * point[0] + rot[i][1] * point[1] + rot[i][2] * point[2];
            }
        }

        /*
		===============
		RotateAroundDirection
		===============
		*/
        public static void RotateAroundDirection( ref matrix3_t axis, float yaw )
        {

            // create an arbitrary axis[1] 
            PerpendicularVector( out axis.y, axis.x );

            // rotate it around axis[0] by yaw
            if ( yaw > 0 )
            {
                vec3_t temp = new vec3_t( );

                q_shared.VectorCopy( axis.y, out temp );
                RotatePointAroundVector( axis.y, axis.x, temp, yaw );
            }

            // cross to get axis[2]
            q_shared.CrossProduct( axis.x, axis.y, out axis.z );
        }

        public static void vectoangles( vec3_t value1, vec3_t angles )
        {
            float forward;
            float yaw, pitch;

            if ( value1[1] == 0 && value1[0] == 0 )
            {
                yaw = 0;
                if ( value1[2] > 0 )
                {
                    pitch = 90;
                }
                else
                {
                    pitch = 270;
                }
            }
            else
            {
                if ( value1[0] > 0 )
                {
                    yaw = ( MathF.Atan2( value1[1], value1[0] ) * 180 / q_shared.M_PI );
                }
                else if ( value1[1] > 0 )
                {
                    yaw = 90;
                }
                else
                {
                    yaw = 270;
                }
                if ( yaw < 0 )
                {
                    yaw += 360;
                }

                forward = MathF.Sqrt( value1[0] * value1[0] + value1[1] * value1[1] );
                pitch = ( MathF.Atan2( value1[2], forward ) * 180 / q_shared.M_PI );
                if ( pitch < 0 )
                {
                    pitch += 360;
                }
            }

            angles[q_shared.PITCH] = -pitch;
            angles[q_shared.YAW] = yaw;
            angles[q_shared.ROLL] = 0;
        }

        /*
		=================
		AnglesToAxis
		=================
		*/
        public static void AnglesToAxis( vec3_t angles, out matrix3_t axis )
        {
            vec3_t right = new vec3_t( );
            axis = new matrix3_t( );
            // angle vectors returns "right" instead of "y axis"
            AngleVectors( angles, out axis.x, out right, out axis.z );
            q_shared.VectorSubtract( vec3_origin, right, out axis.y );
        }

        public static void AxisClear( ref matrix3_t axis )
        {
            axis.x.x = 1;
            axis.x.y = 0;
            axis.x.z = 0;
            axis.y.x = 0;
            axis.y.y = 1;
            axis.y.z = 0;
            axis.z.x = 0;
            axis.z.y = 0;
            axis.z.z = 1;
        }

        public static void AxisCopy( matrix3_t @in, out matrix3_t @out )
        {
            @out = new matrix3_t( );
            q_shared.VectorCopy( @in.x, out @out.x );
            q_shared.VectorCopy( @in.y, out @out.y );
            q_shared.VectorCopy( @in.z, out @out.z );
        }

        public static void ProjectPointOnPlane( out vec3_t dst, vec3_t p, vec3_t normal )
        {
            float d;
            vec3_t n = new vec3_t( );
            float inv_denom;

            inv_denom = q_shared.DotProduct( normal, normal );
            inv_denom = 1.0f / inv_denom;

            d = q_shared.DotProduct( normal, p ) * inv_denom;

            n[0] = normal[0] * inv_denom;
            n[1] = normal[1] * inv_denom;
            n[2] = normal[2] * inv_denom;

            dst = new vec3_t( );
            dst[0] = p[0] - d * n[0];
            dst[1] = p[1] - d * n[1];
            dst[2] = p[2] - d * n[2];
        }

        /*
		================
		MakeNormalVectors

		Given a normalized forward vector, create two
		other perpendicular vectors
		================
		*/
        public static void MakeNormalVectors( vec3_t forward, out vec3_t right, out vec3_t up )
        {
            float d;

            // this rotate and negate guarantees a vector
            // not colinear with the original
            right = new vec3_t( );
            right.x = -forward.x;
            right.z = forward.y;
            right.x = forward.z;

            d = q_shared.DotProduct( right, forward );
            q_shared.VectorMA( right, -d, forward, out right );
            VectorNormalize( ref right );
            q_shared.CrossProduct( right, forward, out up );
        }


        public static void VectorRotate( vec3_t @in, matrix3_t matrix, vec3_t @out )
        {
            @out.x = q_shared.DotProduct( @in, matrix.x );
            @out.y = q_shared.DotProduct( @in, matrix.y );
            @out.z = q_shared.DotProduct( @in, matrix.z );
        }

        //============================================================================

        /*
		** float q_rsqrt( float number )
		*/
        public static float Q_rsqrt( float number )
        {
            unsafe
            {
                long i;
                float x2, y;
                const float threehalfs = 1.5F;

                x2 = number * 0.5F;
                y = number;
                i = *( long* ) &y;                        // evil floating point bit level hacking
                i = 0x5f3759df - ( i >> 1 );             // what the fuck? 
                y = *( float* ) &i;
                y = y * ( threehalfs - ( x2 * y * y ) );   // 1st iteration
                return y;
            }
        }

        public static float Q_fabs( float f )
        {
            unsafe
            {
                int tmp = *( int* ) &f;
                tmp &= 0x7FFFFFFF;
                return *( float* ) &tmp;
            }
        }

        //============================================================

        /*
		===============
		LerpAngle

		===============
		*/
        public static float LerpAngle( float from, float to, float frac )
        {
            float a;

            if ( to - from > 180 )
            {
                to -= 360;
            }
            if ( to - from < -180 )
            {
                to += 360;
            }
            a = from + frac * ( to - from );

            return a;
        }


        /*
		=================
		AngleSubtract

		Always returns a value from -180 to 180
		=================
		*/
        public static float AngleSubtract( float a1, float a2 )
        {
            float a;

            a = a1 - a2;
            while ( a > 180 )
            {
                a -= 360;
            }
            while ( a < -180 )
            {
                a += 360;
            }
            return a;
        }


        public static void AnglesSubtract( vec3_t v1, vec3_t v2, out vec3_t v3 )
        {
            v3 = new vec3_t( );
            v3.x = AngleSubtract( v1.x, v2.x );
            v3.y = AngleSubtract( v1.y, v2.y );
            v3.z = AngleSubtract( v1.z, v2.z );
        }


        public static float AngleMod( float a )
        {
            a = ( float ) ( ( 360.0 / 65536 ) * ( ( int ) ( a * ( 65536 / 360.0 ) ) & 65535 ) );
            return a;
        }


        /*
		=================
		AngleNormalize360

		returns angle normalized to the range [0 <= angle < 360]
		=================
		*/
        public static float AngleNormalize360( float angle )
        {
            return ( float ) ( ( 360.0 / 65536 ) * ( ( int ) ( angle * ( 65536 / 360.0 ) ) & 65535 ) );
        }


        /*
		=================
		AngleNormalize180

		returns angle normalized to the range [-180 < angle <= 180]
		=================
		*/
        public static float AngleNormalize180( float angle )
        {
            angle = AngleNormalize360( angle );
            if ( angle > 180.0 )
            {
                angle -= 360.0f;
            }
            return angle;
        }


        /*
		=================
		AngleDelta

		returns the normalized delta from angle1 to angle2
		=================
		*/
        public static float AngleDelta( float angle1, float angle2 )
        {
            return AngleNormalize180( angle1 - angle2 );
        }


        //============================================================


        /*
		=================
		SetPlaneSignbits
		=================
		*/
        public static void SetPlaneSignbits( ref cplane_t @out )
        {
            // for fast box on planeside test
            var bits = 0;
            for ( var j = 0; j < 3; j++ )
            {
                if ( @out.normal[j] < 0 )
                {
                    bits |= 1 << j;
                }
            }
            @out.signbits = ( byte ) bits;
        }

        /*
		==================
		BoxOnPlaneSide

		Returns 1, 2, or 1 + 2

		// this is the slow, general version
		int BoxOnPlaneSide2 (vec3_t emins, vec3_t emaxs, struct cplane_s *p)
		{
			int		i;
			float	dist1, dist2;
			int		sides;
			vec3_t	corners[2];

			for (i=0 ; i<3 ; i++)
			{
				if (p.normal[i] < 0)
				{
					corners[0][i] = emins[i];
					corners[1][i] = emaxs[i];
				}
				else
				{
					corners[1][i] = emins[i];
					corners[0][i] = emaxs[i];
				}
			}
			dist1 = DotProduct (p.normal, corners[0]) - p.dist;
			dist2 = DotProduct (p.normal, corners[1]) - p.dist;
			sides = 0;
			if (dist1 >= 0)
				sides = 1;
			if (dist2 < 0)
				sides |= 2;

			return sides;
		}

		==================
		*/

        public static int BoxOnPlaneSide( vec3_t emins, vec3_t emaxs, cplane_t p )
        {
            float dist1, dist2;
            int sides;

            // fast axial cases
            if ( p.type < 3 )
            {
                if ( p.dist <= emins[p.type] )
                    return 1;
                if ( p.dist >= emaxs[p.type] )
                    return 2;
                return 3;
            }

            // general case
            switch ( p.signbits )
            {
                case 0:
                    dist1 = p.normal[0] * emaxs[0] + p.normal[1] * emaxs[1] + p.normal[2] * emaxs[2];
                    dist2 = p.normal[0] * emins[0] + p.normal[1] * emins[1] + p.normal[2] * emins[2];
                    break;
                case 1:
                    dist1 = p.normal[0] * emins[0] + p.normal[1] * emaxs[1] + p.normal[2] * emaxs[2];
                    dist2 = p.normal[0] * emaxs[0] + p.normal[1] * emins[1] + p.normal[2] * emins[2];
                    break;
                case 2:
                    dist1 = p.normal[0] * emaxs[0] + p.normal[1] * emins[1] + p.normal[2] * emaxs[2];
                    dist2 = p.normal[0] * emins[0] + p.normal[1] * emaxs[1] + p.normal[2] * emins[2];
                    break;
                case 3:
                    dist1 = p.normal[0] * emins[0] + p.normal[1] * emins[1] + p.normal[2] * emaxs[2];
                    dist2 = p.normal[0] * emaxs[0] + p.normal[1] * emaxs[1] + p.normal[2] * emins[2];
                    break;
                case 4:
                    dist1 = p.normal[0] * emaxs[0] + p.normal[1] * emaxs[1] + p.normal[2] * emins[2];
                    dist2 = p.normal[0] * emins[0] + p.normal[1] * emins[1] + p.normal[2] * emaxs[2];
                    break;
                case 5:
                    dist1 = p.normal[0] * emins[0] + p.normal[1] * emaxs[1] + p.normal[2] * emins[2];
                    dist2 = p.normal[0] * emaxs[0] + p.normal[1] * emins[1] + p.normal[2] * emaxs[2];
                    break;
                case 6:
                    dist1 = p.normal[0] * emaxs[0] + p.normal[1] * emins[1] + p.normal[2] * emins[2];
                    dist2 = p.normal[0] * emins[0] + p.normal[1] * emaxs[1] + p.normal[2] * emaxs[2];
                    break;
                case 7:
                    dist1 = p.normal[0] * emins[0] + p.normal[1] * emins[1] + p.normal[2] * emins[2];
                    dist2 = p.normal[0] * emaxs[0] + p.normal[1] * emaxs[1] + p.normal[2] * emaxs[2];
                    break;
                default:
                    dist1 = dist2 = 0;      // shut up compiler
                    break;
            }

            sides = 0;
            if ( dist1 >= p.dist )
                sides = 1;
            if ( dist2 < p.dist )
                sides |= 2;

            return sides;
        }

        /*
		=================
		RadiusFromBounds
		=================
		*/
        public static float RadiusFromBounds( vec3_t mins, vec3_t maxs )
        {
            vec3_t corner = new vec3_t( );
            float a, b;

            for ( var i = 0; i < 3; i++ )
            {
                a = MathF.Abs( mins[i] );
                b = MathF.Abs( maxs[i] );
                corner[i] = a > b ? a : b;
            }

            return q_shared.VectorLength( corner );
        }

        public static void ClearBounds( ref vec3_t mins, ref vec3_t maxs )
        {
            mins.x = mins.y = mins.z = 99999;
            maxs.x = maxs.y = maxs.z = -99999;
        }

        public static void AddPointToBounds( vec3_t v, ref vec3_t mins, ref vec3_t maxs )
        {
            if ( v.x < mins.x )
                mins.x = v.x;

            if ( v.x > maxs.x )
                maxs.x = v.x;

            if ( v.y < mins.y )
                mins.y = v.y;

            if ( v.y > maxs.y )
                maxs.y = v.y;

            if ( v.z < mins.z )
                mins.z = v.z;

            if ( v.z > maxs.z )
                maxs.z = v.z;
        }

        public static float VectorNormalize( ref vec3_t v )
        {
            // NOTE: TTimo - Apple G4 altivec source uses double?
            float length, ilength;

            length = v.x * v.x + v.y * v.y + v.z * v.z;
            length = ( float ) Math.Sqrt( length );

            if ( length > 0 )
            {
                ilength = 1 / length;
                v.x *= ilength;
                v.y *= ilength;
                v.z *= ilength;
            }

            return length;
        }

        public static float VectorNormalize( ref vec4_t v )
        {
            // NOTE: TTimo - Apple G4 altivec source uses double?
            float length, ilength;

            length = v.x * v.x + v.y * v.y + v.z * v.z;
            length = ( float ) Math.Sqrt( length );

            if ( length > 0 )
            {
                ilength = 1 / length;
                v.x *= ilength;
                v.y *= ilength;
                v.z *= ilength;
                v.w *= ilength;
            }

            return length;
        }

        public static float VectorNormalize2( vec3_t v, out vec3_t @out )
        {
            float length, ilength;

            length = v.x * v.x + v.y * v.y + v.z * v.z;
            length = ( float ) Math.Sqrt( length );
            @out = new vec3_t( );

            if ( length > 0 )
            {
                ilength = 1 / length;
                @out.x = v.x * ilength;
                @out.y = v.y * ilength;
                @out.z = v.z * ilength;
            }
            else
            {
                q_shared.VectorClear( ref @out );
            }

            return length;

        }

        public static void _VectorMA( vec3_t veca, float scale, vec3_t vecb, out vec3_t vecc )
        {
            vecc = new vec3_t( );
            vecc[0] = veca[0] + scale * vecb[0];
            vecc[1] = veca[1] + scale * vecb[1];
            vecc[2] = veca[2] + scale * vecb[2];
        }


        public static float _DotProduct( vec3_t v1, vec3_t v2 )
        {
            return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
        }

        public static void _VectorSubtract( vec3_t veca, vec3_t vecb, out vec3_t @out )
        {
            @out = new vec3_t( );
            @out[0] = veca[0] - vecb[0];
            @out[1] = veca[1] - vecb[1];
            @out[2] = veca[2] - vecb[2];
        }

        public static void _VectorAdd( vec3_t veca, vec3_t vecb, out vec3_t @out )
        {
            @out = new vec3_t( );
            @out[0] = veca[0] + vecb[0];
            @out[1] = veca[1] + vecb[1];
            @out[2] = veca[2] + vecb[2];
        }

        public static void _VectorCopy( vec3_t @in, out vec3_t @out )
        {
            @out = new vec3_t( );
            @out.x = @in.x;
            @out.y = @in.y;
            @out.z = @in.z;
        }

        public static void _VectorScale( vec3_t @in, float scale, out vec3_t @out )
        {
            @out = new vec3_t( );
            @out.x = @in.x * scale;
            @out.y = @in.y * scale;
            @out.z = @in.z * scale;
        }

        public static void Vector4Scale( vec4_t @in, float scale, out vec4_t @out )
        {
            @out = new vec4_t( );
            @out.x = @in.x * scale;
            @out.y = @in.y * scale;
            @out.z = @in.z * scale;
            @out.w = @in.w * scale;
        }


        public static int Q_log2( int val )
        {
            int answer;

            answer = 0;
            while ( ( val >>= 1 ) != 0 )
            {
                answer++;
            }
            return answer;
        }



        /*
		=================
		PlaneTypeForNormal
		=================
		*/
        /*
		int	PlaneTypeForNormal (vec3_t normal) {
			if ( normal[0] == 1.0 )
				return PLANE_X;
			if ( normal[1] == 1.0 )
				return PLANE_Y;
			if ( normal[2] == 1.0 )
				return PLANE_Z;
			
			return PLANE_NON_AXIAL;
		}
		*/


        /*
		================
		MatrixMultiply
		================
		*/
        public static void MatrixMultiply( float[][] in1, float[][] in2, out float[][] @out )
        {
            @out = new float[3][];
            @out[0] = new float[3];
            @out[1] = new float[3];
            @out[2] = new float[3];

            @out[0][0] = in1[0][0] * in2[0][0] + in1[0][1] * in2[1][0] +
                        in1[0][2] * in2[2][0];
            @out[0][1] = in1[0][0] * in2[0][1] + in1[0][1] * in2[1][1] +
                        in1[0][2] * in2[2][1];
            @out[0][2] = in1[0][0] * in2[0][2] + in1[0][1] * in2[1][2] +
                        in1[0][2] * in2[2][2];
            @out[1][0] = in1[1][0] * in2[0][0] + in1[1][1] * in2[1][0] +
                        in1[1][2] * in2[2][0];
            @out[1][1] = in1[1][0] * in2[0][1] + in1[1][1] * in2[1][1] +
                        in1[1][2] * in2[2][1];
            @out[1][2] = in1[1][0] * in2[0][2] + in1[1][1] * in2[1][2] +
                        in1[1][2] * in2[2][2];
            @out[2][0] = in1[2][0] * in2[0][0] + in1[2][1] * in2[1][0] +
                        in1[2][2] * in2[2][0];
            @out[2][1] = in1[2][0] * in2[0][1] + in1[2][1] * in2[1][1] +
                        in1[2][2] * in2[2][1];
            @out[2][2] = in1[2][0] * in2[0][2] + in1[2][1] * in2[1][2] +
                        in1[2][2] * in2[2][2];
        }

        public static void AngleVectors( vec3_t angles, out vec3_t forward, out vec3_t right, out vec3_t up )
        {
            float angle;
            float sr, sp, sy, cr, cp, cy;
            // static to help MS compiler fp bugs

            angle = angles[q_shared.YAW] * ( q_shared.M_PI * 2 / 360 );
            sy = MathF.Sin( angle );
            cy = MathF.Cos( angle );
            angle = angles[q_shared.PITCH] * ( q_shared.M_PI * 2 / 360 );
            sp = MathF.Sin( angle );
            cp = MathF.Cos( angle );
            angle = angles[q_shared.ROLL] * ( q_shared.M_PI * 2 / 360 );
            sr = MathF.Sin( angle );
            cr = MathF.Cos( angle );

            forward = new vec3_t( );
            forward[0] = cp * cy;
            forward[1] = cp * sy;
            forward[2] = -sp;

            right = new vec3_t( );
            right[0] = ( -1 * sr * sp * cy + -1 * cr * -sy );
            right[1] = ( -1 * sr * sp * sy + -1 * cr * cy );
            right[2] = -1 * sr * cp;

            up = new vec3_t( );
            up[0] = ( cr * sp * cy + -sr * -sy );
            up[1] = ( cr * sp * sy + -sr * cy );
            up[2] = cr * cp;
        }

        /*
		** assumes "src" is normalized
		*/
        public static void PerpendicularVector( out vec3_t dst, vec3_t src )
        {
            int pos;
            int i;
            float minelem = 1.0F;
            vec3_t tempvec = new vec3_t( );
            /*
			** find the smallest magnitude axially aligned vector
			*/
            for ( pos = 0, i = 0; i < 3; i++ )
            {
                if ( MathF.Abs( src[i] ) < minelem )
                {
                    pos = i;
                    minelem = MathF.Abs( src[i] );
                }
            }
            tempvec[0] = tempvec[1] = tempvec[2] = 0.0F;
            tempvec[pos] = 1.0F;

            /*
			** project the point onto the plane defined by src
			*/
            ProjectPointOnPlane( out dst, tempvec, src );

            /*
			** normalize the result
			*/
            VectorNormalize( ref dst );
        }
    }
}
