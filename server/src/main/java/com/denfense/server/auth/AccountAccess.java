package com.denfense.server.auth;

import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

@Component @RequiredArgsConstructor
public class AccountAccess {
    public static final String PRINCIPAL_ATTRIBUTE = AccountAccess.class.getName() + ".principal";
    private final AuthEnvironment environment;
    public AccountPrincipal requirePrincipal() {
        AccountPrincipal principal = currentPrincipal();
        if (principal == null) throw AuthException.unauthorized();
        return principal;
    }
    public AccountPrincipal currentPrincipal() {
        if (!(RequestContextHolder.getRequestAttributes() instanceof ServletRequestAttributes attrs)) return null;
        return (AccountPrincipal) attrs.getRequest().getAttribute(PRINCIPAL_ATTRIBUTE);
    }
    public String username(String requested) {
        AccountPrincipal principal = currentPrincipal();
        if (principal != null) {
            if (!principal.username().equals(requested)) throw AuthException.forbidden();
            return principal.username();
        }
        if (RequestContextHolder.getRequestAttributes() instanceof ServletRequestAttributes attrs
                && environment.allowsLegacy(attrs.getRequest())) return requested;
        throw AuthException.unauthorized();
    }
}
