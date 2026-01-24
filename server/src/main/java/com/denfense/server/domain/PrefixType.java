package com.denfense.server.domain;

import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum PrefixType {
    NONE(0, "없음"),
    PROLIFIC(1, "다산의"), // 소환 시 일정 확률로 1마리 더 (증식)
    GREEDY(2, "탐욕의"),   // 처치 시 골드 획득량 증가
    SWIFT(3, "광속의"),    // 공격 속도 증가
    PREDATORY(4, "포식의"), // 보스 몬스터에게 추가 데미지
    OBESE(5, "비만의"),    // 체력 증가 (탱커 역할)
    SLIME(99, "꽝");       // 실패작 (슬라임)

    private final int code;
    private final String description;
}
