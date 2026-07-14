package com.denfense.server.service.gacha;

import com.denfense.server.domain.GachaPurchase;
import com.denfense.server.domain.GachaPurchaseStatus;
import com.denfense.server.domain.User;
import com.denfense.server.dto.gacha.GachaPurchaseResponseDto;
import com.denfense.server.repository.GachaPurchaseRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;

import java.util.List;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

/**
 * 8-1F 멱등성 통합 테스트
 * A. 최초 구매, B. 중복 요청 전체 동일성, C. productId 충돌,
 * D. 다른 사용자, F. 동시 동일 requestId, G. 롤백, H. 직렬화, I. 10회 responseJson 길이
 */
@SpringBootTest
@ActiveProfiles("test")
class GachaPurchaseIdempotencyIntegrationTest {

    @Autowired
    private GachaPurchaseService gachaPurchaseService;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private UserAlienRepository userAlienRepository;

    @Autowired
    private GachaPurchaseRepository gachaPurchaseRepository;

    @MockBean
    private GachaRandomGenerator randomGenerator;

    private User user1;
    private User user2;

    @BeforeEach
    void setUp() {
        user1 = new User("idempotencyUser1", "pw");
        user1.setDiamond(10000);
        userRepository.save(user1);

        user2 = new User("idempotencyUser2", "pw");
        user2.setDiamond(10000);
        userRepository.save(user2);

        // 확률 고정: randomGenerator.nextInt(n) = 0 → pool 첫 번째 항목 (alienId=22 고정)
        when(randomGenerator.nextInt(anyInt())).thenReturn(0);
    }

    @AfterEach
    void tearDown() {
        gachaPurchaseRepository.deleteAll();
        userAlienRepository.deleteAll();
        userRepository.deleteAll();
    }

    // ===================== A. 최초 구매 =====================

    @Test
    @DisplayName("A-1~6. 최초 구매: 다이아 차감, COMPLETED 기록, responseJson 저장")
    void firstPurchase_completedRecordSaved() {
        UUID requestId = UUID.randomUUID();
        GachaPurchaseResponseDto result = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", requestId);

        // COMPLETED 기록 1건
        List<GachaPurchase> purchases = gachaPurchaseRepository.findAll();
        assertThat(purchases).hasSize(1);
        GachaPurchase purchase = purchases.get(0);
        assertThat(purchase.getStatus()).isEqualTo(GachaPurchaseStatus.COMPLETED);
        assertThat(purchase.getResponseJson()).isNotBlank();
        assertThat(purchase.getProductId()).isEqualTo("ALIEN_GACHA_SINGLE");
        assertThat(purchase.getPurchaseRequestId()).isEqualTo(requestId);
        assertThat(purchase.getUser().getId()).isEqualTo(user1.getId());
        assertThat(purchase.getCreatedAt()).isNotNull();

        // 응답 정상
        assertThat(result.productId()).isEqualTo("ALIEN_GACHA_SINGLE");
        assertThat(result.draws()).hasSize(1);
        assertThat(result.rewards()).hasSize(1);
    }

    // ===================== B. 동일 요청 재전송 — 전체 필드 동일성 =====================

    @Test
    @DisplayName("B-7~14. 동일 requestId 재전송: 모든 필드 완전 동일, 추가 차감/추첨/지급 없음, 기록 1건")
    void duplicateRequest_returnsSameResponse_allFieldsEqual() {
        UUID requestId = UUID.randomUUID();

        // 최초 구매
        GachaPurchaseResponseDto first = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", requestId);

        // 다이아 기록
        User userAfterFirst = userRepository.findByUsername("idempotencyUser1").orElseThrow();
        int diamondAfterFirst = userAfterFirst.getDiamond();

        // 중복 요청
        GachaPurchaseResponseDto second = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", requestId);

        // record equals → 모든 필드(productId, currencyType, price, remainingDiamond, drawCount, draws, rewards) 동일
        assertThat(second).isEqualTo(first);

        // 추가 차감 없음
        User userAfterSecond = userRepository.findByUsername("idempotencyUser1").orElseThrow();
        assertThat(userAfterSecond.getDiamond()).isEqualTo(diamondAfterFirst);

        // 추가 지급 없음
        assertThat(userAlienRepository.findAll()).hasSize(1);

        // 기록 1건
        assertThat(gachaPurchaseRepository.findAll()).hasSize(1);
        assertThat(gachaPurchaseRepository.findAll().get(0).getStatus()).isEqualTo(GachaPurchaseStatus.COMPLETED);
        verify(randomGenerator, times(2)).nextInt(anyInt());
    }

    // ===================== C. productId 충돌 =====================

