#version 460 core
#include "common_uniforms.glsl"

layout (location = 1) in vec4 position;

uniform sampler2D tex;

out float vs_time;
out vec2 uv;


void main(void)
{
    gl_Position = position;

    uv = position.xy;
    vs_time = time;
}
