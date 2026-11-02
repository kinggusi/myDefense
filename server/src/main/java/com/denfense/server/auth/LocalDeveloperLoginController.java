package com.denfense.server.auth;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Profile;
import org.springframework.web.bind.annotation.*;

@RestController @RequiredArgsConstructor
@Profile("(local | dev) & !prod & !production")
@ConditionalOnProperty(name = "mydefense.auth.local-accounts.enabled", havingValue = "true")
public class LocalDeveloperLoginController {
    private final LocalDeveloperAccountService service;
    private final AuthEnvironment environment;
    @PostMapping("/api/dev/auth/login")
    public AuthDtos.SessionResponse login(HttpServletRequest request, HttpServletResponse response,
                                          @Valid @RequestBody LoginRequest body) {
        if (!environment.allowsLegacy(request)) throw AuthException.forbidden();
        response.setHeader("Cache-Control", "no-store");
        response.setHeader("Pragma", "no-cache");
        return service.login(body.username(), body.password());
    }
    public record LoginRequest(@NotBlank @Pattern(regexp = "[A-Za-z0-9_-]{3,64}") String username,
                               @NotBlank @Size(max = 72) String password) {
        @Override public String toString() { return "LocalDeveloperLoginRequest[REDACTED]"; }
    }
}
