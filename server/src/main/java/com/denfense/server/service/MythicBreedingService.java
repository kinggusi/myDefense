package com.denfense.server.service;

import com.denfense.server.balance.MythicBreedingRecipeBalance;
import com.denfense.server.domain.*;
import com.denfense.server.dto.breeding.MythicBreedingDtos;
import com.denfense.server.exception.*;
import com.denfense.server.repository.*;
import com.denfense.server.service.balance.MythicBreedingBalanceRegistry;
import org.springframework.transaction.annotation.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.*;
import java.util.concurrent.ThreadLocalRandom;

@Service @RequiredArgsConstructor
public class MythicBreedingService {
    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final MythicBreedingSlotRepository slotRepository;
    private final MythicBreedingParentRepository parentRepository;
    private final MythicBreedingAccelerationRepository accelerationRepository;
    private final MythicBreedingRequestRepository requestRepository;
    private final MythicBreedingBalanceRegistry balance;

    @Transactional
    public MythicBreedingDtos.SlotsResponse slots(String username) {
        User user = lockUser(username); List<MythicBreedingSlot> slots = ensureSlots(user);
        Instant now=Instant.now(); slots.forEach(s -> { if (s.isReady(now)) s.markReady(); });
        var config=balance.getConfig();
        return new MythicBreedingDtos.SlotsResponse(slots.stream().map(this::slotDto).toList(), user.getAccountLevel(),
                user.getDiamond(), config.slot2UnlockLevel(), config.slot2GemPrice(), config.slot3GemPrice(),
                config.durationSeconds(), config.accelerationUnitSeconds(), config.accelerationUnitDiamondCost());
    }
    @Transactional(readOnly=true)
    public MythicBreedingDtos.CandidatesResponse candidates(String username) {
        User user = findUser(username); List<UserAlien> owned=userAlienRepository.findAllByUser(user).stream().filter(a -> a.getAlienSpec().getGrade()== AlienSpec.Grade.MYTHIC).toList();
        Set<Long> busy=parentRepository.findByUserAliens(owned).stream().map(p -> p.getUserAlien().getId()).collect(java.util.stream.Collectors.toSet());
        return new MythicBreedingDtos.CandidatesResponse(owned.stream().map(a -> new MythicBreedingDtos.Candidate(a.getId(),a.getAlienSpec().getId(),a.getAlienSpec().getName(),a.getLevel(),!busy.contains(a.getId()))).toList());
    }
    @Transactional
    public MythicBreedingDtos.Slot unlock(String username,int slotNo,String requestId) {
        if (requestId==null||requestId.isBlank()) throw new BusinessException(ErrorCode.INVALID_REQUEST);
        User user=lockUser(username);
        MythicBreedingRequest prior=findPrior(user,requestId,MythicBreedingRequestOperation.UNLOCK,slotNo,"UNLOCK");
        if(prior!=null) return slotFromRequest(prior);
        ensureSlots(user); MythicBreedingSlot slot=getSlot(user,slotNo);
        if (slotNo==1) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_SLOT_LOCKED,"기본 슬롯은 구매할 수 없습니다.");
        if (slot.getStatus()!=MythicBreedingSlotStatus.LOCKED) {
            MythicBreedingDtos.Slot response=slotDto(slot);
            requestRepository.save(history(user,requestId,MythicBreedingRequestOperation.UNLOCK,slotNo,"UNLOCK",response,null,null));
            return response;
        }
        int price=slotNo==2?balance.getConfig().slot2GemPrice():slotNo==3?balance.getConfig().slot3GemPrice():-1;
        if (price<0) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_SLOT_NOT_FOUND);
        user.spendDiamond(price); slot.unlock(MythicBreedingUnlockSource.GEM,requestId);
        MythicBreedingDtos.Slot response=slotDto(slot);
        requestRepository.save(history(user,requestId,MythicBreedingRequestOperation.UNLOCK,slotNo,"UNLOCK",response,null,null));
        return response;
    }
    @Transactional
    public MythicBreedingDtos.StartResponse start(String username,int slotNo,MythicBreedingDtos.StartRequest req) {
        if(req==null||req.requestId()==null||req.requestId().isBlank()) throw new BusinessException(ErrorCode.INVALID_REQUEST);
        if(Objects.equals(req.parentUserAlienIdA(),req.parentUserAlienIdB())||req.parentUserAlienIdA()==null||req.parentUserAlienIdB()==null) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_INVALID_PARENT);
        String payload=parentPayload(req.parentUserAlienIdA(),req.parentUserAlienIdB());
        User user=lockUser(username);
        MythicBreedingRequest prior=findPrior(user,req.requestId(),MythicBreedingRequestOperation.START,slotNo,payload);
        if(prior!=null) return new MythicBreedingDtos.StartResponse(prior.getSlotNo(),prior.getResponseStatus(),prior.getResponseReadyAt());
        ensureSlots(user); MythicBreedingSlot slot=getSlot(user,slotNo); Instant now=now();
        if(slot.getStatus()!=MythicBreedingSlotStatus.AVAILABLE) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_SLOT_BUSY);
        UserAlien a=userAlienRepository.findById(req.parentUserAlienIdA()).orElseThrow(()->new BusinessException(ErrorCode.MYTHIC_BREEDING_INVALID_PARENT));
        UserAlien b=userAlienRepository.findById(req.parentUserAlienIdB()).orElseThrow(()->new BusinessException(ErrorCode.MYTHIC_BREEDING_INVALID_PARENT));
        if(!Objects.equals(a.getUser().getId(),user.getId())||!Objects.equals(b.getUser().getId(),user.getId())||a.getAlienSpec().getGrade()!= AlienSpec.Grade.MYTHIC||b.getAlienSpec().getGrade()!= AlienSpec.Grade.MYTHIC||parentRepository.existsByUserAlien(a)||parentRepository.existsByUserAlien(b)) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_INVALID_PARENT);
        long result=pickResult(a.getAlienSpec().getId(), b.getAlienSpec().getId()); slot.start(result,now,now.plusSeconds(balance.getConfig().durationSeconds()),req.requestId()); parentRepository.save(new MythicBreedingParent(slot,a,MythicBreedingParentOrder.A)); parentRepository.save(new MythicBreedingParent(slot,b,MythicBreedingParentOrder.B));
        MythicBreedingDtos.StartResponse response=new MythicBreedingDtos.StartResponse(slotNo,slot.getStatus().name(),slot.getReadyAt());
        requestRepository.save(history(user,req.requestId(),MythicBreedingRequestOperation.START,slotNo,payload,null,response,null));
        return response;
    }
    @Transactional
    public MythicBreedingDtos.ClaimResponse claim(String username,int slotNo,MythicBreedingDtos.ClaimRequest req) {
        if(req==null||req.requestId()==null||req.requestId().isBlank()) throw new BusinessException(ErrorCode.INVALID_REQUEST);
        User user=lockUser(username);
        MythicBreedingRequest prior=findPrior(user,req.requestId(),MythicBreedingRequestOperation.CLAIM,slotNo,"CLAIM");
        if(prior!=null) return new MythicBreedingDtos.ClaimResponse(prior.getSlotNo(),prior.getResponseResultAlienId(),prior.getResponseStatus(),prior.getResponseClaimedAt());
        ensureSlots(user); MythicBreedingSlot slot=getSlot(user,slotNo);
        if(slot.isReady(now())) slot.markReady();
        if(slot.getStatus()!=MythicBreedingSlotStatus.REWARD_READY) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_NOT_READY);
        long result=Optional.ofNullable(slot.getResultAlienId()).orElseThrow(()->new BusinessException(ErrorCode.MYTHIC_BREEDING_RESULT_NOT_FOUND));
        AlienSpec spec=alienSpecRepository.findById(result).orElseThrow(()->new BusinessException(ErrorCode.ALIEN_SPEC_NOT_FOUND));
        UserAlien existing=userAlienRepository.findByUserAndAlienSpec(user,spec).orElse(null); if(existing==null) userAlienRepository.save(new UserAlien(user,spec)); else existing.addPieces(balance.getConfig().duplicateRewardPieces());
        parentRepository.deleteAllByBreedingSlot(slot); Instant claimed=now(); slot.claim(claimed,req.requestId());
        MythicBreedingDtos.ClaimResponse response=new MythicBreedingDtos.ClaimResponse(slotNo,result,slot.getStatus().name(),slot.getClaimedAt());
        requestRepository.save(history(user,req.requestId(),MythicBreedingRequestOperation.CLAIM,slotNo,"CLAIM",null,null,response));
        return response;
    }
    @Transactional
    public MythicBreedingDtos.AccelerateResponse accelerate(String username, int slotNo, MythicBreedingDtos.AccelerateRequest req) {
        if (req == null || req.requestId() == null || req.requestId().isBlank() || req.units() <= 0)
            throw new BusinessException(ErrorCode.MYTHIC_BREEDING_ACCELERATION_INVALID);
        User user = lockUser(username);
        ensureSlots(user);
        MythicBreedingSlot slot = getSlot(user, slotNo);
        var prior = accelerationRepository.findByUserAndRequestId(user, req.requestId()).orElse(null);
        if (prior != null) {
            if (!Objects.equals(prior.getSlot().getId(), slot.getId())
                    || prior.getRequestedUnits() != req.units())
                throw new BusinessException(ErrorCode.MYTHIC_BREEDING_REQUEST_CONFLICT);
            return new MythicBreedingDtos.AccelerateResponse(slotNo, prior.getResponseStatus(), prior.getRequestedUnits(),
                    prior.getAppliedUnits(), prior.getSpentDiamond(), prior.getRemainingDiamond(), prior.getReadyAtAfter());
        }
        Instant now = now();
        if (slot.isReady(now)) slot.markReady();
        if (slot.getStatus() != MythicBreedingSlotStatus.BREEDING || slot.getReadyAt() == null)
            throw new BusinessException(ErrorCode.MYTHIC_BREEDING_ACCELERATION_INVALID);
        int secondsPerUnit = balance.getConfig().accelerationUnitSeconds();
        long remainingSeconds = Math.max(0, java.time.Duration.between(now, slot.getReadyAt()).getSeconds());
        int remainingUnits = (int) Math.max(1, (remainingSeconds + secondsPerUnit - 1) / secondsPerUnit);
        int appliedUnits = Math.min(req.units(), remainingUnits);
        int spentDiamond = Math.multiplyExact(appliedUnits, balance.getConfig().accelerationUnitDiamondCost());
        user.spendDiamond(spentDiamond);
        Instant acceleratedReadyAt = slot.getReadyAt().minusSeconds((long) appliedUnits * secondsPerUnit);
        if (acceleratedReadyAt.isBefore(now)) acceleratedReadyAt = now;
        slot.accelerate(acceleratedReadyAt, now);
        accelerationRepository.save(new MythicBreedingAcceleration(user, slot, req.requestId(), req.units(),
                appliedUnits, spentDiamond, slot.getStatus().name(), user.getDiamond(), acceleratedReadyAt, now));
        return new MythicBreedingDtos.AccelerateResponse(slotNo, slot.getStatus().name(), req.units(), appliedUnits,
                spentDiamond, user.getDiamond(), acceleratedReadyAt);
    }
    private User findUser(String name){return userRepository.findByUsername(name).orElseThrow(()->new BusinessException(ErrorCode.USER_NOT_FOUND));}
    private User lockUser(String name){return userRepository.findByUsernameForUpdate(name).orElseThrow(()->new BusinessException(ErrorCode.USER_NOT_FOUND));}
    private List<MythicBreedingSlot> ensureSlots(User u){var all=slotRepository.findAllByUserOrderBySlotNo(u); for(int i=all.size()+1;i<=balance.getConfig().slotCount();i++) all.add(slotRepository.save(new MythicBreedingSlot(u,i,i==1?MythicBreedingSlotStatus.AVAILABLE:MythicBreedingSlotStatus.LOCKED,i==1?MythicBreedingUnlockSource.DEFAULT:null))); if(u.getAccountLevel()>=balance.getConfig().slot2UnlockLevel()){all.stream().filter(s->s.getSlotNo()==2&&s.getStatus()==MythicBreedingSlotStatus.LOCKED).findFirst().ifPresent(s->s.unlock(MythicBreedingUnlockSource.LEVEL,"LEVEL:"+u.getId()));} return all;}
    private MythicBreedingSlot getSlot(User u,int no){return slotRepository.findForUpdate(u,no).orElseThrow(()->new BusinessException(ErrorCode.MYTHIC_BREEDING_SLOT_NOT_FOUND));}
    private MythicBreedingDtos.Slot slotDto(MythicBreedingSlot s){return new MythicBreedingDtos.Slot(s.getSlotNo(),s.getStatus().name(),s.getUnlockSource()==null?null:s.getUnlockSource().name(),s.getStartedAt(),s.getStatus()==MythicBreedingSlotStatus.BREEDING||s.getStatus()==MythicBreedingSlotStatus.REWARD_READY?s.getReadyAt():null);}
    private String parentPayload(long a,long b){return Math.min(a,b)+":"+Math.max(a,b);}
    private MythicBreedingRequest findPrior(User user,String requestId,MythicBreedingRequestOperation operation,int slotNo,String payload){
        MythicBreedingRequest prior=requestRepository.findByUserAndRequestId(user,requestId).orElse(null);
        if(prior==null)return null;
        if(prior.getOperation()!=operation||prior.getSlotNo()!=slotNo||!prior.getPayloadKey().equals(payload))
            throw new BusinessException(ErrorCode.MYTHIC_BREEDING_REQUEST_CONFLICT);
        return prior;
    }
    private MythicBreedingRequest history(User user,String requestId,MythicBreedingRequestOperation operation,int slotNo,String payload,
                                          MythicBreedingDtos.Slot slot,MythicBreedingDtos.StartResponse start,MythicBreedingDtos.ClaimResponse claim){
        return new MythicBreedingRequest(user,requestId,operation,slotNo,payload,
                slot!=null?slot.status():start!=null?start.status():claim.status(),
                slot==null?null:slot.unlockSource(),slot==null?null:slot.startedAt(),
                slot!=null?slot.readyAt():start==null?null:start.readyAt(),
                claim==null?null:claim.resultAlienId(),claim==null?null:claim.claimedAt(),now());
    }
    private MythicBreedingDtos.Slot slotFromRequest(MythicBreedingRequest request){
        return new MythicBreedingDtos.Slot(request.getSlotNo(),request.getResponseStatus(),request.getResponseUnlockSource(),
                request.getResponseStartedAt(),request.getResponseReadyAt());
    }
    private Instant now(){return Instant.now().truncatedTo(ChronoUnit.MICROS);}
    private long pickResult(long parentAlienIdA, long parentAlienIdB){MythicBreedingRecipeBalance recipe=balance.getRecipe(parentAlienIdA,parentAlienIdB); int total=recipe.standardWeightEach()*recipe.standardResultAlienIds().size()+recipe.exclusive19Weight()+recipe.exclusive20Weight(), n=ThreadLocalRandom.current().nextInt(total); for(long alienId:recipe.standardResultAlienIds()){n-=recipe.standardWeightEach();if(n<0)return alienId;} n-=recipe.exclusive19Weight();if(n<0)return recipe.exclusive19AlienId();return recipe.exclusive20AlienId();}
}
