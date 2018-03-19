
#if VERTEX_SHADER
    layout (location = 10) in float time;
#endif

layout (location = 11) uniform float window_width;
layout (location = 12) uniform float window_height;
layout (location = 13) uniform bool paused;

layout (location = 20) uniform vec3 cam_position;
layout (location = 21) uniform vec3 cam_target;
layout (location = 22) uniform float cam_focaldist;
layout (location = 23) uniform int cam_eye;

#if VERTEX_SHADER
    layout (location = 30) uniform mat4 cam_projection;
    layout (location = 31) uniform mat4 cam_modelview;
    layout (location = 32) uniform mat4 cam_normalview;
#endif

#include "common.glsl"
