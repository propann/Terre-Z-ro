shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_toon, specular_toon;

uniform float wetness : hint_range(0.0, 1.0) = 0.0;
uniform float snow_cover : hint_range(0.0, 1.0) = 0.0;
uniform bool xray_mode = false;
uniform vec4 xray_color : source_color = vec4(0.97, 0.45, 0.09, 1.0);

void fragment() {
    vec3 base = COLOR.rgb;
    float wet = clamp(wetness, 0.0, 1.0);

    vec3 weathered = mix(base, base * 0.72, wet);
    float snow = clamp(snow_cover, 0.0, 1.0);
    vec3 snowy = mix(
        weathered,
        vec3(0.78, 0.82, 0.84),
        snow * 0.58
    );

    if (xray_mode) {
        vec3 scanned = mix(snowy, xray_color.rgb, 0.72);
        ALBEDO = scanned;
        EMISSION = xray_color.rgb * 0.65;
        ROUGHNESS = 0.22;
        METALLIC = 0.28;
        SPECULAR = 0.65;
    } else {
        ALBEDO = snowy;
        ROUGHNESS = mix(
            mix(0.82, 0.24, wet),
            0.90,
            snow
        );
        METALLIC = mix(0.08, 0.14, wet);
        SPECULAR = mix(0.20, 0.62, wet);
    }
}
