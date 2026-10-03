@tool
extends GPUParticles2D
@export_tool_button("Check pixel offset", "CanvasItem") var ClickMeButton = CheckPixelOffsetter
@export var drawEmissionArea : bool = true:
	get: return drawEmissionArea
	set(value):
		drawEmissionArea = value
		queue_redraw()

@export var emissionAreaColor : Color = Color(0.8, 0.0, 0.6, 0.5):
	get: return emissionAreaColor
	set(value):
		emissionAreaColor = value
		queue_redraw()

@export var direction : Vector2 = Vector2.UP:
	get: return direction
	set(value):
		direction = value
		CheckMat()
		processMat.set_shader_parameter("direction", Vec2To3(direction))

@export var emissionArea : Vector2 = Vector2.ONE:
	get: return emissionArea
	set(value):
		emissionArea = Vector2(clamp(value.x, 0.0, 9999.0), clamp(value.y, 0.0, 9999.0))
		CheckMat()
		processMat.set_shader_parameter("emission_box_extents", Vec2To3(emissionArea / 2.0))
		queue_redraw()

@export var emissionVelocity : Vector2:
	get: return emissionVelocity
	set(value):
		if (value.x > emissionVelocity.y):
			value = Vector2(value.x, value.x)
		if (value.y < emissionVelocity.x):
			value = Vector2(value.y, value.y)
		emissionVelocity = value
		CheckMat()
		processMat.set_shader_parameter("initial_linear_velocity_min", emissionVelocity.x)
		processMat.set_shader_parameter("initial_linear_velocity_max", emissionVelocity.y)

@export_range(0.0, 360.0, 1.0, "suffix: degrees") var spread : float = 0.0:
	get: return spread
	set(value):
		spread = value
		CheckMat()
		processMat.set_shader_parameter("spread", spread / 2.0)

@export var alphaCurve : Curve:
	get: return alphaCurve
	set(value):
		alphaCurve = value
		CheckMat()
		var curveTex = CurveTexture.new()
		curveTex.curve = alphaCurve
		processMat.set_shader_parameter("alpha_curve", curveTex if curveTex != null else null)

@export var animOffset : Vector2:
	get: return animOffset
	set(value):
		value = Vector2(clamp(value.x, 0.0, 1.0), clamp(value.y, 0.0, 1.0))
		if (value.x > animOffset.y):
			value = Vector2(value.x, value.x)
		if (value.y < animOffset.x):
			value = Vector2(value.y, value.y)
		animOffset = value
		CheckMat()
		processMat.set_shader_parameter("anim_offset_min", animOffset.x)
		processMat.set_shader_parameter("anim_offset_max", animOffset.y)

@export var animSpeed : Vector2:
	get: return animSpeed
	set(value):
		value = Vector2(clamp(value.x, 0.0, 9999.0), clamp(value.y, 0.0, 9999.0))
		if (value.x > animSpeed.y):
			value = Vector2(value.x, value.x)
		if (value.y < animSpeed.x):
			value = Vector2(value.y, value.y)
		animSpeed = value
		CheckMat()
		processMat.set_shader_parameter("anim_speed_min", animSpeed.x)
		processMat.set_shader_parameter("anim_speed_max", animSpeed.y)

@export var gravity : Vector2 = Vector2.ZERO:
	get: return gravity
	set(value):
		gravity = value
		CheckMat()
		processMat.set_shader_parameter("gravity", gravity)

var processMat : ShaderMaterial
@export var emissionAreaTex : Texture2D = load("res://addons/PPP/Emission_bounds.png")

func Vec2To3(input : Vector2):
	return Vector3(input.x, input.y, 0.0)

# Called when the node enters the scene tree for the first time.
func _ready():
	if (Engine.is_editor_hint()):
		if (process_material == null):
			processMat = load("res://addons/PPP/PPPProcessMat.tres").duplicate()
			process_material = processMat

		interpolate = false

		if (emissionAreaTex == null):
			emissionAreaTex = load("res://addons/PPP/Emission_bounds.png")
	else:
		processMat = process_material

	CheckPixelOffsetter()

func _notification(what):
	if (what == NOTIFICATION_DRAW):
		SetDrawEmissionArea()
	elif (what == NOTIFICATION_TRANSFORM_CHANGED):
		queue_redraw()

func CheckPixelOffsetter():
	if (processMat != null):
		var frames = GetFrames()
		var textureSize = texture.get_size() if texture != null else Vector2.ONE
		var x = 0 if fmod(textureSize.x / frames.x, 2.0) == 0.0 else 1
		var y = 0 if fmod(textureSize.y / frames.y, 2.0) == 0.0 else 1
		processMat.set_shader_parameter("useHalfShiftX", x)
		processMat.set_shader_parameter("useHalfShiftY", y)

func SetDrawEmissionArea():
	if (emissionAreaTex != null && drawEmissionArea && Engine.is_editor_hint()):
		var rectRID = get_canvas_item()
		var offset : Rect2 = Rect2(Vector2.ZERO, Vector2.ZERO)
		var posAndSize : Rect2 = Rect2(-emissionArea / 2.0, emissionArea)

		var frames = GetFrames()

		RenderingServer.canvas_item_add_set_transform(rectRID, Transform2D(Vector2.RIGHT * frames.x, Vector2.DOWN * frames.y, Vector2.ZERO))
		RenderingServer.canvas_item_set_default_texture_filter(rectRID, RenderingServer.CanvasItemTextureFilter.CANVAS_ITEM_TEXTURE_FILTER_NEAREST)
		RenderingServer.canvas_item_add_nine_patch(rectRID, posAndSize, offset, emissionAreaTex.get_rid(), Vector2.ONE, Vector2.ONE, RenderingServer.NINE_PATCH_TILE, RenderingServer.NINE_PATCH_TILE, true, emissionAreaColor)

func GetFrames():
	var frames := Vector2.ONE
	if (material is CanvasItemMaterial):
		frames = Vector2(material.particles_anim_h_frames, material.particles_anim_v_frames)
	return frames

func CheckMat():
	if (processMat == null):
		processMat = process_material
