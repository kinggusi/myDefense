package com.denfense.server.service;

import com.denfense.server.auth.AuthService;
import com.denfense.server.domain.*;
import com.denfense.server.dto.breeding.MythicBreedingDtos;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.*;
import com.denfense.server.service.balance.MythicBreedingBalanceRegistry;
import com.denfense.server.service.random.EconomyRandomSource;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import java.time.Instant;
import java.util.ArrayList;
import java.util.Base64;
import java.util.UUID;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest(properties = {
        "spring.datasource.url=jdbc:h2:mem:breeding-security;MODE=MySQL;DB_CLOSE_DELAY=-1",
        "spring.jpa.hibernate.ddl-auto=create-drop"})
@ActiveProfiles("dev")
@AutoConfigureMockMvc
class MythicBreedingSecurityIntegrationTest {
    @Autowired MythicBreedingService service;
    @Autowired MythicBreedingBalanceRegistry balance;
    @Autowired UserRepository users;
    @Autowired UserAlienRepository inventory;
    @Autowired AlienSpecRepository specs;
    @Autowired MythicBreedingSlotRepository slots;
    @Autowired MythicBreedingParentRepository parents;
    @Autowired MythicBreedingRequestRepository receipts;
    @Autowired AuthService auth;
    @Autowired MockMvc mvc;
    @MockBean EconomyRandomSource random;

    @Test void secureRandomInjectionPreservesEachBucketOfCanonicalRecipe() {
        var recipe = balance.getRecipe(29, 30);
        var ids = new ArrayList<>(recipe.standardResultAlienIds());
        ids.add(recipe.exclusive19AlienId());
        ids.add(recipe.exclusive20AlienId());
        var weights = new ArrayList<Integer>();
        recipe.standardResultAlienIds().forEach(id -> weights.add(recipe.standardWeightEach()));
        weights.add(recipe.exclusive19Weight());
        weights.add(recipe.exclusive20Weight());
        int total = weights.stream().mapToInt(Integer::intValue).sum();
        int start = 0;
        for (int bucket = 0; bucket < ids.size(); bucket++) {
            int end = start + weights.get(bucket);
            for (int roll : new int[]{start, end - 1}) {
                when(random.nextInt(total)).thenReturn(roll);
                User user = users.saveAndFlush(new User("secure-breed-" + UUID.randomUUID(), null));
                var first = owned(user, 29);
                var second = owned(user, 30);
                service.start(user.getUsername(), 1,
                        new MythicBreedingDtos.StartRequest(first.getId(), second.getId(), "start"));
                assertThat(slots.findByUserAndSlotNo(user, 1).orElseThrow().getResultAlienId())
                        .as("roll %s must stay in canonical bucket %s", roll, bucket).isEqualTo(ids.get(bucket));
            }
            start = end;
        }
        verify(random, times(ids.size() * 2)).nextInt(total);
    }

    @ParameterizedTest
    @ValueSource(ints = {2147483618, Integer.MAX_VALUE})
    void overflowRejectsAtomicallyAndSameJwtRequestCanClaimAfterMakingRoom(int existingPieces) throws Exception {
        byte[] secret = new byte[32];
        new java.security.SecureRandom().nextBytes(secret);
        var session = auth.guest(Base64.getUrlEncoder().withoutPadding().encodeToString(secret));
        User user = users.findById(session.user().userId()).orElseThrow();
        int diamondsBefore = user.getDiamond();
        when(random.nextInt(anyInt())).thenReturn(0);
        var first = owned(user, 29);
        var second = owned(user, 30);
        service.start(user.getUsername(), 1,
                new MythicBreedingDtos.StartRequest(first.getId(), second.getId(), "start"));
        var slot = slots.findByUserAndSlotNo(user, 1).orElseThrow();
        long resultId = slot.getResultAlienId();
        var result = owned(user, resultId);
        result.setPieces(existingPieces);
        inventory.saveAndFlush(result);
        // Expired BREEDING (not pre-marked ready) also proves the transient markReady is rolled back.
        Instant readyAt = Instant.now().minusSeconds(10);
        slot.start(resultId, readyAt.minusSeconds(balance.getConfig().durationSeconds()), readyAt, "start");
        slots.saveAndFlush(slot);
        String body = "{\"requestId\":\"claim-cap\"}";
        String bearer = "Bearer " + session.accessToken();

        mvc.perform(post("/api/mythic-breeding/slots/1/claim").param("username", user.getUsername())
                        .header("Authorization", bearer).contentType("application/json").content(body))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.code").value("MYTHIC_BREEDING_BALANCE_CAP"))
                .andExpect(jsonPath("$.message").value(ErrorCode.MYTHIC_BREEDING_BALANCE_CAP.getMessage()));
        assertThat(inventory.findById(result.getId()).orElseThrow().getPieces()).isEqualTo(existingPieces);
        var unchanged = slots.findByUserAndSlotNo(user, 1).orElseThrow();
        assertThat(unchanged.getStatus()).isEqualTo(MythicBreedingSlotStatus.BREEDING);
        assertThat(unchanged.getResultAlienId()).isEqualTo(resultId);
        assertThat(unchanged.getClaimRequestId()).isNull();
        assertThat(parents.findAllByBreedingSlot(unchanged)).hasSize(2);
        assertThat(receipts.findByUserAndRequestId(user, "claim-cap")).isEmpty();
        assertThat(users.findById(user.getId()).orElseThrow().getDiamond()).isEqualTo(diamondsBefore);

        result.setPieces(Integer.MAX_VALUE - balance.getConfig().duplicateRewardPieces());
        inventory.saveAndFlush(result);
        for (int attempt = 0; attempt < 2; attempt++) {
            mvc.perform(post("/api/mythic-breeding/slots/1/claim").param("username", user.getUsername())
                            .header("Authorization", bearer).contentType("application/json").content(body))
                    .andExpect(status().isOk()).andExpect(jsonPath("$.resultAlienId").value(resultId));
            assertThat(inventory.findById(result.getId()).orElseThrow().getPieces()).isEqualTo(Integer.MAX_VALUE);
        }
        assertThat(slots.findByUserAndSlotNo(user, 1).orElseThrow().getStatus()).isEqualTo(MythicBreedingSlotStatus.AVAILABLE);
        assertThat(parents.findAllByBreedingSlot(slot)).isEmpty();
        assertThat(receipts.findByUserAndRequestId(user, "claim-cap")).isPresent();
    }

    private UserAlien owned(User user, long alienId) {
        var spec = specs.findById(alienId).orElseThrow();
        return inventory.findByUserAndAlienSpec(user, spec)
                .orElseGet(() -> inventory.saveAndFlush(new UserAlien(user, spec)));
    }
}
