// Unity 2021.3 LTS 기준, "코드로 번들 구성 + 빌드" 딱 이것만 합니다.
using UnityEditor;
using UnityEngine;
using System.IO;

public static class Build_AssetBundles_Simple
{
    // 메뉴 한 줄 (툴바에서 실행하려면 이 정도는 필요해요)
    [MenuItem("Tools/Build AssetBundles (Simple)")]
    static void BuildSimple()
    {
        // 1) 이 번들에 넣을 에셋들(반드시 "Assets/..." 프로젝트 내부 경로)
        //    지금은 최소 예제로 Cube.prefab 하나만!
        string[] assetPaths = {
            "Assets/Prefabs/Cube.prefab"
        };

        // 2) 번들 한 개 정의: 이름은 "mybundle"
        var b = new AssetBundleBuild
        {
            assetBundleName  = "mybundle",
            assetNames       = assetPaths,
            // addressableNames는 옵션(안 적어도 됩니다)
            // addressableNames = new[] { "cube" }
        };

        // 3) 출력 폴더 준비
        string outDir = "AssetBundles";
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        // 4) 빌드 (LZ4: 런타임 로드 빠름)
        var builds = new[] { b };
        var options = BuildAssetBundleOptions.ChunkBasedCompression;
        var target  = EditorUserBuildSettings.activeBuildTarget; // 현재 에디터 플랫폼으로 빌드

        var manifest = BuildPipeline.BuildAssetBundles(outDir, builds, options, target);
        if (manifest == null)
        {
            Debug.LogError("❌ AssetBundle 빌드 실패");
            return;
        }

        Debug.Log("✅ 빌드 완료: " + Path.GetFullPath(outDir));
        EditorUtility.RevealInFinder(outDir); // 결과 폴더 열기
    }
}