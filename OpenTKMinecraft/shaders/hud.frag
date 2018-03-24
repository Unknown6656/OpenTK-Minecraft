#version 460 core

uniform sampler2D overlayTexture;

in vec3 vs_position;
in vec2 vs_texcoord;

out vec4 color;


void main(void)
{
    color = texture(overlayTexture, vs_texcoord);
}
