package com.denfense.server.service.impl;

import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;

import com.denfense.server.dto.response.UserAlienResponseDto;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.ShopService;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.*;

@Service
@RequiredArgsConstructor
public class ShopServiceImpl implements ShopService {

    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;

    // 1회 뽑기 비용 (추후 엑셀 데이터나 DB 설정으로 빼는 것을 권장)
    private final int GACHA_COST = 500;

    /**
     * 왹져 뽑기 (가챠)
     * - 레벨업을 바로 시키지 않고 '조각(Pieces)'을 지급합니다.
     * - 없는 왹져를 처음 뽑으면: 1개는 '몸통(1레벨)'이 되고, 나머지 중복분만 조각이 됩니다.
     * - 이미 있는 왹져를 뽑으면: 뽑은 개수만큼 전부 조각이 됩니다.
     */
    @Transactional
    public List<UserAlienResponseDto> gachaAlien(String username, int count) {
        // 1. 유효성 검사 (1회 또는 10회만 허용)
        if (count != 1 && count != 10) {
            throw new IllegalArgumentException("뽑기는 1회 또는 10회만 가능합니다.");
        }

        // 2. 유저 조회
        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new IllegalArgumentException("존재하지 않는 유저입니다."));

        // 3. 잔액 확인 및 차감
        int totalCost = GACHA_COST * count;
        if (user.getGold() < totalCost) {
            throw new IllegalStateException("골드가 부족합니다. (필요: " + totalCost + ", 보유: " + user.getGold() + ")");
        }

        // 골드 차감 (Dirty Checking으로 트랜잭션 종료 시 자동 UPDATE 됨)
        user.setGold(user.getGold() - totalCost);

        // 4. 추첨 로직 (메모리 상에서 먼저 결과 정산)
        // Key: AlienID, Value: 이번에 뽑힌 횟수
        // 예: 10회 뽑기 결과 -> {1번왹져: 3마리, 2번왹져: 7마리}
        Map<Integer, Integer> drawResult = new HashMap<>();
        Random random = new Random();

        for (int i = 0; i < count; i++) {
            // 현재는 1번, 2번 왹져 중 랜덤 (나중엔 확률 테이블 적용 필요)
            int alienId = random.nextInt(2) + 1;
            drawResult.put(alienId, drawResult.getOrDefault(alienId, 0) + 1);
        }

        // 5. DB 반영 및 결과 리스트 생성
        List<UserAlienResponseDto> responseList = new ArrayList<>();

        for (Integer alienId : drawResult.keySet()) {
            int obtainedCount = drawResult.get(alienId); // 뽑은 개수

            // 일단 DB에서 찾아봄
            UserAlien myAlien = userAlienRepository.findByUserAndAlienId(user, alienId)
                    .orElse(null);

            if (myAlien == null) {
                // [CASE A] 신규 획득: 처음부터 계산 다 끝내고 딱 1번만 저장(INSERT)
                UserAlien newAlien = new UserAlien(user, alienId);

                // 로직: 1개는 몸통(레벨1), 나머지는 조각
                // 예: 3개 뽑음 -> 1개 몸통, 2개 조각
                newAlien.setPieces(obtainedCount - 1);

                userAlienRepository.save(newAlien); // 여기서 INSERT 1회 끝!
                responseList.add(new UserAlienResponseDto(newAlien));
            } else {
                // [CASE B] 중복 획득: 기존 정보 수정 (Dirty Checking -> UPDATE)
                myAlien.addPieces(obtainedCount);

                // 명시적으로 업데이트된 객체를 DTO로 변환
                responseList.add(new UserAlienResponseDto(myAlien));
            }
        }

        return responseList;
    }
}
