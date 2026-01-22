package com.denfense.server.repository;

import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface UserAlienRepository extends JpaRepository<UserAlien, Long> {

    // 특정 유저의 모든 왹져 목록 가져오기
    List<UserAlien> findAllByUser(User user);

    // SELECT * FROM user_aliens WHERE user_id = ? AND alien_id = ?
    Optional<UserAlien> findByUserAndAlienId(User user, int alienId);
}
