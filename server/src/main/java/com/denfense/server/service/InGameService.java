package com.denfense.server.service;

import com.denfense.server.dto.response.UseInjectorResponseDto;
import com.denfense.server.dto.response.WaveSpawnDto;
import com.denfense.server.game.object.BoardObject;
import com.denfense.server.game.object.InGameAlien;

import java.util.List;

public interface InGameService {

    InGameAlien processMerge(Long userId, Long sourceId, Long targetId);

    BoardObject summonAlien(Long userId);

    UseInjectorResponseDto useInjector(Long userId, Long injectorId, Long alienId);

    List<WaveSpawnDto> startNextWave(Long userId);
    int killMonster(Long userId, Long monsterSpecId);
    WaveSpawnDto spawnMissionBoss(Long userId);





}
