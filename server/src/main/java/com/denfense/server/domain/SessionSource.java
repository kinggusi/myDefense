package com.denfense.server.domain;

/**
 * Server-owned origin of a trusted battle roster. Clients must never choose
 * this value through Settlement or roster request payloads.
 */
public enum SessionSource {
    PRODUCTION,
    LOCAL_DEVELOPMENT,
    VALIDATION_FIXTURE
}
