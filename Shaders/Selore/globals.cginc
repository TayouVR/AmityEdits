// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2025 Tayou <git@tayou.org>
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
#ifndef SELORE_GLOBALS
#define SELORE_GLOBALS

#define SELORE_LIGHT_ROLE_UNKNOWN 0
#define SELORE_LIGHT_ROLE_HOLE 1
#define SELORE_LIGHT_ROLE_RING_TWOWAY 2
#define SELORE_LIGHT_ROLE_RING_ONEWAY 3
#define SELORE_LIGHT_ROLE_NORMAL 4

struct Selore_LightInfo {
	float3 position;
	int role;
};

struct Selore_OrificeData {
	float3 position;
	float3 normal; // The calculated forward direction
	float3 normalLightPosition; // The raw position of the normal light
	int type;
	bool isValid;
};

float Selore_Channel;
			
float Selore_PenetratorEnabled;
float Selore_PenetratorLength;
float3 Selore_StartPosition;
float3 Selore_StartRotation;
float Selore_DeformStrength;
float Selore_AllTheWayThrough;            

float Selore_BezierHandleSize;
float Selore_SplineDebug;

// TODO: see SPS, add blendshape baking feature, as some users may need it.

#endif
