#version 460 core
#include "common_uniforms.glsl"

uniform sampler2D tex;

in float vs_time;
in vec2 uv;

out vec4 color;


void main(void)
{
    color = vec4(uv, 0, 1); // vec4(sin(vs_time * 10) / 2 + 0.5);
}
