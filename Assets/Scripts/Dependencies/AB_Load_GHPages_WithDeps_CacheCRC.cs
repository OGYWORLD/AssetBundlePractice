using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AB_Load_GHPages_WithDeps_CacheCRC : MonoBehaviour
{
    [SerializeField] string baseUrl = "https://ogyworld.github.io/AssetBundlePractice"; // 번들 폴더 URL (끝에 / 없음)
    [SerializeField] string manifestBundleName = "dependency";                       // 글로벌 매니페스트 번들 파일명
    [SerializeField] string mainBundleName = "cube";                                 // 메인 번들 파일명
    [SerializeField] string assetName = "cube";                                      // 꺼낼 프리팹 이름

    IEnumerator Start()
    {
        // 1) 글로벌 매니페스트 번들 다운로드
        AssetBundle manifestAB = null;
        using (var uwr = UnityWebRequestAssetBundle.GetAssetBundle(baseUrl + "/" + manifestBundleName))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(uwr.error); 
                yield break;
            }
            manifestAB = DownloadHandlerAssetBundle.GetContent(uwr);
            if (!manifestAB)
            {
                Debug.LogError("manifest bundle null"); 
                yield break;
            }
        }

        // 2) AssetBundleManifest 꺼내기
        var manifest = manifestAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        if (!manifest)
        {
            Debug.LogError("AssetBundleManifest not found");
            manifestAB.Unload(false);
            yield break;
        }

        // 3) 의존성 목록
        var deps = manifest.GetAllDependencies(mainBundleName);

        // 4) 의존성 번들들: 해시(글로벌 매니페스트) + CRC(per-bundle .manifest)로 다운로드
        var depBundles = new List<AssetBundle>(deps.Length);
        for (int i = 0; i < deps.Length; i++)
        {
            var depName = deps[i];

            // (A) 해시: 글로벌 매니페스트에서
            var depHash = manifest.GetAssetBundleHash(depName);

            // (B) CRC: 각 번들의 .manifest 텍스트에서 파싱
            uint depCrc;
            using (var mwr = UnityWebRequest.Get(baseUrl + "/" + depName + ".manifest"))
            {
                yield return mwr.SendWebRequest();
                if (mwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"dep .manifest fail ({depName}): {mwr.error}");
                    Cleanup(null, depBundles, manifestAB);
                    yield break;
                }
                depCrc = ParseCRC(mwr.downloadHandler.text);
            }

            // (C) 캐시 + CRC 다운로드
            var depCached = new CachedAssetBundle(depName, depHash);
            using (var uwr = UnityWebRequestAssetBundle.GetAssetBundle(baseUrl + "/" + depName, depCached, depCrc))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"dep download fail ({depName}): {uwr.error}");
                    Cleanup(null, depBundles, manifestAB);
                    yield break;
                }
                var depAB = DownloadHandlerAssetBundle.GetContent(uwr);
                if (!depAB)
                {
                    Debug.LogError("dep bundle null: " + depName);
                    Cleanup(null, depBundles, manifestAB);
                    yield break;
                }
                depBundles.Add(depAB);
            }
        }

        // 5) 메인 번들: 동일하게 해시 + CRC
        var mainHash = manifest.GetAssetBundleHash(mainBundleName);
        uint mainCrc;
        using (var mwr = UnityWebRequest.Get(baseUrl + "/" + mainBundleName + ".manifest"))
        {
            yield return mwr.SendWebRequest();
            if (mwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("main .manifest fail: " + mwr.error);
                Cleanup(null, depBundles, manifestAB);
                yield break;
            }
            mainCrc = ParseCRC(mwr.downloadHandler.text);
        }

        AssetBundle mainAB = null;
        var mainCached = new CachedAssetBundle(mainBundleName, mainHash);
        using (var uwr = UnityWebRequestAssetBundle.GetAssetBundle(baseUrl + "/" + mainBundleName, mainCached, mainCrc))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("main download fail: " + uwr.error);
                Cleanup(null, depBundles, manifestAB);
                yield break;
            }
            mainAB = DownloadHandlerAssetBundle.GetContent(uwr);
            if (!mainAB) 
            { Debug.LogError("main bundle null");
                Cleanup(null, depBundles, manifestAB);
                yield break; }
        }

        // 6) 에셋 Load → 7) Instantiate
        var go = mainAB.LoadAsset<GameObject>(assetName)
              ?? mainAB.LoadAsset<GameObject>("assets/prefabs/" + assetName + ".prefab");
        if (!go)
        {
            Debug.LogError("asset not found: " + assetName);
            Cleanup(mainAB, depBundles, manifestAB);
            yield break;
        }
        Instantiate(go);

        // 8) 정리(핸들만 해제)
        Cleanup(mainAB, depBundles, manifestAB);
    }

    static uint ParseCRC(string text)
    {
        var m = Regex.Match(text, @"CRC:\s*(\d+)");
        return uint.Parse(m.Groups[1].Value);
    }

    static void Cleanup(AssetBundle main, List<AssetBundle> deps, AssetBundle manifestAB)
    {
        if (main) main.Unload(false);
        if (deps != null)
        {
            for (int i = 0; i < deps.Count; i++)
            {
                if (deps[i])
                {
                    deps[i].Unload(false);
                }
            }
            deps.Clear();
        }

        if (manifestAB)
        {
            manifestAB.Unload(false);
        }
    }
}
