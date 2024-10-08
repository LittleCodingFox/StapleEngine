#ifndef STAPLE_SHADER_GUARD
#define STAPLE_SHADER_GUARD

#include <bgfx_shader.sh>

#ifdef SKINNING
uniform mat4 u_boneMatrices[255];

mat4 StapleGetSkinningMatrix(vec4 indices, vec4 weights)
{
	return mul(u_model[0], weights.x * u_boneMatrices[int(indices.x)] +
			weights.y * u_boneMatrices[int(indices.y)] +
			weights.z * u_boneMatrices[int(indices.z)] + 
			weights.w * u_boneMatrices[int(indices.w)]);
}
#endif

#endif