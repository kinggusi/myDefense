package com.denfense.server.service;

import com.denfense.server.balance.MythicBreedingResultBalance;
import com.denfense.server.domain.*;
import com.denfense.server.dto.breeding.MythicBreedingDtos;
import com.denfense.server.exception.*;
import com.denfense.server.repository.*;
import com.denfense.server.service.balance.MythicBreedingBalanceRegistry;
import org.springframework.transaction.annotation.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.*;
import java.util.concurrent.ThreadLocalRandom;

@Service @RequiredArgsConstructor
public class MythicBreedingService {
    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final MythicBreedingSlotRepository slotRepository;
    private final MythicBreedingParentRepository parentRepository;
    private final MythicBreedingBalanceRegistry balance;

    @Transactional
    public MythicBreedingDtos.SlotsResponse slots(String username) {
        User user = lockUser(username); List<MythicBreedingSlot> slots = ensureSlots(user);
        Instant now=Instant.now(); slots.forEach(s -> { if (s.isReady(now)) s.markReady(); });
        return new MythicBreedingDtos.SlotsResponse(slots.stream().map(this::slotDto).toList());
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
        User user=lockUser(username); ensureSlots(user); MythicBreedingSlot slot=getSlot(user,slotNo);
        if (slotNo==1) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_SLOT_LOCKED,"기본 슬롯은 구매할 수 없습니다.");
        if (slot.getStatus()!=MythicBreedingSlotStatus.LOCKED) return slotDto(slot);
        int price=slotNo==2?balance.getConfig().slot2GemPrice():slotNo==3?balance.getConfig().slot3GemPrice():-1;
        if (price<0) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_SLOT_NOT_FOUND);
        user.decreaseDiamond(price); slot.unlock(MythicBreedingUnlockSource.GEM,requestId); return slotDto(slot);
    }
    @Transactional
    public MythicBreedingDtos.StartResponse start(String username,int slotNo,MythicBreedingDtos.StartRequest req) {
        if(req==null||req.requestId()==null||req.requestId().isBlank()) throw new BusinessException(ErrorCode.INVALID_REQUEST);
        User user=lockUser(username); ensureSlots(user); MythicBreedingSlot slot=getSlot(user,slotNo); Instant now=Instant.now();
        if(req.requestId().equals(slot.getStartRequestId()) && slot.getStatus()==MythicBreedingSlotStatus.BREEDING) return new MythicBreedingDtos.StartResponse(slotNo,slot.getStatus().name(),slot.getReadyAt());
        if(slot.getStatus()!=MythicBreedingSlotStatus.AVAILABLE) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_SLOT_BUSY);
        if(Objects.equals(req.parentUserAlienIdA(),req.parentUserAlienIdB())||req.parentUserAlienIdA()==null||req.parentUserAlienIdB()==null) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_INVALID_PARENT);
        UserAlien a=userAlienRepository.findById(req.parentUserAlienIdA()).orElseThrow(()->new BusinessException(ErrorCode.MYTHIC_BREEDING_INVALID_PARENT));
        UserAlien b=userAlienRepository.findById(req.parentUserAlienIdB()).orElseThrow(()->new BusinessException(ErrorCode.MYTHIC_BREEDING_INVALID_PARENT));
        if(!Objects.equals(a.getUser().getId(),user.getId())||!Objects.equals(b.getUser().getId(),user.getId())||a.getAlienSpec().getGrade()!= AlienSpec.Grade.MYTHIC||b.getAlienSpec().getGrade()!= AlienSpec.Grade.MYTHIC||parentRepository.existsByUserAlien(a)||parentRepository.existsByUserAlien(b)) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_INVALID_PARENT);
        long result=pickResult(); slot.start(result,now,now.plusSeconds(balance.getConfig().durationSeconds()),req.requestId()); parentRepository.save(new MythicBreedingParent(slot,a,MythicBreedingParentOrder.A)); parentRepository.save(new MythicBreedingParent(slot,b,MythicBreedingParentOrder.B));
        return new MythicBreedingDtos.StartResponse(slotNo,slot.getStatus().name(),slot.getReadyAt());
    }
    @Transactional
    public MythicBreedingDtos.ClaimResponse claim(String username,int slotNo,MythicBreedingDtos.ClaimRequest req) {
        if(req==null||req.requestId()==null||req.requestId().isBlank()) throw new BusinessException(ErrorCode.INVALID_REQUEST);
        User user=lockUser(username); ensureSlots(user); MythicBreedingSlot slot=getSlot(user,slotNo);
        if(req.requestId().equals(slot.getClaimRequestId()) && slot.getLastClaimedAlienId()!=null) return new MythicBreedingDtos.ClaimResponse(slotNo,slot.getLastClaimedAlienId(),slot.getStatus().name(),slot.getClaimedAt());
        if(slot.isReady(Instant.now())) slot.markReady();
        if(slot.getStatus()!=MythicBreedingSlotStatus.REWARD_READY) throw new BusinessException(ErrorCode.MYTHIC_BREEDING_NOT_READY);
        long result=Optional.ofNullable(slot.getResultAlienId()).orElseThrow(()->new BusinessException(ErrorCode.MYTHIC_BREEDING_RESULT_NOT_FOUND));
        AlienSpec spec=alienSpecRepository.findById(result).orElseThrow(()->new BusinessException(ErrorCode.ALIEN_SPEC_NOT_FOUND));
        UserAlien existing=userAlienRepository.findByUserAndAlienSpec(user,spec).orElse(null); if(existing==null) userAlienRepository.save(new UserAlien(user,spec)); else existing.addPieces(balance.getConfig().duplicateRewardPieces());
        parentRepository.deleteAllByBreedingSlot(slot); Instant claimed=Instant.now(); slot.claim(claimed,req.requestId());
        return new MythicBreedingDtos.ClaimResponse(slotNo,result,slot.getStatus().name(),slot.getClaimedAt());
    }
    private User findUser(String name){return userRepository.findByUsername(name).orElseThrow(()->new BusinessException(ErrorCode.USER_NOT_FOUND));}
    private User lockUser(String name){return userRepository.findByUsernameForUpdate(name).orElseThrow(()->new BusinessException(ErrorCode.USER_NOT_FOUND));}
    private List<MythicBreedingSlot> ensureSlots(User u){var all=slotRepository.findAllByUserOrderBySlotNo(u); for(int i=all.size()+1;i<=balance.getConfig().slotCount();i++) all.add(slotRepository.save(new MythicBreedingSlot(u,i,i==1?MythicBreedingSlotStatus.AVAILABLE:MythicBreedingSlotStatus.LOCKED,i==1?MythicBreedingUnlockSource.DEFAULT:null))); return all;}
    private MythicBreedingSlot getSlot(User u,int no){return slotRepository.findForUpdate(u,no).orElseThrow(()->new BusinessException(ErrorCode.MYTHIC_BREEDING_SLOT_NOT_FOUND));}
    private MythicBreedingDtos.Slot slotDto(MythicBreedingSlot s){return new MythicBreedingDtos.Slot(s.getSlotNo(),s.getStatus().name(),s.getUnlockSource()==null?null:s.getUnlockSource().name(),s.getStartedAt(),s.getStatus()==MythicBreedingSlotStatus.BREEDING||s.getStatus()==MythicBreedingSlotStatus.REWARD_READY?s.getReadyAt():null);}
    private long pickResult(){var enabled=balance.getResults().stream().filter(MythicBreedingResultBalance::enabled).toList(); int total=enabled.stream().mapToInt(MythicBreedingResultBalance::weight).sum(), n=ThreadLocalRandom.current().nextInt(total); for(var r:enabled){n-=r.weight();if(n<0)return r.alienId();} return enabled.get(enabled.size()-1).alienId();}
}
