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
    @Autowired MythicBreedingAccelerationRepository accelerations;
    @Autowired MythicBreedingRequestRepository requests;
    @Autowired BattlePlayerSettlementRepository battlePlayers;
    @Autowired BattleSettlementRepository battles;
    @Autowired BattleRewardClaimRepository rewardClaims;

    @BeforeEach void clean() { rewardClaims.deleteAllInBatch(); battlePlayers.deleteAllInBatch(); battles.deleteAllInBatch(); requests.deleteAllInBatch(); accelerations.deleteAllInBatch(); parents.deleteAllInBatch(); slots.deleteAllInBatch(); aliens.deleteAllInBatch(); users.deleteAllInBatch(); }

    @Test void initializesUnlocksAndUnlockRetryDoesNotChargeTwice() {
        User u=user("breed-unlock",20000); MythicBreedingDtos.SlotsResponse initial=service.slots(u.getUsername());
        assertThat(initial.slots()).hasSize(3); assertThat(initial.slots().get(0).status()).isEqualTo("AVAILABLE");
        int before=users.findById(u.getId()).orElseThrow().getDiamond();
        service.unlock(u.getUsername(),2,"unlock-1"); service.unlock(u.getUsername(),2,"unlock-2");
        assertThat(users.findById(u.getId()).orElseThrow().getDiamond()).isEqualTo(before-5000);
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

    @Test void duplicateResultAddsThirtyPieces() throws Exception {
        User u=user("breed-duplicate",0); UserAlien a=owned(u,29), b=owned(u,30);
        service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"s")); MythicBreedingSlot breeding=slots.findByUserAndSlotNo(u,1).orElseThrow();
        AlienSpec resultSpec = mythicSpec(breeding.getResultAlienId());
        UserAlien result=aliens.findByUserAndAlienSpec(u,resultSpec).orElseGet(()->owned(u,breeding.getResultAlienId())); result.setPieces(2); aliens.save(result); forceReady(breeding);
        service.claim(u.getUsername(),1,new MythicBreedingDtos.ClaimRequest("c"));
        assertThat(aliens.findByUserAndAlienSpec(u,result.getAlienSpec()).orElseThrow().getPieces()).isEqualTo(32);
    }

    @Test void slotTwoUnlocksForAccountLevelThirtyWithoutDiamondCharge() {
        User u=user("breed-level",0); u.setAccountLevel(30); users.saveAndFlush(u);
        var response=service.slots(u.getUsername());
        assertThat(response.slots().get(1).status()).isEqualTo("AVAILABLE");
        assertThat(response.slots().get(1).unlockSource()).isEqualTo("LEVEL");
        assertThat(users.findById(u.getId()).orElseThrow().getDiamond()).isZero();
    }

    @Test void accelerationChargesOneHundredDiamondsPerTenMinutesAndRetryIsIdempotent() {
        User u=user("breed-acceleration",1000); UserAlien a=owned(u,29), b=owned(u,30);
        service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"start-acc"));
        var first=service.accelerate(u.getUsername(),1,new MythicBreedingDtos.AccelerateRequest("acc-1",2));
        var retry=service.accelerate(u.getUsername(),1,new MythicBreedingDtos.AccelerateRequest("acc-1",2));
        assertThat(first.appliedUnits()).isEqualTo(2); assertThat(first.spentDiamond()).isEqualTo(200);
        assertThat(retry.spentDiamond()).isEqualTo(200); assertThat(users.findById(u.getId()).orElseThrow().getDiamond()).isEqualTo(800);
    }

    @Test void startRetryAfterReadyReturnsOriginalResponseAndRequestReuseConflicts() throws Exception {
        User u=user("breed-start-history",0); UserAlien a=owned(u,29), b=owned(u,30), c=owned(u,31);
        var first=service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"start-history"));
        forceReady(slots.findByUserAndSlotNo(u,1).orElseThrow());
        var retry=service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(b.getId(),a.getId(),"start-history"));
        assertThat(retry).isEqualTo(first);
        assertThatThrownBy(()->service.start(u.getUsername(),2,new MythicBreedingDtos.StartRequest(a.getId(),c.getId(),"start-history")))
                .isInstanceOfSatisfying(BusinessException.class,e->assertThat(e.getErrorCode()).isEqualTo(ErrorCode.MYTHIC_BREEDING_REQUEST_CONFLICT));
    }

    @Test void claimHistorySurvivesNextBreedingCycle() throws Exception {
        User u=user("breed-claim-history",0); UserAlien a=owned(u,29), b=owned(u,30), c=owned(u,31);
        service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"start-cycle-1"));
        forceReady(slots.findByUserAndSlotNo(u,1).orElseThrow());
        var first=service.claim(u.getUsername(),1,new MythicBreedingDtos.ClaimRequest("claim-history"));
        service.start(u.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),c.getId(),"start-cycle-2"));
        var retry=service.claim(u.getUsername(),1,new MythicBreedingDtos.ClaimRequest("claim-history"));
        assertThat(retry).isEqualTo(first);
        assertThat(slots.findByUserAndSlotNo(u,1).orElseThrow().getStatus()).isEqualTo(MythicBreedingSlotStatus.BREEDING);
    }

    @Test void accelerationRejectsInsufficientDiamondAndCanFinishImmediately() {
        User poor=user("breed-acc-poor",0); UserAlien a=owned(poor,29), b=owned(poor,30);
        service.start(poor.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"start-poor"));
        assertThatThrownBy(()->service.accelerate(poor.getUsername(),1,new MythicBreedingDtos.AccelerateRequest("acc-poor",1)))
                .isInstanceOf(BusinessException.class);

        User rich=user("breed-acc-instant",20000); UserAlien c=owned(rich,31), d=owned(rich,32);
        service.start(rich.getUsername(),1,new MythicBreedingDtos.StartRequest(c.getId(),d.getId(),"start-instant"));
        var response=service.accelerate(rich.getUsername(),1,new MythicBreedingDtos.AccelerateRequest("acc-instant",9999));
        assertThat(response.status()).isEqualTo("REWARD_READY");
        assertThat(response.appliedUnits()).isEqualTo(144);
        assertThat(users.findById(rich.getId()).orElseThrow().getDiamond()).isEqualTo(5600);
    }

    @Test void accelerationRequestScopeIsPerUserAndPayloadConflictIsRejected() {
        User first=user("breed-acc-scope-a",1000); UserAlien a=owned(first,29), b=owned(first,30);
        service.start(first.getUsername(),1,new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"start-scope-a"));
        service.accelerate(first.getUsername(),1,new MythicBreedingDtos.AccelerateRequest("shared-acc-id",1));
        assertThatThrownBy(()->service.accelerate(first.getUsername(),1,new MythicBreedingDtos.AccelerateRequest("shared-acc-id",2)))
                .isInstanceOfSatisfying(BusinessException.class,e->assertThat(e.getErrorCode()).isEqualTo(ErrorCode.MYTHIC_BREEDING_REQUEST_CONFLICT));

        User second=user("breed-acc-scope-b",1000); UserAlien c=owned(second,31), d=owned(second,32);
        service.start(second.getUsername(),1,new MythicBreedingDtos.StartRequest(c.getId(),d.getId(),"start-scope-b"));
        assertThatCode(()->service.accelerate(second.getUsername(),1,new MythicBreedingDtos.AccelerateRequest("shared-acc-id",1))).doesNotThrowAnyException();
    }

    @Test void sameStartRequestConcurrentCallsReturnOneStoredBreeding() throws Exception {
        User u=user("breed-same-request",0); UserAlien a=owned(u,29), b=owned(u,30);
        var request=new MythicBreedingDtos.StartRequest(a.getId(),b.getId(),"same-concurrent-start");
        ExecutorService pool=Executors.newFixedThreadPool(2); CountDownLatch ready=new CountDownLatch(2), start=new CountDownLatch(1);
        Callable<MythicBreedingDtos.StartResponse> call=()->{ready.countDown();start.await();return service.start(u.getUsername(),1,request);};
        Future<MythicBreedingDtos.StartResponse> first=pool.submit(call), second=pool.submit(call);
        ready.await(); start.countDown();
        var firstResponse=first.get(); var secondResponse=second.get(); pool.shutdown();
        assertThat(firstResponse).isEqualTo(secondResponse);
        assertThat(slots.findAll()).hasSize(3);
        assertThat(parents.findAll()).hasSize(2);
        assertThat(requests.findAll()).hasSize(1);
    }

    private User user(String name,int diamond){User u=new User(name,"pw");u.setDiamond(diamond);return users.save(u);}
    private UserAlien owned(User u,long id){return aliens.save(new UserAlien(u,mythicSpec(id)));}
    private AlienSpec mythicSpec(long id){return specs.findById(id).orElseGet(()->{AlienSpec s=new AlienSpec();s.setId(id);s.setName("Mythic-"+id);s.setGrade(AlienSpec.Grade.MYTHIC);return specs.save(s);});}
    private void forceReady(MythicBreedingSlot s)throws Exception{Field ready=MythicBreedingSlot.class.getDeclaredField("readyAt");ready.setAccessible(true);ready.set(s,Instant.now().minusSeconds(1));Field status=MythicBreedingSlot.class.getDeclaredField("status");status.setAccessible(true);status.set(s,MythicBreedingSlotStatus.REWARD_READY);slots.saveAndFlush(s);}
}
