#ifndef SELORE_LIGHTS
#define SELORE_LIGHTS

#include <HLSLSupport.cginc>
#include <UnityShaderVariables.cginc>

#include "globals.cginc"

// Helper to extract range from Unity light attenuation
float GetCalculatedRange(int i) {
    return (0.005 * sqrt(1000000 - unity_4LightAtten0[i])) / sqrt(unity_4LightAtten0[i]);
}

// Determine the role of a light based on its range and the selected channel
int GetLightRole(float range, int channel) {
    float dec = fmod(range, 0.1);
    float eps = 0.004; // Tolerance for float comparison

    // Target decimal values based on channel
    // Channel 0: Hole .01, Ring .02, Normal .05
    // Channel 1: Hole .03, Ring .04, Normal .06
    float targetHole = (channel == 0) ? 0.01 : 0.03;
    float targetRing = (channel == 0) ? 0.02 : 0.04;
    float targetNormal = (channel == 0) ? 0.05 : 0.06;

    if (abs(dec - targetHole) < eps) return SELORE_LIGHT_ROLE_HOLE;
    if (abs(dec - targetNormal) < eps) return SELORE_LIGHT_ROLE_NORMAL;

    // Ring Check (includes subtype logic)
    // Check basic ring ID
    if (abs(dec - targetRing) < eps) return SELORE_LIGHT_ROLE_RING_TWOWAY;

    // Check one-way ring ID (base + 0.005, e.g., #.#25)
    if (abs(dec - (targetRing + 0.005)) < eps) return SELORE_LIGHT_ROLE_RING_ONEWAY;

    return SELORE_LIGHT_ROLE_UNKNOWN;
}

// Scans all 4 vertex lights and categorizes them
void ScanLights(int channel, out Selore_LightInfo lights[4]) {
    [unroll]
    for (int i = 0; i < 4; i++) {
        lights[i].role = SELORE_LIGHT_ROLE_UNKNOWN;
        lights[i].position = float3(0, 0, 0);

        // Check brightness threshold (< 0.01 on RGB)
        if (length(unity_LightColor[i].rgb) < 0.01) {
            float range = GetCalculatedRange(i);
            lights[i].role = GetLightRole(range, channel);

            float4 posWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
            lights[i].position = mul(unity_WorldToObject, posWorld).xyz;
        }
    }
}

// Main logic: Finds up to two valid orifices by pairing holes/rings with normal lights
void GetOrifices(int channel, float3 startPos, out Selore_OrificeData o1, out Selore_OrificeData o2) {
    Selore_LightInfo lights[4];
    ScanLights(channel, lights);

    // Initialize defaults
    o1.isValid = false;
    o1.type = SELORE_LIGHT_ROLE_UNKNOWN;
    o1.position = float3(0, 0, 0);
    o1.normal = float3(0, 0, 1);
    o1.normalLightPosition = float3(0, 0, 0);
    o2.isValid = false;
    o2.type = SELORE_LIGHT_ROLE_UNKNOWN;
    o2.position = float3(0, 0, 0);
    o2.normal = float3(0, 0, 1);
    o2.normalLightPosition = float3(0, 0, 0);

    int foundCount = 0;
    float maxDist = 0.1; // Maximum distance between orifice light and normal light

    [unroll]
    for (int i = 0; i < 4; i++) {
        int role = lights[i].role;

        // If we found a potential orifice light
        if (role == SELORE_LIGHT_ROLE_HOLE || role == SELORE_LIGHT_ROLE_RING_TWOWAY || role == SELORE_LIGHT_ROLE_RING_ONEWAY) {
            // Search for a nearby Normal light to pair with
            int bestNormal = -1;
            float minDist = 100.0;

            [unroll]
            for (int j = 0; j < 4; j++) {
                if (lights[j].role == SELORE_LIGHT_ROLE_NORMAL) {
                    float d = distance(lights[i].position, lights[j].position);
                    if (d < maxDist && d < minDist) {
                        minDist = d;
                        bestNormal = j;
                    }
                }
            }

            // If a pair was found
            if (bestNormal != -1) {
                Selore_OrificeData res;
                res.isValid = true;
                res.type = role;
                res.position = lights[i].position;
                res.normalLightPosition = lights[bestNormal].position;
                // Calculate direction: From Orifice Position -> Normal Position
                res.normal = -normalize(lights[i].position - lights[bestNormal].position);
                if (role == SELORE_LIGHT_ROLE_RING_TWOWAY
                    && distance(startPos, res.position) < distance(startPos, res.normalLightPosition)) {
                    res.normal = normalize(lights[i].position - lights[bestNormal].position);
                }

                if (foundCount == 0) {
                    o1 = res;
                    foundCount++;
                }
                else if (foundCount == 1) {
                    o2 = res;
                    foundCount++;
                }
            }
        }
        if (foundCount >= 2) break;
    }

    // Sort based on distance to startPos
    if (o1.isValid && o2.isValid) {
        if (distance(startPos, o1.position) > distance(startPos, o2.position)) {
            Selore_OrificeData temp = o1;
            o1 = o2;
            o2 = temp;
        }
    }
    else if (!o1.isValid && o2.isValid) {
        // Fallback safety: if only O2 was found (unlikely with current logic), move it to O1
        o1 = o2;
        o2.isValid = false;
    }
}
#endif
