# Shader Bindinigs
_This is an internal documentation document_

## `scene_xshader.xxxx`

| Location | Data Type | Content |
|----------|-----------|---------|
| location `0` | `vec3` | Vertex position in object space |
| location `1` | `vec3` | Vertex normal in object space |
| location `2` | `vec4` | Vertex color (rgba) or texture coordinates (rg -> yx) |
| location `3` | `vec3` | Vertex tangent (right-hand-rule `TBN`) in object space |
| location `4` | `vec3` | Vertex bitangent (right-hand-rule `TBN`) in object space |
| location `5` | `int` | Texture size (e.g. `16` for 16x16) |
| location `6` | `bool` | Indicates whether the game is paused |
| location `7` | `float` | Time (in seconds) |
| location `8` | `float` | Host window width (in px, with border) |
| location `9` | `float` | Host window height (in px, with border) |
| location `10`| `vec3` | Camera position in world space |
| location `11`| `vec3` | Camera focus target in world space |
| location `12`| `float` | Camera focal distance (distance between focus target and position) in world space |
| | | |
| location `20`| `mat4` | Projection matrix |
| location `21`| `mat4` | Model-View vertex matrix |
| location `22`| `mat4` | Model-View normal matrix |
| | | |
| location `30`| `float` | Ambient brightness [0..1] |
| location `31`| `int` | Light count |
| binding `1`, std140| `LightBlock::Light[]` | Light data |

## `hud_xshader.xxxx`

| Location | Data Type | Content |
|----------|-----------|---------|
| location `0` | `vec3` | Vertex position |
| location `1` | `vec3` | Vertex normal |
| location `2` | `vec2` | Vertex texture coordinates (uv) |
| location `3` | `vec4` | Vertex color (rgba) |
| | | |
| location `6` | `bool` | Indicates whether the game is paused |
| location `7` | `float` | Time (in seconds) |
| location `8` | `float` | Host window width (in px, with border) |
| location `9` | `float` | Host window height (in px, with border) |
| | | |
| | | |