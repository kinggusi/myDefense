package com.denfense.server.service;

import com.denfense.server.domain.GameSettlement;
import com.denfense.server.domain.User;
import com.denfense.server.dto.request.GameFinishRequestDto;
import com.denfense.server.dto.response.GameFinishResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.session.GameSession;
import com.denfense.server.repository.GameSettlementRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.reward.GameReward;
import com.denfense.server.service.reward.GameRewardContext;
import com.denfense.server.service.reward.GameRewardPolicy;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;

import java.time.LocalDateTime;
import java.util.Optional;

@Slf4j
@Service
@RequiredArgsConstructor
public class GameSettlementService {

    private final UserRepository userRepository;
    private final GameSessionManager sessionManager;
    private final GameSettlementRepository settlementRepository;
    private final GameRewardPolicy rewardPolicy;

    @Transactional
    public GameFinishResponseDto finishGame(GameFinishRequestDto requestDto) {
        String username = requestDto.getUsername();
        String reqSessionId = requestDto.getSessionId();

        // 1. User 기본 검증
        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND, "유저를 찾을 수 없습니다."));

        // 2. 빠른 재시도 조회 (락 획득 전) - 선택적이지만 안전한 흐름을 위해 먼저 확인
        Optional<GameSettlement> existingOpt = settlementRepository.findBySessionId(reqSessionId);
        if (existingOpt.isPresent()) {
            return buildAlreadyProcessedResponse(user, existingOpt.get());
        }

        // 3. 세션 검증
        GameSession session;
        try {
            session = sessionManager.getSession(user.getId());
        } catch (BusinessException e) {
            throw new BusinessException(ErrorCode.GAME_SESSION_NOT_FOUND, "게임 세션이 존재하지 않습니다.");
        }

        // 4. 소유권 및 세션 ID 일치 검증
        if (!reqSessionId.equals(session.getSessionId())) {
            throw new BusinessException(ErrorCode.GAME_SESSION_OWNERSHIP_MISMATCH, "요청한 sessionId가 현재 활성 세션과 일치하지 않습니다.");
        }

        // 5. 게임 종료 상태 검증
        if (!session.isGameOver()) {
            throw new BusinessException(ErrorCode.GAME_NOT_FINISHED, "게임이 아직 종료되지 않았습니다.");
        }

        // 6. User PESSIMISTIC_WRITE 락 획득
        User lockedUser = userRepository.findByUsernameForUpdate(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND, "유저를 찾을 수 없습니다."));

        // 7. 락 획득 후 재조회 (동시 요청 방어 최종선)
        Optional<GameSettlement> existingUnderLock = settlementRepository.findBySessionId(reqSessionId);
        if (existingUnderLock.isPresent()) {
            return buildAlreadyProcessedResponse(lockedUser, existingUnderLock.get());
        }

        // 8. 보상 계산
        GameRewardContext context = new GameRewardContext(session.getClearedWave());
        GameReward reward = rewardPolicy.calculate(context);

        // 9. User 보상 지급
        lockedUser.earnGold(reward.accountGold());

        // 10. 정산 기록 저장
        GameSettlement settlement = GameSettlement.builder()
                .sessionId(reqSessionId)
                .userId(lockedUser.getId())
                .clearedWave(session.getClearedWave())
                .rewardGold(reward.accountGold())
                .accountGoldAfter(lockedUser.getGold())
                .finishedAt(LocalDateTime.now())
                .build();
        
        settlementRepository.save(settlement);

        // 11. 트랜잭션 커밋 후 세션 제거
        TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
            @Override
            public void afterCommit() {
                try {
                    // compare-remove 방식으로 정확히 해당 세션만 제거
                    sessionManager.removeSession(lockedUser.getId(), session);
                } catch (Exception e) {
                    log.error("게임 정산 후 인메모리 세션 제거 중 예외 발생. userId={}, sessionId={}", lockedUser.getId(), reqSessionId, e);
                }
            }
        });

        // 12. 응답 반환
        return GameFinishResponseDto.builder()
                .userId(lockedUser.getId())
                .username(lockedUser.getUsername())
                .sessionId(settlement.getSessionId())
                .clearedWave(settlement.getClearedWave())
                .rewardGold(settlement.getRewardGold())
                .accountGoldAfter(settlement.getAccountGoldAfter())
                .finishedAt(settlement.getFinishedAt())
                .alreadyProcessed(false)
                .serverTime(LocalDateTime.now())
                .build();
    }

    private GameFinishResponseDto buildAlreadyProcessedResponse(User user, GameSettlement settlement) {
        if (!user.getId().equals(settlement.getUserId())) {
            throw new BusinessException(ErrorCode.GAME_SESSION_OWNERSHIP_MISMATCH, "정산 기록의 소유자와 현재 요청자가 다릅니다.");
        }
        
        return GameFinishResponseDto.builder()
                .userId(settlement.getUserId())
                .username(user.getUsername())
                .sessionId(settlement.getSessionId())
                .clearedWave(settlement.getClearedWave())
                .rewardGold(settlement.getRewardGold())
                .accountGoldAfter(settlement.getAccountGoldAfter())
                .finishedAt(settlement.getFinishedAt())
                .alreadyProcessed(true)
                .serverTime(LocalDateTime.now())
                .build();
    }
}
