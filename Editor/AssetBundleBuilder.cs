using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityUpdater;

public class UpdaterData : ScriptableObject
{
    public int version;
    public string copyPath;
}

public static class ZipArchiveExtensions
{
    public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
    {
        if (!overwrite)
        {
            archive.ExtractToDirectory(destinationDirectoryName);
            return;
        }
        foreach (ZipArchiveEntry file in archive.Entries)
        {
            string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
            string directory = Path.GetDirectoryName(completeFileName);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (file.Name != "")
                file.ExtractToFile(completeFileName, true);
        }
    }
}
public class UpdaterWindow : EditorWindow
{
    
    private static bool HasSettingsFile()
    {
        if (AssetDatabase.IsValidFolder("Assets/UnityUpdater") == false)
        {
            return false;
        }

        return AssetDatabase.LoadAssetAtPath<UpdaterData>("Assets/UnityUpdater/UpdaterData.asset") == null ?    
            false : true;
        
    }

    private static UpdaterData data;

    [MenuItem("Window/AutoUpdater")]
    public static void ShowWindow()
    {


        if (HasSettingsFile() == false)
        {
            Debug.Log("No settings file found creating one!");

            data = ScriptableObject.CreateInstance<UpdaterData>();
            data.version = 1;
            if (AssetDatabase.IsValidFolder("Assets/UnityUpdater") == false)
                AssetDatabase.CreateFolder("Assets", "UnityUpdater");

            string name = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/UnityUpdater/UpdaterData.asset");
            AssetDatabase.CreateAsset(data, name);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = data;
        }
        else
        {
            LoadSettingsFile();

        }
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(UpdaterWindow));
    }

    private static void LoadSettingsFile()
    {
        Debug.Log("Found settings file loading data!");
        data = AssetDatabase.LoadAssetAtPath<UpdaterData>("Assets/UnityUpdater/UpdaterData.asset");
        if (data == null)
        {
            Debug.LogError("Failed to load the data file for UnityUpdater");
        }
    }

    private static List<AssetBundleData> oldBundles = new List<AssetBundleData>();
    private static List<AssetBundleData> newBundles = new List<AssetBundleData>();

    private void LoadOldBundleData()
    {
        oldBundles.Clear();
        newBundles.Clear();
        var path = Path.Combine(Application.streamingAssetsPath, EditorUserBuildSettings.activeBuildTarget.ToString(), "versionData.json");
        if (!File.Exists(path))
            return;
        var json = File.ReadAllText(path);
        oldBundles = JsonConvert.DeserializeObject<List<AssetBundleData>>(json);
        
    }
    private void BuildNewBundles(string oPath)
    {
        var names = AssetDatabase.GetAllAssetBundleNames();
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
        
        foreach (string assetBundle in names)
        {
            Debug.Log(assetBundle + " _ name!");
            var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundle);

            AssetBundleBuild build = new AssetBundleBuild();
            build.assetBundleName = assetBundle;
            build.assetNames = assetPaths;
            builds.Add(build);


            if (Directory.Exists(Application.streamingAssetsPath) == false)
                Directory.CreateDirectory(Application.streamingAssetsPath);



            if (!Directory.Exists(oPath))
                Directory.CreateDirectory(oPath);

            BuildPipeline.BuildAssetBundles(oPath, builds.ToArray(),

            BuildAssetBundleOptions.ChunkBasedCompression |
            BuildAssetBundleOptions.DeterministicAssetBundle,
            EditorUserBuildSettings.activeBuildTarget);


        }
        AssetDatabase.Refresh();
    }
    private bool shouldCopy = false;
    void OnGUI()
    {
        if (data == null)
            LoadSettingsFile();

        EditorGUILayout.BeginVertical();
        data.version = EditorGUILayout.IntField("Version: ", data.version);


        shouldCopy = EditorGUILayout.Toggle("Copy to local path?", shouldCopy);
        if(shouldCopy)
        {
            EditorGUILayout.BeginHorizontal();
            data.copyPath = EditorGUILayout.TextField("Copy path", data.copyPath);
            if(GUILayout.Button("change")== true)
            {
                data.copyPath = EditorUtility.OpenFolderPanel("Load png Textures", "", "");
            }
        }

        var targetString = EditorUserBuildSettings.activeBuildTarget.ToString();
        var oPath = Path.Combine(Application.streamingAssetsPath, targetString);

        if (GUILayout.Button("Build Bundles"))
        {
            LoadOldBundleData();
            BuildNewBundles(oPath);
            LoadGlobalManifest(oPath, targetString);
        }
        EditorGUILayout.EndVertical();

    }

    
    private void LoadGlobalManifest(string oPath, string target)
    {

        AssetBundle manifestBundle = AssetBundle.LoadFromFile(Path.Combine(oPath, target));
        AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        
        foreach (var bundle in manifest.GetAllAssetBundles())
        {
            Debug.Log(bundle);
            LoadBundleData(bundle, oPath);
        }
        CompareBundles(oPath, target);


        var json = JsonConvert.SerializeObject(newBundles, Formatting.Indented);
        Debug.Log(json);
        File.WriteAllText(Path.Combine(oPath, "versionData.json"), json);
        File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "version.txt"), data.version.ToString());
        AddFileToZip("versionData.json", target, oPath);

        if(shouldCopy == true)
        {
            var srcFile = Path.Combine(Application.streamingAssetsPath, target + "_verion_" + data.version + ".zip");
            if (File.Exists(srcFile))
            {
                var filePath = Path.Combine(data.copyPath, target + "_verion_" + data.version + ".zip");
                if(Directory.Exists(Path.GetDirectoryName(filePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));


                File.Copy(srcFile, filePath, true);

                using (FileStream zipToOpen = new FileStream(filePath, FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        var outPath = Path.Combine(Path.GetDirectoryName(filePath), target);
                        if (Directory.Exists(outPath) == false)
                            Directory.CreateDirectory(outPath);
                        archive.ExtractToDirectory(outPath, true);
                    }
                }
                File.Copy(Path.Combine(Application.streamingAssetsPath, "version.txt"), Path.Combine(data.copyPath, "version.txt"), true);
            }
        }

    }

    
    private void CompareBundles(string oPath, string target)
    {
        foreach (var newBundle in newBundles)
        {
            if (oldBundles.Contains(newBundle) == false)
            {
                AddFileToZip(newBundle.name, target, oPath);
                AddFileToZip(newBundle.name + ".manifest", target, oPath);
            }
        }

        AddFileToZip(target, target, oPath);
        AddFileToZip(target + ".manifest", target, oPath);
        
    }

    static void AddFileToZip(string fileName, string target, string oPath)
    {
        var zipPath = Path.Combine(Application.streamingAssetsPath, target + "_verion_" + data.version + ".zip");
        using (FileStream zipToOpen = new FileStream(zipPath, FileMode.OpenOrCreate))
        {
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
            {
                archive.CreateEntryFromFile(Path.Combine(oPath, fileName), fileName);
            }
        }
    }

    private static void LoadBundleData(string name, string oPath)
    {
        var path = Path.Combine(oPath, name);
        if (BuildPipeline.GetCRCForAssetBundle(path, out uint crc))
        {
            if(BuildPipeline.GetHashForAssetBundle(path, out Hash128 hash))
            {
                var assetData = new AssetBundleData()
                {
                    name = name,
                    crc = crc,
                    hash = hash.ToString()
                };
                newBundles.Add(assetData);
                
            }
        }

        
    }

}