#version 460 core
#include "common_uniforms.glsl"

uniform sampler2D renderedColor;
uniform sampler2D renderedDepth;

in float vs_time;
in float vs_aspectratio;
in vec2 vs_pos;
in vec2 uv;

out vec4 color;


void main(void)
{
    color = texture(renderedColor, uv);

    float dist = length(vs_pos * vec2(vs_aspectratio, 1));

    if (paused)
    {
        float gray = dot(color.rgb, vec3(0.299, 0.587, 0.114));

        gray += sin(vs_time) / 15;

        color = vec4(gray, gray, gray, color.a);
    }

    // color *= (1 - dist / 3);
}
