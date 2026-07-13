package com.denfense.server.service.balance;

import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.repository.AlienSpecRepository;
import jakarta.persistence.EntityManager;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Comparator;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

@Service
@RequiredArgsConstructor
public class AlienSpecSeedService {

    private final AlienSpecConsistencyProperties properties;
    private final AlienSpecRepository alienSpecRepository;
    private final BalanceRegistry balanceRegistry;
    private final EntityManager entityManager;

    @Transactional
    public AlienSpecSeedResult seed() {
        if (!properties.isSeedEnabled()) {
            return AlienSpecSeedResult.disabled();
        }

        Set<Long> existingIds = new HashSet<>(alienSpecRepository.findAllIds());

        List<AlienSpecBalance> balances = balanceRegistry.getAllAlienSpecs().stream()
                .sorted(Comparator.comparingLong(AlienSpecBalance::alienId))
                .toList();

        int inserted = 0;
        int skipped = 0;

        for (AlienSpecBalance balance : balances) {
            long id = balance.alienId();

            if (existingIds.contains(id)) {
                skipped++;
                continue;
            }

            AlienSpec entity = mapToEntity(balance);
            entityManager.persist(entity);
            inserted++;
        }

        if (inserted > 0) {
            entityManager.flush();
        }

        return new AlienSpecSeedResult(true, inserted, skipped);
    }

    private AlienSpec mapToEntity(AlienSpecBalance balance) {
        AlienSpec entity = new AlienSpec();
        entity.setId(balance.alienId());
        entity.setName(balance.name());
        entity.setDescription(balance.description());
        entity.setGrade(AlienSpec.Grade.valueOf(balance.grade()));
        entity.setBaseAtk(balance.baseAttack());
        entity.setBaseMp(balance.baseMp());
        entity.setAtkSpeed(balance.attackSpeed());
        entity.setRange(balance.attackRange());
        entity.setEvolutionTargetId(balance.evolutionTargetId());
        entity.setLocked(balance.isLocked());
        return entity;
    }
}
