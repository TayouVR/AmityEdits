#ifndef AMITY_SPS_INC_RESOLVER_FRAG
#define AMITY_SPS_INC_RESOLVER_FRAG

#include "sps_cell_frag.cginc"
#include "sps_cell_layout.cginc"
#include "sps_dictionary.cginc"
#include "sps_id.cginc"
#include "sps_resolver_geom.cginc"
#include "sps_resolver_shader_types.cginc"
#include "sps_resolver_types.cginc"
#include "sps_types.cginc"
#include "sps_utils.cginc"

// Return true when the socket cell's tags satisfy this plug's include/exclude rules.
bool sps_resolver_tag_match(
    SpsCell socketCell,
    uint playerId
) {
    uint socketTags[SPS_SOCKET_PAYLOAD_TAG_COUNT];
    [unroll]
    for (uint i = 0u; i < SPS_SOCKET_PAYLOAD_TAG_COUNT; i++) {
        socketTags[i] = socketCell.read_uint(
            sps_cell_pixel_index_from_payload_index(SPS_SOCKET_PAYLOAD_TAG_START + i));
    }

    float includeValues[4] = {
        _SPS_TagInclude1, _SPS_TagInclude2, _SPS_TagInclude3, _SPS_TagInclude4
    };
    float includeSelfValues[4] = {
        _SPS_TagInclude1Self, _SPS_TagInclude2Self, _SPS_TagInclude3Self, _SPS_TagInclude4Self
    };
    float includeOthersValues[4] = {
        _SPS_TagInclude1Others, _SPS_TagInclude2Others, _SPS_TagInclude3Others, _SPS_TagInclude4Others
    };
    float excludeValues[4] = {
        _SPS_TagExclude1, _SPS_TagExclude2, _SPS_TagExclude3, _SPS_TagExclude4
    };
    float excludeSelfValues[4] = {
        _SPS_TagExclude1Self, _SPS_TagExclude2Self, _SPS_TagExclude3Self, _SPS_TagExclude4Self
    };
    float excludeOthersValues[4] = {
        _SPS_TagExclude1Others, _SPS_TagExclude2Others, _SPS_TagExclude3Others, _SPS_TagExclude4Others
    };

    uint myPlayerId = sps_player_id();

    for (uint ti = 0u; ti < 4u; ti++) {
        uint tagHash = sps_to_uint(includeValues[ti]);
        if (tagHash == 0u) continue;

        bool found = false;
        [unroll]
        for (uint si = 0u; si < SPS_SOCKET_PAYLOAD_TAG_COUNT; si++) {
            if (socketTags[si] == tagHash) found = true;
        }
        if (!found) return false;

        bool selfOk = sps_to_bool(includeSelfValues[ti]);
        bool othersOk = sps_to_bool(includeOthersValues[ti]);
        bool isSelf = (playerId == myPlayerId);
        if (isSelf && !selfOk) return false;
        if (!isSelf && !othersOk) return false;
    }

    for (uint ei = 0u; ei < 4u; ei++) {
        uint tagHash = sps_to_uint(excludeValues[ei]);
        if (tagHash == 0u) continue;

        bool found = false;
        [unroll]
        for (uint sj = 0u; sj < SPS_SOCKET_PAYLOAD_TAG_COUNT; sj++) {
            if (socketTags[sj] == tagHash) found = true;
        }
        if (!found) continue;

        bool selfOk = sps_to_bool(excludeSelfValues[ei]);
        bool othersOk = sps_to_bool(excludeOthersValues[ei]);
        bool isSelf = (playerId == myPlayerId);
        if (isSelf && selfOk) return false;
        if (!isSelf && othersOk) return false;
    }

    return true;
}

// Is `slotIndex` the primary (first valid) replica slot for the given cell?
bool sps_resolver_is_primary(SpsTexture tex, uint slotIndex, uint uniqueId, uint playerId) {
    uint seed = sps_hash_id(uniqueId, playerId);
    [unroll]
    for (uint replica = 0u; replica < SPS_CELL_REPLICA_COUNT; replica++) {
        int candidate = (int)sps_hashed_screen_slot_index_from_id(seed, replica);
        if (candidate < 0 || candidate >= (int)sps_socket_slot_count()) continue;
        SpsCell c = sps_get_cell(tex, candidate);
        if (!sps_cell_check_magic(c)) continue;
        return candidate == (int)slotIndex;
    }
    return false;
}

