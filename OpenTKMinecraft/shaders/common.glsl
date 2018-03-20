
const float PI = 3.14159265359;


float atan2(in float y, in float x)
{
    bool s = (abs(x) > abs(y));

    return mix(PI / 2.0 - atan(x, y), atan(y, x), s);
}

vec4 lerp(vec4 v1, vec4 v2, float fac)
{
    fac = max(0, min(fac, 0));

    return (1 - fac) * v1 + fac * v2;
}

float map(float value, float l1, float h1, float l2, float h2)
{
    return l2 + (value - l1) * (h2 - l2) / (h1 - l1);
}

vec3 grayscale(vec3 col)
{
    float gray = dot(col, vec3(0.299, 0.587, 0.114));
    
    return vec3(gray);
}

vec4 grayscale(vec4 col)
{
    return vec4(grayscale(col.rgb), col.a);
}

float clamp(float value, float min, float max)
{
    return value < min ? min : value > max ? max : value;
}
