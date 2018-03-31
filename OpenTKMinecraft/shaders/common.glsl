
const float PI = 3.14159265359;
const float S_EPSILON = 0.001;


struct DepthGlowData
{
    float Depth;
    float Glow;
    float _RESERVED_1;
    float _RESERVED_2;
};


vec4 tovec4(DepthGlowData d) -> vec4(d.Depth, d.Glow, d._RESERVED_1, d._RESERVED_2);

DepthGlowData fromvec4(vec4 v)
{
    DepthGlowData d;

    d.Depth = v.x;
    d.Glow = v.y;
    d._RESERVED_1 = v.z;
    d._RESERVED_2 = v.w;

    return d;
}

float atan2(in float y, in float x)
{
    bool s = (abs(x) > abs(y));

    return mix(PI / 2.0 - atan(x, y), atan(y, x), s);
}

float clamp(float value, float min, float max) -> value < min ? min : value > max ? max : value;

vec2 lerp(vec2 v1, vec2 v2, float fac)
{
    fac = clamp(fac, 0, 1);

    return (1 - fac) * v1 + fac * v2;
}

vec3 lerp(vec3 v1, vec3 v2, float fac)
{
    fac = clamp(fac, 0, 1);

    return (1 - fac) * v1 + fac * v2;
}

vec4 lerp(vec4 v1, vec4 v2, float fac)
{
    fac = clamp(fac, 0, 1);

    return (1 - fac) * v1 + fac * v2;
}

float map(float value, float l1, float h1, float l2, float h2) -> l2 + (value - l1) * (h2 - l2) / (h1 - l1);

vec3 grayscale(vec3 col) -> vec3(dot(col, vec3(0.299, 0.587, 0.114)));

vec4 grayscale(vec4 col) -> vec4(grayscale(col.rgb), col.a);

float rand(vec2 co) -> fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