// Does the dictionary cell advertise the given group as in-use?
bool sps_resolver_dictionary_group_present(SpsTexture tex, uint group) {
    SpsCell dict = sps_get_cell(tex, SPS_DICTIONARY_INDEX);
    if (!sps_cell_check_magic(dict)) return true; // no dictionary -> assume present
    float4 magic = dict.read_rgba_raw(group);
    return all(magic == SPS_CELL_DICTIONARY_MAGIC);
}

// Effective plug length in world meters (scaled by _SPS_Enabled "Apply Fraction").
float sps_resolver_effective_length() {
    return _SPS_BakedLength * saturate(_SPS_Enabled);
}

// Find the best socket cell for this plug and return its slot index (or -1).
// Strategy: (1) same player+id fast path, then (2) dictionary-guided tag scan
// so sockets with a different uniqueId are still discoverable.
int sps_resolver_find_socket(
    SpsTexture gridTex,
    uint myUniqueId,
    uint myPlayerId
) {
    int slotCount = (int)sps_socket_slot_count();

    // (1) Direct same-id lookup across the plug's own replica slots.
    uint ownSeed = sps_hash_id(myUniqueId, myPlayerId);
    [unroll]
    for (uint r = 0u; r < SPS_CELL_REPLICA_COUNT; r++) {
        int idx = (int)sps_hashed_screen_slot_index_from_id(ownSeed, r);
        if (idx < 0 || idx >= slotCount) continue;
        SpsCell cell = sps_get_cell(gridTex, idx);
        if (!sps_cell_check_magic(cell)) continue;
        if (cell.read_uint(SPS_HEADER_PRODUCT_INDEX) != SPS_PRODUCT_SOCKET) continue;
        if (sps_cell_header_player_id(cell) != myPlayerId) continue;
        if (sps_cell_header_unique_id(cell) == myUniqueId) return idx;
    }

    // (2) Dictionary-guided tag scan: walk slots in the dictionary groups this
    // plug occupies, looking for sockets whose tags match our rules. First
    // match wins (VRCFury uses proximity scoring; first-hit keeps this simple
    // and deterministic) - so stop scanning as soon as one is found.
    uint groupCount = min((uint)SPS_CELL_DICTIONARY_GROUP_COUNT,
        (slotCount + (int)SPS_CELL_DICTIONARY_GROUP_SIZE - 1) / (int)SPS_CELL_DICTIONARY_GROUP_SIZE);

    for (uint slot = 0u; slot < (uint)slotCount; slot++) {
        uint group = slot / SPS_CELL_DICTIONARY_GROUP_SIZE;
        if (group >= groupCount) break;
        if (!sps_resolver_dictionary_group_present(gridTex, group)) continue;

        SpsCell cell = sps_get_cell(gridTex, (int)slot);
        if (!sps_cell_check_magic(cell)) continue;
        if (cell.read_uint(SPS_HEADER_PRODUCT_INDEX) != SPS_PRODUCT_SOCKET) continue;

        uint cellPlayerId = sps_cell_header_player_id(cell);
        if (!sps_resolver_tag_match(cell, cellPlayerId)) continue;
        if (!sps_resolver_is_primary(gridTex, slot, sps_cell_header_unique_id(cell), cellPlayerId)) continue;

        return (int)slot;
    }

    return -1;
}

// Locate a socket cell by unique id + player id across its replica slots.
// Returns the slot index, or -1 when no live replica exists.
int sps_resolver_find_socket_by_id(
    SpsTexture gridTex,
    uint uniqueId,
    uint playerId
) {
    int slotCount = (int)sps_socket_slot_count();
    uint seed = sps_hash_id(uniqueId, playerId);
    [unroll]
    for (uint r = 0u; r < SPS_CELL_REPLICA_COUNT; r++) {
        int idx = (int)sps_hashed_screen_slot_index_from_id(seed, r);
        if (idx < 0 || idx >= slotCount) continue;
        SpsCell cell = sps_get_cell(gridTex, idx);
        if (!sps_cell_check_magic(cell)) continue;
        if (cell.read_uint(SPS_HEADER_PRODUCT_INDEX) != SPS_PRODUCT_SOCKET) continue;
        if (sps_cell_header_player_id(cell) != playerId) continue;
        if (sps_cell_header_unique_id(cell) != uniqueId) continue;
        return idx;
    }
    return -1;
}

