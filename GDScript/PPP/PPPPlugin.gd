@tool
extends EditorPlugin

func _enter_tree():
	# Initialization of the plugin goes here.
	add_custom_type("Pixel-particle-2D", "GPUParticles2D", load("res://addons/PPP/PixelPerfectParticles.gd"), load("res://addons/PPP/pixel_particle_icon.png"))
	pass


func _exit_tree():
	# Clean-up of the plugin goes here.
	remove_custom_type("Pixel-particle-2D")
	pass
