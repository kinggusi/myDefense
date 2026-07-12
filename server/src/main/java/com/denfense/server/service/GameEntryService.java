package com.denfense.server.service;

import com.denfense.server.domain.User;
import com.denfense.server.dto.response.GameEntryResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.session.GameSession;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;

import java.time.LocalDateTime;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.locks.ReentrantLock;

@Slf4j
@Service
@RequiredArgsConstructor
public class GameEntryService {

    private final UserRepository userRepository;
    private final GameSessionManager sessionManager;
    private final HeartPolicy heartPolicy;

    @Transactional
    public GameEntryResponseDto enterGame(String username) {
        if (username == null || username.trim().isEmpty()) {
            throw new BusinessException(ErrorCode.INVALID_REQUEST, "username은 필수입니다.");
        }

        // 1. 유저 비관적 락 조회 (DB 직렬화)
        User user = userRepository.findByUsernameForUpdate(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND, "유저를 찾을 수 없습니다."));

        // 2. 유저별 인메모리 락 획득 (DB 커밋 이후와 세션 등록 사이의 경합 방지)
        ReentrantLock lock = sessionManager.getEntryLock(user.getId());
        lock.lock();
        AtomicBoolean released = new AtomicBoolean(false);
        try {
            // 3. 활성 세션 검사 (재접속)
            GameSession activeSession = sessionManager.getActiveSession(user.getId());
            if (activeSession != null) {
                HeartSnapshot snapshot = heartPolicy.calculate(user.getHeart(), user.getLastHeartUpdateTime());
                // 재접속의 경우 DB 상태 변경이 없으므로 TransactionSynchronization이 안 불릴 수 있음
                // 어차피 여기서 바로 리턴되므로 finally 블록에 의해 unlock 됨
                return GameEntryResponseDto.builder()
                        .userId(user.getId())
                        .username(user.getUsername())
                        .sessionId(String.valueOf(user.getId()))
                        .remainingHeart(snapshot.calculatedHeart())
                        .inGameGold(activeSession.getInGameGold())
                        .reconnected(true)
                        .createdAt(activeSession.getCreatedAt())
                        .serverTime(LocalDateTime.now())
                        .build();
            }

            // 4. 신규 입장 하트 처리
            HeartSnapshot snapshot = heartPolicy.calculate(user.getHeart(), user.getLastHeartUpdateTime());
            user.applyHeartSnapshot(snapshot);
            user.spendHeart(1);

            // 5. 신규 세션 준비
            GameSession newSession = new GameSession(user.getId());

            // 6. 트랜잭션 커밋 후 세션 인메모리 등록 (afterCommit 훅)
            TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
                @Override
                public void afterCommit() {
                    GameSession existing = sessionManager.putIfAbsent(user.getId(), newSession);
                    if (existing != null && !existing.isGameOver()) {
                        log.error("afterCommit 세션 등록 실패: 이미 활성 세션이 존재함. userId={}", user.getId());
                    }
                }

                @Override
                public void afterCompletion(int status) {
                    releaseEntryLock(lock, released);
                }
            });

            // 7. DTO 응답 생성
            return GameEntryResponseDto.builder()
                    .userId(user.getId())
                    .username(user.getUsername())
                    .sessionId(String.valueOf(user.getId()))
                    .remainingHeart(user.getHeart())
                    .inGameGold(newSession.getInGameGold())
                    .reconnected(false)
                    .createdAt(newSession.getCreatedAt())
                    .serverTime(LocalDateTime.now())
                    .build();
        } catch (Exception e) {
            // 예외 발생 시 catch 블록에서 선제적 방어 해제
            releaseEntryLock(lock, released);
            throw e;
        } finally {
            // 재접속 분기 등으로 Synchronization이 타지 않는 경우 여기서 해제 보장
            if (!TransactionSynchronizationManager.isSynchronizationActive() ||
                TransactionSynchronizationManager.getSynchronizations().stream().noneMatch(s -> s.getClass().getName().contains("GameEntryService"))) {
                releaseEntryLock(lock, released);
            }
        }
    }

    private void releaseEntryLock(ReentrantLock lock, AtomicBoolean released) {
        if (released.compareAndSet(false, true) && lock.isHeldByCurrentThread()) {
            lock.unlock();
        }
    }
}
