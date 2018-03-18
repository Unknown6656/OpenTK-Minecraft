// compare to  OpenTKMinecraft::Components::TextureType
#define TEX_DIFF 0x0
#define TEX_AMBT 0x1
#define TEX_DISP 0x2
#define TEX_GLOW 0x3
#define TEX_NORM 0x4
#define TEX_GLSS 0x5
#define TEX_SPEC 0x6
#define TEX_SUBS 0x7
#define TEX_REFL 0x8
#define TEX_PARX 0x9
#define TEX_DETL 0xa
#define TEX_FLOW 0xb

// compare to OpenTKMinecraft::Components::CameraStereoMode
#define CAM_NORMAL 0
#define CAM_LEFT 1
#define CAM_RIGHT 2

#define MODE_CLAMP 1
#define MODE_REPEAT 2
#define MODE_MIRROR 3
#define MODE_ZERO 4

#define MAX_GLOSS 255

#define MAX_LIGHTS 256

// compare to OpenTKMinecraft::Components::LightMode
#define LIGHT_AMBIENT 0
#define LIGHT_POINT 1
#define LIGHT_SPOT 2
#define LIGHT_DIRECTIONAL 3

#define FLOW_EPSILON 0.001

struct Light
{
    vec4 Position;
    vec4 Direction;
    vec4 Color;
    float Exponent;
    float Falloff;
    uint Mode;
    uint IsActive;
};
struct FlowInfo
{
    float lerpv;
    vec2 flow_uv_1;
    vec2 flow_uv_2;
};

uniform sampler2D tex;


vec4 lerp(vec4 v1, vec4 v2, float fac)
{
    fac = max(0, min(fac, 0));

    return (1 - fac) * v1 + fac * v2;
}

float map(float value, float l1, float h1, float l2, float h2)
{
    return l2 + (value - l1) * (h2 - l2) / (h1 - l1);
}
