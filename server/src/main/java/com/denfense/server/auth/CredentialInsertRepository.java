package com.denfense.server.auth;

import jakarta.persistence.EntityManager;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Repository;

/** Assigned credential IDs must never use JPA merge: an existing identity is immutable. */
@Repository @RequiredArgsConstructor
public class CredentialInsertRepository {
    private final EntityManager entityManager;

    public void insertGuest(GuestCredential credential) {
        entityManager.persist(credential);
        entityManager.flush();
    }

    public void insertLocal(LocalDeveloperCredential credential) {
        entityManager.persist(credential);
        entityManager.flush();
    }
}
