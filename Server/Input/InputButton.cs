using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CustomNetworking.Server;

/// <summary>
/// An input action that can come from a mouse, keyboard or controller.
/// </summary>
[Flags]
public enum InputButton : ulong
{
	[Display(Name = "Move Forward"), Category("Movement")] Forward = 1,
    [Display(Name = "Move Backward"), Category("Movement")] Back = 2,
    [Display(Name = "Strafe Left"), Category("Movement")] Left = 4,
    [Display(Name = "Strafe Right"), Category("Movement")] Right = 8,
    [Display(Name = "Jump"), Category("Movement")] Jump = 16, // 0x0000000000000010
    [Display(Name = "Duck"), Category("Movement")] Duck = 32, // 0x0000000000000020
    [Display(Name = "Run"), Category("Movement")] Run = 64, // 0x0000000000000040
    [Display(Name = "Walk"), Category("Movement")] Walk = 128, // 0x0000000000000080
    [Display(Name = "Primary Attack"), Category("Combat")] PrimaryAttack = 256, // 0x0000000000000100
    [Display(Name = "Secondary Attack"), Category("Combat")] SecondaryAttack = 512, // 0x0000000000000200
    [Display(Name = "Reload"), Category("Combat")] Reload = 1024, // 0x0000000000000400
    [Display(Name = "Grenade"), Category("Combat")] Grenade = 2048, // 0x0000000000000800
    [Display(Name = "Drop"), Category("Combat")] Drop = 4096, // 0x0000000000001000
    [Display(Name = "Use"), Category("Misc")] Use = 8192, // 0x0000000000002000
    [Display(Name = "Flashlight"), Category("Misc")] Flashlight = 16384, // 0x0000000000004000
    [Display(Name = "View"), Category("Misc")] View = 32768, // 0x0000000000008000
    [Display(Name = "Zoom"), Category("Misc")] Zoom = 65536, // 0x0000000000010000
    [Display(Name = "Menu"), Category("Menu")] Menu = 131072, // 0x0000000000020000
    [Display(Name = "Scoreboard"), Category("Menu")] Score = 262144, // 0x0000000000040000
    [Display(Name = "Open Chat"), Category("Menu")] Chat = 524288, // 0x0000000000080000
    [Display(Name = "Voice"), Category("Menu")] Voice = 1048576, // 0x0000000000100000
    [Display(Name = "Next Item"), Category("Items")] SlotNext = 2097152, // 0x0000000000200000
    [Display(Name = "Prev Item"), Category("Items")] SlotPrev = 4194304, // 0x0000000000400000
    [Display(Name = "Slot 1"), Category("Items")] Slot1 = 8388608, // 0x0000000000800000
    [Display(Name = "Slot 2"), Category("Items")] Slot2 = 16777216, // 0x0000000001000000
    [Display(Name = "Slot 3"), Category("Items")] Slot3 = 33554432, // 0x0000000002000000
    [Display(Name = "Slot 4"), Category("Items")] Slot4 = 67108864, // 0x0000000004000000
    [Display(Name = "Slot 5"), Category("Items")] Slot5 = 134217728, // 0x0000000008000000
    [Display(Name = "Slot 6"), Category("Items")] Slot6 = 268435456, // 0x0000000010000000
    [Display(Name = "Slot 7"), Category("Items")] Slot7 = 536870912, // 0x0000000020000000
    [Display(Name = "Slot 8"), Category("Items")] Slot8 = 1073741824, // 0x0000000040000000
    [Display(Name = "Slot 9"), Category("Items")] Slot9 = 2147483648, // 0x0000000080000000
    [Display(Name = "Slot 0"), Category("Items")] Slot0 = 4294967296, // 0x0000000100000000
}
