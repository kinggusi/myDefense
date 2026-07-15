package com.denfense.server.service.gacha;

import com.denfense.server.balance.GachaGradeEntryBalance;
import com.denfense.server.balance.GachaPoolBalance;
import com.denfense.server.balance.ShopProductBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.GachaPurchase;
import com.denfense.server.domain.GachaPurchaseStatus;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.gacha.GachaDrawDto;
import com.denfense.server.dto.gacha.GachaPurchaseResponseDto;
import com.denfense.server.dto.gacha.GachaRewardDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.GachaPurchaseRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceRegistry;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Spy;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import com.fasterxml.jackson.databind.ObjectMapper;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anySet;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.mockito.Mockito.never;

@ExtendWith(MockitoExtension.class)
class GachaPurchaseServiceTest {

    @Mock
    private UserRepository userRepository;

    @Mock
    private UserAlienRepository userAlienRepository;

    @Mock
    private AlienSpecRepository alienSpecRepository;

    @Mock
    private GachaDrawService gachaDrawService;

    @Mock
    private BalanceRegistry balanceRegistry;

    @Mock
    private GachaPurchaseRepository gachaPurchaseRepository;

    @Spy
    private ObjectMapper objectMapper = new ObjectMapper();

    @InjectMocks
    private GachaPurchaseService gachaPurchaseService;

    private User user;
    private AlienSpec spec22; // NORMAL
    private AlienSpec spec29; // MYTHIC

    @BeforeEach
    void setUp() {
        user = new User("testUser", "pw");
        user.setDiamond(5000);

        spec22 = new AlienSpec();
        spec22.setId(22L);
        spec22.setGrade(AlienSpec.Grade.NORMAL);

        spec29 = new AlienSpec();
        spec29.setId(29L);
        spec29.setGrade(AlienSpec.Grade.MYTHIC);
    }

    private ShopProductBalance createProduct(String productId, int price, int drawCount, boolean active, String currencyType) {
        return new ShopProductBalance(productId, "Test Product", currencyType, price, drawCount, "POOL1", active);
    }

    // A. 단일 구매
    @Test
    @DisplayName("1,2,3,4. SINGLE 상품 구매 성공 - price 500 차감, 남은 다이아 반환, 미보유 신규 해금")
    void purchaseSingle_success_newUnlock() {
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(balanceRegistry.getShopProduct("SINGLE")).thenReturn(createProduct("SINGLE", 500, 1, true, "DIAMOND"));
        
        List<GachaDrawResult> drawResults = List.of(new GachaDrawResult("SINGLE", "POOL1", "NORMAL", 22L));
        when(gachaDrawService.draw("SINGLE")).thenReturn(drawResults);
        when(alienSpecRepository.findAllById(anySet())).thenReturn(List.of(spec22));
        when(userAlienRepository.findByUserAndAlienSpecIdIn(user, Set.of(22L))).thenReturn(List.of());

        GachaPurchaseResponseDto result = gachaPurchaseService.purchase("testUser", "SINGLE", UUID.randomUUID());

        // 잔액 차감 확인
        assertThat(user.getDiamond()).isEqualTo(4500);

        // 결과 DTO 확인
        assertThat(result.productId()).isEqualTo("SINGLE");
        assertThat(result.currencyType()).isEqualTo("DIAMOND");
        assertThat(result.price()).isEqualTo(500);
        assertThat(result.remainingDiamond()).isEqualTo(4500);
        assertThat(result.drawCount()).isEqualTo(1);
        assertThat(result.draws()).hasSize(1);
        assertThat(result.draws().get(0).alienId()).isEqualTo(22L);
        
        assertThat(result.rewards()).hasSize(1);
        GachaRewardDto reward = result.rewards().get(0);
        assertThat(reward.alienId()).isEqualTo(22L);
        assertThat(reward.grade()).isEqualTo("NORMAL");
        assertThat(reward.occurrenceCount()).isEqualTo(1);
        assertThat(reward.newlyUnlocked()).isTrue();
        assertThat(reward.piecesAdded()).isEqualTo(49);
        assertThat(reward.currentLevel()).isEqualTo(1);
        assertThat(reward.currentPieces()).isEqualTo(49);

        // 저장 확인
        verify(userAlienRepository).saveAll(any());
    }

