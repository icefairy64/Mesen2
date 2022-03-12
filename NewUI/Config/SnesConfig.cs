﻿using Mesen.Interop;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mesen.Config
{
	public class SnesConfig : BaseConfig<SnesConfig>
	{
		//Input
		[Reactive] public ControllerConfig Port1 { get; set; } = new ControllerConfig();
		[Reactive] public ControllerConfig Port2 { get; set; } = new ControllerConfig();
		
		[Reactive] public ControllerConfig Port1A { get; set; } = new ControllerConfig();
		[Reactive] public ControllerConfig Port1B { get; set; } = new ControllerConfig();
		[Reactive] public ControllerConfig Port1C { get; set; } = new ControllerConfig();
		[Reactive] public ControllerConfig Port1D { get; set; } = new ControllerConfig();
		
		[Reactive] public ControllerConfig Port2A { get; set; } = new ControllerConfig();
		[Reactive] public ControllerConfig Port2B { get; set; } = new ControllerConfig();
		[Reactive] public ControllerConfig Port2C { get; set; } = new ControllerConfig();
		[Reactive] public ControllerConfig Port2D { get; set; } = new ControllerConfig();

		[Reactive] public ConsoleRegion Region { get; set; } = ConsoleRegion.Auto;

		//Video
		[Reactive] public bool BlendHighResolutionModes { get; set; } = false;
		[Reactive] public bool HideBgLayer1 { get; set; } = false;
		[Reactive] public bool HideBgLayer2 { get; set; } = false;
		[Reactive] public bool HideBgLayer3 { get; set; } = false;
		[Reactive] public bool HideBgLayer4 { get; set; } = false;
		[Reactive] public bool HideSprites { get; set; } = false;
		[Reactive] public bool DisableFrameSkipping { get; set; } = false;

		[Reactive] [MinMax(0, 100)] public UInt32 OverscanLeft { get; set; } = 0;
		[Reactive] [MinMax(0, 100)] public UInt32 OverscanRight { get; set; } = 0;
		[Reactive] [MinMax(0, 100)] public UInt32 OverscanTop { get; set; } = 7;
		[Reactive] [MinMax(0, 100)] public UInt32 OverscanBottom { get; set; } = 8;

		//Audio
		[Reactive] public bool EnableCubicInterpolation { get; set; } = false;

		//Emulation
		[Reactive] public bool EnableRandomPowerOnState { get; set; } = false;
		[Reactive] public bool EnableStrictBoardMappings { get; set; } = false;
		[Reactive] public RamState RamPowerOnState { get; set; } = RamState.Random;

		//Overclocking
		[Reactive] [MinMax(0, 1000)] public UInt32 PpuExtraScanlinesBeforeNmi { get; set; } = 0;
		[Reactive] [MinMax(0, 1000)] public UInt32 PpuExtraScanlinesAfterNmi { get; set; } = 0;
		[Reactive] [MinMax(100, 1000)] public UInt32 GsuClockSpeed { get; set; } = 100;

		//BSX
		[Reactive] public bool BsxUseCustomTime { get; set; } = false;
		[Reactive] public DateTimeOffset BsxCustomDate { get; set; } = new DateTimeOffset(1995, 1, 1, 0, 0, 0, TimeSpan.Zero);
		[Reactive] public TimeSpan BsxCustomTime { get; set; } = TimeSpan.Zero;

		public void ApplyConfig()
		{
			ConfigApi.SetSnesConfig(new InteropSnesConfig() {
				Port1 = Port1.ToInterop(),
				Port1A = Port1A.ToInterop(),
				Port1B = Port1B.ToInterop(),
				Port1C = Port1C.ToInterop(),
				Port1D = Port1D.ToInterop(),

				Port2 = Port2.ToInterop(),
				Port2A = Port2A.ToInterop(),
				Port2B = Port2B.ToInterop(),
				Port2C = Port2C.ToInterop(),
				Port2D = Port2D.ToInterop(),

				Region = this.Region,

				BlendHighResolutionModes = this.BlendHighResolutionModes,
				HideBgLayer1 = this.HideBgLayer1,
				HideBgLayer2 = this.HideBgLayer2,
				HideBgLayer3 = this.HideBgLayer3,
				HideBgLayer4 = this.HideBgLayer4,
				HideSprites = this.HideSprites,
				DisableFrameSkipping = this.DisableFrameSkipping,

				OverscanLeft = this.OverscanLeft,
				OverscanRight = this.OverscanRight,
				OverscanTop = this.OverscanTop,
				OverscanBottom = this.OverscanBottom,

				EnableCubicInterpolation = this.EnableCubicInterpolation,

				EnableRandomPowerOnState = this.EnableRandomPowerOnState,
				EnableStrictBoardMappings = this.EnableStrictBoardMappings,
				PpuExtraScanlinesBeforeNmi = this.PpuExtraScanlinesBeforeNmi,
				PpuExtraScanlinesAfterNmi = this.PpuExtraScanlinesAfterNmi,
				GsuClockSpeed = this.GsuClockSpeed,
				RamPowerOnState = this.RamPowerOnState,
				BsxCustomDate = this.BsxCustomDate.Ticks + this.BsxCustomTime.Ticks
			});
		}

		public void InitializeDefaults(DefaultKeyMappingType defaultMappings)
		{
			List<KeyMapping> mappings = new List<KeyMapping>();
			if(defaultMappings.HasFlag(DefaultKeyMappingType.Xbox)) {
				KeyMapping mapping = new();
				KeyPresets.ApplyXboxLayout(mapping, 0, ControllerType.SnesController);
				mappings.Add(mapping);
			}
			if(defaultMappings.HasFlag(DefaultKeyMappingType.Ps4)) {
				KeyMapping mapping = new();
				KeyPresets.ApplyPs4Layout(mapping, 0, ControllerType.SnesController);
				mappings.Add(mapping);
			}
			if(defaultMappings.HasFlag(DefaultKeyMappingType.WasdKeys)) {
				KeyMapping mapping = new();
				KeyPresets.ApplyWasdLayout(mapping, ControllerType.SnesController);
				mappings.Add(mapping);
			}
			if(defaultMappings.HasFlag(DefaultKeyMappingType.ArrowKeys)) {
				KeyMapping mapping = new();
				KeyPresets.ApplyArrowLayout(mapping, ControllerType.SnesController);
				mappings.Add(mapping);
			}

			Port1.Type = ControllerType.SnesController;
			Port1.TurboSpeed = 2;
			if(mappings.Count > 0) {
				Port1.Mapping1 = mappings[0];
				if(mappings.Count > 1) {
					Port1.Mapping2 = mappings[1];
					if(mappings.Count > 2) {
						Port1.Mapping3 = mappings[2];
						if(mappings.Count > 3) {
							Port1.Mapping4 = mappings[3];
						}
					}
				}
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct InteropSnesConfig
	{
		public InteropControllerConfig Port1;
		public InteropControllerConfig Port2;
		
		public InteropControllerConfig Port1A;
		public InteropControllerConfig Port1B;
		public InteropControllerConfig Port1C;
		public InteropControllerConfig Port1D;
		
		public InteropControllerConfig Port2A;
		public InteropControllerConfig Port2B;
		public InteropControllerConfig Port2C;
		public InteropControllerConfig Port2D;

		public ConsoleRegion Region;

		[MarshalAs(UnmanagedType.I1)] public bool BlendHighResolutionModes;
		[MarshalAs(UnmanagedType.I1)] public bool HideBgLayer1;
		[MarshalAs(UnmanagedType.I1)] public bool HideBgLayer2;
		[MarshalAs(UnmanagedType.I1)] public bool HideBgLayer3;
		[MarshalAs(UnmanagedType.I1)] public bool HideBgLayer4;
		[MarshalAs(UnmanagedType.I1)] public bool HideSprites;
		[MarshalAs(UnmanagedType.I1)] public bool DisableFrameSkipping;

		public UInt32 OverscanLeft;
		public UInt32 OverscanRight;
		public UInt32 OverscanTop;
		public UInt32 OverscanBottom;

		[MarshalAs(UnmanagedType.I1)] public bool EnableCubicInterpolation;

		[MarshalAs(UnmanagedType.I1)] public bool EnableRandomPowerOnState;
		[MarshalAs(UnmanagedType.I1)] public bool EnableStrictBoardMappings;
		public RamState RamPowerOnState;

		public UInt32 PpuExtraScanlinesBeforeNmi;
		public UInt32 PpuExtraScanlinesAfterNmi;
		public UInt32 GsuClockSpeed;

		public long BsxCustomDate;
	}
}
