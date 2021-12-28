﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mesen.Debugger;
using Mesen.Utilities;

namespace Mesen.Interop
{
	public class DebugApi
	{
		private const string DllPath = "MesenSCore.dll";
		[DllImport(DllPath)] public static extern void InitializeDebugger();
		[DllImport(DllPath)] public static extern void ReleaseDebugger();

		[DllImport(DllPath)] public static extern void ResumeExecution();
		[DllImport(DllPath)] public static extern void Step(CpuType cpuType, Int32 instructionCount, StepType type = StepType.Step);

		[DllImport(DllPath)] public static extern void StartLogTraceToFile([MarshalAs(UnmanagedType.LPUTF8Str)]string filename);
		[DllImport(DllPath)] public static extern void StopLogTraceToFile();

		[DllImport(DllPath)] public static extern void SetTraceOptions(CpuType cpuType, InteropTraceLoggerOptions options);

		[DllImport(DllPath, EntryPoint = "GetExecutionTrace")] private static extern UInt32 GetExecutionTraceWrapper(IntPtr output, UInt32 startOffset, UInt32 maxRowCount);
		public static TraceRow[] GetExecutionTrace(UInt32 startOffset, UInt32 maxRowCount)
		{
			TraceRow[] rows = new TraceRow[maxRowCount];

			GCHandle handle = GCHandle.Alloc(rows, GCHandleType.Pinned);
			IntPtr ptr = handle.AddrOfPinnedObject();
			UInt32 rowCount = DebugApi.GetExecutionTraceWrapper(ptr, startOffset, maxRowCount);
			handle.Free();

			Array.Resize(ref rows, (int)rowCount);

			return rows;
		}

		[DllImport(DllPath, EntryPoint = "GetDebuggerLog")] private static extern IntPtr GetDebuggerLogWrapper();
		public static string GetLog() { return Utf8Utilities.PtrToStringUtf8(DebugApi.GetDebuggerLogWrapper()).Replace("\n", Environment.NewLine); }

		[DllImport(DllPath, EntryPoint = "GetDisassemblyOutput")] private static extern UInt32 GetDisassemblyOutputWrapper(CpuType type, UInt32 address, [In,Out]InteropCodeLineData[] lineData, UInt32 rowCount);
		public static CodeLineData[] GetDisassemblyOutput(CpuType type, UInt32 address, UInt32 rowCount)
		{
			InteropCodeLineData[] rows = new InteropCodeLineData[rowCount];
			for(int i = 0; i < rowCount; i++) {
				rows[i].Comment = new byte[1000];
				rows[i].Text = new byte[1000];
				rows[i].ByteCode = new byte[4];
			}

			UInt32 resultCount = DebugApi.GetDisassemblyOutputWrapper(type, address, rows, rowCount);

			CodeLineData[] result = new CodeLineData[resultCount];
			for(int i = 0; i < resultCount; i++) {
				result[i] = new CodeLineData(rows[i], type);
			}
			return result;
		}

		[DllImport(DllPath)] public static extern int SearchDisassembly(CpuType type, [MarshalAs(UnmanagedType.LPUTF8Str)]string searchString, int startPosition, int endPosition, [MarshalAs(UnmanagedType.I1)]bool searchBackwards);

		[DllImport(DllPath)] private static extern void GetCpuState(IntPtr state, CpuType cpuType);
		public unsafe static T GetCpuState<T>(CpuType cpuType) where T : struct, BaseState
		{
			byte* ptr = stackalloc byte[Marshal.SizeOf(typeof(T))];
			DebugApi.GetCpuState((IntPtr)ptr, cpuType);
			return Marshal.PtrToStructure<T>((IntPtr)ptr);
		}

		[DllImport(DllPath)] private static extern void GetPpuState(IntPtr state, CpuType cpuType);
		public unsafe static T GetPpuState<T>(CpuType cpuType) where T : struct, BaseState
		{
			byte* ptr = stackalloc byte[Marshal.SizeOf(typeof(T))];
			DebugApi.GetPpuState((IntPtr)ptr, cpuType);
			return Marshal.PtrToStructure<T>((IntPtr)ptr);
		}

		[DllImport(DllPath)] private static extern void GetConsoleState(IntPtr state, ConsoleType consoleType);
		public unsafe static T GetConsoleState<T>(ConsoleType consoleType) where T : struct, BaseState
		{
			byte* ptr = stackalloc byte[Marshal.SizeOf(typeof(T))];
			DebugApi.GetConsoleState((IntPtr)ptr, consoleType);
			return Marshal.PtrToStructure<T>((IntPtr)ptr);
		}

		[DllImport(DllPath)] public static extern void SetScriptTimeout(UInt32 timeout);
		[DllImport(DllPath)] public static extern Int32 LoadScript(string name, [MarshalAs(UnmanagedType.LPUTF8Str)]string content, Int32 scriptId = -1);
		[DllImport(DllPath)] public static extern void RemoveScript(Int32 scriptId);
		[DllImport(DllPath, EntryPoint = "GetScriptLog")] private static extern IntPtr GetScriptLogWrapper(Int32 scriptId);
		public static string GetScriptLog(Int32 scriptId) { return Utf8Utilities.PtrToStringUtf8(DebugApi.GetScriptLogWrapper(scriptId)).Replace("\n", Environment.NewLine); }

		[DllImport(DllPath)] public static extern Int32 EvaluateExpression([MarshalAs(UnmanagedType.LPUTF8Str)]string expression, CpuType cpuType, out EvalResultType resultType, [MarshalAs(UnmanagedType.I1)]bool useCache);

