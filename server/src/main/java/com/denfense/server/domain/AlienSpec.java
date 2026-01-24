package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Getter
@Setter
@NoArgsConstructor
@Table(name = "alien_specs")
public class AlienSpec {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String name;
    private String description;
    private int baseAtk;
    private int baseMp;
    private double atkSpeed;

    // ⚠️ [주의] 'range'는 MySQL 예약어라 에러 날 수 있음 -> 컬럼명 변경 추천
    @Column(name = "attack_range")
    private double range;

    private Long evolutionTargetId;
    private boolean isLocked;

    // ==========================================
    // 1. Enum 정의 (설계도)
    // ==========================================
    public enum Grade {
        NORMAL, EPIC, UNIQUE, LEGEND, EVOLUTION;

        public Grade getNext() {
            int nextIndex = this.ordinal() + 1;
            if (nextIndex >= values().length) {
                throw new IllegalStateException("이미 최종 등급입니다: " + this.name());
            }
            return values()[nextIndex];
        }
    }

    // ==========================================
    // 2. 변수 선언 (실제 사용) - 여기 딱 한 번만!
    // ==========================================
    @Enumerated(EnumType.STRING)
    private Grade grade;
}