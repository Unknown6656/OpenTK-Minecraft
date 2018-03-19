#version 460 core
#include "scene_uniforms.glsl"

out float vs_time;
out vec2 vs_texcoord;
out vec3 vs_worldpos;
out vec3 vs_position;
out vec3 vs_bitangent;
out vec3 vs_tangent;
out vec3 vs_normal;
out vec3 vs_eyedir;
out vec4 vs_color;
out mat3 vs_TBN;


void main(void)
{
    gl_Position = cam_projection * cam_modelview * vec4(position, 1);
    
    vs_normal = normalize((cam_normalview * vec4(normal, 0)).xyz);
    vs_tangent = normalize((cam_normalview * vec4(tangent, 0)).xyz);
    vs_bitangent = normalize((cam_normalview * vec4(bitangent, 0)).xyz);
    vs_TBN = transpose(mat3(
        vs_tangent,
        vs_bitangent,
        vs_normal
    ));
    vs_worldpos = (cam_modelview * vec4(position, 1)).xyz;
    vs_eyedir = normalize(cam_position - vs_worldpos);
    vs_texcoord = vec2(vcolor.y, 1 - vcolor.x);
    vs_position = position;
    vs_color = vcolor;
    vs_time = time;
}
