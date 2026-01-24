package com.denfense.server.dto.response;

import com.denfense.server.domain.MonsterSpec;
import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class WaveSpawnDto {
    private MonsterSpec monsterSpec; // 몬스터 원본 정보 (이름, 기본스탯 등)
    private int count;               // 등장 마릿수 (예: 40)
    private double hpMultiplier;     // 체력 배율 (예: 1.5배)
}