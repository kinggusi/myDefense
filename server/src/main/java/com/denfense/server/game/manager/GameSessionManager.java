package com.denfense.server.game.manager;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.game.session.GameSession;
import org.springframework.stereotype.Component;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.locks.ReentrantLock;

/**
 * 전체 게임 세션을 관리하는 매니저
 * (실제로는 Redis 등을 쓰겠지만, 여기서는 메모리 Map 사용)
 */
@Component
public class GameSessionManager {

    // 전체 접속 유저의 게임 세션 저장소 (Key: UserId)
    private final Map<Long, GameSession> sessions = new ConcurrentHashMap<>();
    
    // 유저 단위의 세션 입장 직렬화를 위한 락
    private final Map<Long, ReentrantLock> userLocks = new ConcurrentHashMap<>();

    /**
     * 게임 시작 시 세션 생성
     * (이미 있다면 덮어쓰기 = 재시작 효과)
     */
    public GameSession createSession(Long userId) {
        GameSession newSession = new GameSession(userId);
        sessions.put(userId, newSession);
        return newSession;
    }

    /**
     * 진행 중인 게임 세션 가져오기
     */
    public GameSession getSession(Long userId) {
        GameSession session = sessions.get(userId);
        if (session == null) {
            throw new BusinessException(ErrorCode.GAME_SESSION_NOT_FOUND, "진행 중인 게임이 없습니다. (UserId: " + userId + ")");
        }
        return session;
    }

    /**
     * 게임 종료 시 세션 삭제 (메모리 정리)
     */
    public void removeSession(Long userId) {
        sessions.remove(userId);
    }

    /**
     * 특정 세션을 삭제 (예외 상황 처리용)
     */
    public void removeSession(Long userId, GameSession expectedSession) {
        sessions.remove(userId, expectedSession);
    }

    /**
     * 활성 세션 여부 확인
     */
    public boolean hasActiveSession(Long userId) {
        GameSession session = sessions.get(userId);
        return session != null && !session.isGameOver();
    }

    /**
     * 활성 세션 가져오기
     */
    public GameSession getActiveSession(Long userId) {
        GameSession session = sessions.get(userId);
        if (session != null && !session.isGameOver()) {
            return session;
        }
        return null;
    }

    /**
     * 세션 삽입 (없을 때만)
     */
    public GameSession putIfAbsent(Long userId, GameSession session) {
        return sessions.putIfAbsent(userId, session);
    }

    /**
     * 유저별 입장 락 획득 (메모리 누수를 감수하고 정합성을 위해 Map에서 제거하지 않음)
     * 고유 사용자 수만큼 ReentrantLock 인스턴스가 유지되지만, 동일 사용자에 대해 항상 같은 락을 보장함.
     */
    public ReentrantLock getEntryLock(Long userId) {
        return userLocks.computeIfAbsent(userId, k -> new ReentrantLock());
    }
}
