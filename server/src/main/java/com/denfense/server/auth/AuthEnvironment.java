package com.denfense.server.auth;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;

@Component
@RequiredArgsConstructor
public class AuthEnvironment {
    private final Environment environment;
    public boolean isDevelopment() {
        var active = java.util.Set.of(environment.getActiveProfiles());
        return (active.contains("local") || active.contains("dev"))
                && !active.contains("prod") && !active.contains("production");
    }
    public boolean allowsLegacy(HttpServletRequest request) {
        String remote = request.getRemoteAddr();
        return isDevelopment() && ("127.0.0.1".equals(remote) || "::1".equals(remote)
                || "0:0:0:0:0:0:0:1".equals(remote));
    }
}
