package com.denfense.server.repository;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface UserAlienRepository extends JpaRepository<UserAlien, Long> {

    // 특정 유저의 모든 왹져 목록 가져오기
    List<UserAlien> findAllByUser(User user);

    // SELECT * FROM user_aliens WHERE user_id = ? AND alien_id = ?
    Optional<UserAlien> findByUserAndAlienSpec(User user, AlienSpec alienSpec);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("SELECT ua FROM UserAlien ua WHERE ua.user = :user AND ua.alienSpec = :alienSpec")
    Optional<UserAlien> findByUserAndAlienSpecForUpdate(@Param("user") User user, @Param("alienSpec") AlienSpec alienSpec);
}
