using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class AB_Load_File_WithDeps_Min : MonoBehaviour
{
    [Header("번들 루트 폴더(프로젝트 루트 기준)")]
    [SerializeField] string bundlesDirName = "AssetBundles/dependency";

    [Header("메인 번들 파일명(빌드 결과와 정확히 동일)")]
    [SerializeField] string mainBundleName = "cube";

    [Header("메인 번들에서 꺼낼 프리팹 이름")]
    [SerializeField] string assetName = "cube";

    [Header("글로벌 매니페스트 파일명(비워두면 자동 추정)")]
    [SerializeField] string manifestFileName = ""; // 비워두면 추정 시도

void Start()
{
    // 0) 번들 루트 절대경로
    var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", bundlesDirName));
    if (!Directory.Exists(root))
    {
        Debug.LogError("번들 폴더 없음: " + root);
        return;
    }

    // 1) 글로벌 매니페스트(바이너리 번들) 열기
    var manifestName = string.IsNullOrEmpty(manifestFileName) ? GuessManifestName(root) : manifestFileName;
    if (string.IsNullOrEmpty(manifestName))
    {
        Debug.LogError("글로벌 매니페스트 파일명을 찾지 못했어요. 인스펙터에 직접 넣어주세요.");
        return;
    }
    var manifestBundle = AssetBundle.LoadFromFile(Path.Combine(root, manifestName));
    if (!manifestBundle)
    {
        Debug.LogError("글로벌 매니페스트 번들 로드 실패");
        return;
    }
    var manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
    if (manifest == null)
    {
        Debug.LogError("AssetBundleManifest 에셋 없음");
        manifestBundle.Unload(false);
        return;
    }

    // 2) 의존성 목록 조회 → 먼저 로드
    var deps = manifest.GetAllDependencies(mainBundleName);
    var depHandles = new List<AssetBundle>(deps.Length);
    for (int i = 0; i < deps.Length; i++)
    {
        var depPath = Path.Combine(root, deps[i]);
        var dep = AssetBundle.LoadFromFile(depPath);
        if (!dep)
        {
            Debug.LogError("의존성 번들 로드 실패: " + depPath);
            Cleanup(null, depHandles, manifestBundle);
            return;
        }
        depHandles.Add(dep);
    }

    // 3) 메인 번들 로드
    var mainPath = Path.Combine(root, mainBundleName);
    var main = AssetBundle.LoadFromFile(mainPath);
    if (!main)
    {
        Debug.LogError("메인 번들 로드 실패: " + mainPath);
        Cleanup(null, depHandles, manifestBundle);
        return;
    }

    // 4) 에셋 Load → RAM 탑재
    GameObject prefab = main.LoadAsset<GameObject>(assetName);
    if (!prefab)
        prefab = main.LoadAsset<GameObject>("assets/prefabs/" + assetName + ".prefab"); // 경로형 대안

    if (!prefab)
    {
        Debug.LogError("프리팹을 찾지 못했어요. 번들 내부 이름을 확인해줘.");
        Cleanup(main, depHandles, manifestBundle);
        return;
    }

    // 5) Instantiate → 씬에 소환
    Instantiate(prefab);

    // 6) 정리: 핸들만 해제(씬 인스턴스 유지)
    Cleanup(main, depHandles, manifestBundle);
}

static string GuessManifestName(string root)
{
    // 폴더 안의 파일명들 가져오기
    var files = Directory.GetFiles(root);
    foreach (var file in files)
    {
        var name = Path.GetFileName(file);
        // 확장자가 없는 파일이면 매니페스트로 간주
        if (!name.Contains("."))
            return name;
    }
    return null; // 못 찾으면 null 반환
}

static void Cleanup(AssetBundle main, List<AssetBundle> deps, AssetBundle manifestBundle)
{
    if (main) main.Unload(false);
    if (deps != null)
    {
        for (int i = 0; i < deps.Count; i++)
            if (deps[i]) deps[i].Unload(false);
        deps.Clear();
    }
    if (manifestBundle) manifestBundle.Unload(false);
}
}
