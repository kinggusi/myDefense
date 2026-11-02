package com.denfense.server.auth;

import jakarta.servlet.http.*;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.method.HandlerMethod;
import org.springframework.web.servlet.HandlerInterceptor;
import org.springframework.web.servlet.config.annotation.*;
import java.util.Set;

@Configuration @RequiredArgsConstructor
public class AuthWebConfiguration implements WebMvcConfigurer {
    private final AuthEnvironment environment;
    private static final Set<String> PUBLIC_AUTH = Set.of("/api/auth/guest", "/api/auth/refresh", "/api/auth/logout",
            "/api/auth/providers", "/api/auth/challenges", "/api/auth/external");
    @Override public void addInterceptors(InterceptorRegistry registry) {
        registry.addInterceptor(new HandlerInterceptor() {
            @Override public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) {
                if (!(handler instanceof HandlerMethod)) return true;
                String path = (String) request.getAttribute(org.springframework.web.servlet.HandlerMapping.BEST_MATCHING_PATTERN_ATTRIBUTE);
                if (path == null) throw AuthException.unauthorized();
                if (!path.startsWith("/api/")) return true;
                if (PUBLIC_AUTH.contains(path)) {
                    response.setHeader("Cache-Control", "no-store");
                    return true;
                }
                boolean legacy = environment.allowsLegacy(request);
                if ((path.startsWith("/api/dev/") || path.startsWith("/api/game/")
                        || path.equals("/api/battle/settlements"))
                        && request.getAttribute(AccountAccess.PRINCIPAL_ATTRIBUTE) != null)
                    throw AuthException.forbidden();
                // Development controllers retain their existing loopback-specific error contracts.
                if (path.startsWith("/api/dev/") && environment.isDevelopment()) return true;
                if (path.startsWith("/api/dev/") || path.startsWith("/api/game/")
                        || path.equals("/api/battle/settlements") || path.equals("/api/shop/gacha")) {
                    if (!legacy) throw AuthException.forbidden();
                }
                if (request.getAttribute(AccountAccess.PRINCIPAL_ATTRIBUTE) == null && !legacy)
                    throw AuthException.unauthorized();
                response.setHeader("Cache-Control", "no-store");
                return true;
            }
        });
    }
}