    // B. 10회 구매 / C. 신규 해금 다수 / E. MYTHIC 신규 획득
    @Test
    @DisplayName("5,6,7,8,9,11,17. TEN 상품 구매 성공 - price 5000 1회 차감, MYTHIC 포함 10개 결과 집계")
    void purchaseTen_success_multipleUnlocks() {
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(balanceRegistry.getShopProduct("TEN")).thenReturn(createProduct("TEN", 5000, 10, true, "DIAMOND"));
        
        List<GachaDrawResult> drawResults = List.of(
                new GachaDrawResult("TEN", "POOL1", "MYTHIC", 29L),
                new GachaDrawResult("TEN", "POOL1", "MYTHIC", 29L),
                new GachaDrawResult("TEN", "POOL1", "MYTHIC", 29L),
                new GachaDrawResult("TEN", "POOL1", "NORMAL", 22L)
                // 생략해서 4개만 한다고 가정 (drawCount=4인 상품처럼) - 테스트 편의상
        );
        // 테스트를 위해 product drawCount 4로 세팅
        when(balanceRegistry.getShopProduct("TEN")).thenReturn(createProduct("TEN", 5000, 4, true, "DIAMOND"));
        when(gachaDrawService.draw("TEN")).thenReturn(drawResults);
        when(alienSpecRepository.findAllById(anySet())).thenReturn(List.of(spec29, spec22));
        when(userAlienRepository.findByUserAndAlienSpecIdIn(user, Set.of(29L, 22L))).thenReturn(List.of());

        GachaPurchaseResponseDto result = gachaPurchaseService.purchase("testUser", "TEN", UUID.randomUUID());

        assertThat(user.getDiamond()).isEqualTo(0);
        assertThat(result.rewards()).hasSize(2);
        
        GachaRewardDto rewardMythic = result.rewards().stream().filter(r -> r.alienId() == 29L).findFirst().get();
        assertThat(rewardMythic.newlyUnlocked()).isTrue();
        assertThat(rewardMythic.occurrenceCount()).isEqualTo(3);
        assertThat(rewardMythic.piecesAdded()).isEqualTo(149); // 50 * 3 - 1
        assertThat(rewardMythic.currentPieces()).isEqualTo(149);
        
        GachaRewardDto rewardNormal = result.rewards().stream().filter(r -> r.alienId() == 22L).findFirst().get();
        assertThat(rewardNormal.newlyUnlocked()).isTrue();
        assertThat(rewardNormal.occurrenceCount()).isEqualTo(1);
        assertThat(rewardNormal.piecesAdded()).isEqualTo(49);
    }

    // D. 중복 지급 / E. MYTHIC 중복
    @Test
    @DisplayName("12,13,14,15,16,18. 보유 왹져 중복 지급 - 기존 pieces 20에 동일 Alien 등장 시 +50")
    void purchaseSingle_duplicate() {
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(balanceRegistry.getShopProduct("SINGLE")).thenReturn(createProduct("SINGLE", 500, 1, true, "DIAMOND"));
        
        List<GachaDrawResult> drawResults = List.of(new GachaDrawResult("SINGLE", "POOL1", "MYTHIC", 29L));
        when(gachaDrawService.draw("SINGLE")).thenReturn(drawResults);
        when(alienSpecRepository.findAllById(anySet())).thenReturn(List.of(spec29));
        
        UserAlien existingAlien = new UserAlien(user, spec29);
        existingAlien.setPieces(20);
        when(userAlienRepository.findByUserAndAlienSpecIdIn(user, Set.of(29L))).thenReturn(List.of(existingAlien));

        GachaPurchaseResponseDto result = gachaPurchaseService.purchase("testUser", "SINGLE", UUID.randomUUID());

        GachaRewardDto reward = result.rewards().get(0);
        assertThat(reward.occurrenceCount()).isEqualTo(1);
        assertThat(reward.newlyUnlocked()).isFalse();
        assertThat(reward.piecesAdded()).isEqualTo(50);
        assertThat(reward.currentPieces()).isEqualTo(70); // 20 + 50
        
        // 새로 saveAll 하지 않음 (dirty checking 사용)
        verify(userAlienRepository, never()).saveAll(any());
    }

