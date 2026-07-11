## Unity Scene Safety

- `.unity` 파일을 텍스트 또는 YAML로 직접 수정하지 않는다.
- `.meta` 파일과 GUID를 직접 만들거나 추측하지 않는다.
- Scene 생성과 변경은 Unity Editor API로 작성한 Editor Tool을 통해 수행한다.
- `EditorSceneManager`, `AssetDatabase`, `PrefabUtility`를 사용한다.
- 기존 에셋 참조는 경로나 `AssetDatabase.FindAssets`로 찾는다.
- 에셋을 찾지 못하면 임의 참조를 만들지 말고 경고를 출력한다.
- Editor Tool 실행 후 사람이 Unity에서 시각적 결과와 Missing Reference를 확인한다.