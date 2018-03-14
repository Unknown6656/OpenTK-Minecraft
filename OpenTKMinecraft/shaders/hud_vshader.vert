#version 460 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 normal;
layout (location = 2) in vec2 texcoord;
layout (location = 3) in vec4 color;

layout (location = 6) uniform bool paused;
layout (location = 7) in float time;
layout (location = 8) uniform float window_width;
layout (location = 9) uniform float window_height;
layout (location = 10) uniform vec3 cam_position;
layout (location = 11) uniform vec3 cam_target;

layout (location = 20) uniform mat4 cam_projection;


out vec3 vs_position;
out vec3 vs_normal;
out vec2 vs_texcoord;
out vec4 vs_color;

void main(void)
{
    gl_Position = position;

    vs_color = color;
    vs_normal = normal;
    vs_position = position;
    vs_texcoord = texcoord;
}
