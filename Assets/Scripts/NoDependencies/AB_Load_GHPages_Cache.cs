using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text.RegularExpressions;

public class AB_Load_GHPages_Cache : MonoBehaviour
{
    [SerializeField] string bundleUrl = "https://ogyworld.github.io/AssetBundlePractice/mybundle";
    [SerializeField] string cacheName = "mybundle";
    [SerializeField] string assetName = "cube";

    IEnumerator Start()
    {
        var murl = bundleUrl + ".manifest";
        using (var mwr = UnityWebRequest.Get(murl))
        {
            yield return mwr.SendWebRequest();
            if (mwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(mwr.error);
                yield break;
            }
            var t = mwr.downloadHandler.text;
            var crc = uint.Parse(Regex.Match(t, @"CRC:\s*(\d+)").Groups[1].Value);
            var hash = Hash128.Parse(Regex.Match(t, @"Hash:\s*([0-9a-fA-F]{32})").Groups[1].Value.ToLower());
            var cached = new CachedAssetBundle(cacheName, hash);

            using (var uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl, cached, crc))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(uwr.error);
                    yield break;
                }
                var bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                var go = bundle.LoadAsset<GameObject>(assetName) ?? bundle.LoadAsset<GameObject>("assets/prefabs/" + assetName + ".prefab");
                if (!go)
                {
                    Debug.LogError("asset not found");
                    yield break;
                }
                Instantiate(go);
                bundle.Unload(false);
            }
        }
    }
}