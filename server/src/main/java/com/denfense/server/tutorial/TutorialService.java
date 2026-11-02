package com.denfense.server.tutorial;

import com.denfense.server.auth.AuthException;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.*;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.util.Objects;
import java.util.UUID;
import static com.denfense.server.tutorial.TutorialDtos.*;

@Service
public class TutorialService {
    private final UserRepository users;
    private final UserAlienRepository inventory;
    private final AlienSpecRepository specs;
    private final TutorialProgressRepository progress;
    private final TutorialReceiptRepository receipts;
    private final ObjectProvider<TutorialPracticeVerifier> verifiers;
    public TutorialService(UserRepository users,UserAlienRepository inventory,AlienSpecRepository specs,
            TutorialProgressRepository progress,TutorialReceiptRepository receipts,ObjectProvider<TutorialPracticeVerifier> verifiers) {
        this.users=users;this.inventory=inventory;this.specs=specs;this.progress=progress;this.receipts=receipts;this.verifiers=verifiers;
    }
    @Transactional public Status status(long userId) { lock(userId);return snapshot(load(userId)); }
    @Transactional public Status command(long userId,Command request) {
        if(request==null||request.requestId()==null||request.expectedRevision()==null||request.expectedRevision()<0||request.operation()==null)
            throw error(HttpStatus.BAD_REQUEST,"TUTORIAL_INVALID_REQUEST","요청 정보를 확인해 주세요.");
        User user=lock(userId);
        TutorialProgress p=load(userId);
        String fingerprint=request.operation()+":"+request.expectedRevision()+":"+request.practiceRunId();
        var previous=receipts.findByUserIdAndRequestId(userId,request.requestId()).orElse(null);
        if(previous!=null) {
            if(!previous.getFingerprint().equals(fingerprint))throw conflict();
            return snapshot(p); // A retry never rewinds the UI to a historical step.
        }
        if(p.getRevision()!=request.expectedRevision())throw conflict();
        switch(request.operation()) {
            case START -> { if(p.getStep()==TutorialStep.NOT_STARTED)p.advance(TutorialStep.PRACTICE_PENDING); }
            case DEFER -> { /* Progress persists; closing is never completion. */ }
            case START_PRACTICE -> {
                requireStep(p,TutorialStep.PRACTICE_PENDING); verifier();
                if(p.getPracticeRunId()==null)p.startPractice(UUID.randomUUID());
            }
            case VERIFY_PRACTICE -> {
                requireStep(p,TutorialStep.PRACTICE_PENDING);
                if(p.getPracticeRunId()==null||!Objects.equals(request.practiceRunId(),p.getPracticeRunId()))throw conflict();
                var evidence=verifier().verify(userId,p.getPracticeRunId());
                if(evidence==null||evidence.userId()!=userId||!p.getPracticeRunId().equals(evidence.runId())||!evidence.complete())
                    throw error(HttpStatus.CONFLICT,"TUTORIAL_PRACTICE_PENDING","연습 전투 완료를 아직 확인하지 못했습니다.");
                p.advance(TutorialStep.COLLECTION_GUIDE);
            }
            case COLLECTION_SEEN -> { requireStep(p,TutorialStep.COLLECTION_GUIDE);p.advance(TutorialStep.UPGRADE_GUIDE); }
            case UPGRADE_SEEN -> { requireStep(p,TutorialStep.UPGRADE_GUIDE);p.advance(TutorialStep.REWARD_READY); }
            case CLAIM -> {
                requireStep(p,TutorialStep.REWARD_READY);
                for(long id:new long[]{29L,30L}) {
                    var spec=specs.findById(id).orElseThrow(()->error(HttpStatus.SERVICE_UNAVAILABLE,"TUTORIAL_REWARD_UNAVAILABLE","보상 데이터를 확인할 수 없습니다."));
                    if(spec.getGrade()!=com.denfense.server.domain.AlienSpec.Grade.MYTHIC)
                        throw error(HttpStatus.SERVICE_UNAVAILABLE,"TUTORIAL_REWARD_UNAVAILABLE","보상 데이터를 확인할 수 없습니다.");
                    if(inventory.findByUserAndAlienSpec(user,spec).isEmpty())inventory.save(new UserAlien(user,spec));
                }
                p.reward();
            }
            case BREEDING_SEEN -> { requireStep(p,TutorialStep.BREEDING_GUIDE);p.complete(); }
        }
        receipts.save(new TutorialReceipt(userId,request.requestId(),fingerprint));
        return snapshot(p);
    }
    private User lock(long userId) { return users.findByIdForUpdate(userId).orElseThrow(AuthException::unauthorized); }
    private TutorialProgress load(long id) { return progress.findById(id).orElseGet(()->progress.save(new TutorialProgress(id))); }
    private TutorialPracticeVerifier verifier() {
        var v=verifiers.getIfAvailable();
        if(v==null||!v.available())throw error(HttpStatus.SERVICE_UNAVAILABLE,"TUTORIAL_PRACTICE_UNAVAILABLE","연습 전투 연결을 준비 중입니다. 진행은 저장되어 있습니다.");
        return v;
    }
    private Status snapshot(TutorialProgress p) {
        var v=verifiers.getIfAvailable();
        return new Status(p.getUserId(),p.getStep().name(),p.getRevision(),p.getPracticeRunId()==null?"":p.getPracticeRunId().toString(),
                v!=null&&v.available(),p.getRewardGrantedAt()!=null);
    }
    private void requireStep(TutorialProgress p,TutorialStep expected) { if(p.getStep()!=expected)throw conflict(); }
    private AuthException conflict() { return error(HttpStatus.CONFLICT,"TUTORIAL_STATE_CHANGED","진행 상태가 변경되었습니다. 다시 조회해 주세요."); }
    private AuthException error(HttpStatus status,String code,String message) { return new AuthException(status,code,message); }
}
