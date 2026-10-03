 using Godot;
using System;

[Tool]
public partial class PixelPerfectParticles : GpuParticles2D
{
	[ExportToolButton("Check pixel offset", Icon = "CanvasItem")]
	public Callable ClickMeButton => Callable.From(CheckPixelOffsetter);

	[Export] bool DrawEmissionArea
	{
		get { return drawEmissionArea; }
		set
		{
			drawEmissionArea = value;
			QueueRedraw();
		}
	}
	bool drawEmissionArea = true;

	[Export] Color EmissionAreaColor
	{
		get {return emissionAreaColor; }
		set
		{
			emissionAreaColor = value;
			QueueRedraw();
		}
	}
	Color emissionAreaColor = new(0.8f, 0.0f, 0.6f, 0.5f);

	//direction (default up -1)
	[Export] public Vector2 Direction
	{
		get { return direction; }
		set
		{
			direction = value;
			CheckMat();
			processMat?.SetShaderParameter("direction", Vec2To3(direction) );
		}	
	}
	Vector2 direction = Vector2.Up;

	//emission size (divide by two to get total width, not radius)
	[Export] public Vector2 EmissionArea
	{
		get { return emissionArea; } 
		set
		{
			emissionArea = new(Mathf.Clamp(value.X, 0f, 9999f), Mathf.Clamp(value.Y, 0f, 9999f));
			CheckMat();
			processMat?.SetShaderParameter("emission_box_extents", Vec2To3(emissionArea / 2.0f));
			QueueRedraw();
		}
	}
	Vector2 emissionArea = Vector2.One;

	//intial velocity (min max)
	[Export] public Vector2 EmissionVelocity
	{
		get { return emissionVelocity; }
		set
		{
			if(value.X > emissionVelocity.Y)
				value = new(value.X, value.X);
			if (value.Y < emissionVelocity.X)
				value = new(value.Y, value.Y);
			emissionVelocity = value;
			CheckMat();
			processMat?.SetShaderParameter("initial_linear_velocity_min", emissionVelocity.X);
			processMat?.SetShaderParameter("initial_linear_velocity_max", emissionVelocity.Y);
		}
	}
	Vector2 emissionVelocity;

	//angle (max)
	[Export(PropertyHint.Range, "0.0,360.0,1.0,suffix: degrees")]
	public float Spread
	{
		get { return spread; }
		set
		{
			spread = value;
			CheckMat();
			processMat?.SetShaderParameter("spread", spread/2.0f);
		}
	}
	float spread;

	//animation curve
	[Export] public Curve AlphaCurve
	{
		get { return alphaCurve; }
		set
		{
			alphaCurve = value;
			CheckMat();
			CurveTexture curveTex = new() { Curve = alphaCurve };
			processMat?.SetShaderParameter("alpha_curve", alphaCurve == null ? null : curveTex);
		}
	}
	Curve alphaCurve;

	//animation offset (min max)
	[Export] public Vector2 AnimOffset
	{
		get { return animOffset; }
		set
		{
			value = new(Mathf.Clamp(value.X, 0f, 1f), Mathf.Clamp(value.Y, 0f, 1f));
			if (value.X > animOffset.Y)
				value = new(value.X, value.X);
			if (value.Y < animOffset.X)
				value = new(value.Y, value.Y);
			animOffset = value;
			CheckMat();
			processMat?.SetShaderParameter("anim_offset_min", animSpeed.X);
			processMat?.SetShaderParameter("anim_offset_max", animSpeed.Y);
		}
	}
	Vector2 animOffset;

	//animation speed (min max)
	[Export] public Vector2 AnimSpeed
	{
		get { return animSpeed; }
		set
		{
			value = new(Mathf.Clamp(value.X, 0f, 9999f), Mathf.Clamp(value.Y, 0f, 9999f));
			if (value.X > animSpeed.Y)
				value = new(value.X, value.X);
			if (value.Y < animSpeed.X)
				value = new(value.Y, value.Y);
			animSpeed = value;
			CheckMat();
			processMat?.SetShaderParameter("anim_speed_min", animSpeed.X);
			processMat?.SetShaderParameter("anim_speed_max", animSpeed.Y);
		}
	}
	Vector2 animSpeed;
	
	//gravity
	[Export] public Vector2 Gravity
	{
		get { return gravity; }
		set
		{
			gravity = value;
			CheckMat();
			processMat?.SetShaderParameter("gravity", Vec2To3(gravity));
		}
	}
	Vector2 gravity;

	ShaderMaterial processMat;

	Vector3 Vec2To3(Vector2 input)
	{
		return new(input.X, input.Y, 0.0f);
	}

	[Export] Texture2D emissionAreaTex = GD.Load<Texture2D>("res://addons/PPP/Emission_bounds.png");
	Rid invalidRID;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			if (ProcessMaterial == null)
			{
				processMat = (ShaderMaterial)GD.Load<ShaderMaterial>("res://addons/PPP/PPPProcessMat.tres").Duplicate();
				ProcessMaterial = processMat;
			}
			Interpolate = false;

			emissionAreaTex ??= GD.Load<Texture2D>("res://addons/PPP/Emission_bounds.png");
		}
		else
		{
			processMat = ProcessMaterial as ShaderMaterial;
		}

		CheckPixelOffsetter();
	}

	void CheckPixelOffsetter()
	{
		if (processMat != null)
		{
			Vector2 frames = GetFrames();
			Vector2 textureSize = Texture != null ? Texture.GetSize() : Vector2.One;
			int x = textureSize.X / frames.X % 2 == 0 ? 0 : 1;
			int y = textureSize.Y / frames.Y % 2 == 0 ? 0 : 1;
			processMat?.SetShaderParameter("useHalfShiftX", x);
			processMat?.SetShaderParameter("useHalfShiftY", y);
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDraw)
		{
			SetDraw();
		}
		else if (what == NotificationTransformChanged)
		{
			QueueRedraw();
		}
	}

	void SetDraw()
	{
		if (emissionAreaTex != null && drawEmissionArea && Engine.IsEditorHint())
		{
			Rid rectRID = GetCanvasItem();
			Rect2 offset = new(Vector2.Zero, Vector2.Zero);
			Rect2 posAndSize = new(-emissionArea / 2f, emissionArea);

			Vector2I frames = GetFrames();

			if (rectRID != invalidRID)
			{
				RenderingServer.CanvasItemAddSetTransform(rectRID, new Transform2D(Vector2.Right * frames.X, Vector2.Down * frames.Y, Vector2.Zero));
				RenderingServer.CanvasItemSetDefaultTextureFilter(rectRID, RenderingServer.CanvasItemTextureFilter.Nearest);
				RenderingServer.CanvasItemAddNinePatch(rectRID, posAndSize, offset, emissionAreaTex.GetRid(),
							new Vector2(1, 1), //left top
							new Vector2(1, 1), //right bottom
							RenderingServer.NinePatchAxisMode.Tile, RenderingServer.NinePatchAxisMode.Tile, true, emissionAreaColor);
			}
			else
			{
				GD.PrintErr(Name + " had invalid RID when trying to draw nine slice sprite");
			}
		}
	}

	Vector2I GetFrames()
	{
		Vector2I frames = new(1, 1);
		if (Material is CanvasItemMaterial itemMat)
		{
			frames.X = itemMat.ParticlesAnimHFrames;
			frames.Y = itemMat.ParticlesAnimVFrames;
		}
		return frames;
	}

	void CheckMat()
	{
		processMat ??= (ShaderMaterial)ProcessMaterial;
	}
}
