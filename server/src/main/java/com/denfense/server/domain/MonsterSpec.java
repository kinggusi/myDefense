package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity
@Getter @NoArgsConstructor
@Table(name = "monster_specs")
public class MonsterSpec {

    @Id @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String name;
    private int hp;
    private double moveSpeed;
    private int dropGold;
    private int damage;

    // ✨ [추가] 몬스터 등급 구분
    @Enumerated(EnumType.STRING)
    private MonsterType type;

    public enum MonsterType {
        NORMAL,       // 일반
        ELITE,        // 엘리트
        WAVE_BOSS,    // 라운드 보스 (10탄마다)
        MISSION_BOSS, // 미션 보스 (유저 소환)
        FINAL_BOSS    // 최종 보스
    }
}