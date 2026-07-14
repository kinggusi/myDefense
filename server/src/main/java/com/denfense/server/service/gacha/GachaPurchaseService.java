package com.denfense.server.service.gacha;

import com.denfense.server.balance.ShopProductBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.GachaPurchase;
import com.denfense.server.domain.GachaPurchaseStatus;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.gacha.GachaDrawDto;
import com.denfense.server.dto.gacha.GachaPurchaseResponseDto;
import com.denfense.server.dto.gacha.GachaRewardDto;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.GachaPurchaseRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceRegistry;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class GachaPurchaseService {

    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final GachaDrawService gachaDrawService;
    private final BalanceRegistry balanceRegistry;
    private final GachaPurchaseRepository gachaPurchaseRepository;
    private final ObjectMapper objectMapper;

    private static final int PIECES_PER_DRAW = 50;
    private static final int PIECES_FOR_UNLOCK = 1;

    @Transactional
    public GachaPurchaseResponseDto purchase(String username, String productId, UUID purchaseRequestId) {
        if (purchaseRequestId == null) {
            throw new IllegalArgumentException("purchaseRequestId는 필수입니다.");
        }

        // 1. User 비관적 락 조회
        User user = userRepository.findByUsernameForUpdate(username)
                .orElseThrow(() -> new IllegalStateException("존재하지 않는 유저입니다: " + username));

        // 2. 저장 응답을 반환하기 전에 동일 키의 productId 충돌부터 검사한다.
        GachaPurchase existingPurchase = gachaPurchaseRepository
                .findByUserAndPurchaseRequestId(user, purchaseRequestId)
                .orElse(null);
        if (existingPurchase != null) {
            if (!existingPurchase.getProductId().equals(productId)) {
                throw new IllegalStateException("동일 purchaseRequestId를 다른 상품 ID로 재사용할 수 없습니다.");
            }
            if (existingPurchase.getStatus() == GachaPurchaseStatus.COMPLETED) {
                return deserializeResponse(existingPurchase.getResponseJson());
            }
            throw new IllegalStateException("이미 처리 중인 구매 요청입니다.");
        }

        // 3. ShopProduct 조회 및 active 검증
        ShopProductBalance product = balanceRegistry.getShopProduct(productId);
        if (product == null) {
            throw new IllegalStateException("존재하지 않는 상품입니다: " + productId);
        }
        if (!product.active()) {
            throw new IllegalStateException("비활성 상품입니다: " + productId);
        }

        // 4. currencyType 지원 여부 검증
        if (!"DIAMOND".equals(product.currencyType())) {
            throw new IllegalStateException("지원하지 않는 재화 타입입니다: " + product.currencyType());
        }

        // 5. PROCESSING을 즉시 INSERT한다. 이후 실패하면 같은 트랜잭션에서 함께 롤백된다.
        GachaPurchase purchase = new GachaPurchase(
                user,
                purchaseRequestId,
                productId,
                GachaPurchaseStatus.PROCESSING
        );
        gachaPurchaseRepository.saveAndFlush(purchase);

        // 6. 잔액 확인 및 차감
        int price = product.price();
        user.decreaseDiamond(price);

        // 7. GachaDrawService.draw(productId) 호출
        List<GachaDrawResult> drawResults = gachaDrawService.draw(productId);

        // 8. 결과 집계 (최초 등장 순서 유지)
        Map<Long, Integer> drawCountsByAlienId = new LinkedHashMap<>();
        Map<Long, String> alienGrades = new LinkedHashMap<>();
        
        List<GachaDrawDto> drawDtos = new ArrayList<>(drawResults.size());
        for (int i = 0; i < drawResults.size(); i++) {
            GachaDrawResult draw = drawResults.get(i);
            Long alienId = draw.alienId();
            drawCountsByAlienId.put(alienId, drawCountsByAlienId.getOrDefault(alienId, 0) + 1);
            alienGrades.putIfAbsent(alienId, draw.grade());
            
            drawDtos.add(new GachaDrawDto(i + 1, alienId, draw.grade()));
        }

        // 9. AlienSpec 일괄 조회
        Set<Long> alienIds = drawCountsByAlienId.keySet();
        List<AlienSpec> specs = alienSpecRepository.findAllById(alienIds);
        if (specs.size() != alienIds.size()) {
            throw new IllegalStateException("추첨된 AlienSpec 중 일부를 DB에서 찾을 수 없습니다.");
        }
        Map<Long, AlienSpec> specMap = specs.stream().collect(Collectors.toMap(AlienSpec::getId, spec -> spec));

        // 10. 기존 UserAlien 일괄 조회
        List<UserAlien> existingUserAliens = userAlienRepository.findByUserAndAlienSpecIdIn(user, alienIds);
        Map<Long, UserAlien> existingUserAlienMap = existingUserAliens.stream()
                .collect(Collectors.toMap(ua -> ua.getAlienSpec().getId(), ua -> ua));

        // 11. 신규/중복 지급
        List<UserAlien> newAliensToSave = new ArrayList<>();
        List<GachaRewardDto> rewards = new ArrayList<>();

        for (Map.Entry<Long, Integer> entry : drawCountsByAlienId.entrySet()) {
            Long alienId = entry.getKey();
            int count = entry.getValue();
            String grade = alienGrades.get(alienId);
            AlienSpec spec = specMap.get(alienId);
            UserAlien userAlien = existingUserAlienMap.get(alienId);

            int totalValue = count * PIECES_PER_DRAW;
            boolean newlyUnlocked;
            int piecesAdded;
            int currentLevel;
            int currentPieces;

            if (userAlien == null) {
                // 미보유
                newlyUnlocked = true;
                piecesAdded = totalValue - PIECES_FOR_UNLOCK;
                
                userAlien = new UserAlien(user, spec);
                // 명함 해금 후 레벨은 1 고정 (기본 생성자가 level 1로 세팅한다고 가정, 필요시 수정)
                userAlien.setPieces(piecesAdded);
                newAliensToSave.add(userAlien);
                
                currentLevel = userAlien.getLevel();
                currentPieces = userAlien.getPieces();
            } else {
                // 보유
                newlyUnlocked = false;
                piecesAdded = totalValue;
                
                userAlien.addPieces(piecesAdded);
                
                currentLevel = userAlien.getLevel();
                currentPieces = userAlien.getPieces();
            }

            rewards.add(new GachaRewardDto(
                    alienId,
                    grade,
                    count,
                    newlyUnlocked,
                    piecesAdded,
                    currentLevel,
                    currentPieces
            ));
        }

        if (!newAliensToSave.isEmpty()) {
            userAlienRepository.saveAll(newAliensToSave);
        }

        // 12. 응답 JSON을 저장하고 COMPLETED로 전환한다. 변경은 트랜잭션 커밋 때 반영된다.
        GachaPurchaseResponseDto response = new GachaPurchaseResponseDto(
                productId,
                product.currencyType(),
                product.price(),
                user.getDiamond(),
                product.drawCount(),
                List.copyOf(drawDtos),
                List.copyOf(rewards)
        );
        purchase.complete(serializeResponse(response));
        return response;
    }

    private String serializeResponse(GachaPurchaseResponseDto response) {
        try {
            return objectMapper.writeValueAsString(response);
        } catch (JsonProcessingException e) {
            throw new IllegalStateException("Gacha 구매 응답 직렬화에 실패했습니다.", e);
        }
    }

    private GachaPurchaseResponseDto deserializeResponse(String responseJson) {
        if (responseJson == null || responseJson.isBlank()) {
            throw new IllegalStateException("완료된 Gacha 구매 응답이 비어 있습니다.");
        }
        try {
            return objectMapper.readValue(responseJson, GachaPurchaseResponseDto.class);
        } catch (JsonProcessingException e) {
            throw new IllegalStateException("저장된 Gacha 구매 응답 역직렬화에 실패했습니다.", e);
        }
    }
}
