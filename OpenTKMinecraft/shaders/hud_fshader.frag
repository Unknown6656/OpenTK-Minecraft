#version 460 core

in vec3 vs_position;
in vec3 vs_normal;
in vec2 vs_texcoord;
in vec4 vs_color;

out vec4 color;


void main(void)
{
    color = vs_color;
}
