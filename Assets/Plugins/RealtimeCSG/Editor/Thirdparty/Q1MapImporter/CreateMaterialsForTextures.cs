// CreateMaterialsForTextures.cs
// C#
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.IO;

public class CreateMaterialsForTextures : ScriptableWizard
{
    public Shader shader;

    [MenuItem("Tools/CreateMaterialsForTextures")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<CreateMaterialsForTextures>("Create Materials", "Create");

    }

    void OnEnable()
    {
        shader = Shader.Find("Diffuse");
    }

    void OnWizardCreate()
    {
        try
        {
            AssetDatabase.StartAssetEditing();
            var textures = Selection.GetFiltered(typeof(Texture), SelectionMode.Assets).Cast<Texture>();
            foreach (var tex in textures)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                var directory = Path.GetDirectoryName(path);
                var filename = Path.GetFileNameWithoutExtension(path);
                //Debug.Log($"Path = {path} directory = {directory} filename = {filename}");
                //path = path.Substring(0, path.LastIndexOf(".")) + ".mat";

                path = Path.Combine(directory, "Materials");
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(directory, "Materials");
                }
                path = Path.Combine(path, filename + ".mat");
                //Debug.Log($"Path = {path}");

                if (AssetDatabase.LoadAssetAtPath(path, typeof(Material)) != null)
                {
                    Debug.LogWarning("Can't create material, it already exists: " + path);
                    continue;
                }
                var mat = new Material(shader);
                mat.mainTexture = tex;
                AssetDatabase.CreateAsset(mat, path);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }
    }
}