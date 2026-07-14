package com.denfense.server.service.gacha;

import com.denfense.server.balance.ShopProductBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.gacha.GachaDrawDto;
import com.denfense.server.dto.gacha.GachaPurchaseResponseDto;
import com.denfense.server.dto.gacha.GachaRewardDto;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class GachaPurchaseService {

    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final GachaDrawService gachaDrawService;
    private final BalanceRegistry balanceRegistry;

    private static final int PIECES_PER_DRAW = 50;
    private static final int PIECES_FOR_UNLOCK = 1;

    @Transactional
    public GachaPurchaseResponseDto purchase(String username, String productId) {
        // 1. User 비관적 락 조회
        User user = userRepository.findByUsernameForUpdate(username)
                .orElseThrow(() -> new IllegalStateException("존재하지 않는 유저입니다: " + username));

        // 2. ShopProduct 조회 및 active 검증
        ShopProductBalance product = balanceRegistry.getShopProduct(productId);
        if (product == null) {
            throw new IllegalStateException("존재하지 않는 상품입니다: " + productId);
        }
        if (!product.active()) {
            throw new IllegalStateException("비활성 상품입니다: " + productId);
        }

        // 3. currencyType 지원 여부 검증
        if (!"DIAMOND".equals(product.currencyType())) {
            throw new IllegalStateException("지원하지 않는 재화 타입입니다: " + product.currencyType());
        }

        // 4. 잔액 확인 및 차감
        int price = product.price();
        user.decreaseDiamond(price);

        // 5. GachaDrawService.draw(productId) 호출
        List<GachaDrawResult> drawResults = gachaDrawService.draw(productId);

        // 6. 결과 집계 (최초 등장 순서 유지)
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

        // 7. AlienSpec 일괄 조회
        Set<Long> alienIds = drawCountsByAlienId.keySet();
        List<AlienSpec> specs = alienSpecRepository.findAllById(alienIds);
        if (specs.size() != alienIds.size()) {
            throw new IllegalStateException("추첨된 AlienSpec 중 일부를 DB에서 찾을 수 없습니다.");
        }
        Map<Long, AlienSpec> specMap = specs.stream().collect(Collectors.toMap(AlienSpec::getId, spec -> spec));

        // 8. 기존 UserAlien 일괄 조회
        List<UserAlien> existingUserAliens = userAlienRepository.findByUserAndAlienSpecIdIn(user, alienIds);
        Map<Long, UserAlien> existingUserAlienMap = existingUserAliens.stream()
                .collect(Collectors.toMap(ua -> ua.getAlienSpec().getId(), ua -> ua));

        // 9. 신규/중복 지급
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

        // 11. 응답 생성
        return new GachaPurchaseResponseDto(
                productId,
                product.currencyType(),
                product.price(),
                user.getDiamond(),
                product.drawCount(),
                List.copyOf(drawDtos),
                List.copyOf(rewards)
        );
    }
}
