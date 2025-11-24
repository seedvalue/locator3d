using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class SceneReport
{
    public string sceneName;
    public string scenePath;
    public int totalObjectCount;
    public List<GameObjectReport> objects = new List<GameObjectReport>();
    public List<string> warnings = new List<string>();
    public Dictionary<string, string> uniqueMeshHashes = new Dictionary<string, string>(); // hash → meshName
    public Dictionary<string, string> uniqueMaterialHashes = new Dictionary<string, string>(); // hash → materialName
}

[Serializable]
public class GameObjectReport
{
    public string name;
    public string instanceID;
    public string tag;
    public int layer;
    public string layerName;
    public bool activeSelf;
    public bool activeInHierarchy;
    public string hierarchyPath;
    public TransformData transform;
    public string prefabStatus;
    public bool isStatic;
    public List<ComponentReport> components = new List<ComponentReport>();
    public List<string> missingDataNotes = new List<string>();
}

[Serializable]
public class TransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

[Serializable]
public class ComponentReport
{
    public string typeName;
    public string fullName;
    public bool isCustomScript;
    public string serializedData;
    public bool partialData = false;
    public List<string> warnings = new List<string>();
    public Dictionary<string, object> enrichedData = new Dictionary<string, object>();
}

public static class SceneAnalyzerAI
{
    private static readonly HashSet<string> _collectedMeshHashes = new HashSet<string>();
    private static readonly HashSet<string> _collectedMaterialHashes = new HashSet<string>();

    [MenuItem("Tools/AI Scene Analyzer/Generate Full Scene Report (with UI & Hashes)")]
    public static void AnalyzeSceneAndSaveReport()
    {
        _collectedMeshHashes.Clear();
        _collectedMaterialHashes.Clear();

        var scene = SceneManager.GetActiveScene();
        var report = new SceneReport
        {
            sceneName = scene.name,
            scenePath = scene.path,
            totalObjectCount = 0
        };

        var rootObjects = scene.GetRootGameObjects().ToList();
        report.totalObjectCount = CountAllGameObjects(rootObjects);

        foreach (var root in rootObjects)
        {
            TraverseGameObject(root, report, "");
        }

        // Заполняем словари уникальных хэшей
        foreach (var obj in report.objects)
        {
            foreach (var comp in obj.components)
            {
                if (comp.enrichedData.TryGetValue("meshHash", out object meshHashObj) && meshHashObj is string meshHash)
                {
                    string meshName = comp.enrichedData.TryGetValue("meshName", out object name) ? name.ToString() : "unnamed";
                    if (!_collectedMeshHashes.Contains(meshHash))
                    {
                        report.uniqueMeshHashes[meshHash] = meshName;
                        _collectedMeshHashes.Add(meshHash);
                    }
                }

                if (comp.enrichedData.TryGetValue("materialHashes", out object matHashesObj) && matHashesObj is string matHashesStr)
                {
                    var hashes = matHashesStr.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < hashes.Length; i++)
                    {
                        string hash = hashes[i].Trim();
                        string matName = "material_" + i;
                        if (comp.enrichedData.TryGetValue("materialNames", out object names) && names is string namesStr)
                        {
                            var nameList = namesStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (i < nameList.Length) matName = nameList[i].Trim();
                        }

                        if (!_collectedMaterialHashes.Contains(hash))
                        {
                            report.uniqueMaterialHashes[hash] = matName;
                            _collectedMaterialHashes.Add(hash);
                        }
                    }
                }
            }
        }

