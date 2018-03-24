#version 460 core

#include "hud_uniforms.glsl"

uniform sampler2D overlayTexture;

in vec3 vs_position;
in vec2 vs_texcoord;

out vec4 color;


vec2 distort(vec2 uv)
{
    uv = uv * 2 - 1;

    float ar = window_width / window_height;
    float r = pow(length(uv), 0.9) * 1.025;
    float phi = atan2(uv.x, uv.y);

    return (vec2(
        r * sin(phi),
        r * cos(phi)
    ) + 1) * 0.5;
}

void main(void)
{
    float d = clamp(pow(length(vs_texcoord * 2 - 1), 2), 0, 1);

    color = texture(overlayTexture, distort(vs_texcoord) * d + vs_texcoord * (1 - d));
}
