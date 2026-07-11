package com.denfense.server.game.object;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MutationType;
import lombok.Getter;
import lombok.Setter;

/**
 * 인게임 메모리 상에서만 존재하는 유닛 객체 (POJO)
 * DB에 저장되지 않음.
 */
@Getter
@Setter
public class InGameAlien {

    private Long id; // 메모리 상의 고유 ID (AtomicLong으로 발급)
    private AlienSpec alienSpec; // 유닛 스펙 정보
    private MutationType pendingMutationType; // NORMAL~LEGENDARY 구간 주입 DNA
    
    @Setter(lombok.AccessLevel.NONE)
    private MutationType activeMutationType;  // MYTHIC 실제 발현 변이
    
    private int mutationRerollCount;    // 재변이 리롤 횟수 카운터
    private int gridX;
    private int gridY;

    public InGameAlien(Long id, AlienSpec alienSpec, MutationType pendingMutationType, MutationType activeMutationType, int mutationRerollCount, int gridX, int gridY) {
        this.id = id;
        this.alienSpec = alienSpec;
        this.pendingMutationType = pendingMutationType;
        this.mutationRerollCount = mutationRerollCount;
        this.gridX = gridX;
        this.gridY = gridY;

        // MYTHIC이 아닌 등급에서는 activeMutationType이 NONE이 되도록 방어
        if (alienSpec.getGrade() != AlienSpec.Grade.MYTHIC) {
            this.activeMutationType = MutationType.NONE;
        } else {
            this.activeMutationType = activeMutationType;
        }
    }

    public void setActiveMutationType(MutationType activeMutationType) {
        if (this.alienSpec.getGrade() != AlienSpec.Grade.MYTHIC) {
            this.activeMutationType = MutationType.NONE;
        } else {
            this.activeMutationType = activeMutationType;
        }
    }
}
