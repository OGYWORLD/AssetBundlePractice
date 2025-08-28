#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class AB_Build_WithDeps_Simple
{ 
    // 메뉴: Tools/AB/Build (Split with Deps)
[MenuItem("Tools/Build (Dependency)")]
static void BuildSplitWithDeps()
{
    // 1) 프로젝트 에셋 경로(반드시 "Assets/..." 풀 경로)
    var texPath    = "Assets/Textures/images.png";
    var matPath    = "Assets/Materials/UnityMaterial.mat";
    var prefabPath = "Assets/Prefabs/cube.prefab";

    // 2) 번들 정의(여러 개) — 네가 쓰던 AssetBundleBuild[] 패턴 그대로
    var texBundle = new AssetBundleBuild
    {
        assetBundleName = "tex",
        assetNames      = new[] { texPath }
        // addressableNames 생략 가능
    };

    var matBundle = new AssetBundleBuild
    {
        assetBundleName = "mat",
        assetNames      = new[] { matPath }
    };

    var prefabBundle = new AssetBundleBuild
    {
        assetBundleName = "cube",
        assetNames      = new[] { prefabPath }
    };

    var builds = new[] { texBundle, matBundle, prefabBundle };

    // 3) 출력 폴더 — 네 샘플에 맞춰 "AssetBundles" 루트 사용
    var outDir = "AssetBundles";
    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

    // 4) 빌드 옵션 — 네 샘플과 동일하게 LZ4(ChunkBasedCompression)만 사용해도 OK
    //    필요하면 StrictMode/ForceRebuild 붙여도 됨
    var options = BuildAssetBundleOptions.ChunkBasedCompression;
    var target  = EditorUserBuildSettings.activeBuildTarget;

    // 5) 빌드 실행 — 반환되는 manifest는 '글로벌 매니페스트' (의존성 정보 포함)
    var manifest = BuildPipeline.BuildAssetBundles(outDir, builds, options, target);
    if (manifest == null)
    {
        Debug.LogError("❌ AssetBundle 빌드 실패");
        return;
    }
    
    EditorUtility.RevealInFinder(outDir);
}
}
#endif
