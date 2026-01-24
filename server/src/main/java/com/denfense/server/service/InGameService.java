package com.denfense.server.service;

import com.denfense.server.dto.response.WaveSpawnDto;
import com.denfense.server.game.object.InGameAlien;

import java.util.List;

public interface InGameService {

    InGameAlien processMerge(Long userId, Long sourceId, Long targetId);

    InGameAlien summonAlien(Long userId);

    List<WaveSpawnDto> startNextWave(Long userId);
    int killMonster(Long userId, Long monsterSpecId);
    WaveSpawnDto spawnMissionBoss(Long userId);





}
