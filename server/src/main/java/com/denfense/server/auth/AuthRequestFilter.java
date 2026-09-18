package com.denfense.server.auth;

import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.servlet.*;
import jakarta.servlet.http.*;
import lombok.RequiredArgsConstructor;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;
import java.io.IOException;
import java.util.Map;

@Component @Order(Ordered.HIGHEST_PRECEDENCE + 20) @RequiredArgsConstructor
public class AuthRequestFilter extends OncePerRequestFilter {
    private final AuthService auth;
    private final AuthEnvironment environment;
    private final ObjectMapper json;
    @Override protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
            throws ServletException, IOException {
        try {
            String path = request.getRequestURI().substring(request.getContextPath().length());
            if (path.startsWith("/api/auth/")) {
                response.setHeader("Cache-Control", "no-store");
                response.setHeader("Pragma", "no-cache");
            }
            if (path.startsWith("/h2-console") && !environment.allowsLegacy(request)) throw AuthException.forbidden();
            String header = request.getHeader("Authorization");
            if (header != null) {
                if (!header.startsWith("Bearer ") || header.length() > 8192 || header.length() <= 7)
                    throw AuthException.unauthorized();
                request.setAttribute(AccountAccess.PRINCIPAL_ATTRIBUTE, auth.authenticate(header.substring(7)));
            }
            chain.doFilter(request, response);
        } catch (AuthException failure) {
            response.setStatus(failure.status().value());
            response.setContentType("application/json");
            response.setCharacterEncoding("UTF-8");
            response.setHeader("Cache-Control", "no-store");
            json.writeValue(response.getWriter(), Map.of("code", failure.code(), "message", failure.getMessage()));
        }
    }
}
