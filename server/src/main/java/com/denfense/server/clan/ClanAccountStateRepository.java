package com.denfense.server.clan;

import org.springframework.data.jpa.repository.JpaRepository;
import java.util.List;

public interface ClanAccountStateRepository extends JpaRepository<ClanAccountState,Long> {
    List<ClanAccountState> findByClanIdOrderByUserIdAsc(long clanId);
    List<ClanAccountState> findTop100ByPendingClanIdOrderByUserIdAsc(long clanId);
    long countByClanId(long clanId);
}
