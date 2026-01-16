# 🛡️ MyDefenseGame - 개발 가이드라인

## 1. 📂 프로젝트 구조 (Project Structure)
우리는 **기능별 분리**와 **공통 모듈(`@Core`)** 사용을 원칙으로 한다.

### Assets/Scripts
- **📂 @Core** : 공통 모듈 (건드리기 전 상의 필수)
  - `Managers/` : 싱글톤 매니저 (GameManager, SoundManager 등)
  - `Utils/` : 헬퍼 클래스, EventManager
  - `Defines/` : `Define.cs` (상수, Enum, 태그 관리)
- **📂 Scenes** : 씬별 로직 스크립트
- **📂 Units** : 유닛/머지 관련 스크립트
- **📂 Battle** : 전투/AI 관련 스크립트

## 2. 📜 코딩 컨벤션 (Conventions)
1. **하드코딩 금지**: 태그, 씬 이름은 반드시 `Define.cs`에 정의 후 사용.
   - ❌ `if (tag == "Monster")`
   - ✅ `if (compareTag(Define.Tags.Monster))`
2. **매니저 접근**: `Singleton<T>` 패턴 사용.
   - `GameManager.Instance.AddGold(100);`
3. **데이터**: 기획 데이터는 `ScriptableObject` 활용.

## 3. 🌿 Git 브랜치 전략
- **main**: 배포 가능한 상태 (터치 금지)
- **dev**: 개발 통합 브랜치 (PR Merge 대상)
- **feature/** : 개별 기능 개발
  - `feature/merge-system` (A)
  - `feature/nav-system` (B)