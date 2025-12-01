// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2023 Tayou <git@tayou.org>
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

using System;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    public class SeloreShaderPatcher : AmityBaseComponent {
        public Renderer renderer;
        public ShaderPatchSelection shaderToPatch;

        public ShaderPatchOptionBase shaderPatchOption {
            get {
                return new ShaderPatchOptionAmity();
            }
        }

        /** Whether or not to patch the shader to deform the mesh */
        public bool featureDeformationEnabled;
        /** If contact senders for triggering socket actions */
        public bool featureContactSenders;
        /** If contact receivers for interpreting by OSCGoesBrr should be added */
        public bool featureToyContactReceivers;
        /** if contacts to expose depth and width to the animator should be generated */
        public bool featureDepthContactReceivers;
        /** automatically rig non-rigged meshes based on plug orientation and length for physics */
        public bool featureAutoRigging;
        /** automatically configure the renderer bounds to well working values for deformation */
        public bool autoConfigureBounds;
        
        /** animate shader deformation, treated as bool - 0 or 1 */
        public float deformationEnabled;

        public bool spsOverrun;
        public bool spsKeepImports;
    }
}

public enum ShaderPatchSelection {
    AmitySPS,
    VRCFurySPS,
    RalivDPS,
    PoiTPS,
}

public abstract class ShaderPatchOptionBase {
    public static string name;
    public static ShaderPatchSelection type;
    public abstract Shader PatchShader(Shader shader);

}

public class ShaderPatchOptionAmity : ShaderPatchOptionBase {
    public static string name = "Amity SPS";
    public static ShaderPatchSelection type = ShaderPatchSelection.AmitySPS;

    public override Shader PatchShader(Shader shader) {
        return shader;
    }
}

public class ShaderPatchOptionVRCFury : ShaderPatchOptionBase {
    public static string name = "VRCFury SPS";
    public static ShaderPatchSelection type = ShaderPatchSelection.VRCFurySPS;

    public override Shader PatchShader(Shader shader) {
        return shader;
    }
}

public class ShaderPatchOptionRalivDPS : ShaderPatchOptionBase {
    public static string name = "Raliv DPS";
    public static ShaderPatchSelection type = ShaderPatchSelection.RalivDPS;

    public override Shader PatchShader(Shader shader) {
        return shader;
    }
}

public class ShaderPatchOptionPoiyomiTPS : ShaderPatchOptionBase {
    public static string name = "Poiyomi TPS";
    public static ShaderPatchSelection type = ShaderPatchSelection.PoiTPS;

    public override Shader PatchShader(Shader shader) {
        return shader;
    }
}

public static class ShaderPathSelectionExtensions 
{
    public static string GetFolderAssetID(this ShaderPatchSelection shaderPatchSelection) 
    {
        switch (shaderPatchSelection) 
        {
            case ShaderPatchSelection.AmitySPS:
                return "6cf9adf85849489b97305dfeecc74768";
            case ShaderPatchSelection.VRCFurySPS:
                return "6cf9adf85849489b97305dfeecc74768";
            case ShaderPatchSelection.RalivDPS:
                return "6cf9adf85849489b97305dfeecc74768";
            case ShaderPatchSelection.PoiTPS:
                return "6cf9adf85849489b97305dfeecc74768";
            default:
                return ""; // Choose a suitable default
        }
    }
}