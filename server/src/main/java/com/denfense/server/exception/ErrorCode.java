package com.denfense.server.exception;

import org.springframework.http.HttpStatus;

public enum ErrorCode {

    GAME_SESSION_NOT_FOUND(HttpStatus.NOT_FOUND, "게임 세션을 찾을 수 없습니다."),
    GAME_ALREADY_OVER(HttpStatus.CONFLICT, "이미 종료된 게임 세션입니다."),
    INSUFFICIENT_GOLD(HttpStatus.CONFLICT, "골드가 부족합니다."),
    BOARD_FULL(HttpStatus.CONFLICT, "보드판이 가득 찼습니다."),
    BOARD_OBJECT_NOT_FOUND(HttpStatus.NOT_FOUND, "해당 보드 객체를 찾을 수 없습니다."),
    INVALID_BOARD_POSITION(HttpStatus.BAD_REQUEST, "유효하지 않은 보드 좌표입니다."),
    BOARD_STATE_INCONSISTENT(HttpStatus.CONFLICT, "보드 상태 불일치 에러가 발생했습니다."),
    INVALID_MERGE(HttpStatus.BAD_REQUEST, "잘못된 합성 시도입니다."),
    INVALID_INJECTOR(HttpStatus.BAD_REQUEST, "잘못된 인젝터 주입 시도입니다."),
    INVALID_REQUEST(HttpStatus.BAD_REQUEST, "잘못된 요청 형식입니다."),
    INTERNAL_SERVER_ERROR(HttpStatus.INTERNAL_SERVER_ERROR, "예상치 못한 서버 에러가 발생했습니다.");

    private final HttpStatus status;
    private final String message;

    ErrorCode(HttpStatus status, String message) {
        this.status = status;
        this.message = message;
    }

    public HttpStatus getStatus() {
        return status;
    }

    public String getMessage() {
        return message;
    }
}
