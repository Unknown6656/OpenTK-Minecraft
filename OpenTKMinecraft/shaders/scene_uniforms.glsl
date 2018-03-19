#include "common_uniforms.glsl"

layout (location = 1) in vec3 position;
layout (location = 2) in vec3 normal;
layout (location = 3) in vec4 vcolor;
layout (location = 4) in vec3 tangent;
layout (location = 5) in vec3 bitangent;
layout (location = 6) uniform int tex_size;

layout (location = 40) uniform float ambient_brightness;
layout (location = 41) uniform int light_count;

#if FRAGMENT_SHADER
    layout (std140, binding = 1) uniform LightBlock {
        Light lights[MAX_LIGHTS];
    } SceneLights;
#endif