		[DllImport(DllPath)] public static extern Int32 GetMemorySize(SnesMemoryType type);
		[DllImport(DllPath)] public static extern Byte GetMemoryValue(SnesMemoryType type, UInt32 address);
		[DllImport(DllPath)] public static extern void SetMemoryValue(SnesMemoryType type, UInt32 address, byte value);
		[DllImport(DllPath)] public static extern void SetMemoryValues(SnesMemoryType type, UInt32 address, [In] byte[] data, Int32 length);
		[DllImport(DllPath)] public static extern void SetMemoryState(SnesMemoryType type, [In] byte[] buffer, Int32 length);

		[DllImport(DllPath)] public static extern AddressInfo GetAbsoluteAddress(AddressInfo relAddress);
		[DllImport(DllPath)] public static extern AddressInfo GetRelativeAddress(AddressInfo absAddress, CpuType cpuType);

		[DllImport(DllPath)] public static extern void SetLabel(uint address, SnesMemoryType memType, string label, string comment);
		[DllImport(DllPath)] public static extern void ClearLabels();

		[DllImport(DllPath)] public static extern void SetBreakpoints([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]InteropBreakpoint[] breakpoints, UInt32 length);

		[DllImport(DllPath)] public static extern void SaveRomToDisk([MarshalAs(UnmanagedType.LPUTF8Str)]string filename, [MarshalAs(UnmanagedType.I1)]bool saveAsIps, CdlStripOption cdlStripOption);

		[DllImport(DllPath, EntryPoint = "GetMemoryValues")] private static extern void GetMemoryValuesWrapper(SnesMemoryType type, UInt32 start, UInt32 end, [In, Out] byte[] buffer);
		public static byte[] GetMemoryValues(SnesMemoryType type, UInt32 start, UInt32 end)
		{
			byte[] buffer = new byte[end - start + 1];
			DebugApi.GetMemoryValuesWrapper(type, start, end, buffer);
			return buffer;
		}

		[DllImport(DllPath, EntryPoint = "GetMemoryState")] private static extern void GetMemoryStateWrapper(SnesMemoryType type, [In, Out] byte[] buffer);
		public static byte[] GetMemoryState(SnesMemoryType type)
		{
			byte[] buffer = new byte[DebugApi.GetMemorySize(type)];
			DebugApi.GetMemoryStateWrapper(type, buffer);
			return buffer;
		}

		[DllImport(DllPath)] private static extern void GetTilemap(CpuType cpuType, InteropGetTilemapOptions options, IntPtr state, byte[] vram, UInt32[] palette, IntPtr outputBuffer);
		public unsafe static void GetTilemap<T>(CpuType cpuType, GetTilemapOptions options, T state, byte[] vram, UInt32[] palette, IntPtr outputBuffer) where T : struct, BaseState
		{
			GCHandle? handle = null;
			IntPtr compareVramPtr = IntPtr.Zero;

			if(options.CompareVram != null) {
				handle = GCHandle.Alloc(options.CompareVram, GCHandleType.Pinned);
				compareVramPtr = handle.Value.AddrOfPinnedObject();
			}

			byte* stateBuffer = stackalloc byte[Marshal.SizeOf(typeof(T))];
			Marshal.StructureToPtr(state, (IntPtr)stateBuffer, false);
			InteropGetTilemapOptions interopOptions = options.ToInterop();
			interopOptions.CompareVram = compareVramPtr;
			DebugApi.GetTilemap(cpuType, interopOptions, (IntPtr)stateBuffer, vram, palette, outputBuffer);
			handle?.Free();
		}

		[DllImport(DllPath)] private static extern FrameInfo GetTilemapSize(CpuType cpuType, InteropGetTilemapOptions options, IntPtr state);
		public unsafe static FrameInfo GetTilemapSize<T>(CpuType cpuType, GetTilemapOptions options, T state) where T : struct, BaseState
		{
			byte* ptr = stackalloc byte[Marshal.SizeOf(typeof(T))];
			Marshal.StructureToPtr(state, (IntPtr)ptr, false);
			return DebugApi.GetTilemapSize(cpuType, options.ToInterop(), (IntPtr)ptr);
		}

		[DllImport(DllPath)] private static extern DebugTilemapTileInfo GetTilemapTileInfo(UInt32 x, UInt32 y, CpuType cpuType, InteropGetTilemapOptions options, byte[] vram, IntPtr state);
		public unsafe static DebugTilemapTileInfo? GetTilemapTileInfo(UInt32 x, UInt32 y, CpuType cpuType, GetTilemapOptions options, byte[] vram, BaseState state)
		{
			Debug.Assert(state.GetType().IsValueType);

			byte* ptr = stackalloc byte[Marshal.SizeOf(state.GetType())];
			Marshal.StructureToPtr(state, (IntPtr)ptr, false);
			DebugTilemapTileInfo info = DebugApi.GetTilemapTileInfo(x, y, cpuType, options.ToInterop(), vram, (IntPtr)ptr);
			return info.Row >= 0 ? info : null;
		}

		[DllImport(DllPath)] public static extern void GetTileView(CpuType cpuType, GetTileViewOptions options, byte[] source, int srcSize, UInt32[] palette, IntPtr buffer);

