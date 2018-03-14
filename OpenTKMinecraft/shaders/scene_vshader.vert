#version 460 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec4 color;
layout (location = 3) in vec3 tangent;
layout (location = 4) in vec3 bitangent;
layout (location = 5) uniform int tex_size;
layout (location = 6) uniform bool paused;
layout (location = 7) in float time;
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
    gl_Position = projection * model_view * vec4(position, 1);

    vs_normal = normalize((mat_normal * vec4(normal, 0)).xyz);
    vs_tangent = normalize((mat_normal * vec4(tangent, 0)).xyz);
    vs_bitangent = normalize((mat_normal * vec4(bitangent, 0)).xyz);
    vs_TBN = transpose(mat3(
        vs_tangent,
        vs_bitangent,
        vs_normal
    ));
    vs_worldpos = (model_view * vec4(position, 1)).xyz;
    vs_eyedir = normalize(cam_position - vs_worldpos);
    vs_texcoord = color.yx;
    vs_position = position;
    vs_color = color;
    vs_time = time;
}