// VRCFury socket eligibility rules (sps_resolver_check_socket). Decides the
// traversal normal/up the chain enters through and whether this socket may be
// entered at all from the given approach. Linked (NEXT_ID) hops skip the
// checks entirely and are handled by the caller.
bool sps_resolver_check_socket(
    float3 socketWorld,
    float3 socketNormal,
    uint socketFlags,
    float3 previousWorld,
    float3 sourceForward,
    bool allowHilt,
    out float3 outNormal
) {
    outNormal = sps_normalize(socketNormal);
    float3 rootDirection = sps_normalize(socketWorld - previousWorld);

    // Two-way rings flip their entry normal to face the approaching plug.
    if (sps_has_flag(socketFlags, SPS_SOCKET_FLAG_DOUBLE_SIDED)
        && dot(outNormal, rootDirection) > 0) {
        outNormal *= -1;
    }

    // Hilt override: when the plug is fully seated in a hole, skip the angle
    // checks (same as VRCFury; only the first chain socket may hilt).
    if (allowHilt && sps_has_flag(socketFlags, SPS_SOCKET_FLAG_HOLE)) {
        float hiltDistance = sps_resolver_effective_length() * 0.5;
        if (sps_length_sq(socketWorld - previousWorld) <= hiltDistance * hiltDistance) {
            return true;
        }
    }

    // The plug would have to turn more than 108 degrees to enter the socket.
    if (dot(-outNormal, sourceForward) < -0.309016994) return false;

    // Even aiming straight at the socket, entry would require more than 144
    // degrees of turning.
    if (dot(outNormal, rootDirection) > 0.809016994) return false;

    // The socket lies behind the plug's current forward direction.
    if (dot(rootDirection, sourceForward) <= 0) return false;

    return true;
}

// Walk the socket chain, VRCFury-style (sps_build_chain): hop 0 resolves the
// first socket by tag/id scan, subsequent hops follow the socket's NEXT_ID
// link when present, otherwise re-scan. The chain stops after a hole, when no
// eligible socket is found, or when SPS_PLUG_MAX_ENTRIES sockets are chained.
void sps_resolver_build_chain(
    SpsTexture gridTex,
    uint myUniqueId,
    uint myPlayerId,
    out int chainCount,
    out int chainSlot[SPS_PLUG_MAX_ENTRIES],
    out float3 chainFwd[SPS_PLUG_MAX_ENTRIES]
) {
    chainCount = 0;
    [unroll]
    for (int i = 0; i < SPS_PLUG_MAX_ENTRIES; i++) {
        chainSlot[i] = -1;
        chainFwd[i] = float3(0, 0, 1);
    }

    float3 plugWorld = sps_object_origin_world();
    float3 plugForward = sps_object_forward_world();

    float3 previousWorld = plugWorld;
    float3 sourceForward = plugForward;
    uint prevNextId = 0u;
    uint prevPlayerId = 0u;

    for (int hop = 0; hop < SPS_PLUG_MAX_ENTRIES; hop++) {
        int slot = -1;
        bool linked = false;

        if (prevNextId > 0u) {
            // Explicit NEXT_ID link: follow it regardless of eligibility.
            slot = sps_resolver_find_socket_by_id(gridTex, prevNextId, prevPlayerId);
            if (slot < 0) break;
            linked = true;
        } else {
            slot = sps_resolver_find_socket(gridTex, myUniqueId, myPlayerId);
            if (slot < 0) break;
        }

        // Reject sockets already in the chain (VRCFury: duplicate reject).
        // Without this, a no-link re-scan could pick the same socket again
        // and loop A -> A -> A.
        bool duplicate = false;
        [unroll]
        for (int prior = 0; prior < SPS_PLUG_MAX_ENTRIES; prior++) {
            if (prior < chainCount && chainSlot[prior] == slot) duplicate = true;
        }
        if (duplicate) break;

        SpsCell cell = sps_get_cell(gridTex, slot);
        float3 socketWorld = sps_cell_header_world(cell);
        float3 socketNormal = sps_cell_header_forward(cell);
        uint socketFlags = cell.read_uint(
            sps_cell_pixel_index_from_payload_index(SPS_SOCKET_PAYLOAD_FLAGS));
        uint socketNextId = cell.read_uint(
            sps_cell_pixel_index_from_payload_index(SPS_SOCKET_PAYLOAD_NEXT_ID));
        uint socketPlayerId = sps_cell_header_player_id(cell);

        float3 traversalNormal = sps_normalize(socketNormal);
        if (!linked) {
            if (!sps_resolver_check_socket(
                socketWorld, socketNormal, socketFlags,
                previousWorld, sourceForward, hop == 0, traversalNormal)) {
                break;
            }
        } else {
            float3 rootDirection = sps_normalize(socketWorld - previousWorld);
            if (sps_has_flag(socketFlags, SPS_SOCKET_FLAG_DOUBLE_SIDED)
                && dot(traversalNormal, rootDirection) > 0) {
                traversalNormal *= -1;
            }
        }

        chainSlot[chainCount] = slot;
        chainFwd[chainCount] = traversalNormal;
        chainCount++;

        // A hole terminates the chain (nothing continues through it).
        if (sps_has_flag(socketFlags, SPS_SOCKET_FLAG_HOLE)) break;

        previousWorld = socketWorld;
        sourceForward = traversalNormal;
        prevNextId = socketNextId;
        prevPlayerId = socketPlayerId;
    }
}

