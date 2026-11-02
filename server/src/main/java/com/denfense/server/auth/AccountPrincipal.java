package com.denfense.server.auth;

import java.security.Principal;

public record AccountPrincipal(long userId, String username, String sessionFamilyId) implements Principal {
    @Override public String getName() { return username; }
}
