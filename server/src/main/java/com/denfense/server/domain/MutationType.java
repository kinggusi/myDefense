package com.denfense.server.domain;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import java.util.List;

@Getter
@RequiredArgsConstructor
public enum MutationType {
    NONE(0, "없음"),
    BERSERK(1, "광폭의"),
    GREEDY(2, "탐욕의"),
    SWIFT(3, "신속한"),
    GIANT(4, "거대한"),
    OBESE(5, "비만의"),
    TOXIC(6, "독성의"),
    FROZEN(7, "빙결의"),
    BLANK(99, "꽝");

    private final int code;
    private final String description;

    /**
     * 인젝터로 생성 가능한 변이 종류의 리스트를 반환합니다. (NONE, BLANK 제외한 7종)
     */
    public static List<MutationType> getInjectableTypes() {
        return List.of(BERSERK, GREEDY, SWIFT, GIANT, OBESE, TOXIC, FROZEN);
    }
}
