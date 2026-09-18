package com.denfense.server.auth;

import org.springframework.http.HttpStatus;

public final class AuthException extends RuntimeException {
    private final HttpStatus status;
    private final String code;
    public AuthException(HttpStatus status, String code, String message) {
        super(message);
        this.status = status;
        this.code = code;
    }
    public HttpStatus status() { return status; }
    public String code() { return code; }
    public static AuthException unauthorized() {
        return new AuthException(HttpStatus.UNAUTHORIZED, "AUTH_REQUIRED", "로그인이 필요하거나 인증이 만료되었습니다.");
    }
    public static AuthException forbidden() {
        return new AuthException(HttpStatus.FORBIDDEN, "ACCOUNT_ACCESS_DENIED", "다른 계정에 접근할 수 없습니다.");
    }
    public static AuthException unavailable() {
        return new AuthException(HttpStatus.SERVICE_UNAVAILABLE, "AUTH_NOT_CONFIGURED", "인증 서버 설정이 준비되지 않았습니다.");
    }
    public static AuthException providerUnavailable() {
        return new AuthException(HttpStatus.SERVICE_UNAVAILABLE, "AUTH_PROVIDER_UNAVAILABLE", "이 로그인 제공자는 아직 연결되지 않았습니다.");
    }
    public static AuthException linkConflict() {
        return new AuthException(HttpStatus.CONFLICT, "ACCOUNT_LINK_CONFLICT", "이미 다른 게임 계정에 연결된 로그인 계정입니다.");
    }
}
