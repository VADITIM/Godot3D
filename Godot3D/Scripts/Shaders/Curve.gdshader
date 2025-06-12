shader_type canvas_item;

uniform float strength : hint_range(-0.1, 0.1) = .05;

void fragment() {
    vec2 uv = UV;

    // Convert UV to center-relative coordinates (-1 to 1)
    vec2 centered = uv * 2.0 - 1.0;

    // Apply subtle curve: push pixels outward or inward from the center
    float offset = strength * (centered.x * centered.x + centered.y * centered.y);
    centered *= 1.0 + offset;

    // Convert back to 0-1 UV space
    uv = (centered + 1.0) * 0.5;

    // Sample the texture
    vec4 tex = vec4(0.0);
    if (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 2.0) {
        tex = texture(TEXTURE, uv);
    }

    // Apply color modulation from node/material
    COLOR = tex * COLOR;
}
