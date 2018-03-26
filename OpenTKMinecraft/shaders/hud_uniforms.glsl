#include "common_uniforms.glsl"

#if VERTEX_SHADER
    layout (location = 1) in vec4 position;
    layout (location = 15) uniform vec4 exclusion;
#endif