		[DllImport(DllPath)] private static extern void GetSpritePreview(CpuType cpuType, GetSpritePreviewOptions options, IntPtr state, byte[] vram, byte[] spriteRam, UInt32[] palette, IntPtr buffer);
		public unsafe static void GetSpritePreview<T>(CpuType cpuType, GetSpritePreviewOptions options, T state, byte[] vram, byte[] spriteRam, UInt32[] palette, IntPtr outputBuffer) where T : struct, BaseState
		{
			byte* ptr = stackalloc byte[Marshal.SizeOf(typeof(T))];
			Marshal.StructureToPtr(state, (IntPtr)ptr, false);
			DebugApi.GetSpritePreview(cpuType, options, (IntPtr)ptr, vram, spriteRam, palette, outputBuffer);
		}

		[DllImport(DllPath)] private static extern FrameInfo GetSpritePreviewSize(CpuType cpuType, GetSpritePreviewOptions options, IntPtr state);
		public unsafe static FrameInfo GetSpritePreviewSize<T>(CpuType cpuType, GetSpritePreviewOptions options, T state) where T : struct, BaseState
		{
			byte* ptr = stackalloc byte[Marshal.SizeOf(typeof(T))];
			Marshal.StructureToPtr(state, (IntPtr)ptr, false);
			return DebugApi.GetSpritePreviewSize(cpuType, options, (IntPtr)ptr);
		}

		[DllImport(DllPath)] private static extern UInt32 GetSpriteList(CpuType cpuType, GetSpritePreviewOptions options, IntPtr state, byte[] vram, byte[] spriteRam, UInt32[] palette, [In,Out]DebugSpriteInfo[] sprites);
		public unsafe static DebugSpriteInfo[] GetSpriteList<T>(CpuType cpuType, GetSpritePreviewOptions options, T state, byte[] vram, byte[] spriteRam, UInt32[] palette) where T : struct, BaseState
		{
			DebugSpriteInfo[] sprites = new DebugSpriteInfo[128];

			byte* statePtr = stackalloc byte[Marshal.SizeOf(typeof(T))];
			Marshal.StructureToPtr(state, (IntPtr)statePtr, false);
			UInt32 spriteCount = DebugApi.GetSpriteList(cpuType, options, (IntPtr)statePtr, vram, spriteRam, palette, sprites);
			Array.Resize(ref sprites, (int)spriteCount);
			return sprites;
		}

		[DllImport(DllPath)] public static extern void SetViewerUpdateTiming(Int32 viewerId, Int32 scanline, Int32 cycle, CpuType cpuType);

		[DllImport(DllPath)] private static extern UInt32 GetDebugEventCount(CpuType cpuType);
		[DllImport(DllPath, EntryPoint = "GetDebugEvents")] private static extern void GetDebugEventsWrapper(CpuType cpuType, [In, Out]DebugEventInfo[] eventArray, ref UInt32 maxEventCount);
		public static DebugEventInfo[] GetDebugEvents(CpuType cpuType)
		{
			UInt32 maxEventCount = GetDebugEventCount(cpuType);
			DebugEventInfo[] debugEvents = new DebugEventInfo[maxEventCount];

			DebugApi.GetDebugEventsWrapper(cpuType, debugEvents, ref maxEventCount);
			if(maxEventCount < debugEvents.Length) {
				//Remove the excess from the array if needed
				Array.Resize(ref debugEvents, (int)maxEventCount);
			}

			return debugEvents;
		}

		[DllImport(DllPath)] public static extern void SetEventViewerConfig(CpuType cpuType, InteropSnesEventViewerConfig config);
		[DllImport(DllPath)] public static extern void SetEventViewerConfig(CpuType cpuType, InteropNesEventViewerConfig config);
		[DllImport(DllPath)] public static extern void SetEventViewerConfig(CpuType cpuType, InteropGbEventViewerConfig config);
		
		[DllImport(DllPath)] public static extern void GetEventViewerEvent(CpuType cpuType, ref DebugEventInfo evtInfo, UInt16 scanline, UInt16 cycle);
		[DllImport(DllPath)] public static extern UInt32 TakeEventSnapshot(CpuType cpuType);

		[DllImport(DllPath)] public static extern FrameInfo GetEventViewerDisplaySize(CpuType cpuType);
		[DllImport(DllPath)] public static extern void GetEventViewerOutput(CpuType cpuType, IntPtr buffer, UInt32 bufferSize);
		
		[DllImport(DllPath, EntryPoint = "GetCallstack")] private static extern void GetCallstackWrapper(CpuType type, [In, Out]StackFrameInfo[] callstackArray, ref UInt32 callstackSize);
		public static StackFrameInfo[] GetCallstack(CpuType type)
		{
			StackFrameInfo[] callstack = new StackFrameInfo[512];
			UInt32 callstackSize = 0;

			DebugApi.GetCallstackWrapper(type, callstack, ref callstackSize);
			Array.Resize(ref callstack, (int)callstackSize);

			return callstack;
		}

		[DllImport(DllPath)] public static extern void ResetProfiler(CpuType type);
		[DllImport(DllPath, EntryPoint = "GetProfilerData")] private static extern void GetProfilerDataWrapper(CpuType type, IntPtr profilerData, ref UInt32 functionCount);
		public static ProfiledFunction[] GetProfilerData(CpuType type)
		{
			ProfiledFunction[] profilerData = new ProfiledFunction[100000];
			UInt32 functionCount = 0;

			GCHandle handle = GCHandle.Alloc(profilerData, GCHandleType.Pinned);
			IntPtr ptr = handle.AddrOfPinnedObject();
			DebugApi.GetProfilerDataWrapper(type, ptr, ref functionCount);
			handle.Free();

			Array.Resize(ref profilerData, (int)functionCount);

			return profilerData;
		}

