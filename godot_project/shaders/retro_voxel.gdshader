shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_toon, specular_toon;

uniform sampler2D palette_texture : filter_nearest;
uniform float ao_strength : hint_range(0.0, 1.0) = 0.35;
uniform bool xray_mode = false;
uniform vec4 xray_color : source_color = vec4(0.97, 0.45, 0.09, 1.0);

void vertex() {
    // Voxel ambient occlusion pass
}

void fragment() {
    vec4 base_color = COLOR;
    
    if (xray_mode) {
        ALBEDO = xray_color.rgb;
        EMISSION = xray_color.rgb * 1.5;
        ALPHA = 0.85;
    } else {
        ALBEDO = base_color.rgb;
        ROUGHNESS = 0.85;
        SPECULAR = 0.15;
    }
}
