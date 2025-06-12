shader_type canvas_item;

void fragment() {
    vec2 uv = UV;

    // Modify the Y coordinate to bulge upward (sinusoidal curve)
    float curve_strength = 0.05; // Adjust as needed
    uv.y -= sin(uv.x * 3.14159) * curve_strength;

    COLOR = texture(TEXTURE, uv);
}
