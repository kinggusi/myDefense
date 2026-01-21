package com.denfense.server.controller;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/users")
@RequiredArgsConstructor
public class UserController {

    private final UserRepository userRepository;

    // [GET] http://localhost:8080/api/users/{username}
    // 예: /api/users/MyDev
    @GetMapping("/{username}")
    public User getUserInfo(@PathVariable String username) {
        return userRepository.findByUsername(username)
                .orElseThrow(() -> new IllegalArgumentException("없는 유저입니다: " + username));
    }
}