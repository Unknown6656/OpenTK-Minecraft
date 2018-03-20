#version 460 core
#include "common_uniforms.glsl"

layout (location = 39) uniform int PredefinedEffect;

uniform sampler2D renderedColor;
uniform sampler2D renderedDepth;
uniform sampler2D renderedEffectiveDepth;

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
#define FX_DEPTH 3


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

vec4 getcolor(vec2 coord, int fx)
{
    if (fx == FX_EDGE)
        return texture(renderedColor, coord) - convolute(coord, mat3( 
            1,  2,  1,
            0,  0,  0,
            -1, -2, -1
        ), mat3( 
            1, 0, -1,
            2, 0, -2,
            1, 0, -1
        ), CONVOLUTION_GRADIENT, true);
    else if (fx == FX_WOBB)
        return texture(renderedColor, coord + 0.01 * vec2(sin(vs_time + window_width * coord.x / 1.25), cos(vs_time + window_height * coord.y / 1.25)));
    else if (fx == FX_DEPTH)
        return texture(renderedDepth, coord);
    else
        return texture(renderedColor, coord);
}

void main(void)
{
    color = vec4(0);

    float dist = clamp(length(vs_pos * vec2(vs_aspectratio, 1)), 0, 1);
    float fog = clamp(pow(dist / 1.1, 2) + 0.2 - texture(renderedDepth, uv).r, 0, 1);





    vec2 tcoord = uv - 0.5;
    vec4 sum = vec4(0);

    for (int i = 0; i < 12; ++i)
    {
        float sc = 1 - 0.1 * (i / 11.0);

        sum += getcolor(tcoord * sc + 0.5, PredefinedEffect);
    }

    sum /= 12;
    color = (sum * fog) + (getcolor(uv, PredefinedEffect) * (1 - fog));



    if (paused)
        color = grayscale(color);
    
    color *= (1 - dist / (paused ? 1.5 : 3));
    color.a = 1;
}
