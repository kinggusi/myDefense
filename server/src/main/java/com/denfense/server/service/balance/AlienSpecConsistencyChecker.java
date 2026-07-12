package com.denfense.server.service.balance;

import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.repository.AlienSpecRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.stream.Collectors;

@Slf4j
@Component
@RequiredArgsConstructor
@Order(20) // Runs after DataInit (which has @Order(10))
public class AlienSpecConsistencyChecker implements CommandLineRunner {

    private final BalanceRegistry balanceRegistry;
    private final AlienSpecRepository alienSpecRepository;
    private final AlienSpecConsistencyProperties properties;

    @Override
    @Transactional(readOnly = true)
    public void run(String... args) throws Exception {
        AlienSpecConsistencyMode mode = properties.getConsistencyMode();
        if (mode == AlienSpecConsistencyMode.OFF) {
            log.debug("AlienSpecConsistencyChecker is OFF. Skipping consistency check.");
            return;
        }

        log.info("Running AlienSpecConsistencyChecker in {} mode...", mode);

        List<AlienSpecBalance> jsonSpecs = balanceRegistry.getAllAlienSpecs();
        List<AlienSpec> dbSpecs = alienSpecRepository.findAll();

        Map<Long, AlienSpecBalance> jsonMap = jsonSpecs.stream()
                .collect(Collectors.toMap(AlienSpecBalance::alienId, s -> s));
        Map<Long, AlienSpec> dbMap = dbSpecs.stream()
                .collect(Collectors.toMap(AlienSpec::getId, s -> s));

        AlienSpecConsistencyResult result = new AlienSpecConsistencyResult();

        // Check for missing IDs (in JSON but not in DB)
        for (Long jsonId : jsonMap.keySet()) {
            if (!dbMap.containsKey(jsonId)) {
                result.addMissingId(jsonId);
            }
        }

        // Check for unknown IDs (in DB but not in JSON) and field mismatches
        for (AlienSpec dbSpec : dbSpecs) {
            Long dbId = dbSpec.getId();
            AlienSpecBalance jsonSpec = jsonMap.get(dbId);

            if (jsonSpec == null) {
                result.addUnknownId(dbId);
                continue;
            }

            // Field comparisons
            compareFields(result, dbId, jsonSpec, dbSpec);
        }

        if (result.isConsistent()) {
            log.info("AlienSpec consistency check passed.");
        } else {
            String summary = result.buildSummaryMessage();
            if (mode == AlienSpecConsistencyMode.WARN) {
                log.warn(summary);
            } else if (mode == AlienSpecConsistencyMode.FAIL) {
                log.error(summary);
                throw new AlienSpecConsistencyException(summary);
            }
        }
    }

    private void compareFields(AlienSpecConsistencyResult result, long id, AlienSpecBalance json, AlienSpec db) {
        if (!Objects.equals(json.name(), db.getName())) {
            result.addFieldMismatch(id, "name", String.valueOf(json.name()), String.valueOf(db.getName()));
        }
        if (!Objects.equals(json.description(), db.getDescription())) {
            result.addFieldMismatch(id, "description", String.valueOf(json.description()), String.valueOf(db.getDescription()));
        }
        String dbGrade = db.getGrade() != null ? db.getGrade().name() : null;
        if (!Objects.equals(json.grade(), dbGrade)) {
            result.addFieldMismatch(id, "grade", String.valueOf(json.grade()), String.valueOf(dbGrade));
        }
        if (json.baseAttack() != db.getBaseAtk()) {
            result.addFieldMismatch(id, "baseAtk", String.valueOf(json.baseAttack()), String.valueOf(db.getBaseAtk()));
        }
        if (json.baseMp() != db.getBaseMp()) {
            result.addFieldMismatch(id, "baseMp", String.valueOf(json.baseMp()), String.valueOf(db.getBaseMp()));
        }
        if (Double.compare(json.attackSpeed(), db.getAtkSpeed()) != 0) {
            result.addFieldMismatch(id, "atkSpeed", String.valueOf(json.attackSpeed()), String.valueOf(db.getAtkSpeed()));
        }
        if (Double.compare(json.attackRange(), db.getRange()) != 0) {
            result.addFieldMismatch(id, "range", String.valueOf(json.attackRange()), String.valueOf(db.getRange()));
        }
        if (!Objects.equals(json.evolutionTargetId(), db.getEvolutionTargetId())) {
            result.addFieldMismatch(id, "evolutionTargetId", String.valueOf(json.evolutionTargetId()), String.valueOf(db.getEvolutionTargetId()));
        }
        if (json.isLocked() != db.isLocked()) {
            result.addFieldMismatch(id, "isLocked", String.valueOf(json.isLocked()), String.valueOf(db.isLocked()));
        }
    }
}
