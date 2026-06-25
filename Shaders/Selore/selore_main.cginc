// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2026 Tayou <git@tayou.org>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
// This file is injected at the end of every CGPROGRAM / HLSLPROGRAM block of any
// shader patched by SelorePatcher. It pulls in the full Selore deform feature and
// exposes selore_apply(...) as the single entrypoint the patcher calls from the
// rewritten vertex function.
#ifndef SELORE_PATCH_MAIN
#define SELORE_PATCH_MAIN

#include "core.cginc"

// Wraps SeloreDeform with the (inout pos, inout normal, inout color) signature the
// patcher injects. Early-exits when the penetrator is disabled, so the cost on
// non-active materials is a single property compare.
void selore_apply(inout float3 pos, inout float3 normal, inout float4 color) {
    if (Selore_PenetratorEnabled < 0.5) return;
    float4 p = float4(pos, 1.0);
    float3 n = normal;
    float4 c = color;
    SeloreDeform(p, n, c);
    pos = p.xyz;
    normal = n;
    // We deliberately do not write back `color`. The reference implementation uses
    // it for spline debug only, and propagating it through arbitrary host shaders
    // is not reliable. Patched shaders therefore ignore the debug visualization.
}

#endif
