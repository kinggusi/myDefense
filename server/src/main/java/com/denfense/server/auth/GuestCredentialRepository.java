package com.denfense.server.auth;
import org.springframework.data.jpa.repository.JpaRepository;
public interface GuestCredentialRepository extends JpaRepository<GuestCredential, String> {}
