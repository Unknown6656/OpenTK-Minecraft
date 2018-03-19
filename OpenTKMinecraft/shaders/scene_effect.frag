#version 460 core
#include "common_uniforms.glsl"

layout (location = 39 ) uniform int PredefinedEffect;

uniform sampler2D renderedColor;
uniform sampler2D renderedDepth;

in float vs_time;
in float vs_aspectratio;
in vec2 vs_pos;
in vec2 uv;

out vec4 color;

#define CONVOLUTION_DIRECTION 1
#define CONVOLUTION_GRADIENT 2

// compare to  OpenTKMinecraft::Components::PredefinedShaderEffect
#define FX_NONE 0
#define FX_EDGE 1
#define FX_WOBB 2


vec4 convolute(vec2 coord, mat3 H, mat3 V, int mode, bool gray)
{
    vec4 hclr = vec4(0);
    vec4 vclr = vec4(0);

    for (int i = 0; i < 3; ++i)
        for (int j = 0; j < 3; ++j)
        {
            vec2 nc = coord + vec2((i - 1) / window_width, (j - 1) / window_height * vs_aspectratio);

            if ((nc.x < 0) || (nc.y < 0) || (nc.x > 1) || (nc.y > 1))
                continue;

            vec4 px = texture(renderedColor, nc);

            if (gray)
                px = grayscale(px);

            hclr += H[i][j] * px;
            vclr += V[i][j] * px;
        }

    vec4 g = vec4(
        sqrt(hclr.r * hclr.r + vclr.r * vclr.r),
        sqrt(hclr.g * hclr.g + vclr.g * vclr.g),
        sqrt(hclr.b * hclr.b + vclr.b * vclr.b),
        sqrt(hclr.a * hclr.a + vclr.a * vclr.a)
    );
    vec4 d = vec4(
        atan2(hclr.r, vclr.r),
        atan2(hclr.g, vclr.g),
        atan2(hclr.b, vclr.b),
        atan2(hclr.a, vclr.a)
    );
    
    return mode == CONVOLUTION_DIRECTION ? d : g;
}

void applyeffect(int fx)
{
    if (fx == FX_EDGE)
        color = texture(renderedColor, uv) - convolute(uv, mat3( 
            1,  2,  1,
            0,  0,  0,
            -1, -2, -1
        ), mat3( 
            1, 0, -1,
            2, 0, -2,
            1, 0, -1
        ), CONVOLUTION_GRADIENT, true);
    else if (fx == FX_WOBB)
        color = texture(renderedColor, uv + 0.01 * vec2(sin(vs_time + window_width * uv.x / 1.25), cos(vs_time + window_height * uv.y / 1.25)));
    else
        color = texture(renderedColor, uv);
}

void main(void)
{
    applyeffect(PredefinedEffect);

    color.a = 1;

    float dist = length(vs_pos * vec2(vs_aspectratio, 1));

    if (paused)
        color = grayscale(color);

    color *= (1 - dist / (paused ? 2.5 : 5));
}
