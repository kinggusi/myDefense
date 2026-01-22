package com.denfense.server.service.impl;

import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.AlienService;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class AlienServcieImpl implements AlienService {

    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;

    // 레벨업 비용 (원래는 엑셀/DB에서 가져와야 함)
    // 예: 1->2강 가는데 조각 10개 필요
    private final int UPGRADE_COST_PER_LEVEL = 5;

    @Transactional
    public UserAlien aleinUpgrade(String username, int alienId) {
        // 1. 유저 확인
        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new IllegalArgumentException("유저 없음"));

        // 2. 내 왹져 찾기
        UserAlien myAlien = userAlienRepository.findByUserAndAlienId(user, alienId)
                .orElseThrow(() -> new IllegalArgumentException("보유하지 않은 왹져입니다."));

        // 3. 강화 가능한지 확인 및 처리
        // (심화: 레벨마다 필요 개수가 다르면 엑셀 데이터 참조 필요)
        int cost = UPGRADE_COST_PER_LEVEL;

        if (myAlien.getPieces() < cost) {
            throw new IllegalStateException("조각이 부족합니다. (보유: " + myAlien.getPieces() + ", 필요: " + cost + ")");
        }

        // 4. 강화 실행
        myAlien.aleinUpgrade(cost);

        return myAlien; // 레벨 오른 정보 리턴
    }
}
