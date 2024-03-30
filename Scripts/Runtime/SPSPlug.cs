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

using UnityEngine;

namespace org.Tayou.AmityEdits {
    public class SPSPlug : AmityBaseComponent {
        public Renderer renderer;
        public ShaderPathSelection shaderToPatch;
    }
}
        
public enum ShaderPathSelection {
    AmitySPS,
    VRCFurySPS,
    RalivDPS,
}

public static class ShaderPathSelectionExtensions 
{
    public static string GetFolderAssetID(this ShaderPathSelection shaderPathSelection) 
    {
        switch (shaderPathSelection) 
        {
            case ShaderPathSelection.AmitySPS:
                return "6cf9adf85849489b97305dfeecc74768";
            case ShaderPathSelection.VRCFurySPS:
                return "6cf9adf85849489b97305dfeecc74768";
            case ShaderPathSelection.RalivDPS:
                return "6cf9adf85849489b97305dfeecc74768";
            default:
                return ""; // Choose a suitable default
        }
    }
    public static string ToString(this ShaderPathSelection shaderPathSelection) 
    {
        switch (shaderPathSelection) 
        {
            case ShaderPathSelection.AmitySPS:
                return "Amity SPS";
            case ShaderPathSelection.VRCFurySPS:
                return "VRCFury SPS";
            case ShaderPathSelection.RalivDPS:
                return "Raliv DPS";
            default:
                return "Unknown"; // Choose a suitable default
        }
    }
}