package com.denfense.server.service.impl;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MonsterSpec;
import com.denfense.server.domain.MutationType;
import com.denfense.server.dto.response.UseInjectorResponseDto;
import com.denfense.server.dto.response.WaveSpawnDto;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.object.BoardObject;
import com.denfense.server.game.object.InGameAlien;
import com.denfense.server.game.session.GameSession;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.MonsterSpecRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.InGameService;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

@Service
@RequiredArgsConstructor
public class InGameServiceImpl implements InGameService {

    private final GameSessionManager sessionManager;
    private final AlienSpecRepository alienSpecRepository;
    private final MonsterSpecRepository monsterSpecRepository;
    private final UserRepository userRepository;
    private final Random random = new Random();

    private static final int MAX_MONSTER_LIMIT = 80;

    /**
     * 머지 처리 메인 로직
     */
    @Override
    public InGameAlien processMerge(Long userId, Long sourceId, Long targetId) {

        GameSession session = sessionManager.getSession(userId);

        // (죽은 사람은 조작 불가)
        checkGameOver(session);
        checkPopulationLimit(session);

        // 2. 유닛 2마리 존재 확인 및 Alien 타입 검증 (인젝터 등 합성 차단)
        BoardObject sourceObj = session.getBoardObject(sourceId);
        BoardObject targetObj = session.getBoardObject(targetId);

        if (!(sourceObj instanceof InGameAlien) || !(targetObj instanceof InGameAlien)) {
            throw new BusinessException(ErrorCode.INVALID_MERGE, "합성은 오직 왹져끼리만 가능합니다.");
        }

        InGameAlien source = (InGameAlien) sourceObj;
        InGameAlien target = (InGameAlien) targetObj;

        // 3. 등급 검사
        if (source.getAlienSpec().getGrade() != target.getAlienSpec().getGrade()) {
            throw new BusinessException(ErrorCode.INVALID_MERGE, "같은 등급끼리만 합칠 수 있습니다.");
        }

        // [A] 결과물 스펙 결정
        AlienSpec resultSpec = null;
        AlienSpec.Grade currentGrade = source.getAlienSpec().getGrade();

        // 1. 같은 종인지 검사
        if (!source.getAlienSpec().getId().equals(target.getAlienSpec().getId())) {
            throw new BusinessException(ErrorCode.INVALID_MERGE, "같은 종끼리만 합칠 수 있습니다.");
        }

        if (currentGrade == AlienSpec.Grade.LEGEND) {
            // 전설 -> 신화: 해당 플레이어가 해금한 신화 풀만 사용
            User user = userRepository.findById(userId)
                    .orElseThrow(() -> new IllegalArgumentException("존재하지 않는 유저입니다."));
            
            List<AlienSpec> unlockedMythics = new ArrayList<>();
            for (UserAlien ua : user.getUserAliens()) {
                AlienSpec spec = ua.getAlienSpec();
                if (spec.getGrade() == AlienSpec.Grade.MYTHIC) {
                    unlockedMythics.add(spec);
                }
            }

            if (unlockedMythics.isEmpty()) {
                throw new IllegalStateException("해금된 신화 등급 왹져가 없습니다.");
            }

            resultSpec = unlockedMythics.get(random.nextInt(unlockedMythics.size()));
        } else {
            // 일반 등급: 다음 등급 전체 풀에서 랜덤
            AlienSpec.Grade nextGrade = currentGrade.getNext();
            List<AlienSpec> pool = alienSpecRepository.findAllByGrade(nextGrade);
            if (pool.isEmpty()) {
                throw new IllegalStateException(nextGrade.name() + " 등급의 왹져 데이터가 존재하지 않습니다.");
            }
            resultSpec = pool.get(random.nextInt(pool.size()));
        }

        // [B] DNA 계승 연산 (원자성 보장: 재료 유닛 제거 전에 연산 완결)
        MutationType inheritedPending = inheritPendingMutation(source.getPendingMutationType(), target.getPendingMutationType());

        // [D] 결과 반영 (모든 검증과 계산 완료 후 재료 제거)
        session.removeAlien(sourceId);
        session.removeAlien(targetId);

        return session.spawnAlien(
                resultSpec,
                inheritedPending,
                MutationType.NONE,
                0,
                target.getGridX(),
                target.getGridY()
        );
    }

    /**
     * DNA 계승 정책 연산
     */
    private MutationType inheritPendingMutation(MutationType sourcePending, MutationType targetPending) {
        MutationType s = (sourcePending == null) ? MutationType.NONE : sourcePending;
        MutationType t = (targetPending == null) ? MutationType.NONE : targetPending;

        if (s == MutationType.NONE && t == MutationType.NONE) {
            return MutationType.NONE;
        }
        if (s != MutationType.NONE && t == MutationType.NONE) {
            return s;
        }
        if (s == MutationType.NONE && t != MutationType.NONE) {
            return t;
        }
        if (s == t) {
            return s;
        }
        // 서로 다른 유효 DNA인 경우 50% 확률로 랜덤 채택 (BLANK도 유효 DNA로 취급)
        return random.nextBoolean() ? s : t;
    }

