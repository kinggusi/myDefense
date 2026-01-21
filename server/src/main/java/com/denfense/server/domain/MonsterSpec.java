package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity
@Getter @NoArgsConstructor
@Table(name = "monster_specs")
public class MonsterSpec {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String name;        // 몬스터 이름
    private int hp;             // 체력
    private double moveSpeed;   // 이동 속도
    private int dropGold;       // 처치 보상
}