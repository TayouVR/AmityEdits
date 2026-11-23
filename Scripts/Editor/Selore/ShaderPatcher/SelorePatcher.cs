
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    public static class SelorePatcher {
        // Bump this whenever the injected code changes so cached patched shaders
        // are invalidated and regenerated.
        private const string HashBuster = "selore-4";

        public static void Patch(Material mat, BuildContext ctx, bool keepImports, ShaderPatchSelection selection, bool spsEnabled = false) {
            if (!mat.shader) return;
            try {
                var renderQueue = mat.renderQueue;
                PatchUnsafe(mat, ctx, keepImports, selection, spsEnabled);
                mat.renderQueue = renderQueue;
            } catch (Exception e) {
                throw new Exception(
                    $"Failed to patch shader with {selection.ToString()}. " +
                    $"Report this on the Amity Github issues. " +
                    $"Maybe this shader isn't supported yet.\n\n{mat.shader.name}\n\n{e.Message}", e);
            }
        }

        /// Locks an unlocked Poiyomi/Thry material (turns `.poiyomi/Poiyomi Toon` into
        /// `Hidden/Locked/...`). Must run BEFORE patching: the unlocked shader is a
        /// huge ThryEditor template that can't be patched, and the patcher's
        /// `Hidden/Locked/` naming special case only applies to locked shaders.
        /// Same approach VRCFury uses. No-op for non-Thry shaders.
        public static void LockPoiyomiIfNeeded(Material mat) {
            if (mat == null || mat.shader == null) return;
            if (mat.shader.name.StartsWith("Hidden/Locked/")) return;

            var optimizer = GetThryOptimizerType();
            if (optimizer == null) return;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var isThry = optimizer.GetMethod("IsShaderUsingThryOptimizer", flags);
            var setLocked = optimizer.GetMethod("SetLockedForAllMaterials", flags);
            if (isThry == null || setLocked == null) return;
            if (!(bool)ReflectionUtils.CallWithOptionalParams(isThry, null, mat.shader)) return;

            Debug.Log($"[Selore] Locking Poiyomi material '{mat.name}' before patching.");
            AssetDatabaseHelper.WithoutAssetEditing(() => {
                var ok = (bool)ReflectionUtils.CallWithOptionalParams(setLocked, null, new[] { mat }, 1);
                if (!ok) {
                    throw new Exception("Poiyomi's lockdown method returned false. Check the console for the reason, " +
                                        "or lock the material manually (and update Poiyomi if it is out of date).");
                }
            });
            if (!mat.shader.name.StartsWith("Hidden/Locked/")) {
                throw new Exception("Failed to lock the Poiyomi material. Try locking it manually in the inspector.");
            }
        }

        private static Type GetThryOptimizerType() {
            return ReflectionUtils.GetTypeFromAnyAssembly("Thry.ThryEditor.ShaderOptimizer")
                   ?? ReflectionUtils.GetTypeFromAnyAssembly("Thry.ShaderOptimizer");
        }

        /// Copies the thry rename suffix onto `copy` so properties stay addressable after locking.
        public static void PreserveThryRenameSuffix(Material original, Material copy) {
            var getSuffix = GetThryOptimizerType()?.GetMethod("GetRenamedPropertySuffix",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (getSuffix == null) return;
            var suffix = (string)getSuffix.Invoke(null, new object[] { original });
            if (suffix != null) copy.SetOverrideTag("thry_rename_suffix", suffix);
        }

        private static Regex GetRegex(string pattern) {
            return new Regex(pattern, RegexOptions.Compiled);
        }

        private static void PatchUnsafe(Material mat, BuildContext ctx, bool keepImports, ShaderPatchSelection selection, bool spsEnabled) {
            var shader = mat.shader;
            var newShader = PatchUnsafe(shader, ctx, keepImports, selection, spsEnabled);
            mat.shader = newShader.shader;
            // should already be in a asset container (not in NDMFs)
            //AssetDatabase.AddObjectToAsset(newShader.shader, ctx.AssetContainer);
        }

        public class PatchResult {
            public Shader shader;
            public int patchedPasses;
        }
        private static PatchResult PatchUnsafe(Shader shader, BuildContext ctx, bool keepImports, ShaderPatchSelection selection, bool spsEnabled, string parentHash = null) {
            var pathToSelore = GetPathToIncludes(selection);
            var contents = ReadFile(shader, selection);

            void Replace(string pattern, string replacement, int count) {
                var startLen = contents.Length + "" + contents.GetHashCode();
                contents = GetRegex(pattern).Replace(contents, replacement, count);
                if (startLen == contents.Length + "" + contents.GetHashCode()) {
                    throw new Exception("Failed to find " + pattern);
                }
            }

            // Skip if the shader is already patched (marker property) or if it already
            // defines the Selore feature natively (e.g. the reference implementation).
            if (contents.Contains("_Selore_Patched") || contents.Contains("Selore_DeformStrength")) {
                return new PatchResult {
                    shader = shader,
                    patchedPasses = 0
                };
            }

            if (parentHash == null) {
                var propertiesContent = ReadAndFlattenPath($"{pathToSelore}/selore_props.cginc");
                Replace(
                    @"((?:^|\n)\s*Properties\s*{)",
                    $"$1\n{propertiesContent}\n",
                    1
                );
                contents = GetRegex(@"\n\s+CustomEditor [^\n]+").Replace(contents, "");
            }

            string seloreMain;
            if (keepImports) {
                seloreMain = $"#include \"{pathToSelore}/selore_main.cginc\"";
            } else {
                seloreMain = ReadAndFlattenPath($"{pathToSelore}/selore_main.cginc");
            }
            
            var md5 = MD5.Create();
            var hashContent = contents + seloreMain + HashBuster + (spsEnabled ? "+sps" : "");
            var hashContentBytes = Encoding.UTF8.GetBytes(hashContent);
            var hashBytes = md5.ComputeHash(hashContentBytes);
            var hash = string.Join("", Enumerable.Range(0, hashBytes.Length)
                .Select(i => hashBytes[i].ToString("x2")));

            if (parentHash != null) {
                hash = $"{parentHash}-{hash}";
            }

            string newShaderName;
            if (shader.name.StartsWith("Hidden/Locked/")) {
                // Special case for Poiyomi
                // This prevents Poiyomi from complaining that the mat isn't locked and bailing on the build
                newShaderName = $"Hidden/Locked/SelorePatched/{hash}";
            } else {
                newShaderName = $"Hidden/SelorePatched/{hash}";
            }
            var alreadyExists = Shader.Find(newShaderName);
            // A previously generated shader that failed to compile is NOT reusable:
            // fall through and regenerate it (which also re-reports the error).
            if (alreadyExists != null && !ShaderUtil.ShaderHasError(alreadyExists)) {
                Debug.Log($"[Selore] Reusing existing patched shader '{newShaderName}'");
                return new PatchResult {
                    shader = alreadyExists,
                    patchedPasses = 0
                };
            }

            Replace(
                @"((?:^|\n)\s*Shader\s*"")([^""]*)",
                $"$1{Regex.Escape(newShaderName)}",
                1
            );

            var patchedPasses = 0;
            contents = WithEachPass(contents,
                pass => {
                    patchedPasses++;
                    try {
                        return PatchPass(pass, seloreMain, false, spsEnabled);
                    } catch (Exception e) {
                        throw new Exception($"Failed to patch pass #{patchedPasses}: " + e.Message, e);
                    }
                },
                rest => {
                    if (GetRegex(@"\n[ \t]*#pragma[ \t]+surface").IsMatch(rest)) {
                        patchedPasses++;
                        try {
                            return PatchPass(rest, seloreMain, true, spsEnabled);
                        } catch (Exception e) {
                            throw new Exception($"Failed to patch surface shader: " + e.Message, e);
                        }
                    }
                    return rest;
                }
            );
            var childShaders = new Dictionary<Shader, Shader>();
            contents = GetRegex(@"\n[ \t]*UsePass[ \t]+""([^""]+)/([^""/]+)""").Replace(contents, match => {
                var shaderName = match.Groups[1].ToString();
                var passName = match.Groups[2].ToString();
                var includedShader = Shader.Find(shaderName);
                if (!includedShader) {
                    throw new Exception("Failed to find included shader: " + shaderName);
                }

                if (!childShaders.TryGetValue(includedShader, out var rewrittenIncludedShader)) {
                    var output = PatchUnsafe(includedShader, ctx, keepImports, selection, spsEnabled, hash);
                    patchedPasses += output.patchedPasses;
                    rewrittenIncludedShader = output.shader;
                    childShaders[includedShader] = rewrittenIncludedShader;
                }
                
                return $"\nUsePass \"{rewrittenIncludedShader.name}/{passName}\"\n";
            });
            if (patchedPasses == 0) {
                throw new Exception($"No passes found");
            }

            var assetContainerPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ctx.AssetContainer))
                ?.Replace('\\', '/');
            var newPathDir = $"{assetContainerPath}/Selore";
            var newPath = $"{newPathDir}/{hash}.shader";
            Debug.Log($"[Selore] Writing patched shader to {newPath}");
            AssetDatabaseHelper.WithoutAssetEditing(() => {
                // The folder must be registered with the AssetDatabase. A folder that only exists
                // on disk (Directory.CreateDirectory) has no .meta, and Unity then never imports
                // the files inside it until the next full refresh, i.e. after the build is over.
                EnsureAssetFolder(newPathDir);
                WriteFile(newPath, contents);
                AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            });
            Debug.Log($"[Selore] Imported {newPath}");

            var newShader = Shader.Find(newShaderName);
            if (!newShader) {
                // Last resort: a full synchronous refresh.
                AssetDatabaseHelper.WithoutAssetEditing(() => {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                });
                newShader = Shader.Find(newShaderName);
            }
            if (!newShader) {
                // Shader.Find returning null means Unity could not even register the
                // shader (ShaderLab parse error, or the declared name doesn't match).
                // Report what we can see instead of a generic message.
                var loaded = AssetDatabase.LoadAssetAtPath<Shader>(newPath);
                var diagnostics = $"[folder registered: {AssetDatabase.IsValidFolder(newPathDir)}, " +
                                  $"file exists: {File.Exists(newPath)}, " +
                                  $"guid: '{AssetDatabase.AssetPathToGUID(newPath)}']";
                var detail = loaded != null
                    ? $"The file imported as shader '{loaded.name}' (expected '{newShaderName}')." + FormatShaderErrors(loaded)
                    : "Unity did not import it as a shader at all (ShaderLab syntax error?). " +
                      "Open the file and check the Console / the shader asset's inspector for the first error. " + diagnostics;
                throw new Exception($"Patch succeeded, but the generated shader could not be registered. {detail}\n\n{newPath}");
            }

            if (ShaderUtil.ShaderHasError(newShader)) {
                // Force compilation of every pass so the messages are populated.
                var testMat = new Material(shader);
                try {
                    for (var i = 0; i < testMat.passCount; i++) {
                        ShaderUtil.CompilePass(testMat, i, true);
                    }
                } finally {
                    UnityEngine.Object.DestroyImmediate(testMat);
                }
                if (ShaderUtil.ShaderHasError(shader)) {
                    throw new Exception(
                        $"The original shader '{shader.name}' ({AssetDatabase.GetAssetPath(shader)}) already has " +
                        "compile errors before patching." + FormatShaderErrors(shader));
                }
                throw new Exception("Patch succeeded, but the patched shader failed to compile." +
                                    FormatShaderErrors(newShader) + $"\n\n{newPath}");
            }

            Debug.Log($"[Selore] Patched shader ready: '{newShaderName}' ({newPath})");

            return new PatchResult {
                shader = newShader,
                patchedPasses = patchedPasses
            };
        }

        /// Creates `folder` (an "Assets/..." path) through the AssetDatabase, one level at a time,
        /// so it gets a .meta and is importable immediately.
        private static void EnsureAssetFolder(string folder) {
            if (string.IsNullOrEmpty(folder) || folder == "Assets" || folder == "Packages") return;
            if (AssetDatabase.IsValidFolder(folder)) return;

            var slash = folder.LastIndexOf('/');
            if (slash <= 0) return;
            var parent = folder.Substring(0, slash);
            var name = folder.Substring(slash + 1);
            EnsureAssetFolder(parent);

            if (Directory.Exists(folder)) {
                // Exists on disk but unknown to the database: let Unity pick it up.
                AssetDatabase.ImportAsset(folder, ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.IsValidFolder(folder)) return;
            }
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name)) && !AssetDatabase.IsValidFolder(folder)) {
                throw new Exception("Failed to create asset folder " + folder);
            }
        }

        private static string FormatShaderErrors(Shader shader, int max = 5) {
            try {
                var errors = ShaderUtil.GetShaderMessages(shader)
                    .Where(m => m.severity == ShaderCompilerMessageSeverity.Error)
                    .Take(max)
                    .Select(m => $"{m.file}:{m.line} {m.message}")
                    .ToArray();
                if (errors.Length == 0) return " (Unity reported no shader messages.)";
                return "\n" + string.Join("\n", errors);
            } catch (Exception e) {
                return $" (could not read shader messages: {e.Message})";
            }
        }

        private static string PatchPass(string pass, string seloreMain, bool isSurfaceShader, bool spsEnabled) {
            // Meta / Never passes are never rendered with deformation; patching them only adds
            // compile risk (and some, like Poiyomi's, have no standard #pragma vertex).
            if (GetRegex(@"""LightMode""\s*=\s*""(?:Meta|Never)""").IsMatch(pass)) {
                return pass;
            }
            var newVertFunction = "seloreVert";
            var pragmaKeyword = isSurfaceShader ? "surface" : "vertex";
            string oldVertFunction = null;
            var foundPragma = false;
            pass = GetRegex(@"(#pragma[ \t]+" + pragmaKeyword + @"[ \t]+)([^\s]+)([^\n]*)").Replace(pass, match => {
                string newPragma;
                if (isSurfaceShader) {
                    var extraParams = match.Groups[3].ToString();
                    extraParams = GetRegex(@"vertex:(\S+)").Replace(extraParams, vertMatch => {
                        oldVertFunction = vertMatch.Groups[1].ToString();
                        return "vertex:" + newVertFunction;
                    });
                    if (oldVertFunction == null) {
                        newPragma = $"{match.Groups[0]} vertex:{newVertFunction}";
                    } else {
                        newPragma = $"{match.Groups[1]}{match.Groups[2]}{extraParams}";
                    }
                } else {
                    oldVertFunction = match.Groups[2].ToString();
                    newPragma = $"{match.Groups[1]}seloreVert{match.Groups[3]}";
                }

                foundPragma = true;
                return $"// {match.Groups[0]}\n{newPragma}\n";
            }, 1);
            if (!foundPragma) {
                throw new Exception($"Failed to find #pragma {pragmaKeyword}");
            }

            if (!isSurfaceShader) {
                // If lightmode is unset (the default of "Always"), set it to ForwardBase
                // so that we actually receive light data
                if (!pass.Contains("\"LightMode\"")) {
                    pass = GetRegex(@"\{").Replace(pass, match => {
                        return match.Groups[0] + "\n    Tags { \"LightMode\" = \"ForwardBase\" }\n";
                    }, 1);
                }
            }

            var flattenedPass = ReadAndFlattenContent(pass, includeLibraryFiles: true);

            string oldStructType;
            string returnType;
            string newInputParams;
            string newPassParams;
            string mainParamName;
            string searchForOldStruct = flattenedPass;
            bool useStructExtends = !isSurfaceShader;
            if (oldVertFunction != null) {
                var foundOldVert = GetRegex(
                        Regex.Escape(oldVertFunction)
                        + @"\s*" // whitespace before param list
                        + @"\(" // start param list
                        + "(" // param list
                            + "(" // Repeating params or preprocessor directive
                                + @"([^#\);]*)"
                                + @"|"
                                + @"(#[^\n]*\n)"
                            + ")*"
                        + ")"
                        + @"\)\s*\{" // end param list and start function bracket
                    )
                    .Matches(flattenedPass)
                    .Cast<Match>()
                    .Select(m => {
                        // The reason we search backward for the return type instead of just including it in the regex
                        // is because it makes the regex REALLY SLOW to have wildcards at the start.
                        var ps = m.Groups[1].ToString();
                        var i = m.Index - 1;
                        if (!Char.IsWhiteSpace(flattenedPass[i])) return null;
                        while (Char.IsWhiteSpace(flattenedPass[i])) i--;
                        var endOfReturnType = i + 1;
                        while (!Char.IsWhiteSpace(flattenedPass[i])) i--;
                        var startOfReturnType = i + 1;
                        var r = flattenedPass.Substring(startOfReturnType, endOfReturnType - startOfReturnType);
                        return Tuple.Create(ps, r);
                    })
                    .Where(t => t != null)
                    .ToArray();
                if (foundOldVert.Length > 1) {
                    foundOldVert = foundOldVert.Distinct().ToArray();
                }
                // Special case for Standard
                if (foundOldVert.Length > 1) {
                    foundOldVert = foundOldVert.Where(m => !m.Item2.Contains("Simple")).ToArray();
                }
                // Special case for Fast Fur
                if (foundOldVert.Length > 1) {
                    if (flattenedPass.Contains("FUR_SKIN_LAYER")) {
                        var skinLayerDefined = flattenedPass.Contains("#define FUR_SKIN_LAYER");
                        foundOldVert = foundOldVert.Where(m => m.Item2 == (skinLayerDefined ? "fragInput" : "hullGeomInput")).ToArray();
                    }
                }

                if (foundOldVert.Length == 0) {
                    throw new Exception("Failed to find vertex method: " + oldVertFunction);
                }

                if (foundOldVert.Length > 1) {
                    throw new Exception("Found vertex method multiple times: "
                                        + oldVertFunction
                                        + "\n"
                                        + string.Join("\n", foundOldVert.Select(f => f.Item2 + " " + f.Item1)));
                }

                var paramList = foundOldVert[0].Item1;
                returnType = foundOldVert[0].Item2;

                var rewrittenInputParams = RewriteParamList(paramList, rewriteFirstParamTypeTo: "SeloreInputs");
                newInputParams = rewrittenInputParams.rewritten;
                oldStructType = rewrittenInputParams.firstParamType;
                mainParamName = rewrittenInputParams.firstParamName;
                var firstParamType = rewrittenInputParams.firstParamType;
                var rewrittenPassParams = RewriteParamList(paramList, stripTypes: true,
                    rewriteFirstParamNameTo: $"({firstParamType}){mainParamName}");
                newPassParams = rewrittenPassParams.rewritten;
            } else {
                oldStructType = "appdata_full";
                returnType = "void";
                newInputParams = "inout SeloreInputs input";
                mainParamName = "input";
                newPassParams = null;
            }

            var oldStructBody = "";
            if (oldStructType != null) {
                var foundOldParam = GetRegex(@"struct\s+" + Regex.Escape(oldStructType) + @"\s*{([^}]*)}")
                    .Match(searchForOldStruct);
                if (foundOldParam.Success) {
                    oldStructBody = foundOldParam.Groups[1].ToString();
                } else {
                    throw new Exception("Failed to find old struct: " + oldStructType);
                }
            }

            var newStructBody = "";
            if (!useStructExtends) newStructBody += oldStructBody;
            string FindParam(string keyword, string defaultName, string defaultType) {
                var match = GetRegex(@"([^ \t]+)[ \t]+([^ \t:]+)[ \t:]+" + Regex.Escape(keyword))
                    .Match(oldStructBody);
                if (match.Success) {
                    return match.Groups[2].ToString();
                }
                newStructBody += $"  {defaultType} {defaultName} : {keyword};\n";
                return defaultName;
            }

            var vertexParam = FindParam("POSITION", "selorePosition", "float3");
            var normalParam = FindParam("NORMAL", "seloreNormal", "float3");
            var vertexIdParam = FindParam("SV_VertexID", "seloreVertexId", "uint");
            var colorParam = FindParam("COLOR", "seloreColor", "float4");

            var newHeader = new List<string>();
            if (spsEnabled) {
                // The SPS branch in core.cginc is gated on this keyword. Without the
                // pragma no variant containing the SPS code path would ever exist.
                newHeader.Add("#pragma multi_compile _ SELORE_SPS");
                // Vertex-stage Texture2D.Load (grid reads) requires SM4+. Unity uses
                // the highest #pragma target present in the pass.
                newHeader.Add("#pragma target 5.0");
            }
            // Enable appdata features in shaders where they may be controlled by preprocessor defines
            // liltoon
            newHeader.Add("#define LIL_APP_POSITION");
            newHeader.Add("#define LIL_APP_NORMAL");
            newHeader.Add("#define LIL_APP_VERTEXID");
            newHeader.Add("#define LIL_APP_COLOR");
            // UnlitWF
            newHeader.Add("#define _V2F_HAS_VERTEXCOLOR");
            // Filamented
            newHeader.Add("#define HAS_ATTRIBUTE_COLOR");

            var newBody = new List<string>();
            newBody.Add(seloreMain);
            var extends = useStructExtends ? $" : {oldStructType}" : "";
            newBody.Add($"struct SeloreInputs{extends} {{");
            newBody.Add(newStructBody);
            newBody.Add("};");

            // Silent Crosstone
            var useEndif = false;
            if (flattenedPass.Contains("SHADER_STAGE_VERTEX") && !isSurfaceShader) {
                useEndif = true;
                newBody.Add("#if (defined(SHADER_STAGE_VERTEX) || defined(SHADER_STAGE_GEOMETRY))");
            }

            newBody.Add($"{returnType} {newVertFunction}({newInputParams}) {{");
            newBody.Add($"  selore_apply({mainParamName}.{vertexParam}.xyz, {mainParamName}.{normalParam}, {mainParamName}.{colorParam});");
            if (newPassParams != null) {
                var ret = returnType == "void" ? "" : "return ";
                newBody.Add($"  {ret}{oldVertFunction}({newPassParams});");
            }
            newBody.Add("}");
            
            // Silent Crosstone
            if (useEndif) {
                newBody.Add("#endif");
            }

            // We add the body to the end of the pass, since otherwise it may be too early and
            // get inserted before includes that are needed for the base data types
            var startCg = pass.IndexOf("CGPROGRAM");
            if (startCg < 0) startCg = pass.IndexOf("HLSLPROGRAM");
            if (startCg > 0) startCg = pass.IndexOf("\n", startCg);
            if (startCg < 0) throw new Exception("Failed to find CGPROGRAM");
            pass = pass.Substring(0, startCg) + "\n"
                   + string.Join("\n", newHeader)
                   + "\n" + pass.Substring(startCg);

            // We add the body to the end of the pass, since otherwise it may be too early and
            // get inserted before includes that are needed for the base data types
            var endCg = pass.LastIndexOf("ENDCG");
            if (endCg < 0) endCg = pass.LastIndexOf("ENDHLSL");
            if (endCg < 0) throw new Exception("Failed to find ENDCG");
            pass = pass.Substring(0, endCg) + "\n"
                   + string.Join("\n", newBody)
                   + "\n" + pass.Substring(endCg);

            return pass;
        }

        public class RewriteParamListOutput {
            public string firstParamName;
            public string firstParamType;
            public string rewritten;
        }
        private static RewriteParamListOutput RewriteParamList(string paramList, string rewriteFirstParamTypeTo = null, string rewriteFirstParamNameTo = null, bool stripTypes = false) {
            string firstParamName = null;
            string firstParamType = null;
            var rewritten = string.Join("\n", paramList.Split('\n').Select(line => {
                if (line.Trim().StartsWith("#")) return line;
                return string.Join(",", line.Split(',').Select(p => {
                    var trimmed = p.Trim();
                    if (trimmed.Length == 0) return p;
                    if (firstParamName == null) {
                        var m = Regex.Match(trimmed, @"(\S+)\s+(\S+)$");
                        firstParamType = m.Groups[1].ToString();
                        firstParamName = m.Groups[2].ToString();
                        if (rewriteFirstParamTypeTo != null) {
                            p = Regex.Replace(p, @"(\S+)(\s+\S+\s*)$", rewriteFirstParamTypeTo+"$2");
                        }
                        if (rewriteFirstParamNameTo != null) {
                            p = Regex.Replace(p, @"(\S+)(\s*)$", rewriteFirstParamNameTo+"$2");
                        }
                    }
                    if (stripTypes) {
                        p = Regex.Replace(p, @":.*", "");
                        p = Regex.Replace(p, @"\S.*\s(\S+)\s*$", "$1");
                    }
                    return p;
                }));
            }));
            return new RewriteParamListOutput() {
                firstParamType = firstParamType,
                firstParamName = firstParamName,
                rewritten = rewritten,
            };
        }

        private static string WithEachPass(string content, Func<string, string> withPass, Func<string, string> withRest) {
            var output = "";
            var lastPassEnd = 0;
            var processedPasses = new List<string>();
            while (true) {
                var nextPassStart = GetRegex(@"\n\s*Pass[\s{]*\s*\n").Match(content, lastPassEnd);
                if (nextPassStart.Success) {
                    var start = nextPassStart.Index;
                    output += content.Substring(lastPassEnd, start - lastPassEnd);
                    var end = IndexOfEndOfNextContext(content, start);
                    var oldPass = content.Substring(start, end - start);
                    var newPass = withPass(oldPass);
                    output += $"__PASS_{processedPasses.Count}__";
                    processedPasses.Add(newPass);
                    lastPassEnd = end;
                } else {
                    output += content.Substring(lastPassEnd);
                    break;
                }
            }

            output = withRest(output);
            for (var i = 0; i < processedPasses.Count; i++) {
                output = output.Replace($"__PASS_{i}__", processedPasses[i]);
            }

            return output;
        }

        private static int IndexOfEndOfNextContext(string str, int start) {
            var bracketLevel = 0;
            var inString = false;
            var inStringEscape = false;
            var inLineComment = false;
            var inBlockComment = false;
            for (var i = start; i < str.Length; i++) {
                var c = str[i];
                if (inLineComment) {
                    if (c == '\n') {
                        inLineComment = false;
                    }
                    continue;
                }
                if (inBlockComment) {
                    if (c == '*' && i != str.Length - 1 && str[i + 1] == '/') {
                        inBlockComment = false;
                    }
                    continue;
                }
                if (inString) {
                    if (inStringEscape) {
                        inStringEscape = false;
                        // skip it, this is a literal
                    } else if (c == '\\') {
                        inStringEscape = true;
                    } else if (c == '"') {
                        inString = false;
                    }
                    continue;
                }

                if (c == '/' && i != str.Length - 1 && str[i + 1] == '*') {
                    inBlockComment = true;
                    i++;
                } else if (c == '/' && i != str.Length - 1 && str[i + 1] == '/') {
                    inLineComment = true;
                    i++;
                } else if (c == '{') {
                    bracketLevel++;
                } else if (c == '}') {
                    bracketLevel--;
                    if (bracketLevel == 0) return i+1;
                } else if (c == '"') {
                    inString = true;
                }
            }
            throw new Exception("Failed to find matching closing bracket");
        }

        private static string WithEachInclude(string contents, string filePath, Func<string, string> replacer = null, bool replaceWithFullPath = false, bool includeLibraryFiles = false) {
            return GetRegex(@"(?:^|\n)(\s*#(?:include|include_with_pragmas)\s"")([^""]+)("")").Replace(contents, match => {
                var before = match.Groups[1].ToString();
                var path = match.Groups[2].ToString();
                var after = match.Groups[3].ToString();
                if (path.StartsWith("/")) path = path.Substring(1);
                string fullPath;
                var attempts = new List<string>();
                var isLib = false;
                if (filePath == null) {
                    fullPath = path;
                    attempts.Add(fullPath);
                } else {
                    var p = path;
                    fullPath = Path.GetDirectoryName(filePath);
                    while (p.StartsWith("..")) {
                        fullPath = Path.GetDirectoryName(fullPath);
                        p = p.Substring(3);
                    }
                    fullPath = Path.Combine(fullPath, p);
                    attempts.Add(fullPath);
                }
                if (!path.Contains("..") && !File.Exists(fullPath)) {
                    fullPath = Path.Combine(EditorApplication.applicationContentsPath, "CGIncludes", path);
                    attempts.Add(fullPath);
                    isLib = true;
                }
                if (!File.Exists(fullPath)) {
                    Debug.LogWarning("Failed to find include at " + string.Join(" or ", attempts));
                    return match.Groups[0].ToString();
                }
                if (!includeLibraryFiles && isLib) {
                    return match.Groups[0].ToString();
                }

                if (replacer != null) {
                    return "\n" + replacer(fullPath) + "\n";
                } else if (replaceWithFullPath) {
                    if (fullPath.Contains("'")) {
                        throw new Exception(
                            "A unity bug prevents SPS from including shaders stored in a folder with a ' in the name. " +
                            "Please rename the folder to remove the quote symbol: " + fullPath);
                    }
                    return "\n" + before + fullPath + after + "\n";
                } else {
                    return "\n" + before + path + after + "\n";
                }
            });
        }

        private static string ReadAndFlattenPath(string path, HashSet<string> included = null, bool includeLibraryFiles = false) {
            if (included == null) {
                included = new HashSet<string>();
            }
            if (included.Contains(path)) return "";
            included.Add(path);
            var content = ReadFile(path);
            return ReadAndFlattenContent(content, included, includeLibraryFiles);
        }
        private static string ReadAndFlattenContent(string content, HashSet<string> included = null, bool includeLibraryFiles = false) {
            var output = new List<string>();
            if (includeLibraryFiles && content.Contains("CGPROGRAM")) {
                content = "#include \"HLSLSupport.cginc\"\n" + content;
                content = "#include \"UnityShaderVariables.cginc\"\n" + content;
                if (content.Contains("#pragma surface ")) {
                    content = "#include \"Lighting.cginc\"\n" + content;
                }
            }
            content = WithEachInclude(content, null, includePath => {
                return ReadAndFlattenPath(includePath, included, includeLibraryFiles);
            }, includeLibraryFiles: includeLibraryFiles);
            output.Add(content);
            return string.Join("\n", output);
        }

        /// Resolve the absolute asset path to the include folder for a given
        /// patch backend. The folder must contain selore_props.cginc /
        /// selore_main.cginc and (optionally) Standard.shader.orig fallbacks.
        private static string GetPathToIncludes(ShaderPatchSelection selection) {
            var guid = selection.GetFolderAssetID();
            if (string.IsNullOrEmpty(guid)) {
                throw new Exception($"No include folder GUID configured for {selection}");
            }
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) {
                throw new Exception($"Could not resolve include folder for {selection} (GUID {guid}). " +
                                    "Make sure the package is installed correctly.");
            }
            return path;
        }

        private static string ReadFile(Shader shader, ShaderPatchSelection selection) {
            var path = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrWhiteSpace(path)) {
                throw new Exception("Failed to find source file for the shader");
            }

            if (path.StartsWith("Resources") || path.StartsWith("Library")) {
                if (shader.name == "Standard" || shader.name == "Standard (Specular setup)") {
                    var includes = GetPathToIncludes(selection);
                    var fileName = shader.name == "Standard"
                        ? "Standard.shader.orig"
                        : "StandardSpecular.shader.orig";
                    var candidate = $"{includes}/{fileName}";
                    if (!File.Exists(candidate)) {
                        throw new Exception(
                            $"Selore wants to patch Unity's built-in {shader.name} shader but " +
                            $"could not find {fileName} in {includes}. " +
                            "See Standard.shader.sourcing.md in the Selore folder for how to provide it.");
                    }
                    path = candidate;
                } else if (shader.name.Contains("Error")) {
                    throw new Exception(
                        "This is an error shader. Please verify that the base material actually loads.");
                } else {
                    throw new Exception(
                        "Selore does not yet support this built-in unity shader: " + shader.name);
                }
            }

            return ReadFile(path);
        }
        private static string ReadFile(string path) {
            string content;
            if (path.EndsWith("orlshader")) {
                var sourceAsset = AssetDatabase.LoadAllAssetsAtPath(path).OfType<TextAsset>().FirstOrDefault();
                if (sourceAsset == null) throw new Exception("Failed to find orlshader source");
                content = sourceAsset.text;
            } else if (path.EndsWith("lilcontainer")) {
                var sourceAsset = AssetDatabase.LoadAllAssetsAtPath(path).OfType<TextAsset>().FirstOrDefault();
                if (sourceAsset != null) {
                    content = sourceAsset.text;
                } else {
                    var lilShaderContainer = ReflectionUtils.GetTypeFromAnyAssembly("lilToon.lilShaderContainer");
                    var unpackMethod = lilShaderContainer.GetMethods()
                        .First(m => m.Name == "UnpackContainer" && m.GetParameters().Length == 2);
                    content = (string)ReflectionUtils.CallWithOptionalParams(unpackMethod, null, path);
                    var shaderLibsPath = (string)lilShaderContainer.GetField("shaderLibsPath",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                    content = content.Replace("\"Includes", "\"" + shaderLibsPath);
                }
            } else {
                StreamReader sr = new StreamReader(path);
                try {
                    content = sr.ReadToEnd();
                } finally {
                    sr.Close();
                }
            }

            content = WithEachInclude(content, path, replaceWithFullPath: true);
            content = content.Replace("\r", "");
            return content;
        }
        
        private static void WriteFile(string path, string content) {
            StreamWriter sw = new StreamWriter(path);
            try {
                sw.Write(content);
            } finally {
                sw.Close();
            }
        }
    }
}
