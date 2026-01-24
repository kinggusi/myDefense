package com.denfense.server.service.impl;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MonsterSpec;
import com.denfense.server.domain.PrefixType;
import com.denfense.server.dto.response.WaveSpawnDto;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.object.InGameAlien;
import com.denfense.server.game.session.GameSession;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.MonsterSpecRepository;
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

        // 2. 유닛 2마리 존재 확인
        InGameAlien source = session.getAlien(sourceId);
        InGameAlien target = session.getAlien(targetId);

        if (source == null || target == null) {
            throw new IllegalArgumentException("유닛이 존재하지 않습니다.");
        }

        // 3. 등급 검사
        if (source.getAlienSpec().getGrade() != target.getAlienSpec().getGrade()) {
            throw new IllegalArgumentException("같은 등급끼리만 합칠 수 있습니다.");
        }

        // [A] 결과물 스펙 결정
        AlienSpec resultSpec = null;
        AlienSpec.Grade currentGrade = source.getAlienSpec().getGrade();

        // 1. 동종 합체
        if (source.getAlienSpec().getId().equals(target.getAlienSpec().getId())) {
            Long nextId = source.getAlienSpec().getEvolutionTargetId();
            if (nextId == null) {
                throw new IllegalStateException("더 이상 고정 진화할 수 없는 유닛입니다.");
            }
            resultSpec = alienSpecRepository.findById(nextId)
                    .orElseThrow(() -> new IllegalStateException("진화 정보가 없습니다. ID: " + nextId));
        }
        // 2. 이종 합체
        else {
            AlienSpec.Grade nextGrade = currentGrade.getNext();
            resultSpec = alienSpecRepository.findRandomByGrade(nextGrade.name())
                    .orElseThrow(() -> new IllegalStateException(nextGrade.name() + " 등급 없음"));
        }

        // [B] 접두사 승계
        PrefixType resultPrefix = (source.getPrefixType() != PrefixType.NONE) ? source.getPrefixType() : PrefixType.NONE;

        // [C] 리스크 (Slime Risk)
        if (resultPrefix != PrefixType.NONE && random.nextInt(100) < 10) {
            resultPrefix = PrefixType.SLIME;
        }

        // [D] 결과 반영
        session.removeAlien(sourceId);
        session.removeAlien(targetId);

        return session.spawnAlien(
                resultSpec,
                resultPrefix,
                target.getGridX(),
                target.getGridY()
        );
    }

    /**
     * summonAlien - 소환
     */
    @Override
    public InGameAlien summonAlien(Long userId) {
        GameSession session = sessionManager.getSession(userId);

        // 죽었거나, 80마리 넘었으면 소환 금지
        checkGameOver(session);
        checkPopulationLimit(session);

        // 2. 돈 차감
        session.spendGold(50);

        // 3. 확률 뽑기
        int chance = random.nextInt(100);
        AlienSpec.Grade grade;

        if (chance < 70) grade = AlienSpec.Grade.NORMAL;
        else if (chance < 95) grade = AlienSpec.Grade.EPIC;
        else grade = AlienSpec.Grade.UNIQUE;

        // 4. 스펙 가져오기
        AlienSpec spec = alienSpecRepository.findRandomByGrade(grade.name())
                .orElseThrow(() -> new IllegalStateException("데이터 없음"));

        // 5. 소환
        return session.spawnAlien(spec, PrefixType.NONE, 0, 0);
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
            throw new IllegalStateException("GAME_OVER");
        }
    }

    private void checkPopulationLimit(GameSession session) {
        if (session.getAliveMonsterCount() > MAX_MONSTER_LIMIT) {
            throw new IllegalStateException("인구수 초과! 몬스터를 먼저 처치하세요!");
        }
    }
}