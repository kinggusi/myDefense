package com.denfense.server.service;

import com.denfense.server.domain.*;
import com.denfense.server.dto.battle.BattleSettlementDtos;
import com.denfense.server.exception.*;
import com.denfense.server.repository.*;
import com.denfense.server.service.balance.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.dao.DataIntegrityViolationException;
import java.util.*;

@Service @RequiredArgsConstructor
public class BattleSettlementService {
 private final BattleSettlementRepository settlements; private final BattlePlayerSettlementRepository players;
 private final UserRepository users; private final MonsterBalanceRegistry monsters; private final BalanceVersionRegistry versions; private final BattleSettlementLookup lookup; private final BattleSettlementWriter writer;
 public BattleSettlementDtos.Response settle(BattleSettlementDtos.Request r){
  validate(r);
  var byRequest=settlements.findByRequestId(r.requestId()).orElse(null);
  if(byRequest!=null){if(!byRequest.getBattleSessionId().equals(r.battleSessionId())||!byRequest.getSummaryHash().equals(r.summaryHash()))throw new BusinessException(ErrorCode.BATTLE_REQUEST_CONFLICT);return response(byRequest,true);}
  var bySession=settlements.findByBattleSessionId(r.battleSessionId()).orElse(null);
  if(bySession!=null){if(!bySession.getSummaryHash().equals(r.summaryHash()))throw new BusinessException(ErrorCode.BATTLE_SETTLEMENT_CONFLICT);return response(bySession,true);}
  BattleSettlement s; try { s=writer.create(r); } catch (DataIntegrityViolationException ex) { var winner=lookup.byRequest(r.requestId()); if(winner!=null)return response(winner,true); winner=lookup.bySession(r.battleSessionId()); if(winner!=null){if(!winner.getSummaryHash().equals(r.summaryHash()))throw new BusinessException(ErrorCode.BATTLE_SETTLEMENT_CONFLICT);return response(winner,true);} throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID); }
  return response(s,false);
 }
 private BattleSettlementDtos.Response response(BattleSettlement s,boolean done){return new BattleSettlementDtos.Response(s.getBattleSessionId(),s.getStatus().name(),done,List.of());}
 private void validate(BattleSettlementDtos.Request r){
  if(r==null||blank(r.requestId())||blank(r.battleSessionId())||blank(r.balanceVersion())||blank(r.contentHash())||blank(r.summaryHash())||r.result()==null||r.players()==null||r.players().size()!=2||r.monsterKills()==null||r.startedAt()==null||r.finishedAt()==null||r.startedAt().isAfter(r.finishedAt())||r.finalWave()<0)throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);
  if(!versions.getBalanceVersion().equals(r.balanceVersion()))throw new BusinessException(ErrorCode.BATTLE_BALANCE_VERSION_MISMATCH);if(!versions.getContentHash().equals(r.contentHash()))throw new BusinessException(ErrorCode.BATTLE_CONTENT_HASH_MISMATCH);
  Set<String> ids=new HashSet<>();Set<Integer> slots=new HashSet<>();int total=0;
  for(var p:r.players()){if(blank(p.playerId())||!ids.add(p.playerId())||!slots.add(p.playerSlot())||p.playerSlot()<1||p.playerSlot()>2||p.kills()<0||p.supportKills()<0||p.bossKills()<0||p.initialInGameGold()<0||p.inGameGoldEarned()<0||p.inGameGoldSpent()<0||p.finalInGameGold()<0||p.initialInGameGold()+p.inGameGoldEarned()-p.inGameGoldSpent()!=p.finalInGameGold()||(p.eliminated()&&p.eliminatedWave()==null)||(!p.eliminated()&&p.eliminatedWave()!=null)||(p.eliminatedWave()!=null&&(p.eliminatedWave()<=0||p.eliminatedWave()>r.finalWave())))throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);total+=p.kills();}
  if(!slots.equals(Set.of(1,2)))throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);Set<String> monsterIds=new HashSet<>();int monsterTotal=0;
  for(var m:r.monsterKills()){if(blank(m.monsterSpecId())||!monsterIds.add(m.monsterSpecId())||m.totalKills()<0||m.bossKills()<0||m.bossKills()>m.totalKills()||m.totalKillGold()<0)throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);try{var spec=monsters.getById(m.monsterSpecId());if(!spec.enabled())throw new BusinessException(ErrorCode.BATTLE_UNKNOWN_MONSTER);if(m.totalKillGold()!=spec.killGold()*m.totalKills())throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);}catch(IllegalArgumentException e){throw new BusinessException(ErrorCode.BATTLE_UNKNOWN_MONSTER);}monsterTotal+=m.totalKills();}
  if(total!=monsterTotal)throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);
 }
 private boolean blank(String s){return s==null||s.trim().isEmpty();}
}