		[DllImport(DllPath)] public static extern void ResetMemoryAccessCounts();
		public static void GetMemoryAccessCounts(SnesMemoryType type, ref AddressCounters[] counters)
		{
			int size = DebugApi.GetMemorySize(type);
			Array.Resize(ref counters, size);
			DebugApi.GetMemoryAccessCountsWrapper(0, (uint)size, type, counters);
		}

		[DllImport(DllPath, EntryPoint = "GetMemoryAccessCounts")] private static extern void GetMemoryAccessCountsWrapper(UInt32 offset, UInt32 length, SnesMemoryType type, [In,Out]AddressCounters[] counts);
		public static AddressCounters[] GetMemoryAccessCounts(UInt32 offset, UInt32 length, SnesMemoryType type)
		{
			AddressCounters[] counts = new AddressCounters[length];
			DebugApi.GetMemoryAccessCountsWrapper(offset, length, type, counts);
			return counts;
		}

		[DllImport(DllPath, EntryPoint = "GetCdlData")] private static extern void GetCdlDataWrapper(UInt32 offset, UInt32 length, SnesMemoryType memType, [In,Out] byte[] cdlData);
		public static byte[] GetCdlData(UInt32 offset, UInt32 length, SnesMemoryType memType)
		{
			byte[] cdlData = new byte[length];
			DebugApi.GetCdlDataWrapper(offset, length, memType, cdlData);
			return cdlData;
		}

		[DllImport(DllPath)] public static extern void SetCdlData(CpuType cpuType, [In]byte[] cdlData, Int32 length);
		[DllImport(DllPath)] public static extern void MarkBytesAs(CpuType cpuType, UInt32 start, UInt32 end, CdlFlags type);

		[DllImport(DllPath, EntryPoint = "AssembleCode")] private static extern UInt32 AssembleCodeWrapper(CpuType cpuType, [MarshalAs(UnmanagedType.LPUTF8Str)]string code, UInt32 startAddress, [In, Out]Int16[] assembledCodeBuffer);
		public static Int16[] AssembleCode(CpuType cpuType, string code, UInt32 startAddress)
		{
			code = code.Replace(Environment.NewLine, "\n");
			Int16[] assembledCode = new Int16[100000];
			UInt32 size = DebugApi.AssembleCodeWrapper(cpuType, code, startAddress, assembledCode);
			Array.Resize(ref assembledCode, (int)size);
			return assembledCode;
		}
	}

	public enum SnesMemoryType
	{
		CpuMemory,
		SpcMemory,
		Sa1Memory,
		NecDspMemory,
		GsuMemory,
		Cx4Memory,
		GameboyMemory,
		NesMemory,
		NesPpuMemory,

		PrgRom,
		WorkRam,
		SaveRam,
		VideoRam,
		SpriteRam,
		CGRam,
		SpcRam,
		SpcRom,
		DspProgramRom,
		DspDataRom,
		DspDataRam,
		Sa1InternalRam,
		GsuWorkRam,
		Cx4DataRam,
		BsxPsRam,
		BsxMemoryPack,

		GbPrgRom,
		GbWorkRam,
		GbCartRam,
		GbHighRam,
		GbBootRom,
		GbVideoRam,
		GbSpriteRam,

		NesPrgRom,
		NesInternalRam,
		NesWorkRam,
		NesSaveRam,
		NesNametableRam,
		NesSpriteRam,
		NesSecondarySpriteRam,
		NesPaletteRam,
		NesChrRam,
		NesChrRom,

		Register
	}

	public static class SnesMemoryTypeExtensions
	{
		public static CpuType ToCpuType(this SnesMemoryType memType)
		{
			switch(memType) {
				case SnesMemoryType.SpcMemory:
				case SnesMemoryType.SpcRam:
				case SnesMemoryType.SpcRom:
					return CpuType.Spc;

				case SnesMemoryType.GsuMemory:
				case SnesMemoryType.GsuWorkRam:
					return CpuType.Gsu;

				case SnesMemoryType.Sa1InternalRam:
				case SnesMemoryType.Sa1Memory:
					return CpuType.Sa1;

				case SnesMemoryType.DspDataRam:
				case SnesMemoryType.DspDataRom:
				case SnesMemoryType.DspProgramRom:
					return CpuType.NecDsp;

				case SnesMemoryType.GbPrgRom:
				case SnesMemoryType.GbWorkRam:
				case SnesMemoryType.GbCartRam:
				case SnesMemoryType.GbHighRam:
				case SnesMemoryType.GbBootRom:
				case SnesMemoryType.GbVideoRam:
				case SnesMemoryType.GbSpriteRam:
				case SnesMemoryType.GameboyMemory:
					return CpuType.Gameboy;

				case SnesMemoryType.NesMemory:
				case SnesMemoryType.NesPrgRom:
				case SnesMemoryType.NesWorkRam:
				case SnesMemoryType.NesSaveRam:
				case SnesMemoryType.NesChrRam:
				case SnesMemoryType.NesChrRom:
				case SnesMemoryType.NesInternalRam:
				case SnesMemoryType.NesNametableRam:
				case SnesMemoryType.NesPaletteRam:
				case SnesMemoryType.NesSpriteRam:
					return CpuType.Nes;

				default:
					return CpuType.Cpu;
			}
		}

		public static bool IsPpuMemory(this SnesMemoryType memType)
		{
			switch(memType) {
				case SnesMemoryType.VideoRam:
				case SnesMemoryType.SpriteRam:
				case SnesMemoryType.CGRam:
				case SnesMemoryType.GbVideoRam:
				case SnesMemoryType.GbSpriteRam:
					return true;

				default:
					return false;
			}
		}

