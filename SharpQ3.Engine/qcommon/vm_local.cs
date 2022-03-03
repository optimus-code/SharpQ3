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

namespace SharpQ3.Engine.qcommon
{
	public static class vm_local
	{
		// Max number of arguments to pass from a vm to engine's syscall handler function for the vm.
		// syscall number + 15 arguments
		#define MAX_VMSYSCALL_ARGS 16

		typedef enum {
			OP_UNDEF, 

			OP_IGNORE, 

			OP_BREAK,

			OP_ENTER,
			OP_LEAVE,
			OP_CALL,
			OP_PUSH,
			OP_POP,

			OP_CONST,
			OP_LOCAL,

			OP_JUMP,

			//-------------------

			OP_EQ,
			OP_NE,

			OP_LTI,
			OP_LEI,
			OP_GTI,
			OP_GEI,

			OP_LTU,
			OP_LEU,
			OP_GTU,
			OP_GEU,

			OP_EQF,
			OP_NEF,

			OP_LTF,
			OP_LEF,
			OP_GTF,
			OP_GEF,

			//-------------------

			OP_LOAD1,
			OP_LOAD2,
			OP_LOAD4,
			OP_STORE1,
			OP_STORE2,
			OP_STORE4,				// *(stack[top-1]) = stack[top]
			OP_ARG,

			OP_BLOCK_COPY,

			//-------------------

			OP_SEX8,
			OP_SEX16,

			OP_NEGI,
			OP_ADD,
			OP_SUB,
			OP_DIVI,
			OP_DIVU,
			OP_MODI,
			OP_MODU,
			OP_MULI,
			OP_MULU,

			OP_BAND,
			OP_BOR,
			OP_BXOR,
			OP_BCOM,

			OP_LSH,
			OP_RSHI,
			OP_RSHU,

			OP_NEGF,
			OP_ADDF,
			OP_SUBF,
			OP_DIVF,
			OP_MULF,

			OP_CVIF,
			OP_CVFI
		} opcode_t;



		typedef int	vmptr_t;

		typedef struct vmSymbol_s {
			struct vmSymbol_s	*next;
			int		symValue;
			int		profileCount;
			char	symName[1];		// variable sized
		} vmSymbol_t;

		#define	VM_OFFSET_PROGRAM_STACK		0
		#define	VM_OFFSET_SYSTEM_CALL		4



		public static	vm_t	currentVM;
		public static int		vm_debugLevel;
	}

	public class vm_t
	{
		// DO NOT MOVE OR CHANGE THESE WITHOUT CHANGING THE VM_OFFSET_* DEFINES
		// USED BY THE ASM CODE
		public int programStack;       // the vm may be recursively entered
		intptr_t(*systemCall)(intptr_t* parms );

		//------------------------------------
		   
		public char name[MAX_QPATH];

		// for dynamic linked modules
		public  void* dllHandle;
		intptr_t( QDECL* entryPoint )(int callNum, ... );

		// for interpreted modules
		public bool currentlyInterpreting;

		public byte* codeBase;
		public int codeLength;

		public int* instructionPointers;
		public int instructionPointersLength;

		public byte* dataBase;
		public int dataMask;

		public int stackBottom;        // if programStack < stackBottom, error

		public int numSymbols;
		public struct vmSymbol_s   *symbols;

		public int callLevel;          // for debug indenting
		public int breakFunction;      // increment breakCount on function entry to this
		public int breakCount;

		// fqpath member added 7/20/02 by T.Ray
		public char fqpath[MAX_QPATH + 1];
	}
}
