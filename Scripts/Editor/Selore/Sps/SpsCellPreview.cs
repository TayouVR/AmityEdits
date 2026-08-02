using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace org.Tayou.AmityEdits.EditorSeloreSps {

    public struct SeloreCellData {
        public uint vendor;
        public uint product;
        public uint version;
        public uint id;
        public uint playerId;
        public Vector3 world;
        public Vector3 forward;
        public Vector3 up;
        public float scale;
        public uint flags;
        public uint nextId;
        public uint[] tags; // length 8
        public Vector3 tangentIn;
        public Vector3 tangentOut;
        public Color debugColor;
    }

    public static class SpsCellPreview {

        // Must match VRCFury SPS cell layout exactly
        public const int CELL_WIDTH = 16;
        public const int CELL_HEIGHT = 16;
        public const int CELL_PAYLOAD_START = CELL_WIDTH; // 16
        public const int CELL_REPLICA_COUNT = 5;
        public const int CELL_DICTIONARY_GROUP_SIZE = 16;
        public const int CELL_DICTIONARY_GROUP_COUNT = 256;
        public const int SOCKET_MAX_SLOTS = 4096;
        public const int SOCKET_PAYLOAD_TAG_COUNT = 8;
        public const uint SOCKET_FLAG_HOLE = 1;
        public const uint SOCKET_FLAG_DOUBLE_SIDED = 2;
        public const uint SOCKET_FLAG_RADIUS_OFFSET = 8;
        public const uint PRODUCT_SOCKET = 1;
        public const uint VENDOR_SPS = 1;
        public const uint VERSION_SPS = 1;

        // Header indices
        const int HEADER_VENDOR_INDEX = 1;
        const int HEADER_PRODUCT_INDEX = 2;
        const int HEADER_VERSION_INDEX = 3;
        const int HEADER_UNIQUE_ID_INDEX = 4;
        const int HEADER_PLAYER_ID_INDEX = 5;
        const int HEADER_DEBUG_INDEX = 6;
        const int HEADER_BOTTOM_ROW_BASE = 240;
        const int HEADER_BOTTOM_ROW_START = 241;
        const int HEADER_WORLD_INDEX = 241;
        const int HEADER_FORWARD_INDEX = 244;
        const int HEADER_UP_INDEX = 247;
        const int HEADER_SCALE_INDEX = 250;

        // Socket payload indices (within payload region)
        const int SOCKET_PAYLOAD_FLAGS = 0;
        const int SOCKET_PAYLOAD_NEXT_ID = 1;
        const int SOCKET_PAYLOAD_TAG_START = 2;
        const int SOCKET_PAYLOAD_TANGENT_IN_START = 10;
        const int SOCKET_PAYLOAD_TANGENT_OUT_START = 13;

        static readonly Color MAGIC_0 = new Color(1, 0, 0, 1);
        static readonly Color MAGIC_1 = new Color(0, 1, 0, 1);
        static readonly Color MAGIC_2 = new Color(0, 0, 1, 1);
        static readonly Color MAGIC_3 = new Color(1, 1, 0, 1);
        static readonly Color DICTIONARY_MAGIC = new Color(1, 0, 1, 1);

        // ------------------------------------------------------------------
        // Encoding — mirrors sps_encode.cginc
        // ------------------------------------------------------------------

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        struct FloatUintUnion {
            [System.Runtime.InteropServices.FieldOffset(0)] public float f;
            [System.Runtime.InteropServices.FieldOffset(0)] public uint u;
        }

        static uint FloatToUintBits(float f) {
            return new FloatUintUnion { f = f }.u;
        }

        static float LinearToGamma(float v) {
            return Mathf.LinearToGammaSpace(v);
        }

        static float GammaToLinear(float v) {
            return Mathf.GammaToLinearSpace(v);
        }

        static Color EncodeUint(uint value) {
            uint r = (value >> 0) & 0xFF;
            uint g = (value >> 8) & 0xFF;
            uint b = (value >> 16) & 0xFF;
            uint a = (value >> 24) & 0xFF;
            float fr = r / 255f;
            float fg = g / 255f;
            float fb = b / 255f;
            float fa = a / 255f;
            // When the project is Linear, the shader applies GammaToLinearSpaceExact
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
                fr = GammaToLinear(fr);
                fg = GammaToLinear(fg);
                fb = GammaToLinear(fb);
                fa = GammaToLinear(fa);
            }
            return new Color(
                Mathf.Clamp01(fr),
                Mathf.Clamp01(fg),
                Mathf.Clamp01(fb),
                Mathf.Clamp01(fa)
            );
        }

        static Color EncodeFloat(float value) {
            return EncodeUint(FloatToUintBits(value));
        }

        // ------------------------------------------------------------------
        // Hash — mirrors sps_cell_hash.cginc
        // ------------------------------------------------------------------

        static uint HashMix(uint x) {
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return x;
        }

        static uint HashWorld(Vector3 worldPos, uint salt) {
            uint h = 2166136261;
            h = (h ^ FloatToUintBits(worldPos.x)) * 16777619;
            h = (h ^ FloatToUintBits(worldPos.y)) * 16777619;
            h = (h ^ FloatToUintBits(worldPos.z)) * 16777619;
            h ^= salt * 2246822519;
            return HashMix(h);
        }

        static uint HashedIndexFromUint(uint seed, uint replica, uint slotCount) {
            return HashMix(seed ^ HashMix(replica)) % Math.Max(slotCount, 1);
        }

        static uint HashId(uint id, uint playerId) {
            if (playerId == 0) return id;
            return HashMix(id ^ HashMix(playerId));
        }

        // ------------------------------------------------------------------
        // Slot / grid helpers
        // ------------------------------------------------------------------

        static int CellGridColumns() {
            return Math.Max(1, (int)(Screen.width / CELL_WIDTH));
        }

        static uint SocketSlotCount() {
            int cols = CellGridColumns();
            int rows = Math.Max(1, (int)(Screen.height / CELL_HEIGHT));
            return (uint)Math.Max(1, Math.Min(cols * rows - 1, SOCKET_MAX_SLOTS));
        }

        public static int[] ComputeReplicaSlots(uint id, uint playerId) {
            uint seed = HashId(id, playerId);
            uint slotCount = SocketSlotCount();
            int[] slots = new int[CELL_REPLICA_COUNT];
            for (int r = 0; r < CELL_REPLICA_COUNT; r++) {
                slots[r] = (int)HashedIndexFromUint(seed, (uint)r, slotCount);
            }
            return slots;
        }

        static Vector2Int CellOriginFromIndex(int index) {
            if (index < 0) return Vector2Int.zero;
            int columns = CellGridColumns();
            uint screenIndex = (uint)(index + 1);
            return new Vector2Int(
                (int)(screenIndex % columns) * CELL_WIDTH,
                (int)(screenIndex / columns) * CELL_HEIGHT
            );
        }

        // ------------------------------------------------------------------
        // Pixel encoding — the core that mirrors the shader fragment output
        // ------------------------------------------------------------------

        static Color? EncodePixel(int index, SeloreCellData data) {
            // Magic corners
            if (index == 0) return MAGIC_0;
            if (index == CELL_WIDTH - 1) return MAGIC_1;
            if (index == (CELL_HEIGHT - 1) * CELL_WIDTH) return MAGIC_2;
            if (index == CELL_HEIGHT * CELL_WIDTH - 1) return MAGIC_3;

            // Header top row
            switch (index) {
                case HEADER_VENDOR_INDEX:
                    return EncodeUint(data.vendor);
                case HEADER_PRODUCT_INDEX:
                    return EncodeUint(data.product);
                case HEADER_VERSION_INDEX:
                    return EncodeUint(data.version);
                case HEADER_UNIQUE_ID_INDEX:
                    return EncodeUint(data.id);
                case HEADER_PLAYER_ID_INDEX:
                    return EncodeUint(data.playerId);
                case HEADER_DEBUG_INDEX: {
                    Color d = data.debugColor;
                    return new Color(
                        Mathf.Clamp01(d.r),
                        Mathf.Clamp01(d.g),
                        Mathf.Clamp01(d.b),
                        Mathf.Clamp01(d.a)
                    );
                }
            }

            // Bottom row: world, forward, up, scale
            if (index >= HEADER_WORLD_INDEX && index < HEADER_WORLD_INDEX + 3) {
                float val = index == HEADER_WORLD_INDEX ? data.world.x
                          : index == HEADER_WORLD_INDEX + 1 ? data.world.y
                          : data.world.z;
                return EncodeFloat(val);
            }
            if (index >= HEADER_FORWARD_INDEX && index < HEADER_FORWARD_INDEX + 3) {
                float val = index == HEADER_FORWARD_INDEX ? data.forward.x
                          : index == HEADER_FORWARD_INDEX + 1 ? data.forward.y
                          : data.forward.z;
                return EncodeFloat(val);
            }
            if (index >= HEADER_UP_INDEX && index < HEADER_UP_INDEX + 3) {
                float val = index == HEADER_UP_INDEX ? data.up.x
                          : index == HEADER_UP_INDEX + 1 ? data.up.y
                          : data.up.z;
                return EncodeFloat(val);
            }
            if (index == HEADER_SCALE_INDEX) {
                return EncodeFloat(data.scale);
            }

            // Payload region (indices >= CELL_PAYLOAD_START = 16)
            if (index >= CELL_PAYLOAD_START) {
                int payloadIndex = index - CELL_PAYLOAD_START;

                if (payloadIndex == SOCKET_PAYLOAD_FLAGS) {
                    return EncodeUint(data.flags);
                }
                if (payloadIndex == SOCKET_PAYLOAD_NEXT_ID) {
                    return EncodeUint(data.nextId);
                }
                if (payloadIndex >= SOCKET_PAYLOAD_TAG_START
                    && payloadIndex < SOCKET_PAYLOAD_TAG_START + SOCKET_PAYLOAD_TAG_COUNT) {
                    uint tagVal = (data.tags != null && data.tags.Length > payloadIndex - SOCKET_PAYLOAD_TAG_START)
                        ? data.tags[payloadIndex - SOCKET_PAYLOAD_TAG_START] : 0u;
                    return EncodeUint(tagVal);
                }
                if (payloadIndex >= SOCKET_PAYLOAD_TANGENT_IN_START
                    && payloadIndex < SOCKET_PAYLOAD_TANGENT_IN_START + 3) {
                    float val = (payloadIndex - SOCKET_PAYLOAD_TANGENT_IN_START) == 0 ? data.tangentIn.x
                              : (payloadIndex - SOCKET_PAYLOAD_TANGENT_IN_START) == 1 ? data.tangentIn.y
                              : data.tangentIn.z;
                    return EncodeFloat(val);
                }
                if (payloadIndex >= SOCKET_PAYLOAD_TANGENT_OUT_START
                    && payloadIndex < SOCKET_PAYLOAD_TANGENT_OUT_START + 3) {
                    float val = (payloadIndex - SOCKET_PAYLOAD_TANGENT_OUT_START) == 0 ? data.tangentOut.x
                              : (payloadIndex - SOCKET_PAYLOAD_TANGENT_OUT_START) == 1 ? data.tangentOut.y
                              : data.tangentOut.z;
                    return EncodeFloat(val);
                }
            }

            return null; // empty pixel (0,0,0,0)
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Renders the full 16x16 cell texture from the given data.
        /// </summary>
        public static Texture2D RenderCell(SeloreCellData data) {
            var tex = new Texture2D(CELL_WIDTH, CELL_HEIGHT, TextureFormat.RGBA32, false, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[CELL_WIDTH * CELL_HEIGHT];

            for (int y = 0; y < CELL_HEIGHT; y++) {
                for (int x = 0; x < CELL_WIDTH; x++) {
                    int index = y * CELL_WIDTH + x;
                    Color? c = EncodePixel(index, data);
                    pixels[index] = c ?? new Color(0, 0, 0, 0);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        /// <summary>
        /// Returns a human-readable description of what the given pixel index contains.
        /// </summary>
        public static string DecodePixel(int index, SeloreCellData data) {
            if (index == 0) return "MAGIC 0 — Cell top-left marker (1,0,0,1)";
            if (index == CELL_WIDTH - 1) return "MAGIC 1 — Cell top-right marker (0,1,0,1)";
            if (index == (CELL_HEIGHT - 1) * CELL_WIDTH) return "MAGIC 2 — Cell bottom-left marker (0,0,1,1)";
            if (index == CELL_HEIGHT * CELL_WIDTH - 1) return "MAGIC 3 — Cell bottom-right marker (1,1,0,1)";

            switch (index) {
                case HEADER_VENDOR_INDEX:
                    return $"VENDOR = {data.vendor} (SPS)";
                case HEADER_PRODUCT_INDEX:
                    return $"PRODUCT = {data.product} ({(data.product == 1 ? "SOCKET" : data.product == 2 ? "PLUG" : "UNKNOWN")})";
                case HEADER_VERSION_INDEX:
                    return $"VERSION = {data.version}";
                case HEADER_UNIQUE_ID_INDEX:
                    return $"UNIQUE ID = {data.id} (0x{data.id:X8})";
                case HEADER_PLAYER_ID_INDEX:
                    return $"PLAYER ID = {data.playerId} {(data.playerId == 0 ? "(self)" : "")}";
                case HEADER_DEBUG_INDEX:
                    return $"DEBUG COLOR = ({data.debugColor.r:F2}, {data.debugColor.g:F2}, {data.debugColor.b:F2}, {data.debugColor.a:F2})";
            }

            if (index == HEADER_WORLD_INDEX) return $"WORLD.X = {data.world.x:F4}";
            if (index == HEADER_WORLD_INDEX + 1) return $"WORLD.Y = {data.world.y:F4}";
            if (index == HEADER_WORLD_INDEX + 2) return $"WORLD.Z = {data.world.z:F4}";
            if (index == HEADER_FORWARD_INDEX) return $"FORWARD.X = {data.forward.x:F4}";
            if (index == HEADER_FORWARD_INDEX + 1) return $"FORWARD.Y = {data.forward.y:F4}";
            if (index == HEADER_FORWARD_INDEX + 2) return $"FORWARD.Z = {data.forward.z:F4}";
            if (index == HEADER_UP_INDEX) return $"UP.X = {data.up.x:F4}";
            if (index == HEADER_UP_INDEX + 1) return $"UP.Y = {data.up.y:F4}";
            if (index == HEADER_UP_INDEX + 2) return $"UP.Z = {data.up.z:F4}";
            if (index == HEADER_SCALE_INDEX) return $"SCALE = {data.scale:F4}";

            if (index >= CELL_PAYLOAD_START) {
                int payloadIndex = index - CELL_PAYLOAD_START;

                if (payloadIndex == SOCKET_PAYLOAD_FLAGS) {
                    string flagStr = "";
                    if ((data.flags & SOCKET_FLAG_HOLE) != 0) flagStr += "HOLE ";
                    if ((data.flags & SOCKET_FLAG_DOUBLE_SIDED) != 0) flagStr += "DOUBLE_SIDED ";
                    if ((data.flags & SOCKET_FLAG_RADIUS_OFFSET) != 0) flagStr += "RADIUS_OFFSET ";
                    if (string.IsNullOrEmpty(flagStr)) flagStr = "(none)";
                    return $"FLAGS = {data.flags} ({flagStr.Trim()})";
                }
                if (payloadIndex == SOCKET_PAYLOAD_NEXT_ID) {
                    return $"NEXT ID = {data.nextId} {(data.nextId == 0 ? "(no chain)" : "")}";
                }
                if (payloadIndex >= SOCKET_PAYLOAD_TAG_START
                    && payloadIndex < SOCKET_PAYLOAD_TAG_START + SOCKET_PAYLOAD_TAG_COUNT) {
                    int tagNum = payloadIndex - SOCKET_PAYLOAD_TAG_START + 1;
                    uint tagVal = (data.tags != null && data.tags.Length > payloadIndex - SOCKET_PAYLOAD_TAG_START)
                        ? data.tags[payloadIndex - SOCKET_PAYLOAD_TAG_START] : 0u;
                    return $"TAG {tagNum} = {tagVal} {(tagVal == 0 ? "(unused)" : $"0x{tagVal:X}")}";
                }
                if (payloadIndex >= SOCKET_PAYLOAD_TANGENT_IN_START
                    && payloadIndex < SOCKET_PAYLOAD_TANGENT_IN_START + 3) {
                    int comp = payloadIndex - SOCKET_PAYLOAD_TANGENT_IN_START;
                    string compName = comp == 0 ? "X" : comp == 1 ? "Y" : "Z";
                    float val = comp == 0 ? data.tangentIn.x : comp == 1 ? data.tangentIn.y : data.tangentIn.z;
                    return $"TANGENT IN.{compName} = {val:F4}";
                }
                if (payloadIndex >= SOCKET_PAYLOAD_TANGENT_OUT_START
                    && payloadIndex < SOCKET_PAYLOAD_TANGENT_OUT_START + 3) {
                    int comp = payloadIndex - SOCKET_PAYLOAD_TANGENT_OUT_START;
                    string compName = comp == 0 ? "X" : comp == 1 ? "Y" : "Z";
                    float val = comp == 0 ? data.tangentOut.x : comp == 1 ? data.tangentOut.y : data.tangentOut.z;
                    return $"TANGENT OUT.{compName} = {val:F4}";
                }

                return $"EMPTY (payload index {payloadIndex}) — not written";
            }

            return $"EMPTY — not written";
        }

        /// <summary>
        /// FNV-1a hash of an SPS tag string, matching VRCFury's HashTag exactly.
        /// Returns a 24-bit value (masked to 0x00ffffff), never 0.
        /// </summary>
        public static uint HashTag(string tag) {
            if (string.IsNullOrWhiteSpace(tag)) return 0;
            var n = tag.Trim().ToLowerInvariant();
            uint h = 2166136261;
            foreach (var c in n) {
                h ^= c;
                h *= 16777619;
            }
            h &= 0x00ffffffu;
            return h == 0 ? 1u : h;
        }

        /// <summary>
        /// Mask an SPS unique ID to 24 bits (never 0). Material float properties
        /// only represent integers exactly up to 2^24, so larger IDs would
        /// silently corrupt when baked via Material.SetFloat.
        /// </summary>
        public static uint MaskId(uint id) {
            id &= 0x00ffffffu;
            return id == 0 ? 1u : id;
        }

        /// <summary>
        /// Deterministic ID from a world position (mirrors the shader's fallback),
        /// masked to 24 bits for float-property safety.
        /// </summary>
        public static uint ComputeIdFromWorld(Vector3 worldPos) {
            return MaskId(HashWorld(worldPos, 0));
        }

        static uint[] BuildTagsFromHole(SeloreHole hole) {
            var result = new uint[8];
            int slot = 0;

            if (hole.spsTags != null) {
                foreach (var t in hole.spsTags) {
                    if (slot >= 8) break;
                    uint h = HashTag(t);
                    if (h != 0) result[slot++] = h;
                }
            }

            if (hole.spsUseSharedTag && slot < 8) {
                result[slot++] = 1337;
            }

            return result;
        }

        /// <summary>
        /// Builds preview data from a SeloreHole's transform state.
        /// </summary>
        public static SeloreCellData BuildPreviewData(SeloreHole hole) {
            if (hole == null) return new SeloreCellData();
            Transform root = hole.targetObject != null ? hole.targetObject : hole.transform;

            uint flags = 0;
            if (hole.role == SeloreRole.Hole) {
                flags |= SOCKET_FLAG_HOLE;
            } else if (hole.role == SeloreRole.Ring) {
                flags |= SOCKET_FLAG_HOLE | SOCKET_FLAG_DOUBLE_SIDED;
            } else if (hole.role == SeloreRole.ReversibleRing) {
                flags |= SOCKET_FLAG_DOUBLE_SIDED;
            }

            // Generate a stable ID from the object's world position hash (mirrors shader fallback)
            Vector3 wPos = root.position;
            uint stableId = HashWorld(wPos, 0);

            return new SeloreCellData {
                vendor = VENDOR_SPS,
                product = PRODUCT_SOCKET,
                version = VERSION_SPS,
                id = stableId,
                playerId = 0,
                world = wPos,
                forward = root.forward,
                up = root.up,
                scale = root.lossyScale.magnitude,
                flags = flags,
                nextId = 0,
                tags = BuildTagsFromHole(hole),
                tangentIn = Vector3.zero,
                tangentOut = Vector3.zero,
                debugColor = new Color(0, 0, 0, 0),
            };
        }
    }
}
