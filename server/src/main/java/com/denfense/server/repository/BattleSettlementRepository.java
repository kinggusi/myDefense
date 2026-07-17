package com.denfense.server.repository; import com.denfense.server.domain.*; import org.springframework.data.jpa.repository.JpaRepository; import java.util.*;
public interface BattleSettlementRepository extends JpaRepository<BattleSettlement,Long>{Optional<BattleSettlement> findByBattleSessionId(String id); Optional<BattleSettlement> findByRequestId(String id);}
