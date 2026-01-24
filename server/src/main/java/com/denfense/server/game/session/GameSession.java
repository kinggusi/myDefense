package com.denfense.server.game.session;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.PrefixType;
import com.denfense.server.game.object.InGameAlien;
import lombok.Getter;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

/**
 * 게임 한 판의 상태를 관리하는 세션 객체
 * 메모리에서만 관리됨.
 */
@Getter
public class GameSession {

    private final Long userId; // 이 세션의 주인

    // 현재 필드에 나와있는 유닛들 (Key: 유닛ID, Value: 유닛객체)
    // ConcurrentHashMap: 멀티스레드 환경에서도 안전함 (혹시 모를 동시성 이슈 방지)
    private final Map<Long, InGameAlien> aliens = new ConcurrentHashMap<>();

    // 유닛 ID 발급기 (1부터 시작, 중복 방지)
    private final AtomicLong idCounter = new AtomicLong(0);
    private int inGameGold = 500;

    public GameSession(Long userId) {
        this.userId = userId;
    }

    /**
     * 유닛 소환 (Spawn)
     * - 고유 ID를 발급하고 Map에 저장합니다.
     */
    public InGameAlien spawnAlien(AlienSpec spec, PrefixType prefix, int x, int y) {
        Long newId = idCounter.incrementAndGet(); // 1, 2, 3... 증가
        InGameAlien newAlien = new InGameAlien(newId, spec, prefix, x, y);

        aliens.put(newId, newAlien);
        return newAlien;
    }

    // ✨ [추가] 돈 쓰기 (소환 시 호출)
    public void spendGold(int amount) {
        if (this.inGameGold < amount) {
            throw new IllegalStateException("골드가 부족합니다! (보유: " + this.inGameGold + ")");
        }
        this.inGameGold -= amount;
    }

    // ✨ [추가] 돈 벌기 (몬스터 처치 시 호출)
    public void earnGold(int amount) {
        this.inGameGold += amount;
    }

    /**
     * 유닛 조회
     */
    public InGameAlien getAlien(Long alienId) {
        return aliens.get(alienId);
    }

    /**
     * 유닛 삭제 (머지 재료로 쓰였을 때 등)
     */
    public void removeAlien(Long alienId) {
        aliens.remove(alienId);
    }

    /**
     * 필드 꽉 찼는지 확인 (선택 사항)
     */
    public boolean isFull(int maxCount) {
        return aliens.size() >= maxCount;
    }

    /**
     * 웨이브 초기
     */
    private int currentWave = 0;

    /**
     * 웨이브 ++
     */
    public void nextWave() {
        this.currentWave++;
    }

    /**
     * 몬스터 장부
     */
    private int aliveMonsterCount = 0;
    
    /**
     * 생사여부
     */
    private boolean isGameOver = false;

    public void setGameOver(boolean status) {
        this.isGameOver = status;
    }

    /**
     * 몬스터 수 추가
     */
    public void addMonsters(int count) {
        this.aliveMonsterCount += count;
    }

    /**
     * 몬스터 수 감소
     */
    public void removeMonster() {
        if (this.aliveMonsterCount > 0) {
            this.aliveMonsterCount--;
        }
    }


}
