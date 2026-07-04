#ifndef AMITY_SPS_INC_RESOLVER_FRAG
#define AMITY_SPS_INC_RESOLVER_FRAG

#include "sps_cell_frag.cginc"
#include "sps_cell_layout.cginc"
#include "sps_id.cginc"
#include "sps_resolver_geom.cginc"
#include "sps_resolver_shader_types.cginc"
#include "sps_resolver_types.cginc"
#include "sps_utils.cginc"

bool sps_resolver_tag_match(
    SpsCell socketCell,
    uint playerId
) {
    uint socketTags[SPS_SOCKET_PAYLOAD_TAG_COUNT];
    [unroll]
    for (uint i = 0u; i < SPS_SOCKET_PAYLOAD_TAG_COUNT; i++) {
        socketTags[i] = socketCell.read_uint(SPS_SOCKET_PAYLOAD_TAG_START + i);
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
            if (socketTags[si] == tagHash) {
                found = true;
                break;
            }
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
            if (socketTags[sj] == tagHash) {
                found = true;
                break;
            }
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

int sps_resolver_find_socket(
    SpsTexture gridTex,
    uint myUniqueId,
    uint myPlayerId,
    int startReplica,
    out SpsCell foundCell
) {
    int slotCount = sps_socket_slot_count();
    uint slotSeed = sps_hash_id(myUniqueId, myPlayerId);

    for (int r = startReplica; r < SPS_CELL_REPLICA_COUNT; r++) {
        int candidateIdx = (int)sps_hashed_screen_slot_index_from_id(slotSeed, (uint)r);
        if (candidateIdx >= slotCount) continue;

        int2 origin = sps_cell_origin_from_index(candidateIdx);
        if (!sps_cell_check_magic(gridTex, uint2(origin))) continue;

        SpsCell cell = sps_get_cell(gridTex, candidateIdx);
        uint product = cell.read_uint(SPS_HEADER_PRODUCT_INDEX);
        if (product != SPS_PRODUCT_SOCKET) continue;

        uint cellPlayerId = cell.read_uint(SPS_HEADER_PLAYER_ID_INDEX);
        if (cellPlayerId == myPlayerId) {
            uint cellId = cell.read_uint(SPS_HEADER_UNIQUE_ID_INDEX);
            if (cellId == myUniqueId) {
                foundCell = cell;
                return candidateIdx;
            }
        }

        if (sps_resolver_tag_match(cell, cellPlayerId)) {
            foundCell = cell;
            return candidateIdx;
        }
    }

    foundCell = sps_get_cell(gridTex, 0);
    return -1;
}

bool sps_resolver_try_write_plug_pixel(
    uint pixelIndex,
    SpsCell socketCell,
    uint socketIndex,
    uint plugUniqueId,
    uint plugPlayerId,
    out float4 rgba
) {
    rgba = 0;

    if (sps_try_get_slot_header_rgba(
        pixelIndex,
        plugUniqueId,
        plugPlayerId,
        SPS_PRODUCT_PLUG,
        sps_cell_header_world(socketCell),
        sps_cell_header_forward(socketCell),
        sps_cell_header_up(socketCell),
        sps_cell_header_scale(socketCell),
        0,
        rgba
    )) return true;

    uint payloadIndex;
    if (!sps_cell_payload_index_from_pixel_index(pixelIndex, payloadIndex)) return false;

    if (payloadIndex == SPS_PLUG_PAYLOAD_SOCKET_ID) {
        rgba = sps_encode_uint((uint)socketIndex);
        return true;
    }
    if (payloadIndex >= SPS_PLUG_PAYLOAD_CHAIN_START
        && payloadIndex < SPS_PLUG_PAYLOAD_CHAIN_START + SPS_PLUG_PAYLOAD_CHAIN_MAX) {
        uint chainOffset = payloadIndex - SPS_PLUG_PAYLOAD_CHAIN_START;
        uint chainValue = chainOffset == 0u ? (uint)socketIndex : 0u;
        rgba = sps_encode_uint(chainValue);
        return true;
    }

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

    SpsCell socketCell;
    int socketIndex = sps_resolver_find_socket(tex, uniqueId, playerId, 0, socketCell);

    if (socketIndex < 0) {
        if (sps_try_get_slot_header_rgba(pixelIndex, uniqueId, playerId, 0, 0, 0, 0, 0, 0, rgba)) return rgba;
        rgba = sps_encode_uint(0u);
        return rgba;
    }

    if (sps_resolver_try_write_plug_pixel(
        pixelIndex, socketCell, (uint)socketIndex,
        uniqueId, playerId, rgba
    )) return rgba;

    return 0;
}

#endif
