#version 460 core

layout (location = 2) in float time;
layout (location = 3) uniform float window_width;
layout (location = 4) uniform float window_height;
layout (location = 10) uniform vec3 cam_position;
layout (location = 11) uniform vec3 cam_target;
layout (location = 20) uniform mat4 projection;
layout (location = 21) uniform mat4 model_view;

uniform sampler2D tex_diffuse;

in vec2 vs_texcoord;
out vec4 color;


void main(void)
{
    color = texelFetch(tex_diffuse, ivec2(vs_texcoord.x, vs_texcoord.y), 0);
}
