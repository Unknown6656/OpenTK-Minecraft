#version 460 core

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

layout (location = 6) uniform bool paused;
layout (location = 8) uniform float window_width;
layout (location = 9) uniform float window_height;
layout (location = 10) uniform vec3 cam_position;
layout (location = 11) uniform vec3 cam_target;
layout (location = 12) uniform float cam_focaldist;

layout (location = 20) uniform mat4 projection;
layout (location = 21) uniform mat4 model_view;
layout (location = 22) uniform mat4 mat_normal;
layout (location = 23) uniform int camera_eye;

layout (location = 30) uniform float ambient_brightness;
layout (location = 31) uniform int light_count;
layout (std140, binding = 1) uniform LightBlock {
    Light lights[MAX_LIGHTS];
} SceneLights;

layout (location = 5) uniform int tex_size;
uniform sampler2D tex;

in float vs_time;
in vec2 vs_texcoord;
in vec3 vs_worldpos;
in vec3 vs_position;
in vec3 vs_bitangent;
in vec3 vs_tangent;
in vec3 vs_normal;
in vec4 vs_color;
in vec3 vs_eyedir;
in mat3 vs_TBN;

out vec4 color;


float lerp(float v1, float v2, float fac)
{
    fac = max(0, min(fac, 0));

    return (1 - fac) * v1 + fac * v2;
}

float map(float value, float l1, float h1, float l2, float h2)
{
    return l2 + (value - l1) * (h2 - l2) / (h1 - l1);
}

vec4 _texture(int type, vec2 uv, int mode)
{
    vec2 coord;

    if (mode == MODE_CLAMP)
        coord = vec2(clamp(uv.x, 0.0, 1.0), clamp(uv.y, 0.0, 1.0));
    else if (mode == MODE_REPEAT)
        coord = vec2(mod(1 + uv.x, 1.0), mod(1 + uv.y, 1.0));
    else if (mode == MODE_MIRROR)
    {
        coord = vec2(mod(2 + uv.x, 2.0), mod(2 + uv.y, 2.0));

        if (coord.x > 1)
            coord.x = 2 - coord.x;
            
        if (coord.y > 1)
            coord.y = 2 - coord.y;
    }
    else if (mode == MODE_ZERO)
        if ((uv.x < 0.0) || (uv.y < 0.0) || (uv.x > 1.0) || (uv.y > 1.0))
            return vec4(0);

    coord.x = (0.5 + int(coord.x * tex_size)) / tex_size;
    coord.y = (0.5 + int(coord.y * tex_size)) / tex_size;
    coord.x /= 4;
    coord.y /= 4;
    coord.x += int(type % 4) * 0.25;
    coord.y += int(type / 4) * 0.25;
    
    return texture(tex, coord);
}

vec4 _flowtex(int type, vec2 uv, FlowInfo nfo, int mode)
{
    vec4 col1 = _texture(type, uv + nfo.flow_uv_1, mode);
    vec4 col2 = _texture(type, uv + nfo.flow_uv_2, mode);

    return lerp(col1, col2, nfo.lerpv);
}

vec3 getlightdir(vec3 p, Light l)
{
    if (l.Mode == LIGHT_DIRECTIONAL)
        return normalize(-l.Direction.xyz);
    else
        return normalize(l.Position.xyz - p);
}

vec3 getrawlightcolor(vec3 p, Light l)
{
    if (l.IsActive != 0)
    {
        if (l.Mode == LIGHT_AMBIENT)
            return l.Color.rgb;
        
        vec3 L = getlightdir(p, l);
        vec3 LD = normalize(l.Direction.xyz);
        float IL = max(dot(L, -LD), 0); // TODO: Light intensity dependending on direction?
        
        if (l.Mode == LIGHT_POINT)
        {
            IL = 1;

            if (l.Falloff > 0)
                IL *= pow(abs(length(p - l.Position.xyz)), -2) / l.Falloff;
        }
        else if (l.Mode == LIGHT_SPOT)
        {
            IL = pow(IL, l.Exponent);
            
            if (l.Falloff > 0)
                IL *= pow(abs(length(p - l.Position.xyz)), -2) / l.Falloff;
        }

        IL *= l.Color.a;
        
        return l.Color.rgb * IL;
    }
    else
        return vec3(0);
}

FlowInfo initflow(float flow_power, float flow_speed)
{
    flow_speed *= vs_time;
    
    vec4 flow = _texture(TEX_FLOW, vs_texcoord, MODE_CLAMP);
    vec2 remap = (flow.rg - vec2(0.5)) * flow_power;
    
    FlowInfo nfo;
    
    nfo.lerpv = abs(2 * (0.5 - mod(flow_speed, 1.0))) * flow.a;
    nfo.flow_uv_1 = remap * mod(flow_speed, 1.0) * flow.a;
    nfo.flow_uv_2 = remap * mod(flow_speed + 0.5, 1.0) * flow.a;

    return FlowInfo;
}

void main(void)
{
    FlowInfo nfo = initflow(-0.5, 1);

    color = _flowtex(TEX_DIFF, vs_texcoord, nfo, MODE_CLAMP);
    return;


    vec4 diffuse = _texture(TEX_DIFF, vs_texcoord, MODE_CLAMP);
    vec4 ambient = _texture(TEX_AMBT, vs_texcoord, MODE_CLAMP);
    vec4 glow = _texture(TEX_GLOW, vs_texcoord, MODE_CLAMP);
    vec3 specular = _texture(TEX_SPEC, vs_texcoord, MODE_CLAMP).rgb;
    vec3 gloss = _texture(TEX_GLSS, vs_texcoord, MODE_CLAMP).rgb * MAX_GLOSS;
    vec3 N = _texture(TEX_NORM, vs_texcoord, MODE_CLAMP).xyz;
    vec3 V = vs_TBN * vs_eyedir;
    vec3 H = normalize(V + N);
    
    vec4 outcolor = ambient * ambient_brightness * diffuse;
    
    for (int i = 0, maxl = min(light_count, MAX_LIGHTS); i < maxl; ++i)
    {
        Light _light = SceneLights.lights[i];
        vec3 light_color = getrawlightcolor(vs_worldpos, _light);

        if (_light.Mode == LIGHT_AMBIENT)
            outcolor += vec4(ambient.rgb * light_color * diffuse.rgb, diffuse.a);
        else
        {
            vec3 L = vs_TBN * getlightdir(vs_worldpos, _light);
            vec3 R = reflect(L, N);
            float LN = max(0, dot(L, N));
            float RV = max(0, dot(R, V));

            vec3 gloss_factor = vec3(
                pow(RV, gloss.r),
                pow(RV, gloss.g),
                pow(RV, gloss.b)
            );
            vec3 contribution = light_color * diffuse.xyz * LN;

            // TODO : glossiness
            // contribution += light_color * specular * gloss_factor;

            outcolor += vec4(contribution * diffuse.a, 0);
        }
    }
    
    color = vec4(outcolor.xyz * (1 - glow.a) + outcolor.xyz * glow.a, outcolor.a + glow.a);

    if (camera_eye == CAM_LEFT)
        color.r = 0;
    else if (camera_eye == CAM_RIGHT)
    {
        color.g = 0;
        color.b = 0;
    }

    if (paused)
    {
        float gray = dot(color.rgb, vec3(0.299, 0.587, 0.114));

        gray += sin(vs_time) / 15;

        color = vec4(gray, gray, gray, color.a);
    }
}
