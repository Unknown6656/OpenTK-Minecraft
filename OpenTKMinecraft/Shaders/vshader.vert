#version 460 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec2 texcoord;
layout (location = 2) in float time;
layout (location = 3) uniform float window_width;
layout (location = 4) uniform float window_height;
layout (location = 10) uniform vec3 cam_position;
layout (location = 11) uniform vec3 cam_target;
layout (location = 20) uniform mat4 projection;
layout (location = 21) uniform mat4 model_view;

out vec2 vs_texcoord;


void main(void)
{
    gl_Position = projection * model_view * position;
    vs_texcoord = texcoord + vec2(time);
}
