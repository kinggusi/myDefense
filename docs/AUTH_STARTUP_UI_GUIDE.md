# 로그인·시작 화면 교체 가이드

## 현재 구현 범위

- `SampleScene`의 로비 초기화 전에 로그인 오버레이를 띄운다. 별도 production 시작 Scene이나 Unity 기본 Splash를 교체한 것은 아니다.
- 기존 우주선 배경, 초록 왹져 마스코트, 짙은 남색 패널과 청록 프레임을 사용한다. 기존 프로필의 배치·글꼴·프레임은 바꾸지 않는다.
- 게스트 프로필 이름은 서버가 발급한 실제 계정 번호로 `Guest-{userId}`를 표시한다(예: `Guest-2`). 내부 username·인증 식별자는 변경하지 않으며 일반 계정은 기존 username을 표시한다.
- 게스트 생성/복구 → 계정 확인 → 유닛 정보 조회 → 로비 준비 순서다. 로딩은 실제 처리 단계이며 임의의 0~100% 진행률이 아니다.
- 메인(전투) 탭의 `계정 연동` 진입에서 현재 계정을 Google/Apple에 연결하는 경계를 제공한다. 실제 제공자 설정/네이티브 SDK가 없으면 버튼을 비활성화한다.

## 아트·문구 교체

1. Unity Project에서 `Assets/Resources/Auth/StartupTheme.asset`을 선택한다.
2. `background`에 배경 Texture, `logo`에 로고 Sprite, `mascot`에 마스코트 Sprite를 넣는다. 로고가 없으면 제목 텍스트를 표시한다.
3. `font`, `accent/text/muted/error`, 제목·부제·시작 버튼·게스트 안내를 변경한다. 배경은 종횡비를 유지해 채우며 장식은 입력을 가로채지 않는다.
4. 배치까지 바꿀 때는 `Assets/Resources/Auth/AuthStartupView.prefab`을 Prefab Mode에서 수정한다. `AuthStartupView`의 버튼·라벨·단계·SafeArea 참조는 유지한다.
5. `Tools/MyDefense/Auth/Rebuild Startup UI`는 테마 값은 유지하지만 Prefab 배치를 재생성한다. 수작업 Prefab 수정 후 무심코 실행하지 않는다.
6. 외부 로그인 실제 활성화 때 Google/Apple 공식 브랜드 버튼 규칙에 맞는 최종 버튼 에셋을 연결한다. 현재 준비 중 표시를 운영 승인된 로그인 버튼으로 간주하지 않는다.

인증 흐름은 `AuthSession`, 화면 흐름은 `AuthStartupController`, 외관은 `AuthStartupView/StartupTheme`로 분리했다. 진입 정책 `GuestFirst/ProviderRequired`도 분리했으나, `ProviderRequired` 전환 전 실제 제공자 구현이 반드시 필요하다.

## 개발 실행 및 계정 분리

- API 설정은 기존 `MYDEFENSE_API_BASE_URL`을 사용한다. 운영은 HTTPS, Editor/Development만 loopback HTTP를 허용한다.
- 기본은 인증 진입이다. 과거 username 로비는 Editor/Development에서 `MYDEFENSE_ENV=local` 또는 `dev`와 `MYDEFENSE_LEGACY_LOBBY=1`이 모두 있어야 한다. 운영 대체 인증이 아니다.
- Windows의 게스트 복구 키/Refresh Token은 DPAPI 암호화 파일로 보관한다. Access Token은 메모리에만 둔다. 동일 파일 동시 사용은 잠금으로 차단한다.
- 한 PC의 두 개발 실행 계정은 시작 전 서로 다른 `MYDEFENSE_AUTH_PROFILE`을 사용한다. 이 변수는 Editor/Development 전용이다. API 주소별 저장 공간도 분리된다.
- 파일이나 비밀 키를 삭제해 새 계정을 만드는 동작은 제공하지 않는다. 저장소 오류가 나면 기존 자료를 보존하고 중단한다.
- **Android Keystore/iOS Keychain 구현은 아직 없다. 모바일에서는 평문으로 우회하지 않고 차단한다.** 기기 빌드 검증 전 플랫폼 저장소 구현이 필요하다.
- 기본 프로필의 H2/create-drop은 영속 운영 계정 DB가 아니다. `local`은 파일 DB + `update`로 분리했다([로컬 보존 가이드](LOCAL_PERSISTENCE_GUIDE.md)). 클라이언트 복구 키가 남아 있어도 서버 DB를 지우면 계정 데이터는 복구되지 않는다.

### Editor 개발 계정 선택

1. `MYDEFENSE_ENV=local` 또는 `dev`, loopback API 서버를 준비한다.
2. Play를 멈춘 상태에서 `Tools > MyDefense > Auth > Development Login`을 연다.
3. `jjangash` 또는 `kingusi`를 선택하고 SampleScene에서 Play한다.
4. 개발 로그인 창에 비밀번호를 직접 입력해 로그인한다. 비밀번호는 저장하지 않는다.
5. 개발 계정은 각자 분리된 보안 저장소를 사용한다. Play를 멈추고 기존 게스트 모드로 되돌리면 게스트 저장소를 다시 사용한다.

이 메뉴는 Editor 개발 테스트용이다. 운영 관리자 권한·무료 재화 지급이나 Standalone EXE 계정 전환 UI를 제공하지 않는다. 실제 로그인 비밀번호 입력 화면은 사용자 확인이 남아 있다.

## Google/Apple 후속 연결

- Unity `IExternalIdentityProvider`: nonce를 네이티브 로그인 SDK에 전달하고 받은 ID Token을 반환한다. `ExternalIdentityProviders.Register`로 연결한다.
- Spring `ExternalIdentityVerifier`: 각 제공자 서명·issuer·audience·만료·nonce를 검증해 안정적인 subject를 반환한다. 현재 실제 검증기는 미구성이다.
- 기존 게스트는 새 계정으로 갈아끼우지 않고 인증된 LINK challenge로 연결한다. 다른 게임 계정에 이미 연결된 제공자 계정은 충돌 처리하며 임의 데이터 병합을 하지 않는다.
- 필요한 외부 설정: Google/Apple 개발자 설정, 앱/Bundle ID, Client ID·허용 audience, 실제 네이티브 SDK, HTTPS 운영 API, Secret 주입. 비밀 값은 Git에 넣지 않는다.
- 계정 삭제·탈퇴, 로그아웃/계정 전환 UI, 제공자 해제·복구 정책과 스토어 출시 심사는 후속 출시 게이트다.

## 검증 화면

- Feature Test Hub의 `P1-5-7-AUTH`, `Assets/Scenes/Tests/AuthStartupUiTest.unity`는 외관 전용 격리 Fixture다. 로그인 네트워크 테스트 Scene이 아니다.
- Fixture는 production Build Settings에 포함하지 않는다. 실제 인증은 `SampleScene`에서 서버와 연결해 검증한다.
- 검증 결과/미실행 항목: [AUTH-STARTUP-2026-09-18](test-reports/AUTH-STARTUP-2026-09-18.md).
