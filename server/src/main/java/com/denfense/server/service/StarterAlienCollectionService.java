package com.denfense.server.service;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.HashSet;
import java.util.List;
import java.util.Set;

@Service
@RequiredArgsConstructor
public class StarterAlienCollectionService {

    private final UserRepository userRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final UserAlienRepository userAlienRepository;

    @Transactional
    public User ensureStarterCollection(String username) {
        User user = userRepository.findByUsernameForUpdate(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND, "유저를 찾을 수 없습니다."));

        Set<Long> ownedAlienIds = new HashSet<>();
        for (UserAlien userAlien : userAlienRepository.findAllByUser(user)) {
            ownedAlienIds.add(userAlien.getAlienSpec().getId());
        }

        List<UserAlien> missingStarters = alienSpecRepository.findAll().stream()
                .filter(spec -> spec.getGrade() != AlienSpec.Grade.MYTHIC)
                .filter(spec -> !ownedAlienIds.contains(spec.getId()))
                .map(spec -> new UserAlien(user, spec))
                .toList();
        if (!missingStarters.isEmpty()) {
            userAlienRepository.saveAll(missingStarters);
            userAlienRepository.flush();
        }
        return user;
    }
}
