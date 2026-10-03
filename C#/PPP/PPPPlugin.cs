#if TOOLS
using Godot;
using System;

[Tool]
public partial class PPPPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		AddCustomType("Pixel-particle-2D", "GPUParticles2D", GD.Load<CSharpScript>("res://addons/PPP/PixelPerfectParticles.cs"),
		GD.Load<Texture2D>("res://addons/PPP/pixel_particle_icon.png"));
	}

	public override void _ExitTree()
	{
		RemoveCustomType("Pixel-particle-2D");
	}
}
#endif
