#version 460 core
#include "scene_common.glsl"
#include "scene_uniforms.glsl"

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


vec4 _texture(int type, vec2 uv, int mode)
{
    vec2 coord;

    if (mode == MODE_CLAMP)
        coord = vec2(clamp(uv.x, 0.0, 1.0), clamp(uv.y, 0.0, 1.0));
    else if (mode == MODE_REPEAT)
        coord = vec2(mod(2 + uv.x, 1.0), mod(2 + uv.y, 1.0));
    else if (mode == MODE_MIRROR)
    {
        coord = vec2(mod(4 + uv.x, 2.0), mod(4 + uv.y, 2.0));

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
    float val = nfo.lerpv;

    if (val < FLOW_EPSILON)
        return _texture(type, uv, mode);
    else
    {
        vec4 col1 = _texture(type, uv + nfo.flow_uv_1, mode);
        vec4 col2 = _texture(type, uv + nfo.flow_uv_2, mode);

        return lerp(col1, col2, val);
    }
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
    
    vec4 flow = _texture(TEX_FLOW, vs_texcoord, MODE_REPEAT);
    vec2 remap = (flow.rg - vec2(0.5)) * flow_power;
    
    FlowInfo nfo;
    
    nfo.lerpv = abs(2 * (0.5 - mod(flow_speed, 1.0))) * flow.a;
    nfo.flow_uv_1 = remap * mod(flow_speed, 1.0) * flow.a;
    nfo.flow_uv_2 = remap * mod(flow_speed + 0.5, 1.0) * flow.a;

    return nfo;
}

void main(void)
{
    FlowInfo nfo = initflow(-0.5, 1);

    int texmode = nfo.lerpv < FLOW_EPSILON ? MODE_CLAMP : MODE_REPEAT;
    vec4 diffuse = _flowtex(TEX_DIFF, vs_texcoord, nfo, texmode);
    vec4 ambient = _flowtex(TEX_AMBT, vs_texcoord, nfo, texmode);
    vec4 glow = _flowtex(TEX_GLOW, vs_texcoord, nfo, texmode);
    vec3 specular = _flowtex(TEX_SPEC, vs_texcoord, nfo, texmode).rgb;
    vec3 gloss = _flowtex(TEX_GLSS, vs_texcoord, nfo, texmode).rgb * MAX_GLOSS;
    vec3 N = _flowtex(TEX_NORM, vs_texcoord, nfo, texmode).xyz;
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

    if (cam_eye == CAM_LEFT)
        color.r = 0;
    else if (cam_eye == CAM_RIGHT)
    {
        color.g = 0;
        color.b = 0;
    }
}
