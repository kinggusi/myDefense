package com.denfense.server.game.manager;

import com.denfense.server.game.session.GameSession;
import org.springframework.stereotype.Component;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 전체 게임 세션을 관리하는 매니저
 * (실제로는 Redis 등을 쓰겠지만, 여기서는 메모리 Map 사용)
 */
@Component
public class GameSessionManager {

    // 전체 접속 유저의 게임 세션 저장소 (Key: UserId)
    private final Map<Long, GameSession> sessions = new ConcurrentHashMap<>();

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
            throw new IllegalStateException("진행 중인 게임이 없습니다. (UserId: " + userId + ")");
        }
        return session;
    }

    /**
     * 게임 종료 시 세션 삭제 (메모리 정리)
     */
    public void removeSession(Long userId) {
        sessions.remove(userId);
    }
}
