package com.denfense.server.auth;

import org.springframework.data.jpa.repository.JpaRepository;

public interface LocalDeveloperCredentialRepository extends JpaRepository<LocalDeveloperCredential, String> {
    boolean existsByUserId(long userId);
}
