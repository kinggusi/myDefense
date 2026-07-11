package com.denfense.server.game.session;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MutationType;
import com.denfense.server.game.object.BoardObject;
import com.denfense.server.game.object.InGameAlien;
import com.denfense.server.game.object.InGameInjector;
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

    // 현재 필드에 나와있는 모든 객체들 (Key: 객체ID, Value: BoardObject)
    private final Map<Long, BoardObject> boardObjects = new ConcurrentHashMap<>();

    // 유닛 ID 발급기 (1부터 시작, 중복 방지)
    private final AtomicLong idCounter = new AtomicLong(0);
    private int inGameGold = 500;
    private int kidnapSuccessCount = 0;

    // 4x6 그리드를 관리할 인메모리 배열
    private final BoardObject[][] grid = new BoardObject[4][6];

    public GameSession(Long userId) {
        this.userId = userId;
    }

    /**
     * 왹져 소환 (Spawn)
     */
    public synchronized InGameAlien spawnAlien(AlienSpec spec, MutationType pendingMutation, MutationType activeMutation, int rerollCount, int x, int y) {
        if (x < 0 || x >= 4 || y < 0 || y >= 6) {
            throw new IllegalArgumentException("유효하지 않은 좌표입니다. (x: " + x + ", y: " + y + ")");
        }
        if (grid[x][y] != null) {
            throw new IllegalStateException("해당 위치에 이미 유닛/인젝터가 존재합니다.");
        }

        Long newId = idCounter.incrementAndGet();
        InGameAlien newAlien = new InGameAlien(newId, spec, pendingMutation, activeMutation, rerollCount, x, y);

        boardObjects.put(newId, newAlien);
        grid[x][y] = newAlien;
        return newAlien;
    }

    /**
     * 생체변이 인젝터 소환 (Spawn)
     */
    public synchronized InGameInjector spawnInjector(MutationType mutationType, int x, int y) {
        if (x < 0 || x >= 4 || y < 0 || y >= 6) {
            throw new IllegalArgumentException("유효하지 않은 좌표입니다. (x: " + x + ", y: " + y + ")");
        }
        if (grid[x][y] != null) {
            throw new IllegalStateException("해당 위치에 이미 유닛/인젝터가 존재합니다.");
        }

        Long newId = idCounter.incrementAndGet();
        InGameInjector newInjector = new InGameInjector(newId, mutationType, x, y);

        boardObjects.put(newId, newInjector);
        grid[x][y] = newInjector;
        return newInjector;
    }

    /**
     * 납치 횟수에 따른 선형 증가 비용 계산
     */
    public synchronized int getCurrentKidnapCost() {
        return 50 + kidnapSuccessCount * 10;
    }

    /**
     * synchronized 왹져 납치 소환 원자 처리
     */
    public synchronized BoardObject kidnapAlien(AlienSpec spec) {
        // AlienSpec null 검증
        if (spec == null) {
            throw new IllegalArgumentException("AlienSpec은 null일 수 없습니다.");
        }

        // 1. 첫 빈칸 검색 (화면 기준 왼쪽->오른쪽, 위->아래 순서)
        int emptyX = -1;
        int emptyY = -1;
        boolean foundEmpty = false;
        for (int i = 3; i >= 0; i--) {
            for (int j = 0; j < 6; j++) {
                if (grid[i][j] == null) {
                    emptyX = i;
                    emptyY = j;
                    foundEmpty = true;
                    break;
                }
            }
            if (foundEmpty) break;
        }
        if (!foundEmpty) {
            throw new IllegalStateException("필드에 빈 공간이 없습니다!");
        }

        // 2. 현재 비용 계산
        int cost = getCurrentKidnapCost();

        // 3. 골드 부족 여부 확인
        if (inGameGold < cost) {
            throw new IllegalStateException("골드가 부족합니다! (보유: " + this.inGameGold + ", 필요: " + cost + ")");
        }

        // 4. 객체 ID 발급 및 객체 생성
        Long newId = idCounter.incrementAndGet();
        InGameAlien newAlien = new InGameAlien(newId, spec, MutationType.NONE, MutationType.NONE, 0, emptyX, emptyY);

        // 5. boardObjects와 grid에 등록
        boardObjects.put(newId, newAlien);
        grid[emptyX][emptyY] = newAlien;

        // 6. inGameGold 차감
        inGameGold -= cost;

        // 7. kidnapSuccessCount 증가
        kidnapSuccessCount++;

        // 8. 생성 객체 반환
        return newAlien;
    }

    /**
     * synchronized 인젝터 납치 소환 원자 처리
     */
    public synchronized BoardObject kidnapInjector(MutationType mutationType) {
        // MutationType 유효성 검증 (null, NONE, BLANK 차단)
        if (mutationType == null || mutationType == MutationType.NONE || mutationType == MutationType.BLANK) {
            throw new IllegalArgumentException("사용 불가능한 MutationType입니다.");
        }

        // 1. 첫 빈칸 검색 (화면 기준 왼쪽->오른쪽, 위->아래 순서)
        int emptyX = -1;
        int emptyY = -1;
        boolean foundEmpty = false;
        for (int i = 3; i >= 0; i--) {
            for (int j = 0; j < 6; j++) {
                if (grid[i][j] == null) {
                    emptyX = i;
                    emptyY = j;
                    foundEmpty = true;
                    break;
                }
            }
            if (foundEmpty) break;
        }
        if (!foundEmpty) {
            throw new IllegalStateException("필드에 빈 공간이 없습니다!");
        }

        // 2. 현재 비용 계산
        int cost = getCurrentKidnapCost();

        // 3. 골드 부족 여부 확인
        if (inGameGold < cost) {
            throw new IllegalStateException("골드가 부족합니다! (보유: " + this.inGameGold + ", 필요: " + cost + ")");
        }

        // 4. 객체 ID 발급 및 객체 생성
        Long newId = idCounter.incrementAndGet();
        InGameInjector newInjector = new InGameInjector(newId, mutationType, emptyX, emptyY);

        // 5. boardObjects와 grid에 등록
        boardObjects.put(newId, newInjector);
        grid[emptyX][emptyY] = newInjector;

        // 6. inGameGold 차감
        inGameGold -= cost;

        // 7. kidnapSuccessCount 증가
        kidnapSuccessCount++;

        // 8. 생성 객체 반환
        return newInjector;
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
     * 객체 조회 (전체 보드용)
     */
    public BoardObject getBoardObject(Long id) {
        return boardObjects.get(id);
    }

    /**
     * 왹져 조회 (기존 API 호환용)
     */
    public InGameAlien getAlien(Long alienId) {
        BoardObject obj = boardObjects.get(alienId);
        if (obj instanceof InGameAlien) {
            return (InGameAlien) obj;
        }
        return null;
    }

    /**
     * 왹져 전용 조회 맵 구성 (기존 호출부 호환 보장)
     * [IMPORTANT] 이 반환 Map은 필터링된 복사본(new ConcurrentHashMap)이므로,
     * 이 Map 자체의 원소를 추가/삭제하는 행위가 실제 GameSession 내부 
     * boardObjects 세션 상태를 변경시키지 않습니다.
     */
    public Map<Long, InGameAlien> getAliens() {
        Map<Long, InGameAlien> alienMap = new ConcurrentHashMap<>();
        for (Map.Entry<Long, BoardObject> entry : boardObjects.entrySet()) {
            if (entry.getValue() instanceof InGameAlien) {
                alienMap.put(entry.getKey(), (InGameAlien) entry.getValue());
            }
        }
        return alienMap;
    }

    /**
     * 전체 보드 점유 객체(왹져 + 인젝터)의 총개수를 조회합니다.
     */
    public int getBoardObjectCount() {
        return boardObjects.size();
    }

    /**
     * 왹져 삭제 (기존 API 호환성 유지를 위해 유지)
     * [WARNING] 메소드명이 Alien 전용이지만, 내부적으로 removeBoardObject를 호출하므로 
     * 실수로 Injector의 ID를 넘기더라도 인젝터 객체까지 삭제 처리가 수행됩니다.
     * 향후 3-B 이후 단계에서 Deprecated 시키거나 지칭 이름을 갱신할 후보군입니다.
     */
    public synchronized void removeAlien(Long alienId) {
        removeBoardObject(alienId);
    }

    /**
     * 객체 삭제 (공통 보드용)
     */
    public synchronized void removeBoardObject(Long id) {
        BoardObject obj = boardObjects.remove(id);
        if (obj != null) {
            grid[obj.getGridX()][obj.getGridY()] = null;
        }
    }

    /**
     * 왹져 이동 (기존 API 호환성 유지를 위해 유지)
     * [WARNING] 메소드명이 Alien 전용이지만, 내부적으로 moveBoardObject를 호출하므로 
     * 실수로 Injector의 ID를 넘기더라도 인젝터 객체까지 이동 처리가 정상 수행됩니다.
     * 향후 3-B 이후 단계에서 Deprecated 시키거나 지칭 이름을 갱신할 후보군입니다.
     */
    public synchronized void moveAlien(Long alienId, int newX, int newY) {
        moveBoardObject(alienId, newX, newY);
    }

    /**
     * 객체 이동 (공통 보드용)
     */
    public synchronized void moveBoardObject(Long id, int newX, int newY) {
        BoardObject obj = boardObjects.get(id);
        if (obj == null) {
            throw new IllegalArgumentException("존재하지 않는 객체입니다.");
        }
        if (newX < 0 || newX >= 4 || newY < 0 || newY >= 6) {
            throw new IllegalArgumentException("유효하지 않은 좌표입니다.");
        }
        if (grid[newX][newY] != null) {
            throw new IllegalStateException("이동할 위치에 이미 유닛/인젝터가 존재합니다.");
        }

        grid[obj.getGridX()][obj.getGridY()] = null;
        obj.setGridX(newX);
        obj.setGridY(newY);
        grid[newX][newY] = obj;
    }

    /**
     * 필드 전체가 가득 찼는지 확인 (왹져 + 인젝터 보드 점유 기준)
     */
    public boolean isFull(int maxCount) {
        return getBoardObjectCount() >= maxCount;
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

    /**
     * 동기화 기반의 원자적 인젝터 사용 처리
     */
    public synchronized InGameAlien applyInjector(Long injectorId, Long alienId) {
        // 1. injectorId와 alienId null 및 동일 ID 검사
        if (injectorId == null || alienId == null) {
            throw new IllegalArgumentException("인젝터 ID와 왹져 ID는 필수입니다.");
        }
        if (injectorId.equals(alienId)) {
            throw new IllegalArgumentException("인젝터 ID와 왹져 ID는 동일할 수 없습니다.");
        }

        // 2. 두 BoardObject 존재 여부 검사
        BoardObject alienObj = boardObjects.get(alienId);
        BoardObject injectorObj = boardObjects.get(injectorId);

        if (alienObj == null || injectorObj == null) {
            throw new IllegalArgumentException("대상 유닛 또는 인젝터를 찾을 수 없습니다.");
        }

        // 3. injector가 InGameInjector인지 검사
        if (!(injectorObj instanceof InGameInjector)) {
            throw new IllegalArgumentException("대상이 올바른 인젝터가 아닙니다.");
        }

        // 4. 대상이 InGameAlien인지 검사
        if (!(alienObj instanceof InGameAlien)) {
            throw new IllegalArgumentException("대상이 올바른 왹져가 아닙니다.");
        }

        InGameAlien alien = (InGameAlien) alienObj;
        InGameInjector injector = (InGameInjector) injectorObj;

        // 5. mutationType이 null, NONE, BLANK가 아닌지 검사
        MutationType mutationType = injector.getMutationType();
        if (mutationType == null || mutationType == MutationType.NONE || mutationType == MutationType.BLANK) {
            throw new IllegalArgumentException("사용 불가능한 인젝터입니다.");
        }

        // 6. 기존 activeMutationType 값 보존 및 7. pendingMutationType 변경
        // (setPendingMutationType 호출은 기존 activeMutationType 값을 그대로 보존 및 유지합니다.)
        alien.setPendingMutationType(mutationType);

        // 8. boardObjects와 grid에서 Injector 제거 (공통 removeBoardObject 재사용)
        removeBoardObject(injectorId);

        // 9. 변경된 Alien 반환
        return alien;
    }
}