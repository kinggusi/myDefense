package com.denfense.server.service;

import com.denfense.server.domain.*;
import com.denfense.server.dto.breeding.MythicBreedingDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.*;
import org.junit.jupiter.api.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.transaction.annotation.Transactional;

import java.lang.reflect.Field;
import java.time.Instant;
import java.util.UUID;
import java.util.concurrent.*;

import static org.assertj.core.api.Assertions.*;

@SpringBootTest
class MythicBreedingIntegrationTest {
    @Autowired MythicBreedingService service;
    @Autowired UserRepository users;
    @Autowired UserAlienRepository aliens;
    @Autowired AlienSpecRepository specs;
    @Autowired MythicBreedingSlotRepository slots;
    @Autowired MythicBreedingParentRepository parents;
    @Autowired BattlePlayerSettlementRepository battlePlayers;
    @Autowired BattleSettlementRepository battles;
    @Autowired BattleRewardClaimRepository rewardClaims;

    @BeforeEach void clean() { rewardClaims.deleteAllInBatch(); battlePlayers.deleteAllInBatch(); battles.deleteAllInBatch(); parents.deleteAllInBatch(); slots.deleteAllInBatch(); aliens.deleteAllInBatch(); users.deleteAllInBatch(); }

    @Test void initializesUnlocksAndUnlockRetryDoesNotChargeTwice() {
        User u=user("breed-unlock",1000); MythicBreedingDtos.SlotsResponse initial=service.slots(u.getUsername());
        assertThat(initial.slots()).hasSize(3); assertThat(initial.slots().get(0).status()).isEqualTo("AVAILABLE");
        int before=users.findById(u.getId()).orElseThrow().getDiamond();
        service.unlock(u.getUsername(),2,"unlock-1"); service.unlock(u.getUsername(),2,"unlock-2");
        assertThat(users.findById(u.getId()).orElseThrow().getDiamond()).isEqualTo(before-500);
    }

    @Test void startHidesResultAndRejectsEarlyClaimThenClaimRetryIsIdempotent() throws Exception {
        User u=user("breed-start",0); UserAlien a=owned(u,29), b=owned(u,30);
        MythicBreedingDtos.StartResponse started=service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"start-1"));
        assertThat(started.readyAt()).isAfter(Instant.now()); assertThat(slots.findByUserAndSlotNo(u,1).orElseThrow().getResultAlienId()).isNotNull();
        assertThatThrownBy(() -> service.claim(u.getUsername(),1,new MythicBreedingDtos.ClaimRequest("claim-1")))
                .isInstanceOfSatisfying(BusinessException.class,e->assertThat(e.getErrorCode()).isEqualTo(ErrorCode.MYTHIC_BREEDING_NOT_READY));
        MythicBreedingSlot slot=slots.findByUserAndSlotNo(u,1).orElseThrow(); forceReady(slot);
        MythicBreedingDtos.ClaimResponse first=service.claim(u.getUsername(),1,new MythicBreedingDtos.ClaimRequest("claim-1"));
        MythicBreedingDtos.ClaimResponse retry=service.claim(u.getUsername(),1,new MythicBreedingDtos.ClaimRequest("claim-1"));
        assertThat(retry.slotNo()).isEqualTo(first.slotNo()); assertThat(retry.resultAlienId()).isEqualTo(first.resultAlienId()); assertThat(parents.findAll()).isEmpty(); assertThat(slots.findByUserAndSlotNo(u,1).orElseThrow().getStatus()).isEqualTo(MythicBreedingSlotStatus.AVAILABLE);
    }

    @Test void sameParentCannotStartTwoSlotsAndSameSlotConcurrentStartHasOneSuccess() throws Exception {
        User u=user("breed-concurrent",0); UserAlien a=owned(u,29), b=owned(u,30), cParent=owned(u,31);
        ExecutorService pool=Executors.newFixedThreadPool(2); CountDownLatch gate=new CountDownLatch(1);
        Callable<Object> c=()->{gate.await(); try{return service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),UUID.randomUUID().toString()));}catch(Exception e){return e;}};
        Future<Object> f1=pool.submit(c),f2=pool.submit(c); gate.countDown(); Object r1=f1.get(),r2=f2.get(); pool.shutdown();
        assertThat(java.util.stream.Stream.of(r1,r2).filter(x->x instanceof MythicBreedingDtos.StartResponse).count()).isEqualTo(1);
        assertThatThrownBy(()->service.start(u.getUsername(),2,new MythicBreedingDtos.StartRequest(a.getId(),cParent.getId(),"other-slot")))
                .isInstanceOf(BusinessException.class);
    }

    @Test void duplicateResultAddsFiftyPieces() throws Exception {
        User u=user("breed-duplicate",0); UserAlien a=owned(u,29), b=owned(u,30);
        service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"s")); MythicBreedingSlot breeding=slots.findByUserAndSlotNo(u,1).orElseThrow();
        AlienSpec resultSpec = mythicSpec(breeding.getResultAlienId());
        UserAlien result=aliens.findByUserAndAlienSpec(u,resultSpec).orElseGet(()->owned(u,breeding.getResultAlienId())); result.setPieces(2); aliens.save(result); forceReady(breeding);
        service.claim(u.getUsername(),1,new MythicBreedingDtos.ClaimRequest("c"));
        assertThat(aliens.findByUserAndAlienSpec(u,result.getAlienSpec()).orElseThrow().getPieces()).isEqualTo(52);
    }

    private User user(String name,int diamond){User u=new User(name,"pw");u.setDiamond(diamond);return users.save(u);}
    private UserAlien owned(User u,long id){return aliens.save(new UserAlien(u,mythicSpec(id)));}
    private AlienSpec mythicSpec(long id){return specs.findById(id).orElseGet(()->{AlienSpec s=new AlienSpec();s.setId(id);s.setName("Mythic-"+id);s.setGrade(AlienSpec.Grade.MYTHIC);return specs.save(s);});}
    private void forceReady(MythicBreedingSlot s)throws Exception{Field ready=MythicBreedingSlot.class.getDeclaredField("readyAt");ready.setAccessible(true);ready.set(s,Instant.now().minusSeconds(1));Field status=MythicBreedingSlot.class.getDeclaredField("status");status.setAccessible(true);status.set(s,MythicBreedingSlotStatus.REWARD_READY);slots.saveAndFlush(s);}
}
