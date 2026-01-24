package com.denfense.server.repository;

import com.denfense.server.domain.MonsterSpec;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface MonsterSpecRepository extends JpaRepository<MonsterSpec, Long> {

    Optional<MonsterSpec> findTopByType(MonsterSpec.MonsterType type);
}
