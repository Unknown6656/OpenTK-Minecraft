#version 460 core
#include "hud_uniforms.glsl"

out vec3 vs_position;
out vec3 vs_normal;
out vec2 vs_texcoord;
out vec4 vs_color;

void main(void)
{
    gl_Position = position;

    vs_color = color;
    vs_normal = normal.xyz;
    vs_position = position.xyz;
    vs_texcoord = texcoord;
}
