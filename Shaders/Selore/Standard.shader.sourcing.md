# Standard.shader.orig / StandardSpecular.shader.orig

The Selore shader patcher supports patching Unity's built-in **Standard** and
**Standard (Specular setup)** shaders, but Unity does not ship the source of
those shaders inside projects. These are bundled with Selore.

This serves as documentation on obtaining them if you don't have them or need 
to update them to a different Unity version.

## How to obtain them

1. Download the built-in shader source for your exact Unity version from
   <https://unity.com/releases/editor/archive> (the "Built in shaders" download
   next to your editor version).
2. Inside the archive, find `DefaultResourcesExtra/Standard.shader` and
   `DefaultResourcesExtra/StandardSpecular.shader`.
3. Copy them next to this file and rename them to:
    - `Standard.shader.orig`
    - `StandardSpecular.shader.orig`

The `.orig` extension prevents Unity from treating them as active shaders.

When both files are present the patcher will use them automatically; otherwise
patching a Standard material will throw a clear error and skip that material.
