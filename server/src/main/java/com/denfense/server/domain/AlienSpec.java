package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity
@Getter
@NoArgsConstructor
@Table(name = "alien_specs") // 테이블 이름
public class AlienSpec {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String name;        // 왹져 이름 (예: 그레이, 렙틸리언)
    private int baseAtk;        // 기본 공격력
    private int baseHp;         // 기본 체력
    private double atkSpeed;    // 공격 속도 (초당 공격 횟수)
    private double range;       // 사거리
}