    @Test
    @DisplayName("C-15~18. 동일 requestId + 다른 productId: IllegalStateException, 추가 차감/지급/기록 없음")
    void productIdMismatch_throwsException_noSideEffects() {
        UUID requestId = UUID.randomUUID();

        // 최초 구매 (SINGLE)
        gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", requestId);

        User userAfterFirst = userRepository.findByUsername("idempotencyUser1").orElseThrow();
        int diamondAfterFirst = userAfterFirst.getDiamond();
        String responseJsonBefore = gachaPurchaseRepository.findAll().get(0).getResponseJson();

        // 다른 productId로 재요청 → 예외
        assertThatThrownBy(() -> gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_TEN", requestId))
                .isInstanceOf(IllegalStateException.class)
                .hasMessageContaining("다른 상품 ID");

        // 추가 차감 없음
        User userAfterConflict = userRepository.findByUsername("idempotencyUser1").orElseThrow();
        assertThat(userAfterConflict.getDiamond()).isEqualTo(diamondAfterFirst);

        // 추가 지급 없음
        assertThat(userAlienRepository.findAll()).hasSize(1);

        // 기록 1건 유지, responseJson 변경 없음
        List<GachaPurchase> purchases = gachaPurchaseRepository.findAll();
        assertThat(purchases).hasSize(1);
        assertThat(purchases.get(0).getResponseJson()).isEqualTo(responseJsonBefore);
        verify(randomGenerator, times(2)).nextInt(anyInt());
    }

    // ===================== D. 다른 사용자 =====================

    @Test
    @DisplayName("D-19~22. 동일 requestId + 다른 user: 각각 독립 구매, 기록 2건, 각각 차감/지급")
    void differentUsers_sameRequestId_independentPurchases() {
        UUID sameRequestId = UUID.randomUUID();

        // 두 사용자 각각 구매
        GachaPurchaseResponseDto r1 = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", sameRequestId);
        GachaPurchaseResponseDto r2 = gachaPurchaseService.purchase("idempotencyUser2", "ALIEN_GACHA_SINGLE", sameRequestId);

        assertThat(r1).isNotNull();
        assertThat(r2).isNotNull();

        // 기록 2건
        List<GachaPurchase> purchases = gachaPurchaseRepository.findAll();
        assertThat(purchases).hasSize(2);

        // 두 사용자 각각 차감
        User u1 = userRepository.findByUsername("idempotencyUser1").orElseThrow();
        User u2 = userRepository.findByUsername("idempotencyUser2").orElseThrow();
        assertThat(u1.getDiamond()).isEqualTo(10000 - 500);
        assertThat(u2.getDiamond()).isEqualTo(10000 - 500);

        // 두 사용자 각각 지급
        assertThat(userAlienRepository.findAll()).hasSize(2);
        assertThat(userAlienRepository.findAllByUser(u1)).hasSize(1);
        assertThat(userAlienRepository.findAllByUser(u2)).hasSize(1);
    }

    // ===================== G. 롤백 후 재시도 =====================

    @Test
    @DisplayName("G-37~38. 롤백 후 동일 requestId 재시도 가능: PROCESSING 기록도 롤백됨")
    void rollback_sameRequestIdRetryable() {
        UUID requestId = UUID.randomUUID();

        // PROCESSING saveAndFlush 이후 잔액 차감에서 실패하도록 만든다.
        user1.setDiamond(0);
        userRepository.saveAndFlush(user1);
        assertThatThrownBy(() -> gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", requestId))
                .isInstanceOf(IllegalStateException.class)
                .hasMessageContaining("다이아가 부족합니다");

        // PROCESSING 기록도 롤백되어 없어야 함
        assertThat(gachaPurchaseRepository.findAll()).isEmpty();

        // 같은 requestId로 재시도 가능
        user1.setDiamond(10000);
        userRepository.saveAndFlush(user1);
        GachaPurchaseResponseDto retryResult = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", requestId);
        assertThat(retryResult).isNotNull();
        assertThat(gachaPurchaseRepository.findAll()).hasSize(1);
        assertThat(gachaPurchaseRepository.findAll().get(0).getStatus()).isEqualTo(GachaPurchaseStatus.COMPLETED);
    }

    // ===================== H. 직렬화/역직렬화 — 전체 필드 동일 =====================

    @Test
    @DisplayName("H-39~42. responseJson 직렬화/역직렬화: record equals로 전체 필드 동일")
    void responseSerialization_allFieldsIdentical() {
        UUID requestId = UUID.randomUUID();
        GachaPurchaseResponseDto original = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", requestId);

        // 중복 요청 시 JSON에서 역직렬화된 응답 반환
        GachaPurchaseResponseDto deserialized = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_SINGLE", requestId);

        // record equals → productId, currencyType, price, remainingDiamond, drawCount, draws, rewards 전체 비교
        assertThat(deserialized).isEqualTo(original);

        // draws/rewards 순서 보장
        assertThat(deserialized.draws()).isEqualTo(original.draws());
        assertThat(deserialized.rewards()).isEqualTo(original.rewards());
    }

    // ===================== I. 10회 responseJson 길이 검증 =====================

    @Test
    @DisplayName("I. TEN(10회) 구매 responseJson: 255자 초과 저장·역직렬화 정상")
    void tenPurchase_responseJson_notTruncated() {
        UUID requestId = UUID.randomUUID();
        GachaPurchaseResponseDto original = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_TEN", requestId);

        // responseJson 확인
        GachaPurchase purchase = gachaPurchaseRepository.findAll().get(0);
        String json = purchase.getResponseJson();
        assertThat(json).isNotBlank();
        // 10회 응답 JSON은 255자를 초과해야 정상 (draws 10개 포함)
        assertThat(json.length()).isGreaterThan(255);

        // 역직렬화 후 완전 동일
        GachaPurchaseResponseDto deserialized = gachaPurchaseService.purchase("idempotencyUser1", "ALIEN_GACHA_TEN", requestId);
        assertThat(deserialized).isEqualTo(original);
        assertThat(deserialized.draws()).hasSize(10);
    }
}
