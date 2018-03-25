#version 460 core
#include "hud_uniforms.glsl"

out vec3 vs_position;
out vec2 vs_texcoord;
out float vs_time;
out vec4 vs_excl;

void main(void)
{
    gl_Position = position;

    vs_time = time;
    vs_position = position.xyz;
    vs_texcoord = vec2(position.x / 2 + 0.5, 0.5 - position.y / 2);
    vs_excl = vec4(
        exclusion.x / window_width,
        exclusion.y / window_height,
        (exclusion.x + exclusion.z) / window_width,
        (exclusion.y + exclusion.z) / window_height
    );
}
