using UnityEditor;
using UnityEngine;
using System.IO;
public class KenneyImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/ThirdParty/Kenney/"))
            return;
        var importer = (ModelImporter)assetImporter;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }
    void OnPostprocessMaterial(Material material)
    {
        if (!assetPath.StartsWith("Assets/ThirdParty/Kenney/"))
            return;
        material.shader = Shader.Find("Standard");
        material.SetFloat("_Glossiness", .28f);
        string dir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        foreach (string name in new[] { "colormap.png", "colormap_red.png", "texture.png" })
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/Textures/" + name);
            if (tex)
            {
                material.mainTexture = tex;
                break;
            }
        }
    }
}
