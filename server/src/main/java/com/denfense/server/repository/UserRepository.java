package com.denfense.server.repository;

import com.denfense.server.domain.User;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface UserRepository extends JpaRepository<User, Long> {

    // "SELECT * FROM game_users WHERE username = ?" 쿼리를 대신해줌
    Optional<User> findByUsername(String username);
}