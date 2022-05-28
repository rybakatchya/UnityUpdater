using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityUpdater
{
    [Serializable]
    public struct AssetBundleData : IEquatable<AssetBundleData>
    {
        public string name;
        public uint crc;
        public string hash;

        public bool Equals(AssetBundleData other)
        {
            if (other.name == name && other.crc == crc && other.hash == hash)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() + crc.GetHashCode() + hash.GetHashCode();
        }
    }

    public class UpdateManager : MonoBehaviour
    {

        public string baseUrl;
        private string target = string.Empty;
        void Start()
        {

#if UNITY_STANDALON_WIN || UNITY_EDITOR_WIN
#if UNITY_64 || UNITY_EDITOR_64
            target = "StandaloneWindows64";
#else
            target = "StandaloneWindows"
#endif

#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
#if UNITY_64 || UNITY_EDITOR_64
            target = StandaloneLinux64;
#else
            target = StandaloneLinux;
#endif
#endif
            var uri = new Uri(new Uri(baseUrl), "game/version.txt");
            Debug.Log(uri.ToString());
            var co = StartCoroutine(DownloadVersionFile(uri.ToString()));
            var path = Path.Combine(Application.streamingAssetsPath, target);
            
        }

        IEnumerator DownloadVersionFile(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        CompareVersions(int.Parse(webRequest.downloadHandler.text));
                        break;
                }
            }
        }
        
        void CompareVersions(int receivedVersion)
        {
            Debug.Log(receivedVersion);
            if(PlayerPrefs.HasKey("version")  == false || PlayerPrefs.GetInt("version") != receivedVersion)
            {
                var baseURI = new Uri(baseUrl);
                var fileURI = new Uri(baseURI, "game/" + target + "/versionData.json");
                Debug.Log(fileURI);
                StartCoroutine(DownloadVersionData(fileURI.ToString()));
            }

        }

        IEnumerator DownloadVersionData(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log(webRequest.downloadHandler.text);
                        CompareVersionData(webRequest.downloadHandler.text);
                        break;
                }
            }
        }

        private void CompareVersionData(string json)
        {
            var path = Path.Combine(Application.streamingAssetsPath, target, "versionData.json");
            List<AssetBundleData> oldBundleData = null;
            if(File.Exists(path))
            {
                var text = File.ReadAllText(path);
                oldBundleData = JsonConvert.DeserializeObject<List<AssetBundleData>>(text);
            }
            else
            {
                oldBundleData = new List<AssetBundleData>();
                
            }


            List<AssetBundleData> newBundleData = JsonConvert.DeserializeObject < List<AssetBundleData>>(json);
            foreach(var bundle in newBundleData)
            {
                if(oldBundleData.Contains(bundle) == false)
                {
                   
                    DownloadBundle(bundle.name);
                }
            }

            StartCoroutine(GetManifestFile(baseUrl + "/" + target + "/" + target + ".manifest", target));
            StartCoroutine(DownloadFile(baseUrl + "/" + target + "/" + target, target));
            if (Directory.Exists(Path.GetDirectoryName(path)) == false)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }

        void DownloadBundle(string bundleName)
        {
            Debug.Log($"Requesting bundle {bundleName}");
            StartCoroutine(DownloadFile(baseUrl + "/" + target + "/" + bundleName, bundleName));
            StartCoroutine(GetManifestFile(baseUrl + "/" + target + "/" + bundleName + ".manifest", bundleName));
        }


        IEnumerator GetManifestFile(string uri, string bundleName)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        File.WriteAllText(Path.Combine(Application.streamingAssetsPath, target, bundleName + ".manifest"), webRequest.downloadHandler.text);
                        break;
                }
            }
        }

        IEnumerator DownloadFile(string uri, string fileName)
        {
            
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;
                Debug.Log($"attempting to download file {fileName} from {uri}");
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:

                        var path = Path.Combine(Application.streamingAssetsPath, target, fileName);
                        if (Directory.Exists(Path.GetDirectoryName(path)) == false)
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllBytes(path, webRequest.downloadHandler.data);
                        break;
                }
            }
        }

    }
}
