package com.denfense.server.service.balance;

import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.repository.AlienSpecRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Collections;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
public class AlienSpecConsistencyCheckerTest {

    @Mock
    private BalanceRegistry balanceRegistry;

    @Mock
    private AlienSpecRepository alienSpecRepository;

    @Mock
    private AlienSpecConsistencyProperties properties;

    @InjectMocks
    private AlienSpecConsistencyChecker checker;

    @BeforeEach
    void setUp() {
        // Default lenient properties
        lenient().when(properties.getConsistencyMode()).thenReturn(AlienSpecConsistencyMode.WARN);
    }

    @Test
    void whenModeIsOff_thenDoesNotAccessRepository() throws Exception {
        when(properties.getConsistencyMode()).thenReturn(AlienSpecConsistencyMode.OFF);

        checker.run();

        verifyNoInteractions(balanceRegistry, alienSpecRepository);
    }

    @Test
    void whenModeIsWarn_andPerfectMatch_thenNoException() throws Exception {
        when(properties.getConsistencyMode()).thenReturn(AlienSpecConsistencyMode.WARN);

        AlienSpecBalance json = new AlienSpecBalance(1L, "A", "Desc", "NORMAL", 10, 5, 1.0, 2.0, null, false);
        AlienSpec db = new AlienSpec();
        db.setId(1L);
        db.setName("A");
        db.setDescription("Desc");
        db.setGrade(AlienSpec.Grade.NORMAL);
        db.setBaseAtk(10);
        db.setBaseMp(5);
        db.setAtkSpeed(1.0);
        db.setRange(2.0);
        db.setEvolutionTargetId(null);
        db.setLocked(false);

        when(balanceRegistry.getAllAlienSpecs()).thenReturn(List.of(json));
        when(alienSpecRepository.findAll()).thenReturn(List.of(db));

        // Should not throw anything
        checker.run();
    }

    @Test
    void whenModeIsFail_andPerfectMatch_thenNoException() throws Exception {
        when(properties.getConsistencyMode()).thenReturn(AlienSpecConsistencyMode.FAIL);

        when(balanceRegistry.getAllAlienSpecs()).thenReturn(Collections.emptyList());
        when(alienSpecRepository.findAll()).thenReturn(Collections.emptyList());

        checker.run();
    }

    @Test
    void whenJsonIdMissingInDb_thenCollectsMissingId() throws Exception {
        when(properties.getConsistencyMode()).thenReturn(AlienSpecConsistencyMode.FAIL);

        AlienSpecBalance json = new AlienSpecBalance(99L, "B", "Desc", "NORMAL", 10, 5, 1.0, 2.0, null, false);
        when(balanceRegistry.getAllAlienSpecs()).thenReturn(List.of(json));
        when(alienSpecRepository.findAll()).thenReturn(Collections.emptyList());

        AlienSpecConsistencyException ex = assertThrows(AlienSpecConsistencyException.class, () -> checker.run());
        assertThat(ex.getMessage()).contains("Missing IDs (in JSON but not in DB): [99]");
    }

    @Test
    void whenDbHasUnknownId_thenCollectsUnknownId() throws Exception {
        when(properties.getConsistencyMode()).thenReturn(AlienSpecConsistencyMode.FAIL);

        AlienSpec db = new AlienSpec();
        db.setId(100L);

        when(balanceRegistry.getAllAlienSpecs()).thenReturn(Collections.emptyList());
        when(alienSpecRepository.findAll()).thenReturn(List.of(db));

        AlienSpecConsistencyException ex = assertThrows(AlienSpecConsistencyException.class, () -> checker.run());
        assertThat(ex.getMessage()).contains("Unknown IDs (in DB but not in JSON): [100]");
    }

    @Test
    void whenFieldMismatches_thenCollectsThemAndThrowsFail() throws Exception {
        when(properties.getConsistencyMode()).thenReturn(AlienSpecConsistencyMode.FAIL);

        AlienSpecBalance json = new AlienSpecBalance(1L, "Name1", "Desc1", "EPIC", 100, 50, 1.5, 2.5, 2L, true);
        AlienSpec db = new AlienSpec();
        db.setId(1L);
        db.setName("Name2");
        db.setDescription("Desc2");
        db.setGrade(AlienSpec.Grade.NORMAL);
        db.setBaseAtk(90);
        db.setBaseMp(40);
        db.setAtkSpeed(1.0);
        db.setRange(2.0);
        db.setEvolutionTargetId(3L);
        db.setLocked(false);

        when(balanceRegistry.getAllAlienSpecs()).thenReturn(List.of(json));
        when(alienSpecRepository.findAll()).thenReturn(List.of(db));

        AlienSpecConsistencyException ex = assertThrows(AlienSpecConsistencyException.class, () -> checker.run());

        String msg = ex.getMessage();
        assertThat(msg).contains("alienId=1, field=name, json=Name1, db=Name2");
        assertThat(msg).contains("alienId=1, field=description, json=Desc1, db=Desc2");
        assertThat(msg).contains("alienId=1, field=grade, json=EPIC, db=NORMAL");
        assertThat(msg).contains("alienId=1, field=baseAtk, json=100, db=90");
        assertThat(msg).contains("alienId=1, field=baseMp, json=50, db=40");
        assertThat(msg).contains("alienId=1, field=atkSpeed, json=1.5, db=1.0");
        assertThat(msg).contains("alienId=1, field=range, json=2.5, db=2.0");
        assertThat(msg).contains("alienId=1, field=evolutionTargetId, json=2, db=3");
        assertThat(msg).contains("alienId=1, field=isLocked, json=true, db=false");
    }

    @Test
    void whenMultipleMismatches_thenSortedByAlienIdAndField() throws Exception {
        when(properties.getConsistencyMode()).thenReturn(AlienSpecConsistencyMode.FAIL);

        AlienSpecBalance json1 = new AlienSpecBalance(2L, "A", "D", "NORMAL", 10, 10, 1.0, 1.0, null, false);
        AlienSpecBalance json2 = new AlienSpecBalance(1L, "B", "D", "NORMAL", 10, 10, 1.0, 1.0, null, false);

        AlienSpec db1 = new AlienSpec(); db1.setId(2L); db1.setName("AX"); db1.setGrade(AlienSpec.Grade.NORMAL);
        AlienSpec db2 = new AlienSpec(); db2.setId(1L); db2.setName("BX"); db2.setGrade(AlienSpec.Grade.NORMAL);

        when(balanceRegistry.getAllAlienSpecs()).thenReturn(List.of(json1, json2));
        when(alienSpecRepository.findAll()).thenReturn(List.of(db1, db2));

        AlienSpecConsistencyException ex = assertThrows(AlienSpecConsistencyException.class, () -> checker.run());

        String msg = ex.getMessage();
        int idx1 = msg.indexOf("alienId=1");
        int idx2 = msg.indexOf("alienId=2");
        assertThat(idx1).isLessThan(idx2);
    }
}