// Write one plug payload pixel for the resolved chain. `pixelIndex` is the
// absolute cell pixel index; chain entry i occupies SPS_PLUG_ENTRY_BASE(i).
bool sps_resolver_try_write_plug_pixel(
    SpsTexture gridTex,
    uint pixelIndex,
    int chainCount,
    int chainSlot[SPS_PLUG_MAX_ENTRIES],
    float3 chainFwd[SPS_PLUG_MAX_ENTRIES],
    out float4 rgba
) {
    rgba = 0;

    // Plug metadata row (R14): baked length/radius + metadata color.
    if (pixelIndex == SPS_PLUG_META_R_INDEX) { rgba = sps_encode_float(_SPS_MetadataColor.r); return true; }
    if (pixelIndex == SPS_PLUG_META_G_INDEX) { rgba = sps_encode_float(_SPS_MetadataColor.g); return true; }
    if (pixelIndex == SPS_PLUG_META_B_INDEX) { rgba = sps_encode_float(_SPS_MetadataColor.b); return true; }
    if (pixelIndex == SPS_PLUG_LENGTH_INDEX) { rgba = sps_encode_float(_SPS_BakedLength); return true; }
    if (pixelIndex == SPS_PLUG_RADIUS_INDEX) { rgba = sps_encode_float(_SPS_BakedRadius); return true; }

    // Radius samples row (R15): 16 samples from the baked set.
    if (pixelIndex >= SPS_PLUG_RADIUS_SAMPLES_START
        && pixelIndex < SPS_PLUG_RADIUS_SAMPLES_START + SPS_PLUG_RADIUS_SAMPLE_COUNT) {
        uint sampleIndex = pixelIndex - SPS_PLUG_RADIUS_SAMPLES_START;
        float4 samples[4] = {
            _SPS_BakedRadiusSamples0, _SPS_BakedRadiusSamples1,
            _SPS_BakedRadiusSamples2, _SPS_BakedRadiusSamples3
        };
        float4 packed = samples[sampleIndex / 4u];
        float value = packed[sampleIndex % 4u];
        rgba = sps_encode_float(value);
        return true;
    }

    // Chain entries S1..S5. Entries beyond the resolved chain stay zero.
    if (pixelIndex >= SPS_PLUG_ENTRY_BASE(0)
        && pixelIndex < SPS_PLUG_ENTRY_BASE(0) + SPS_PLUG_ENTRY_STRIDE * SPS_PLUG_MAX_ENTRIES) {
        uint entryIndex = (pixelIndex - SPS_PLUG_ENTRY_BASE(0)) / SPS_PLUG_ENTRY_STRIDE;
        uint entryOffset = pixelIndex - SPS_PLUG_ENTRY_BASE(0) - entryIndex * SPS_PLUG_ENTRY_STRIDE;

        if ((int)entryIndex >= chainCount) {
            rgba = sps_encode_uint(0u);
            return true;
        }

        SpsCell socketCell = sps_get_cell(gridTex, chainSlot[entryIndex]);
        uint socketFlags = socketCell.read_uint(
            sps_cell_pixel_index_from_payload_index(SPS_SOCKET_PAYLOAD_FLAGS));
        float3 socketWorld = sps_cell_header_world(socketCell);
        float3 socketUp = sps_cell_header_up(socketCell);

        if (entryOffset < 3u) {
            rgba = sps_encode_float(socketWorld[entryOffset]);
            return true;
        }
        if (entryOffset < 6u) {
            rgba = sps_encode_float(chainFwd[entryIndex][entryOffset - 3u]);
            return true;
        }
        if (entryOffset < 9u) {
            rgba = sps_encode_float(socketUp[entryOffset - 6u]);
            return true;
        }
        if (entryOffset == SPS_PLUG_ENTRY_FLAGS) {
            rgba = sps_encode_uint(socketFlags);
            return true;
        }
        if (entryOffset == SPS_PLUG_ENTRY_GUIDE) {
            // Reserved (VRCFury: guide-target marker). Not authored yet.
            rgba = sps_encode_uint(0u);
            return true;
        }
        if (entryOffset >= SPS_PLUG_ENTRY_TIN && entryOffset < SPS_PLUG_ENTRY_TIN + 3u) {
            float3 tangentIn = socketCell.read_float3(
                sps_cell_pixel_index_from_payload_index(SPS_SOCKET_PAYLOAD_TANGENT_IN_START));
            rgba = sps_encode_float(tangentIn[entryOffset - SPS_PLUG_ENTRY_TIN]);
            return true;
        }
        if (entryOffset >= SPS_PLUG_ENTRY_TOUT && entryOffset < SPS_PLUG_ENTRY_TOUT + 3u) {
            float3 tangentOut = socketCell.read_float3(
                sps_cell_pixel_index_from_payload_index(SPS_SOCKET_PAYLOAD_TANGENT_OUT_START));
            rgba = sps_encode_float(tangentOut[entryOffset - SPS_PLUG_ENTRY_TOUT]);
            return true;
        }
        rgba = sps_encode_uint(0u);
        return true;
    }

    // Everything else (zero gaps) -> zero.
    rgba = sps_encode_uint(0u);
    return true;
}

