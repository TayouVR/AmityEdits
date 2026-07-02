#ifndef AMITY_SPS_INC_FLAGS
#define AMITY_SPS_INC_FLAGS

bool amity_sps_has_flag(uint flags, uint flag) {
    return (flags & flag) != 0;
}

void amity_sps_set_flag(inout uint flags, uint flag) {
    flags |= flag;
}

#endif
