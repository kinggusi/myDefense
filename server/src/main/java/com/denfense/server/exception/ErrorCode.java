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
    USER_NOT_FOUND(HttpStatus.NOT_FOUND, "사용자 정보를 찾을 수 없습니다."),
    SHOP_PRODUCT_NOT_FOUND(HttpStatus.NOT_FOUND, "가챠 상품을 찾을 수 없습니다."),
    SHOP_PRODUCT_INACTIVE(HttpStatus.UNPROCESSABLE_ENTITY, "비활성화된 가챠 상품입니다."),
    GACHA_POOL_NOT_FOUND(HttpStatus.NOT_FOUND, "가챠 풀을 찾을 수 없습니다."),
    GACHA_POOL_INACTIVE(HttpStatus.UNPROCESSABLE_ENTITY, "비활성화된 가챠 풀입니다."),
    UNSUPPORTED_CURRENCY(HttpStatus.UNPROCESSABLE_ENTITY, "지원하지 않는 가챠 구매 재화입니다."),
    INSUFFICIENT_DIAMOND(HttpStatus.CONFLICT, "다이아가 부족합니다."),
    PURCHASE_REQUEST_CONFLICT(HttpStatus.CONFLICT, "동일한 구매 요청 ID를 다른 상품에 사용할 수 없습니다."),
    PURCHASE_ALREADY_PROCESSING(HttpStatus.CONFLICT, "이미 처리 중인 구매 요청입니다."),
    ALIEN_SPEC_NOT_FOUND(HttpStatus.NOT_FOUND, "추첨된 Alien 정보를 찾을 수 없습니다."),
    PURCHASE_RESPONSE_RESTORE_FAILED(HttpStatus.INTERNAL_SERVER_ERROR, "저장된 가챠 구매 응답을 복원하지 못했습니다."),
    USER_ALIEN_NOT_FOUND(HttpStatus.NOT_FOUND, "보유하지 않은 왹져입니다."),
    MAX_ALIEN_LEVEL_REACHED(HttpStatus.CONFLICT, "왹져가 최대 레벨에 도달했습니다."),
    INSUFFICIENT_ALIEN_PIECES(HttpStatus.CONFLICT, "왹져 전용 조각 및 대체 코인이 부족합니다."),
    INSUFFICIENT_ACCOUNT_GOLD(HttpStatus.CONFLICT, "계정 골드가 부족합니다."),
    INSUFFICIENT_GROWTH_CELL(HttpStatus.CONFLICT, "성장 세포가 부족합니다."),
    INVALID_UPGRADE_REQUEST(HttpStatus.BAD_REQUEST, "잘못된 강화 요청입니다."),
    INSUFFICIENT_HEART(HttpStatus.CONFLICT, "하트가 부족합니다."),
    GAME_SESSION_CREATE_FAILED(HttpStatus.INTERNAL_SERVER_ERROR, "게임 세션 생성에 실패했습니다."),
    INVALID_GAME_ENTRY_REQUEST(HttpStatus.BAD_REQUEST, "잘못된 게임 입장 요청입니다."),
    GAME_SESSION_OWNERSHIP_MISMATCH(HttpStatus.FORBIDDEN, "세션 소유권이 일치하지 않습니다."),
    GAME_NOT_FINISHED(HttpStatus.BAD_REQUEST, "아직 게임이 종료되지 않았습니다."),
    INVALID_GAME_FINISH_REQUEST(HttpStatus.BAD_REQUEST, "잘못된 게임 종료 요청입니다."),
    GAME_REWARD_CALCULATION_FAILED(HttpStatus.INTERNAL_SERVER_ERROR, "보상 계산에 실패했습니다."),
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