		public static bool IsRelativeMemory(this SnesMemoryType memType)
		{
			switch(memType) {
				case SnesMemoryType.CpuMemory:
				case SnesMemoryType.SpcMemory:
				case SnesMemoryType.Sa1Memory:
				case SnesMemoryType.GsuMemory:
				case SnesMemoryType.NecDspMemory:
				case SnesMemoryType.Cx4Memory:
				case SnesMemoryType.GameboyMemory:
				case SnesMemoryType.NesMemory:
					return true;
			}
			return false;
		}

		public static bool SupportsLabels(this SnesMemoryType memType)
		{
			switch(memType) {
				case SnesMemoryType.PrgRom:
				case SnesMemoryType.WorkRam:
				case SnesMemoryType.SaveRam:
				case SnesMemoryType.Register:
				case SnesMemoryType.SpcRam:
				case SnesMemoryType.SpcRom:
				case SnesMemoryType.Sa1InternalRam:
				case SnesMemoryType.GbPrgRom:
				case SnesMemoryType.GbWorkRam:
				case SnesMemoryType.GbCartRam:
				case SnesMemoryType.GbHighRam:
				case SnesMemoryType.GbBootRom:
				case SnesMemoryType.NesPrgRom:
				case SnesMemoryType.NesWorkRam:
				case SnesMemoryType.NesSaveRam:
				case SnesMemoryType.NesInternalRam:
					return true;
			}

			return false;
		}

		public static bool SupportsWatch(this SnesMemoryType memType)
		{
			switch(memType) {
				case SnesMemoryType.CpuMemory:
				case SnesMemoryType.SpcMemory:
				case SnesMemoryType.Sa1Memory:
				case SnesMemoryType.GsuMemory:
				case SnesMemoryType.NecDspMemory:
				case SnesMemoryType.Cx4Memory:
				case SnesMemoryType.GameboyMemory:
				case SnesMemoryType.NesMemory:
					return true;
			}

			return false;
		}

		public static bool SupportsCdl(this SnesMemoryType memType)
		{
			switch(memType) {
				case SnesMemoryType.CpuMemory:
				case SnesMemoryType.GameboyMemory:
				case SnesMemoryType.NesMemory:				
				case SnesMemoryType.PrgRom:
				case SnesMemoryType.GbPrgRom:
				case SnesMemoryType.NesPrgRom:
					return true;
			}

			return false;
		}

