#version 460 core
#include "common_uniforms.glsl"

uniform sampler2D renderedColor;
uniform sampler2D renderedDepth;

in float vs_time;
in vec2 uv;

out vec4 color;


void main(void)
{
    color = texture(renderedColor, uv / 2); //  + texture(renderedDepth, uv);
}
