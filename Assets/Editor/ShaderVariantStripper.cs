using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenUniverse.Editor
{
    public class ShaderVariantStripper : IPreprocessShaders
    {
        private const string ShaderVariantCollectionAssetPath =
            "Assets/Runtime/OpenLoader/OpenLoaderShaders.shadervariants";

        private readonly ShaderVariantCollection _shaderVariantCollection;
        private ShaderVariantCollection.ShaderVariant _shaderVariantCache;
        private readonly List<string> _compilerKeywords;

        public int callbackOrder => 0;

        public ShaderVariantStripper()
        {
            _shaderVariantCollection = AssetDatabase.LoadAssetAtPath(ShaderVariantCollectionAssetPath,
                typeof(ShaderVariantCollection)
            ) as ShaderVariantCollection;

            _compilerKeywords = new List<string>();
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderData)
        {
            if (shader.name.Contains("Universal Render Pipeline")
                || shader.name.Contains("TextMeshPro")
                || shader.name.Contains("Default")
                || shader.name.Contains("Hidden")
                || shader.name.Contains("UI")
                || shader.name.Contains("Sprites")
                || shader.name.Contains("Skybox")
            ) return;

            _shaderVariantCache.shader = shader;
            _shaderVariantCache.passType = snippet.passType;
            for (var i = 0; i < shaderData.Count; ++i)
            {
                var internalShaderData = shaderData[i];
                var shaderKeywords = internalShaderData.shaderKeywordSet.GetShaderKeywords();

                _compilerKeywords.Clear();
                foreach (var internalKeyword in shaderKeywords)
                {
                    _compilerKeywords.Add(ShaderKeyword.GetGlobalKeywordName(internalKeyword));
                }

                _shaderVariantCache.keywords = _compilerKeywords.ToArray();

                if (_shaderVariantCollection.Contains(_shaderVariantCache)) continue;
                shaderData.RemoveAt(i);
                i--;
            }
        }
    }
}
