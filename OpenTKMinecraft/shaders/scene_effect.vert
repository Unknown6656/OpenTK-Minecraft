#version 460 core
#include "common_uniforms.glsl"

layout (location = 1) in vec4 position;

out float vs_time;
out float vs_aspectratio;
out vec2 vs_pos;
out vec2 uv;


void main(void)
{
    gl_Position = position;
    
    vs_pos = position.xy;
    vs_aspectratio = window_width / window_height;
    vs_time = time;
    
    uv = vs_pos / 2 + 0.5;
}
