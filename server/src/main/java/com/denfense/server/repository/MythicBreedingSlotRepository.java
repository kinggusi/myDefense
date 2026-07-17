package com.denfense.server.repository;
import com.denfense.server.domain.*;
import org.springframework.data.jpa.repository.*;
import org.springframework.data.repository.query.Param;
import jakarta.persistence.LockModeType;
import java.util.*;
public interface MythicBreedingSlotRepository extends JpaRepository<MythicBreedingSlot,Long> {
    List<MythicBreedingSlot> findAllByUserOrderBySlotNo(User user);
    Optional<MythicBreedingSlot> findByUserAndSlotNo(User user,int slotNo);
    @Lock(LockModeType.PESSIMISTIC_WRITE) @Query("select s from MythicBreedingSlot s where s.user=:user and s.slotNo=:slotNo") Optional<MythicBreedingSlot> findForUpdate(@Param("user") User user,@Param("slotNo") int slotNo);
}
