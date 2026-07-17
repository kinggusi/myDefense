package com.denfense.server.repository;
import com.denfense.server.domain.*;
import org.springframework.data.jpa.repository.*;
import org.springframework.data.repository.query.Param;
import java.util.*;
public interface MythicBreedingParentRepository extends JpaRepository<MythicBreedingParent,Long> {
    List<MythicBreedingParent> findAllByBreedingSlot(MythicBreedingSlot slot);
    boolean existsByUserAlien(UserAlien alien);
    @Query("select p from MythicBreedingParent p where p.userAlien in :aliens") List<MythicBreedingParent> findByUserAliens(@Param("aliens") Collection<UserAlien> aliens);
    void deleteAllByBreedingSlot(MythicBreedingSlot slot);
}
