package com.denfense.server.auth;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.util.UUID;

@Service @RequiredArgsConstructor
public class GuestAccountWriter {
    private final GuestCredentialRepository guests;
    private final UserRepository users;
    private final CredentialInsertRepository credentialInserts;
    @Transactional
    public long findOrCreate(String secretHash) {
        var existing = guests.findById(secretHash);
        if (existing.isPresent()) return existing.get().getUser().getId();
        User user = users.save(new User("guest-" + UUID.randomUUID().toString().replace("-", ""), null));
        user.setHeart(com.denfense.server.service.HeartPolicy.MAX_HEART);
        user.setLastHeartUpdateTime(java.time.LocalDateTime.now());
        credentialInserts.insertGuest(new GuestCredential(secretHash, user));
        return user.getId();
    }
}