float4 sps_resolver_frag(SpsTexture tex, g2f input) {
    uint pixelIndex;
    float4 rgba = 0;
    if (sps_cell_frag(input.cellIndex, input.vertex, pixelIndex, rgba)) return rgba;

    uint uniqueId = sps_id();
    if (uniqueId == 0u) uniqueId = sps_hash_world(sps_object_origin_world(), 0u);
    uint playerId = sps_player_id();

    // The slot header always carries the plug's OWN transform (matching
    // VRCFury). The resolved socket target lives in the entry payload, not
    // the header, so consumers reading the header see where the plug actually
    // is (e.g. the debug overlay) rather than where its target is.
    if (sps_try_get_slot_header_rgba(
        pixelIndex,
        uniqueId,
        playerId,
        SPS_PRODUCT_PLUG,
        sps_object_origin_world(),
        sps_object_forward_world(),
        sps_object_up_world(),
        sps_object_scale_world(),
        0,
        rgba
    )) return rgba;

    // Resolve the socket chain (S1..S5). When nothing resolves the entries
    // stay zero, which sps_plug.cginc treats as "no target"; the metadata
    // payload (baked length/radius) is still written so consumers can always
    // read the plug's size.
    int chainCount;
    int chainSlot[SPS_PLUG_MAX_ENTRIES];
    float3 chainFwd[SPS_PLUG_MAX_ENTRIES];
    sps_resolver_build_chain(tex, uniqueId, playerId, chainCount, chainSlot, chainFwd);

    if (sps_resolver_try_write_plug_pixel(
        tex,
        pixelIndex,
        chainCount,
        chainSlot,
        chainFwd,
        rgba
    )) return rgba;

    return 0;
}

#endif