		public static string GetShortName(this SnesMemoryType memType)
		{
			return memType switch {
				SnesMemoryType.CpuMemory => "CPU",
				SnesMemoryType.SpcMemory => "SPC",
				SnesMemoryType.Sa1Memory => "SA1",
				SnesMemoryType.GsuMemory => "GSU",
				SnesMemoryType.NecDspMemory => "DSP",

				SnesMemoryType.PrgRom => "PRG",
				SnesMemoryType.WorkRam => "WRAM",
				SnesMemoryType.SaveRam => "SRAM",
				SnesMemoryType.VideoRam => "VRAM",
				SnesMemoryType.SpriteRam => "OAM",
				SnesMemoryType.CGRam => "CG",

				SnesMemoryType.SpcRam => "RAM",
				SnesMemoryType.SpcRom => "ROM",

				SnesMemoryType.DspProgramRom => "DSP",
				SnesMemoryType.Sa1InternalRam => "IRAM",
				SnesMemoryType.GsuWorkRam => "GWRAM",

				SnesMemoryType.BsxPsRam => "PSRAM",
				SnesMemoryType.BsxMemoryPack => "MPACK",

				SnesMemoryType.GameboyMemory => "CPU",
				SnesMemoryType.GbPrgRom => "PRG",
				SnesMemoryType.GbWorkRam => "WRAM",
				SnesMemoryType.GbCartRam => "SRAM",
				SnesMemoryType.GbHighRam => "HRAM",
				SnesMemoryType.GbBootRom => "BOOT",
				SnesMemoryType.GbVideoRam => "VRAM",
				SnesMemoryType.GbSpriteRam => "OAM",

				SnesMemoryType.NesMemory => "CPU",
				SnesMemoryType.NesPrgRom => "PRG",
				SnesMemoryType.NesWorkRam => "WRAM",
				SnesMemoryType.NesSaveRam => "SRAM",
				SnesMemoryType.NesSpriteRam => "SPR",
				SnesMemoryType.NesPaletteRam => "PAL",
				SnesMemoryType.NesNametableRam => "NTRAM",
				SnesMemoryType.NesInternalRam => "RAM",
				SnesMemoryType.NesChrRom => "CHR",
				SnesMemoryType.NesChrRam => "CHR",

				SnesMemoryType.Register => "REG",

				_ => throw new Exception("invalid type"),
			};
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct AddressCounters
	{
		public UInt32 Address;
		public UInt32 ReadCount;
		public UInt64 ReadStamp;

		public byte UninitRead;
		public UInt32 WriteCount;
		public UInt64 WriteStamp;

		public UInt32 ExecCount;
		public UInt64 ExecStamp;
	}

	public struct AddressInfo
	{
		public Int32 Address;
		public SnesMemoryType Type;
	}

	public enum MemoryOperationType
	{
		Read = 0,
		Write = 1,
		ExecOpCode = 2,
		ExecOperand = 3,
		DmaRead = 4,
		DmaWrite = 5,
		DummyRead = 6
	}

	public struct MemoryOperationInfo
	{
		public UInt32 Address;
		public Int32 Value;
		public MemoryOperationType Type;
	}

	public enum DebugEventType
	{
		Register,
		Nmi,
		Irq,
		Breakpoint,
		BgColorChange,
		SpriteZeroHit,
		DmcDmaRead,
	}

	public struct DmaChannelConfig
	{
		[MarshalAs(UnmanagedType.I1)] public bool DmaActive;

		[MarshalAs(UnmanagedType.I1)] public bool InvertDirection;
		[MarshalAs(UnmanagedType.I1)] public bool Decrement;
		[MarshalAs(UnmanagedType.I1)] public bool FixedTransfer;
		[MarshalAs(UnmanagedType.I1)] public bool HdmaIndirectAddressing;

		public byte TransferMode;

		public UInt16 SrcAddress;
		public byte SrcBank;

		public UInt16 TransferSize;
		public byte DestAddress;

		public UInt16 HdmaTableAddress;
		public byte HdmaBank;
		public byte HdmaLineCounterAndRepeat;

		[MarshalAs(UnmanagedType.I1)] public bool DoTransfer;
		[MarshalAs(UnmanagedType.I1)] public bool HdmaFinished;
		[MarshalAs(UnmanagedType.I1)] public bool UnusedFlag;
	}

	public struct DebugEventInfo
	{
		public MemoryOperationInfo Operation;
		public DebugEventType Type;
		public UInt32 ProgramCounter;
		public Int16 Scanline;
		public UInt16 Cycle;
		public Int16 BreakpointId;
		public byte DmaChannel;
		public DmaChannelConfig DmaChannelInfo;
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct InteropEventViewerCategoryCfg
	{
		[MarshalAs(UnmanagedType.I1)]
		public bool Visible;

		public UInt32 Color;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class InteropSnesEventViewerConfig
	{
		public InteropEventViewerCategoryCfg Irq;
		public InteropEventViewerCategoryCfg Nmi;
		public InteropEventViewerCategoryCfg MarkedBreakpoints;

		public InteropEventViewerCategoryCfg PpuRegisterReads;
		public InteropEventViewerCategoryCfg PpuRegisterCgramWrites;
		public InteropEventViewerCategoryCfg PpuRegisterVramWrites;
		public InteropEventViewerCategoryCfg PpuRegisterOamWrites;
		public InteropEventViewerCategoryCfg PpuRegisterMode7Writes;
		public InteropEventViewerCategoryCfg PpuRegisterBgOptionWrites;
		public InteropEventViewerCategoryCfg PpuRegisterBgScrollWrites;
		public InteropEventViewerCategoryCfg PpuRegisterWindowWrites;
		public InteropEventViewerCategoryCfg PpuRegisterOtherWrites;

		public InteropEventViewerCategoryCfg ApuRegisterReads;
		public InteropEventViewerCategoryCfg ApuRegisterWrites;
		public InteropEventViewerCategoryCfg CpuRegisterReads;
		public InteropEventViewerCategoryCfg CpuRegisterWrites;
		public InteropEventViewerCategoryCfg WorkRamRegisterReads;
		public InteropEventViewerCategoryCfg WorkRamRegisterWrites;

		[MarshalAs(UnmanagedType.I1)] public bool ShowPreviousFrameEvents;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public byte[] ShowDmaChannels = new byte[8];
	}

	[StructLayout(LayoutKind.Sequential)]
	public class InteropNesEventViewerConfig
	{
		public InteropEventViewerCategoryCfg Irq;
		public InteropEventViewerCategoryCfg Nmi;
		public InteropEventViewerCategoryCfg MarkedBreakpoints;

		public InteropEventViewerCategoryCfg MapperRegisterWrites;
		public InteropEventViewerCategoryCfg MapperRegisterReads;
		public InteropEventViewerCategoryCfg ApuRegisterWrites;
		public InteropEventViewerCategoryCfg ApuRegisterReads;
		public InteropEventViewerCategoryCfg ControlRegisterWrites;
		public InteropEventViewerCategoryCfg ControlRegisterReads;
		
		public InteropEventViewerCategoryCfg Ppu2000Write;
		public InteropEventViewerCategoryCfg Ppu2001Write;
		public InteropEventViewerCategoryCfg Ppu2003Write;
		public InteropEventViewerCategoryCfg Ppu2004Write;
		public InteropEventViewerCategoryCfg Ppu2005Write;
		public InteropEventViewerCategoryCfg Ppu2006Write;
		public InteropEventViewerCategoryCfg Ppu2007Write;

		public InteropEventViewerCategoryCfg Ppu2002Read;
		public InteropEventViewerCategoryCfg Ppu2004Read;
		public InteropEventViewerCategoryCfg Ppu2007Read;

		public InteropEventViewerCategoryCfg DmcDmaReads;
		public InteropEventViewerCategoryCfg SpriteZeroHit;

		[MarshalAs(UnmanagedType.I1)] public bool ShowPreviousFrameEvents;
		[MarshalAs(UnmanagedType.I1)] public bool ShowNtscBorders;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class InteropGbEventViewerConfig
	{
		public InteropEventViewerCategoryCfg Irq;
		public InteropEventViewerCategoryCfg MarkedBreakpoints;

		public InteropEventViewerCategoryCfg PpuRegisterReads;
		public InteropEventViewerCategoryCfg PpuRegisterCgramWrites;
		public InteropEventViewerCategoryCfg PpuRegisterVramWrites;
		public InteropEventViewerCategoryCfg PpuRegisterOamWrites;
		public InteropEventViewerCategoryCfg PpuRegisterBgScrollWrites;
		public InteropEventViewerCategoryCfg PpuRegisterWindowWrites;
		public InteropEventViewerCategoryCfg PpuRegisterOtherWrites;

		public InteropEventViewerCategoryCfg ApuRegisterReads;
		public InteropEventViewerCategoryCfg ApuRegisterWrites;
		public InteropEventViewerCategoryCfg CpuRegisterReads;
		public InteropEventViewerCategoryCfg CpuRegisterWrites;

		[MarshalAs(UnmanagedType.I1)] public bool ShowPreviousFrameEvents;
	}

	public enum TilemapDisplayMode
	{
		Default,
		Grayscale,
		AttributeView
	}

	public class GetTilemapOptions
	{
		public byte Layer;
		public byte[]? CompareVram;
		public bool HighlightTileChanges;
		public bool HighlightAttributeChanges;
		public TilemapDisplayMode DisplayMode;

		public InteropGetTilemapOptions ToInterop()
		{
			return new InteropGetTilemapOptions() {
				Layer = Layer,
				HighlightTileChanges = HighlightTileChanges,
				HighlightAttributeChanges = HighlightAttributeChanges,
				DisplayMode = DisplayMode
			};
		}
	}

	public struct InteropGetTilemapOptions
	{
		public byte Layer;
		public IntPtr CompareVram;
		[MarshalAs(UnmanagedType.I1)] public bool HighlightTileChanges;
		[MarshalAs(UnmanagedType.I1)] public bool HighlightAttributeChanges;
		public TilemapDisplayMode DisplayMode;
	}

	public enum TileBackground
	{
		Default = 0,
		PaletteColor = 1,
		Black = 2,
		White = 3,
		Magenta = 4
	}

	public enum NullableBoolean
	{
		Undefined = -1,
		False = 0,
		True = 1
	}

	public struct DebugTilemapTileInfo
	{
		public Int32 Row;
		public Int32 Column;

		public Int32 Width;
		public Int32 Height;

		public Int32 TileMapAddress;

		public Int32 TileIndex;
		public Int32 TileAddress;

		public Int32 PaletteIndex;
		public Int32 PaletteAddress;

		public NullableBoolean HorizontalMirroring;
		public NullableBoolean VerticalMirroring;
		public NullableBoolean HighPriority;
	};

	public struct GetTileViewOptions
	{
		public TileFormat Format;
		public TileLayout Layout;
		public TileBackground Background;
		public Int32 Width;
		public Int32 Height;
		public Int32 StartAddress;
		public Int32 Palette;
	}

	public struct GetSpritePreviewOptions
	{
		public Int32 SelectedSprite;
	}

	public struct DebugSpriteInfo
	{
		public UInt16 SpriteIndex;
		public UInt16 TileIndex;
		public Int16 X;
		public Int16 Y;

		public byte Palette;
		public byte Priority;
		public byte Width;
		public byte Height;
		[MarshalAs(UnmanagedType.I1)] public bool HorizontalMirror;
		[MarshalAs(UnmanagedType.I1)] public bool VerticalMirror;
		[MarshalAs(UnmanagedType.I1)] public bool UseSecondTable;
		[MarshalAs(UnmanagedType.I1)] public bool Visible;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64 * 64)]
		public UInt32[] SpritePreview;
	}

	public enum TileFormat
	{
		Bpp2,
		Bpp4,
		Bpp8,
		DirectColor,
		Mode7,
		Mode7DirectColor,
		NesBpp2,
	}

	public enum TileLayout
	{
		Normal,
		SingleLine8x16,
		SingleLine16x16
	};

	[Serializable]
	public struct InteropTraceLoggerOptions
	{
		[MarshalAs(UnmanagedType.I1)] public bool Enabled;
		[MarshalAs(UnmanagedType.I1)] public bool IndentCode;
		[MarshalAs(UnmanagedType.I1)] public bool UseLabels;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1000)]
		public byte[] Condition;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1000)]
		public byte[] Format;
	}

	public enum EvalResultType
	{
		Numeric = 0,
		Boolean = 1,
		Invalid = 2,
		DivideBy0 = 3,
		OutOfScope = 4
	}
	
	public struct StackFrameInfo
	{
		public UInt32 Source;
		public UInt32 Target;
		public UInt32 Return;
		public AddressInfo AbsReturn;
		public StackFrameFlags Flags;
	};

	public enum StackFrameFlags
	{
		None = 0,
		Nmi = 1,
		Irq = 2
	}
	
	public enum CpuType : byte
	{
		Cpu,
		Spc,
		NecDsp,
		Sa1,
		Gsu,
		Cx4,
		Gameboy,
		Nes
	}

	public static class CpuTypeExtensions
	{
		public static SnesMemoryType ToMemoryType(this CpuType cpuType)
		{
			return cpuType switch {
				CpuType.Cpu => SnesMemoryType.CpuMemory,
				CpuType.Spc => SnesMemoryType.SpcMemory,
				CpuType.NecDsp => SnesMemoryType.NecDspMemory,
				CpuType.Sa1 => SnesMemoryType.Sa1Memory,
				CpuType.Gsu => SnesMemoryType.GsuMemory,
				CpuType.Cx4 => SnesMemoryType.Cx4Memory,
				CpuType.Gameboy => SnesMemoryType.GameboyMemory,
				CpuType.Nes => SnesMemoryType.NesMemory,
				_ => throw new Exception("Invalid CPU type"),
			};
		}

		public static SnesMemoryType GetVramMemoryType(this CpuType cpuType)
		{
			return cpuType switch {
				CpuType.Cpu => SnesMemoryType.VideoRam,
				CpuType.Gameboy => SnesMemoryType.GbVideoRam,
				CpuType.Nes => SnesMemoryType.NesPpuMemory,
				_ => throw new Exception("Invalid CPU type"),
			};
		}

		public static SnesMemoryType GetSpriteRamMemoryType(this CpuType cpuType)
		{
			return cpuType switch {
				CpuType.Cpu => SnesMemoryType.SpriteRam,
				CpuType.Gameboy => SnesMemoryType.GbSpriteRam,
				CpuType.Nes => SnesMemoryType.NesSpriteRam,
				_ => throw new Exception("Invalid CPU type"),
			};
		}

		public static int GetAddressSize(this CpuType cpuType)
		{
			return cpuType switch {
				CpuType.Cpu => 6,
				CpuType.Spc => 4,
				CpuType.NecDsp => 4,
				CpuType.Sa1 => 6,
				CpuType.Gsu => 6,
				CpuType.Cx4 => 6,
				CpuType.Gameboy => 4,
				CpuType.Nes => 4,
				_ => throw new Exception("Invalid CPU type"),
			};
		}

		public static int GetByteCodeSize(this CpuType cpuType)
		{
			return cpuType switch {
				CpuType.Cpu => 4,
				CpuType.Spc => 3,
				CpuType.NecDsp => 3,
				CpuType.Sa1 => 4,
				CpuType.Gsu => 3,
				CpuType.Cx4 => 4,
				CpuType.Gameboy => 3,
				CpuType.Nes => 3,
				_ => throw new Exception("Invalid CPU type"),
			};
		}

		public static DebuggerFlags GetDebuggerFlag(this CpuType cpuType)
		{
			return cpuType switch {
				CpuType.Cpu => DebuggerFlags.CpuDebuggerEnabled,
				CpuType.Spc => DebuggerFlags.SpcDebuggerEnabled,
				CpuType.NecDsp => DebuggerFlags.NecDspDebuggerEnabled,
				CpuType.Sa1 => DebuggerFlags.Sa1DebuggerEnabled,
				CpuType.Gsu => DebuggerFlags.GsuDebuggerEnabled,
				CpuType.Cx4 => DebuggerFlags.Cx4DebuggerEnabled,
				CpuType.Gameboy => DebuggerFlags.GbDebuggerEnabled,
				CpuType.Nes => DebuggerFlags.NesDebuggerEnabled,
				_ => throw new Exception("Invalid CPU type"),
			};
		}
	}

	public static class ConsoleTypeExtensions
	{
		public static CpuType GetMainCpuType(this ConsoleType type)
		{
			return type switch {
				ConsoleType.Snes => CpuType.Cpu,
				ConsoleType.Nes => CpuType.Nes,
				ConsoleType.Gameboy or ConsoleType.GameboyColor => CpuType.Gameboy,
				_ => throw new Exception("Invalid type")
			};
		}
	}

	public enum StepType
	{
		Step,
		StepOut,
		StepOver,
		PpuStep,
		PpuScanline,
		PpuFrame,
		SpecificScanline,
	}

	public enum BreakSource
	{
		Unspecified = -1,
		Breakpoint = 0,
		CpuStep = 1,
		PpuStep = 2,
		BreakOnBrk = 3,
		BreakOnCop = 4,
		BreakOnWdm = 5,
		BreakOnStp = 6,
		BreakOnUninitMemoryRead = 7,
		
		GbInvalidOamAccess = 8,
		GbInvalidVramAccess = 9,
		GbDisableLcdOutsideVblank = 10,
		GbInvalidOpCode = 11,
		GbNopLoad = 12,
		GbOamCorruption = 13,
	}

	public struct BreakEvent
	{
		public BreakSource Source;
		public MemoryOperationInfo Operation;
		public Int32 BreakpointId;
	}

	public enum CdlStripOption
	{
		StripNone = 0,
		StripUnused = 1,
		StripUsed = 2
	}

	public enum CdlFlags : byte
	{
		None = 0x00,
		Code = 0x01,
		Data = 0x02,
		JumpTarget = 0x04,
		SubEntryPoint = 0x08,

		IndexMode8 = 0x10,
		MemoryMode8 = 0x20,
	}

	public struct ProfiledFunction
	{
		public UInt64 ExclusiveCycles;
		public UInt64 InclusiveCycles;
		public UInt64 CallCount;
		public UInt64 MinCycles;
		public UInt64 MaxCycles;
		public AddressInfo Address;
	}

	public unsafe struct TraceRow
	{
		public UInt32 ProgramCounter;
		public CpuType Type;

		public fixed byte ByteCode[4];
		public byte ByteCodeSize;

		public fixed byte LogOutput[500];

		public unsafe string GetOutput()
		{
			fixed(byte* output = LogOutput) {
				int i;
				for(i = 0; i < 500; i++) {
					if(output[i] == 0) {
						break;
					}
				}

				return UTF8Encoding.UTF8.GetString(output, i);
			}
		}

		public unsafe string GetByteCode()
		{
			fixed(byte* output = ByteCode) {
				StringBuilder sb = new StringBuilder();
				int i;
				for(i = 0; i < ByteCodeSize && i < 4; i++) {
					sb.Append(ByteCode[i].ToString("X2") + " ");
				}
				return sb.ToString().Trim();
			}
		}
	}
}
