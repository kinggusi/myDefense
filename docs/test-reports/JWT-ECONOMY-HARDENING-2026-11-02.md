# JWT·경제 경계 감사 및 뽑기·교배 보완 (2026-11-02)

## 범위와 승인

- User/System. 사용자가 승인한 뽑기·교배 난수 강화 및 교배 중복 조각의 정수 상한 방어다.
- 가격, 확률 가중치, 조합표, 보상량, 유닛 데이터, Battle/Shared 계약은 변경하지 않는다.
- 운영 로그인·매칭 구현을 재개하는 작업이 아니다. 기존 운영 전환 유예와 fail-closed를 유지한다.

## 감사 결과

- 신규 `/api/shop/purchases`는 JWT Principal의 userId를 사용한다. 강화·교배 등 username 기반 경로는 Principal 계정과 일치해야 한다.
- 서버 상품/가격/한도와 quote revision을 검증하며, 클라이언트가 보낸 가격으로 차감하지 않는다.
- 상점·뽑기는 User 비관락, 트랜잭션, 계정별 requestId와 payload fingerprint/저장 응답으로 중복 차감·지급을 막는다. 구 무료 뽑기와 구 신규 구매 경로는 닫혀 있다.
- Unity는 토큰 갱신 시 경제 POST를 무조건 재전송하지 않는다. 상점은 미확정 요청을 저장하고 동일 requestId로 재시도한다.
- 개발용 익명 경계는 local/dev + loopback에 제한되고 prod/production 혼합 시 차단한다. 신규 상점은 loopback에서도 Principal이 필요하다.
- 변경 전 집중 회귀: Auth 47 + Gacha 46 + Shop 40 + Breeding 11 = **144/144 PASS**.

## 구현

- `EconomyRandomSource`의 서버 전용 `SecureRandom.nextInt(bound)`를 뽑기와 교배가 사용한다. 가챠의 기존 `GachaRandomGenerator` 계약은 유지한다.
- 기존 서버 가중치 합계와 각 구간 선택 알고리즘은 그대로다. 난수원만 교체하며 클라이언트 seed나 난수를 입력받지 않는다.
- 교배 중복 보상은 long으로 예상 조각 합을 계산한다. `Integer.MAX_VALUE`를 초과하면 `MYTHIC_BREEDING_BALANCE_CAP`/HTTP 409로 거부한다. 정확히 상한인 합계는 허용한다.
- 상한 거부 시 재화, 조각, 부모 점유, 교배 결과·슬롯, 수령 영수증을 보존한다. 조각을 사용해 여유가 생기면 같은 requestId로 수령할 수 있고 성공 이후 재전송은 한 번만 지급한다.
- 전역 `UserAlien.addPieces`는 변경하지 않는다. 상점의 기존 SecureRandom도 변경하지 않는다.
- 기존 Unity 교배 오류 경로는 서버 body/message를 표시하므로 새 한글 상한 안내를 전달한다. 기존 raw JSON 오류 표시를 디자인 개선하거나 실제 화면 상한 fixture를 추가하는 작업은 이번 범위가 아니다.

## 검증

- 신규 단위: 난수 bound 검증·상한 배타성, 기존 가챠 어댑터 위임 계약.
- 신규 통합: canonical 교배 조합의 모든 결과 구간 양끝(일반 5종 + 전용 2종), 상한 직전/최댓값에서 overflow 거부, 준비 상태 전이까지 원자적 rollback, 실제 JWT HTTP 409 code/message, 상한 정확히 도달 및 동일 요청 1회 지급.
- 실제 사용자 DB/계정은 사용하지 않는다. 테스트별 H2 메모리/임시 파일 DB만 사용한다.
- 변경 후 집중 회귀: **149/149 PASS** (22 suites, 실패/오류 0). 기존 144건과 신규 5건이다.
- 전체 서버: **557/557 PASS** (86 suites, 실패/오류 0), BalanceTool: **84/84 PASS** (8 suites, 실패/오류 0). `gradlew test balanceToolTest --no-daemon`, BUILD SUCCESSFUL.
- `compileJava`, `compileTestJava`, 대상 파일 `git diff --check` PASS. Spring의 기존 MockBean/SpyBean deprecated 계열 경고는 남아 있으며 이번 새 테스트도 기존 MockBean 방식을 사용한다.
- 독립 read-only 리뷰 **PASS**, 필수 수정 0, Breaking Change 없음, Compile Risk Low. PM은 승인된 난수원·교배 조각 한도 보완 범위를 수락했다. 운영 출시 및 전체 보안 감사 완료 판정은 아니다.

## 남은 운영 게이트

- 실제 Google/Apple SDK·토큰 검증기, iOS/Android 보안 저장소, HTTPS/Secret·키 회전, rate limit, 운영 DB migration·백업·감사·세션 정리, production 매칭/Photon identity는 별도다.
- JWT는 사용자 신원 확인이며 유저 Host의 전투 결과가 진실임을 증명하지 않는다.
- 본 테스트는 실제 모바일 로그인/상점 사용자 조작 E2E나 운영 침투 테스트를 대체하지 않는다. 새 상점 구매→토큰 만료/갱신→동일 요청 복구의 실제 클라이언트 E2E는 운영 전 별도 확인한다.
- 최초 신규 테스트 컴파일에서 package-private AuthSecrets를 참조한 테스트 오류가 있었으며, 테스트 자체의 SecureRandom 비밀 생성으로 수정했다. 인증 production 클래스의 가시성은 확대하지 않았다.
- 커밋·푸시하지 않았다.
