#ifndef AMITY_SPS_INC_PLUG
#define AMITY_SPS_INC_PLUG

// This file is included in the main Selore shader to read SPS plug data
// from _VFGridFinal (captured by DataGrabPass after resolver runs).

#include "sps_cell_layout.cginc"
#include "sps_id.cginc"
#include "sps_resolver_types.cginc"

// Read plug cell data for deformation.
// If UseSps is false, returns false and leaves output unchanged.
// If true, reads the grid to find the plug cell matching (id, playerId)
// and populates the Selore orifice data from the first chain entry.
bool selore_sps_read_plug(
    float useSps,
    SpsTexture gridTex,
    uint uniqueId,
    uint playerId,
    out float3 outWorld,
    out float3 outForward,
    out float3 outUp,
    out float outScale
) {
    outWorld = 0;
    outForward = 0;
    outUp = 0;
    outScale = 1;

    if (!sps_to_bool(useSps)) return false;

    uint slotCount = sps_socket_slot_count();
    uint slotSeed = sps_hash_id(uniqueId, playerId);

    for (int r = 0; r < SPS_CELL_REPLICA_COUNT; r++) {
        uint candidateIdx = sps_hashed_screen_slot_index_from_id(slotSeed, (uint)r);
        if (candidateIdx >= slotCount) continue;

        int2 origin = sps_cell_origin_from_index((int)candidateIdx);
        if (!sps_cell_check_magic(gridTex, uint2(origin))) continue;

        SpsCell cell = sps_get_cell(gridTex, (int)candidateIdx);
        uint product = cell.read_uint(SPS_HEADER_PRODUCT_INDEX);
        if (product != SPS_PRODUCT_PLUG) continue;

        uint cellPlayerId = cell.read_uint(SPS_HEADER_PLAYER_ID_INDEX);
        uint cellId = cell.read_uint(SPS_HEADER_UNIQUE_ID_INDEX);
        if (cellPlayerId != playerId || cellId != uniqueId) continue;

        // Found our plug cell. Read the first chain entry (socket index).
        uint socketIndex = cell.read_uint(SPS_PLUG_PAYLOAD_SOCKET_ID);

        // Read the socket cell referenced by the plug
        SpsCell socketCell = sps_get_cell(gridTex, (int)socketIndex);
        if (!sps_cell_check_magic(socketCell)) continue;
        uint socketProduct = socketCell.read_uint(SPS_HEADER_PRODUCT_INDEX);
        if (socketProduct != SPS_PRODUCT_SOCKET) continue;

        // Extract orifice data from socket header
        outWorld = sps_cell_header_world(socketCell);
        outForward = sps_cell_header_forward(socketCell);
        outUp = sps_cell_header_up(socketCell);
        outScale = sps_cell_header_scale(socketCell);
        return true;
    }

    return false;
}

#endif
