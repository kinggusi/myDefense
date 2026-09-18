package com.denfense.server.auth;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.HeartPolicy;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Profile;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.LocalDateTime;
import java.util.List;

@Service @RequiredArgsConstructor
@Profile("(local | dev) & !prod & !production")
@ConditionalOnProperty(name = "mydefense.auth.local-accounts.enabled", havingValue = "true")
public class LocalDeveloperAccountService {
    private final LocalDeveloperCredentialRepository credentials;
    private final UserRepository users;
    private final AuthService auth;
    private final CredentialInsertRepository credentialInserts;
    private final BCryptPasswordEncoder passwords = new BCryptPasswordEncoder(12);
    private final String missingAccountHash = passwords.encode(java.util.UUID.randomUUID().toString());

    @Transactional
    public void provision(List<String> usernames, String password) {
        if (password == null || password.length() < 8 || password.getBytes(java.nio.charset.StandardCharsets.UTF_8).length > 72)
            throw new IllegalArgumentException("Local account password must contain 8+ characters and at most 72 UTF-8 bytes.");
        if (usernames.isEmpty() || usernames.stream().anyMatch(n -> n == null || !n.matches("[A-Za-z0-9_-]{3,64}"))
                || usernames.stream().distinct().count() != usernames.size())
            throw new IllegalArgumentException("Local account names must be unique and use 3-64 safe characters.");
        for (String name : usernames) {
            if (credentials.existsById(name)) continue; // Never reset credentials or progress at startup.
            if (users.findByUsername(name).isPresent())
                throw new IllegalStateException("Existing account cannot be adopted by local bootstrap: " + name);
            User user = new User(name, null);
            user.setHeart(HeartPolicy.MAX_HEART);
            user.setLastHeartUpdateTime(LocalDateTime.now());
            users.saveAndFlush(user);
            credentialInserts.insertLocal(new LocalDeveloperCredential(name, user, passwords.encode(password)));
        }
    }

    @Transactional
    public AuthDtos.SessionResponse login(String username, String password) {
        if (username == null || password == null
                || password.getBytes(java.nio.charset.StandardCharsets.UTF_8).length > 72)
            throw AuthException.unauthorized();
        var credential = credentials.findById(username);
        String hash = credential.map(LocalDeveloperCredential::getPasswordHash).orElse(missingAccountHash);
        if (!passwords.matches(password, hash) || credential.isEmpty()) throw AuthException.unauthorized();
        return auth.createSession(credential.get().getUser().getId());
    }
}
