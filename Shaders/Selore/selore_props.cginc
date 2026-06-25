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
// This file is injected into the Properties { ... } block of any shader patched by
// SelorePatcher. It declares all Selore_* properties the deform feature needs, plus
// a hidden _Selore_Patched marker the patcher uses to detect already-patched shaders.

[Header(Selore Penetrator Options)]
[Toggle] Selore_PenetratorEnabled ("Penetrator Enabled", Float) = 0
Selore_DeformStrength ("Deform Strength", Range(0,1)) = 1
[Vector3] Selore_StartPosition ("Start Position", Vector) = (0,0,0,0)
Selore_StartRotation ("Start Rotation", Vector) = (0,0,0,0)
Selore_PenetratorLength ("Length", Float) = 0.2
[Enum(Channel 0,0,Channel 1,1)] Selore_Channel ("Channel", Float) = 0
[Toggle] Selore_AllTheWayThrough ("All The Way Through", Float) = 0
[HideInInspector] Selore_BezierHandleSize ("Bezier Handle Size", Range(0.05,0.5)) = 0.15
[HideInInspector] Selore_SplineDebug ("Spline Debug (reference impl only)", Float) = 0
[HideInInspector] _Selore_Patched ("Selore Patched", Float) = 1