    /**
     * summonAlien - 소환
     */
    @Override
    public BoardObject summonAlien(Long userId) {
        GameSession session = sessionManager.getSession(userId);

        // 1. 세션/게임 상태 검증
        checkGameOver(session);
        checkPopulationLimit(session);

        // 2. 99.5% Alien / 0.5% Injector 결과 결정
        int kidnapChance = random.nextInt(10000);

        if (kidnapChance >= 9950) {
            // Mutation Injector 0.5% 스폰 (7종 동일 확률로 균등 소환)
            List<MutationType> pool = MutationType.getInjectableTypes();
            MutationType injectorType = pool.get(random.nextInt(pool.size()));

            // 4. GameSession의 Kidnap 전용 원자 메서드 호출
            return session.kidnapInjector(injectorType);
        } else {
            // Normal Alien 99.5% 스폰 (NORMAL 등급 고정 - Kidnap Policy 적용)
            AlienSpec spec = drawNormalAlienSpec();

            // 4. GameSession의 Kidnap 전용 원자 메서드 호출
            return session.kidnapAlien(spec);
        }
    }

    /**
     * Kidnap Policy: 소환 대상 Normal 등급의 AlienSpec 명세를 안전하게 추출합니다.
     */
    private AlienSpec drawNormalAlienSpec() {
        return alienSpecRepository.findRandomByGrade(AlienSpec.Grade.NORMAL.name())
                .orElseThrow(() -> new IllegalStateException("NORMAL 등급 왹져 데이터가 존재하지 않습니다."));
    }

    @Override
    public UseInjectorResponseDto useInjector(Long userId, Long injectorId, Long alienId) {
        GameSession session = sessionManager.getSession(userId);
        checkGameOver(session);

        // synchronized 세션 원자 처리 메서드 호출 (activeMutationType 값 보존)
        InGameAlien alien = session.applyInjector(injectorId, alienId);

        return new UseInjectorResponseDto(
                alien.getId(),
                alien.getPendingMutationType(),
                alien.getActiveMutationType(),
                injectorId,
                alien.getGridX(),
                alien.getGridY()
        );
    }

    /**
     * startNextWave - 웨이브관리
     */
    @Override
    public List<WaveSpawnDto> startNextWave(Long userId) {
        GameSession session = sessionManager.getSession(userId);

        if (session.getAliveMonsterCount() > MAX_MONSTER_LIMIT || session.isGameOver()) {
            session.setGameOver(true);
            return new ArrayList<>();
        }

        session.nextWave();
        int wave = session.getCurrentWave();

        List<WaveSpawnDto> spawnPlan = new ArrayList<>();
        double hpMultiplier = Math.pow(1.2, wave - 1);


        int newMonsterCount = 0;

        //  보스 라운드
        if (wave % 10 == 0) {
            MonsterSpec boss = getMonsterByType(MonsterSpec.MonsterType.WAVE_BOSS);
            spawnPlan.add(new WaveSpawnDto(boss, 1, hpMultiplier));
            newMonsterCount = 1; // 카운트 설정
        }
        //  일반 라운드
        else {
            int totalCount = 50;
            int eliteCount = (wave % 10 - 1) * 10;
            if (eliteCount < 0) eliteCount = 0;
            if (eliteCount > 50) eliteCount = 50;
            int normalCount = totalCount - eliteCount;

            if (normalCount > 0) {
                spawnPlan.add(new WaveSpawnDto(getMonsterByType(MonsterSpec.MonsterType.NORMAL), normalCount, hpMultiplier));
            }
            if (eliteCount > 0) {
                spawnPlan.add(new WaveSpawnDto(getMonsterByType(MonsterSpec.MonsterType.ELITE), eliteCount, hpMultiplier));
            }
            newMonsterCount = totalCount; // 카운트 설정
        }

        session.addMonsters(newMonsterCount);
        return spawnPlan;
    }

    /**
     * spawnMissionBoss - 미션보스 소환
     */
    @Override
    public WaveSpawnDto spawnMissionBoss(Long userId) {
        MonsterSpec boss = getMonsterByType(MonsterSpec.MonsterType.MISSION_BOSS);
        GameSession session = sessionManager.getSession(userId);
        session.addMonsters(1);
        return new WaveSpawnDto(boss, 1, 1.0);
    }

    /**
     * killMonster - 몬스터 킬
     */
    @Override
    public int killMonster(Long userId, Long monsterSpecId) {
        GameSession session = sessionManager.getSession(userId);

        session.removeMonster();

        MonsterSpec spec = monsterSpecRepository.findById(monsterSpecId)
                .orElseThrow(() -> new IllegalArgumentException("몬스터 정보 없음"));

        session.earnGold(spec.getDropGold());
        return session.getInGameGold();
    }

    /**
     * Helper Methods
     */
    private MonsterSpec getMonsterByType(MonsterSpec.MonsterType type) {
        return monsterSpecRepository.findTopByType(type)
                .orElseThrow(() -> new IllegalStateException("DB에 [" + type + "] 타입 몬스터 데이터가 없습니다!"));
    }

    private void checkGameOver(GameSession session) {
        if (session.isGameOver()) {
            throw new BusinessException(ErrorCode.GAME_ALREADY_OVER, "이미 종료된 게임 세션입니다.");
        }
    }

    private void checkPopulationLimit(GameSession session) {
        if (session.getAliveMonsterCount() > MAX_MONSTER_LIMIT) {
            throw new BusinessException(ErrorCode.BOARD_FULL, "인구수 초과! 몬스터를 먼저 처치하세요!");
        }
    }

    @Override
    public BoardObject moveBoardObject(Long userId, Long objectId, int newX, int newY) {
        GameSession session = sessionManager.getSession(userId);
        
        // 게임 상태 검증
        checkGameOver(session);

        // 세션의 moveBoardObject 호출
        return session.moveBoardObject(objectId, newX, newY);
    }
}