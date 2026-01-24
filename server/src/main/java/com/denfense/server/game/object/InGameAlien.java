package com.denfense.server.game.object;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.PrefixType;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

/**
 * 인게임 메모리 상에서만 존재하는 유닛 객체 (POJO)
 * DB에 저장되지 않음.
 */
@Getter
@Setter
@AllArgsConstructor
public class InGameAlien {

    private Long id; // 메모리 상의 고유 ID (AtomicLong으로 발급)
    private AlienSpec alienSpec; // 유닛 스펙 정보
    private PrefixType prefixType; // 접두사 (생체변이)
    private int gridX;
    private int gridY;
}
