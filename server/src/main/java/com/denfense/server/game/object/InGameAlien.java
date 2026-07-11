package com.denfense.server.game.object;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.BoardObjectType;
import com.denfense.server.domain.MutationType;
import lombok.Getter;
import lombok.Setter;

/**
 * 인게임 메모리 상에서만 존재하는 유닛 객체 (POJO)
 * DB에 저장되지 않음.
 */
@Getter
@Setter
public class InGameAlien extends BoardObject {

    private AlienSpec alienSpec; // 유닛 스펙 정보
    private MutationType pendingMutationType; // NORMAL~LEGENDARY 구간 주입 DNA
    
    @Setter(lombok.AccessLevel.NONE)
    private MutationType activeMutationType;  // MYTHIC 실제 발현 변이
    
    private int mutationRerollCount;    // 재변이 리롤 횟수 카운터

    public InGameAlien(Long id, AlienSpec alienSpec, MutationType pendingMutationType, MutationType activeMutationType, int mutationRerollCount, int gridX, int gridY) {
        super(id, gridX, gridY);
        this.alienSpec = alienSpec;
        this.pendingMutationType = pendingMutationType != null ? pendingMutationType : MutationType.NONE;
        this.mutationRerollCount = mutationRerollCount;

        MutationType targetActive = activeMutationType != null ? activeMutationType : MutationType.NONE;
        // MYTHIC이 아닌 등급에서는 activeMutationType이 NONE이 되도록 방어
        if (alienSpec.getGrade() != AlienSpec.Grade.MYTHIC) {
            this.activeMutationType = MutationType.NONE;
        } else {
            this.activeMutationType = targetActive;
        }
    }

    public void setActiveMutationType(MutationType activeMutationType) {
        MutationType targetActive = activeMutationType != null ? activeMutationType : MutationType.NONE;
        if (this.alienSpec.getGrade() != AlienSpec.Grade.MYTHIC) {
            this.activeMutationType = MutationType.NONE;
        } else {
            this.activeMutationType = targetActive;
        }
    }

    @Override
    public BoardObjectType getObjectType() {
        return BoardObjectType.ALIEN;
    }
}
