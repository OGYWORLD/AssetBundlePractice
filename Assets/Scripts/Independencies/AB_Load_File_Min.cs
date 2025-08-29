using UnityEngine;
using System.IO;

public class AB_Load_File_Min : MonoBehaviour
{
    void Start()
    {
        // 1) 번들 파일 경로 (에디터/PC 기준)
        //    project/Assets/../AssetBundles/mybundle 로 풀어서 절대경로로 만듦
        string bundlePath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../AssetBundles/mybundle")
        );

        // 2) 번들 열기
        var bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            Debug.LogError("❌ 번들 로드 실패: " + bundlePath);
            return;
        }

        // (도움) 내부 자산 이름들 찍어보기 — 실제 키 확인용
        Debug.Log("----- [Bundle Assets] -----");
        foreach (var n in bundle.GetAllAssetNames())
            Debug.Log(n); // 보통 소문자/풀경로: "assets/prefabs/cube.prefab"

        // 3) 에셋 꺼내기: 이름은 두 가지 시도로 테스트
        GameObject prefab = null;

        // (A) addressableNames를 빌드 때 줬다면 이 이름으로도 로드 가능 (안 줬다면 실패해도 정상)
        prefab = bundle.LoadAsset<GameObject>("cube");

        // (B) 번들 내부의 실제 저장 이름(소문자 + 풀경로)로도 시도
        if (prefab == null)
            prefab = bundle.LoadAsset<GameObject>("assets/prefabs/cube.prefab");

        if (prefab == null)
        {
            Debug.LogError("❌ 프리팹을 못 찾음. 위에서 찍힌 이름으로 LoadAsset 해봐.");
            return;
        }

        // 4) 씬에 소환!
        Instantiate(prefab, Vector3.zero, Quaternion.identity);

        // 5) 메모리 관리: 핸들만 해제(인스턴스는 유지)
        bundle.Unload(false);
    }
}