    // F. 오류
    @Test
    @DisplayName("19. 사용자 없음 예외")
    void userNotFound() {
        when(userRepository.findByUsernameForUpdate("unknown")).thenReturn(Optional.empty());
        assertThatThrownBy(() -> gachaPurchaseService.purchase("unknown", "SINGLE", UUID.randomUUID()))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.USER_NOT_FOUND));
    }

    @Test
    @DisplayName("20. 상품 없음 예외")
    void productNotFound() {
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(balanceRegistry.getShopProduct("NOT_FOUND")).thenReturn(null);
        
        assertThatThrownBy(() -> gachaPurchaseService.purchase("testUser", "NOT_FOUND", UUID.randomUUID()))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.SHOP_PRODUCT_NOT_FOUND));
    }

    @Test
    @DisplayName("21. 상품 비활성 예외")
    void inactiveProduct() {
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(balanceRegistry.getShopProduct("INACTIVE")).thenReturn(createProduct("INACTIVE", 500, 1, false, "DIAMOND"));
        
        assertThatThrownBy(() -> gachaPurchaseService.purchase("testUser", "INACTIVE", UUID.randomUUID()))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.SHOP_PRODUCT_INACTIVE));
    }

    @Test
    @DisplayName("23. 지원하지 않는 currencyType 예외")
    void unsupportedCurrency() {
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(balanceRegistry.getShopProduct("GOLD_GACHA")).thenReturn(createProduct("GOLD_GACHA", 500, 1, true, "GOLD"));
        
        assertThatThrownBy(() -> gachaPurchaseService.purchase("testUser", "GOLD_GACHA", UUID.randomUUID()))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.UNSUPPORTED_CURRENCY));
    }

    @Test
    @DisplayName("24. 다이아 부족 예외")
    void insufficientDiamond() {
        user.setDiamond(100); // 500보다 작음
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(balanceRegistry.getShopProduct("SINGLE")).thenReturn(createProduct("SINGLE", 500, 1, true, "DIAMOND"));
        
        assertThatThrownBy(() -> gachaPurchaseService.purchase("testUser", "SINGLE", UUID.randomUUID()))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.INSUFFICIENT_DIAMOND));
        
        // 차감 안됨
        assertThat(user.getDiamond()).isEqualTo(100);
        // draw 호출 안됨
        verify(gachaDrawService, never()).draw(any());
    }
    
    @Test
    @DisplayName("25. AlienSpec DB 누락 시 예외 발생 및 전체 롤백 트리거")
    void alienSpecMissingInDb() {
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(balanceRegistry.getShopProduct("SINGLE")).thenReturn(createProduct("SINGLE", 500, 1, true, "DIAMOND"));
        
        List<GachaDrawResult> drawResults = List.of(new GachaDrawResult("SINGLE", "POOL1", "NORMAL", 999L));
        when(gachaDrawService.draw("SINGLE")).thenReturn(drawResults);
        
        when(alienSpecRepository.findAllById(anySet())).thenReturn(List.of()); // 비어 있음 (DB 불일치)
        
        assertThatThrownBy(() -> gachaPurchaseService.purchase("testUser", "SINGLE", UUID.randomUUID()))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.ALIEN_SPEC_NOT_FOUND));
    }

    @Test
    @DisplayName("PROCESSING 구매 기록 재요청은 PURCHASE_ALREADY_PROCESSING")
    void purchaseAlreadyProcessing() {
        UUID requestId = UUID.randomUUID();
        GachaPurchase existing = new GachaPurchase(user, requestId, "SINGLE", GachaPurchaseStatus.PROCESSING);
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(gachaPurchaseRepository.findByUserAndPurchaseRequestId(user, requestId)).thenReturn(Optional.of(existing));

        assertThatThrownBy(() -> gachaPurchaseService.purchase("testUser", "SINGLE", requestId))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.PURCHASE_ALREADY_PROCESSING));
        verify(gachaDrawService, never()).draw(any());
    }

    @Test
    @DisplayName("FAILED 구매 기록 재요청은 INTERNAL_SERVER_ERROR")
    void failedPurchaseState() {
        UUID requestId = UUID.randomUUID();
        GachaPurchase existing = new GachaPurchase(user, requestId, "SINGLE", GachaPurchaseStatus.FAILED);
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(gachaPurchaseRepository.findByUserAndPurchaseRequestId(user, requestId)).thenReturn(Optional.of(existing));

        assertThatThrownBy(() -> gachaPurchaseService.purchase("testUser", "SINGLE", requestId))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.INTERNAL_SERVER_ERROR));
        verify(gachaDrawService, never()).draw(any());
    }

    @Test
    @DisplayName("COMPLETED 구매의 빈 responseJson은 PURCHASE_RESPONSE_RESTORE_FAILED")
    void completedPurchaseWithBlankResponse() {
        UUID requestId = UUID.randomUUID();
        GachaPurchase existing = new GachaPurchase(user, requestId, "SINGLE", GachaPurchaseStatus.PROCESSING);
        existing.complete(" ");
        when(userRepository.findByUsernameForUpdate("testUser")).thenReturn(Optional.of(user));
        when(gachaPurchaseRepository.findByUserAndPurchaseRequestId(user, requestId)).thenReturn(Optional.of(existing));

        assertThatThrownBy(() -> gachaPurchaseService.purchase("testUser", "SINGLE", requestId))
                .isInstanceOfSatisfying(BusinessException.class,
                        e -> assertThat(e.getErrorCode()).isEqualTo(ErrorCode.PURCHASE_RESPONSE_RESTORE_FAILED));
        verify(gachaDrawService, never()).draw(any());
    }
}