        string defaultName = $"{scene.name}_AI_Report_UI_Hashes.json";
        string path = EditorUtility.SaveFilePanel("Save AI Scene Report (with UI & Hashes)", "", defaultName, "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(path, json);
            Debug.Log($"✅ AI Scene Report (with UI & Hashes) saved to:\n{path}");
            EditorUtility.RevealInFinder(path);
        }
    }

    private static int CountAllGameObjects(List<GameObject> roots)
    {
        int count = 0;
        foreach (var root in roots)
        {
            count += 1 + root.GetComponentsInChildren<Transform>(true).Length - 1;
        }
        return count;
    }

    private static void TraverseGameObject(GameObject go, SceneReport report, string parentPath)
    {
        string currentPath = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";
        var goReport = new GameObjectReport
        {
            name = go.name,
            instanceID = go.GetInstanceID().ToString(),
            tag = go.CompareTag("Untagged") ? "Untagged" : go.tag,
            layer = go.layer,
            layerName = LayerMask.LayerToName(go.layer),
            activeSelf = go.activeSelf,
            activeInHierarchy = go.activeInHierarchy,
            hierarchyPath = currentPath,
            transform = GetTransformData(go.transform),
            prefabStatus = GetPrefabStatus(go),
            isStatic = go.isStatic
        };

        var components = go.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var compReport = AnalyzeComponent(comp);
            goReport.components.Add(compReport);
        }

        if (goReport.components.Count == 0)
        {
            goReport.missingDataNotes.Add("No components found.");
        }

        report.objects.Add(goReport);

        foreach (Transform child in go.transform)
        {
            TraverseGameObject(child.gameObject, report, currentPath);
        }
    }

    private static TransformData GetTransformData(Transform t)
    {
        return new TransformData
        {
            position = t.position,
            rotation = t.rotation,
            scale = t.localScale
        };
    }

    private static string GetPrefabStatus(GameObject go)
    {
#if UNITY_2021_2_OR_NEWER
        var status = PrefabUtility.GetPrefabInstanceStatus(go);
        return status.ToString();
#else
        var type = PrefabUtility.GetPrefabType(go);
        return type.ToString();
#endif
    }

    private static ComponentReport AnalyzeComponent(Component comp)
    {
        var report = new ComponentReport
        {
            typeName = comp.GetType().Name,
            fullName = comp.GetType().FullName,
            isCustomScript = comp is MonoBehaviour && !comp.GetType().IsDefined(typeof(UnityEditor.InitializeOnLoadAttribute), false)
        };

        try
        {
            if (comp.GetType().IsSerializable || comp is MonoBehaviour)
            {
                report.serializedData = JsonUtility.ToJson(comp, true);
            }
            else
            {
                report.warnings.Add("Not serializable via JsonUtility.");
                report.partialData = true;
            }
        }
        catch (Exception e)
        {
            report.warnings.Add($"Serialization failed: {e.Message}");
            report.partialData = true;
        }

        EnrichComponentData(comp, report);
        return report;
    }

    private static void EnrichComponentData(Component comp, ComponentReport report)
    {
        // === UI Components ===
        if (comp is Canvas canvas)
        {
            report.enrichedData["renderMode"] = canvas.renderMode.ToString();
            report.enrichedData["sortingOrder"] = canvas.sortingOrder;
            report.enrichedData["overrideSorting"] = canvas.overrideSorting;
            report.enrichedData["referencePixelsPerUnit"] = canvas.referencePixelsPerUnit;
            if (canvas.worldCamera != null)
                report.enrichedData["worldCamera"] = canvas.worldCamera.name;
        }

        else if (comp is Text text)
        {
            report.enrichedData["text"] = text.text;
            report.enrichedData["fontSize"] = text.fontSize;
            report.enrichedData["font"] = text.font != null ? text.font.name : "null";
            report.enrichedData["color"] = text.color.ToString();
            report.enrichedData["alignment"] = text.alignment.ToString();
            report.enrichedData["resizeTextForBestFit"] = text.resizeTextForBestFit;
        }

        else if (comp is TMPro.TMP_Text tmpText)
        {
            report.enrichedData["text"] = tmpText.text;
            report.enrichedData["fontSize"] = tmpText.fontSize;
            report.enrichedData["font"] = tmpText.font != null ? tmpText.font.name : "null";
            report.enrichedData["color"] = tmpText.color.ToString();
            report.enrichedData["alignment"] = tmpText.alignment.ToString();
        }

        else if (comp is Button button)
        {
            report.enrichedData["interactable"] = button.interactable;
            report.enrichedData["navigation"] = button.navigation.mode.ToString();
            var image = button.GetComponent<Image>();
            if (image != null && image.sprite != null)
                report.enrichedData["buttonSprite"] = image.sprite.name;
        }

        else if (comp is Image image)
        {
            report.enrichedData["imageType"] = image.type.ToString();
            report.enrichedData["fillMethod"] = image.fillMethod.ToString();
            if (image.sprite != null)
                report.enrichedData["sprite"] = image.sprite.name;
            report.enrichedData["color"] = image.color.ToString();
        }

        else if (comp is RawImage rawImage)
        {
            if (rawImage.texture != null)
                report.enrichedData["texture"] = rawImage.texture.name;
            report.enrichedData["color"] = rawImage.color.ToString();
        }

        else if (comp is Toggle toggle)
        {
            report.enrichedData["isOn"] = toggle.isOn;
            report.enrichedData["interactable"] = toggle.interactable;
        }

        else if (comp is Slider slider)
        {
            report.enrichedData["value"] = slider.value;
            report.enrichedData["minValue"] = slider.minValue;
            report.enrichedData["maxValue"] = slider.maxValue;
            report.enrichedData["wholeNumbers"] = slider.wholeNumbers;
        }

        // === Mesh & Materials (with Hashes) ===
        else if (comp is MeshFilter mf)
        {
            if (mf.sharedMesh != null)
            {
                var mesh = mf.sharedMesh;
                report.enrichedData["meshName"] = mesh.name;
                report.enrichedData["vertexCount"] = mesh.vertexCount;
                report.enrichedData["triangleCount"] = mesh.triangles.Length / 3;

                string meshHash = ComputeMeshHash(mesh);
                report.enrichedData["meshHash"] = meshHash;
            }
            else
            {
                report.warnings.Add("MeshFilter has no mesh.");
                report.partialData = true;
            }
        }

        else if (comp is Renderer rend)
        {
            report.enrichedData["materialCount"] = rend.sharedMaterials.Length;
            var matNames = new List<string>();
            var matHashes = new List<string>();

            foreach (var mat in rend.sharedMaterials)
            {
                string name = mat != null ? mat.name : "null";
                matNames.Add(name);

                if (mat != null)
                {
                    string hash = ComputeMaterialHash(mat);
                    matHashes.Add(hash);
                }
                else
                {
                    matHashes.Add("null_mat");
                }
            }

            report.enrichedData["materialNames"] = string.Join(", ", matNames);
            report.enrichedData["materialHashes"] = string.Join(";", matHashes);
            report.enrichedData["isVisible"] = rend.isVisible;
        }

        // === Standard Components (Rigidbody, Collider, etc.) ===
        else if (comp is Collider col)
        {
            report.enrichedData["isTrigger"] = col.isTrigger;
            report.enrichedData["boundsCenter"] = col.bounds.center.ToString();
            report.enrichedData["boundsSize"] = col.bounds.size.ToString();

            if (col is BoxCollider) report.enrichedData["colliderType"] = "Box";
            else if (col is SphereCollider) report.enrichedData["colliderType"] = "Sphere";
            else if (col is CapsuleCollider) report.enrichedData["colliderType"] = "Capsule";
            else if (col is MeshCollider) report.enrichedData["colliderType"] = "Mesh";
            else report.enrichedData["colliderType"] = "Other";
        }

        else if (comp is Rigidbody rb)
        {
            report.enrichedData["mass"] = rb.mass;
            report.enrichedData["useGravity"] = rb.useGravity;
            report.enrichedData["isKinematic"] = rb.isKinematic;
        }

        else if (comp is Animator anim)
        {
            report.enrichedData["animatorController"] = anim.runtimeAnimatorController?.name ?? "null";
            report.enrichedData["hasAvatar"] = anim.avatar != null;
            report.enrichedData["isHumanoid"] = anim.isHuman;
        }

        else if (comp is AudioSource audio)
        {
            report.enrichedData["clipName"] = audio.clip?.name ?? "null";
            report.enrichedData["volume"] = audio.volume;
            report.enrichedData["isPlaying"] = audio.isPlaying;
        }

        else if (comp is Light light)
        {
            report.enrichedData["lightType"] = light.type.ToString();
            report.enrichedData["intensity"] = light.intensity;
            report.enrichedData["range"] = light.range;
            report.enrichedData["color"] = light.color.ToString();
        }

        else if (comp is Camera cam)
        {
            report.enrichedData["fieldOfView"] = cam.fieldOfView;
            report.enrichedData["nearClipPlane"] = cam.nearClipPlane;
            report.enrichedData["farClipPlane"] = cam.farClipPlane;
            report.enrichedData["isActive"] = cam.isActiveAndEnabled;
        }

        // === Script path ===
        else if (comp is MonoBehaviour mb)
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(mb));
            if (!string.IsNullOrEmpty(scriptPath))
            {
                report.enrichedData["scriptPath"] = scriptPath;
            }
            else
            {
                report.warnings.Add("Script asset path not found.");
                report.partialData = true;
            }
        }

        // Если ничего не добавлено — помечаем как нераспознанный компонент
        if (report.enrichedData.Count == 0 && string.IsNullOrEmpty(report.serializedData))
        {
            report.warnings.Add("No data extracted. Unknown or unsupported component type.");
            report.partialData = true;
        }
    }

    // === Хэширование ===
    private static string ComputeMeshHash(Mesh mesh)
    {
        try
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            using (var sha = SHA256.Create())
            {
                byte[] data = new byte[vertices.Length * 12 + triangles.Length * 4];
                Buffer.BlockCopy(Array.ConvertAll(vertices, v => BitConverter.GetBytes(v.x)).SelectMany(b => b).ToArray(), 0, data, 0, vertices.Length * 4);
                Buffer.BlockCopy(Array.ConvertAll(vertices, v => BitConverter.GetBytes(v.y)).SelectMany(b => b).ToArray(), 0, data, vertices.Length * 4, vertices.Length * 4);
                Buffer.BlockCopy(Array.ConvertAll(vertices, v => BitConverter.GetBytes(v.z)).SelectMany(b => b).ToArray(), 0, data, vertices.Length * 8, vertices.Length * 4);
                Buffer.BlockCopy(Array.ConvertAll(triangles, t => BitConverter.GetBytes(t)).SelectMany(b => b).ToArray(), 0, data, vertices.Length * 12, triangles.Length * 4);

                byte[] hash = sha.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to compute mesh hash for {mesh.name}: {e.Message}");
            return "hash_error";
        }
    }

    private static string ComputeMaterialHash(Material mat)
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(mat.shader?.name ?? "null_shader");
            sb.Append("|");
            sb.Append(mat.color.r).Append(",").Append(mat.color.g).Append(",").Append(mat.color.b).Append(",").Append(mat.color.a);
            sb.Append("|");
            var mainTex = mat.mainTexture;
            sb.Append(mainTex != null ? mainTex.name + "_" + mainTex.width + "x" + mainTex.height : "no_mainTex");

            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to compute material hash for {mat.name}: {e.Message}");
            return "hash_error";
        }
    }
}