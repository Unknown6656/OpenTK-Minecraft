﻿#version 460 core
#include "hud_uniforms.glsl"

uniform sampler2D overlayTexture;

in vec3 vs_position;
in vec2 vs_texcoord;
in float vs_time;
in vec4 vs_excl;

out vec4 color;


vec2 distort(vec2 uv, float amount)
{
    uv = uv * 2 - 1;

    float ar = window_width / window_height;
    float r = pow(length(uv), 0.9 / amount) * 1.025 * amount;
    float phi = atan2(uv.x, uv.y);

    return (vec2(
        r * sin(phi),
        r * cos(phi)
    ) + 1) * 0.5;
}

vec2 distort(vec2 uv) -> distort(uv, 1);

void main(void)
{
    if (!in_use)
    {
        color = texture(overlayTexture, vs_texcoord);

        return;
    }

    float d = clamp(pow(length(vs_texcoord * 2 - 1), 2), 0, 1);
    bool inwindow = (vs_texcoord.x >= vs_excl.x) && (vs_texcoord.y >= vs_excl.y) && (vs_texcoord.x <= vs_excl.z) && (vs_texcoord.y <= vs_excl.w);
    vec2 coord = lerp(vs_texcoord, distort(vs_texcoord), d);
    vec4 orgclr = texture(overlayTexture, coord);
    
    if (paused)
    {
        vec2 offs = vec2(5 / window_width, 0);
        vec4 c1 = texture(overlayTexture, coord - offs);
        vec4 c3 = texture(overlayTexture, coord + offs);
        vec2 uv = vs_texcoord;

        uv.x /= 1000;
        uv = uv * vec2(window_width / 100, window_height / 10) + rand(uv * 0.002);

        float f = tan(uv.y * window_width + vs_time * 4 + sin(uv.x) / 1000);

        color = lerp(orgclr, vec4(
            c1.r,
            orgclr.g,
            c3.b,
            c1.a + orgclr.a + c3.b
        ), 0.1 + d + f) * 0.65 + clamp(1 - f, 0, 1) * 0.1;
        
        if (inwindow)
        {
            color = lerp(color, texture(overlayTexture, vs_texcoord), 0.9);
            color.a *= 0.8;
        }
    }
    else
        color = inwindow ? texture(overlayTexture, vs_texcoord) : orgclr;
}
