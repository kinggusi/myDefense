package com.denfense.server.repository;

import com.denfense.server.domain.AlienSpec;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.Optional;

public interface AlienSpecRepository extends JpaRepository<AlienSpec, Long> {

    List<AlienSpec> findAllByGrade(AlienSpec.Grade grade);

    // [핵심] 특정 등급 중 랜덤으로 1개 뽑기 (MySQL 기준 RAND())
    // 성능 최적화를 위해 LIMIT 1 사용
    @Query(value = "SELECT * FROM alien_specs WHERE grade = :grade ORDER BY RAND() LIMIT 1", nativeQuery = true)
    Optional<AlienSpec> findRandomByGrade(@Param("grade") String gradeName);
}
