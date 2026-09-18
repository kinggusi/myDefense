package com.denfense.server.auth;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.Optional;
public interface ExternalIdentityRepository extends JpaRepository<ExternalIdentity, Long> {
    Optional<ExternalIdentity> findByProviderAndIssuerAndSubject(String provider, String issuer, String subject);
    boolean existsByUserId(long userId);
}
