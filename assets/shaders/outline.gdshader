shader_type canvas_item;

// Draws a solid outline around the opaque pixels of a sprite. The sprite needs
// at least `thickness` transparent pixels of margin around the artwork.
uniform vec4 line_color : source_color = vec4(1.0, 0.9, 0.4, 1.0);
uniform float thickness : hint_range(0.0, 8.0) = 2.0;

void fragment() {
	vec4 base = texture(TEXTURE, UV);
	vec2 step = TEXTURE_PIXEL_SIZE * thickness;

	float neighbour = 0.0;
	neighbour += texture(TEXTURE, UV + vec2(step.x, 0.0)).a;
	neighbour += texture(TEXTURE, UV - vec2(step.x, 0.0)).a;
	neighbour += texture(TEXTURE, UV + vec2(0.0, step.y)).a;
	neighbour += texture(TEXTURE, UV - vec2(0.0, step.y)).a;
	neighbour += texture(TEXTURE, UV + step).a;
	neighbour += texture(TEXTURE, UV - step).a;
	neighbour += texture(TEXTURE, UV + vec2(step.x, -step.y)).a;
	neighbour += texture(TEXTURE, UV + vec2(-step.x, step.y)).a;

	float edge = clamp(neighbour, 0.0, 1.0) * (1.0 - base.a);
	COLOR = mix(base, line_color, edge);
	COLOR.a = max(base.a, edge * line_color.a);
}
