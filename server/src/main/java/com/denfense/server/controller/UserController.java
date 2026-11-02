package com.denfense.server.controller;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@Tag(name = "User" , description = "유저 정보")
@RestController
@RequestMapping("/api/users")
@RequiredArgsConstructor
public class UserController {

    private final UserRepository userRepository;
    private final com.denfense.server.auth.AccountAccess accounts;

    // [GET] http://localhost:8080/api/users/{username}
    // 예: /api/users/MyDev
    @Operation(summary = "유저 정보 찾기.", description = "유저 정보 찾기.")
    @GetMapping("/{username}")
    public PublicUser getUserInfo(@PathVariable String username) {
        accounts.username(username);
        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new IllegalArgumentException("없는 유저입니다: " + username));
        return new PublicUser(user.getId(), user.getUsername());
    }
    public record PublicUser(long id, String username) {}
}
