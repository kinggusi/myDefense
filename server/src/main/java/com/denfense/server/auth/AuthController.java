package com.denfense.server.auth;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController @RequestMapping("/api/auth") @RequiredArgsConstructor
public class AuthController {
    private final AuthService service;
    private final ExternalAccountService external;
    private final AccountAccess accounts;
    @PostMapping("/guest") public AuthDtos.SessionResponse guest(@Valid @RequestBody AuthDtos.GuestRequest request) {
        return service.guest(request.guestSecret());
    }
    @PostMapping("/refresh") public AuthDtos.SessionResponse refresh(@Valid @RequestBody AuthDtos.RefreshRequest request) {
        return service.refresh(request.refreshToken());
    }
    @PostMapping("/logout") public ResponseEntity<Void> logout(@Valid @RequestBody AuthDtos.RefreshRequest request) {
        service.logout(request.refreshToken()); return ResponseEntity.noContent().build();
    }
    @GetMapping("/me") public AuthDtos.UserInfo me() { return service.userInfo(accounts.requirePrincipal().userId()); }
    @GetMapping("/providers") public AuthDtos.ProvidersResponse providers() { return external.providers(); }
    @PostMapping("/challenges") public AuthDtos.ChallengeResponse challenge(@Valid @RequestBody AuthDtos.ChallengeRequest request) {
        Long userId = "LINK".equals(request.purpose()) ? accounts.requirePrincipal().userId() : null;
        return external.challenge(request.provider(), request.purpose(), userId);
    }
    @PostMapping("/external") public AuthDtos.SessionResponse login(@Valid @RequestBody AuthDtos.ExternalRequest request) {
        return external.login(request);
    }
    @PostMapping("/links") public AuthDtos.UserInfo link(@Valid @RequestBody AuthDtos.ExternalRequest request) {
        try { return external.link(accounts.requirePrincipal().userId(), request); }
        catch (org.springframework.dao.DataIntegrityViolationException concurrentLink) {
            throw AuthException.linkConflict();
        }
    }
}
