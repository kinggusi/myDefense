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
 private final UserRepository users; private final MonsterBalanceRegistry monsters; private final BalanceVersionRegistry versions; private final BalanceRegistry balances; private final BattleSettlementLookup lookup; private final BattleSettlementWriter writer;
 private final PlanetBattleBalanceRegistry planetBattles;
 private final BattleRewardGrantService rewardGrantService;
 private final BattleSessionRosterRegistry battleSessionRosters;
 private final WaveBalanceRegistry waveBalances;
 public BattleSettlementDtos.Response settle(BattleSettlementDtos.Request r){
  validateEnvelope(r);
  var byRequest=settlements.findByRequestId(r.requestId()).orElse(null);
  if(byRequest!=null){if(!byRequest.getBattleSessionId().equals(r.battleSessionId())||!byRequest.getSummaryHash().equals(r.summaryHash())||!Objects.equals(byRequest.getMapId(),r.mapId()))throw new BusinessException(ErrorCode.BATTLE_REQUEST_CONFLICT);return response(byRequest,true,r);}
  var bySession=settlements.findByBattleSessionId(r.battleSessionId()).orElse(null);
  if(bySession!=null){if(!bySession.getSummaryHash().equals(r.summaryHash())||!Objects.equals(bySession.getMapId(),r.mapId()))throw new BusinessException(ErrorCode.BATTLE_SETTLEMENT_CONFLICT);return response(bySession,true,r);}
  validateNewSettlement(r);
  BattleSettlement s; try { s=writer.create(r); } catch (DataIntegrityViolationException ex) { var winner=lookup.byRequest(r.requestId()); if(winner!=null)return response(winner,true,r); winner=lookup.bySession(r.battleSessionId()); if(winner!=null){if(!winner.getSummaryHash().equals(r.summaryHash())||!Objects.equals(winner.getMapId(),r.mapId()))throw new BusinessException(ErrorCode.BATTLE_SETTLEMENT_CONFLICT);return response(winner,true,r);} throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID); }
  return response(s,false,r);
 }
 private BattleSettlementDtos.Response response(BattleSettlement s,boolean done,BattleSettlementDtos.Request request){return new BattleSettlementDtos.Response(s.getBattleSessionId(),s.getStatus().name(),done,rewardGrantService.grant(s,request));}
 private void validateEnvelope(BattleSettlementDtos.Request r){
  if(r==null||blank(r.requestId())||blank(r.battleSessionId())||blank(r.balanceVersion())||blank(r.contentHash())||blank(r.summaryHash())||blank(r.mapId())||!validResult(r.result())||r.players()==null||r.players().size()!=2||r.monsterKills()==null||r.startedAt()==null||r.finishedAt()==null||r.startedAt().isAfter(r.finishedAt()))throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);
 }
 private void validateNewSettlement(BattleSettlementDtos.Request r){
  if(r.finalWave()<0||r.finalWave()>balances.getBattleRewardBalance().maxWave()||("VICTORY".equals(r.result())&&r.finalWave()!=balances.getBattleRewardBalance().maxWave()))throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);
  if(!versions.getBalanceVersion().equals(r.balanceVersion()))throw new BusinessException(ErrorCode.BATTLE_BALANCE_VERSION_MISMATCH);if(!versions.getContentHash().equals(r.contentHash()))throw new BusinessException(ErrorCode.BATTLE_CONTENT_HASH_MISMATCH);
  BattleSessionRosterRegistry.Roster roster=battleSessionRosters.requireComplete(r.battleSessionId());
  if(!roster.mapId().equals(r.mapId())||!roster.balanceVersion().equals(r.balanceVersion())||!roster.contentHash().equalsIgnoreCase(r.contentHash()))throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
  Set<String> ids=new HashSet<>();Set<Integer> slots=new HashSet<>();int total=0;
  for(var p:r.players()){if(p==null||blank(p.playerId())||!ids.add(p.playerId())||!slots.add(p.playerSlot())||p.playerSlot()<1||p.playerSlot()>2||users.findByUsername(p.playerId()).isEmpty()||p.kills()<0||p.supportKills()<0||p.bossKills()<0||p.initialInGameGold()<0||p.inGameGoldEarned()<0||p.inGameGoldSpent()<0||p.finalInGameGold()<0||p.initialInGameGold()+p.inGameGoldEarned()-p.inGameGoldSpent()!=p.finalInGameGold()||(p.eliminated()&&p.eliminatedWave()==null)||(!p.eliminated()&&p.eliminatedWave()!=null)||(p.eliminatedWave()!=null&&(p.eliminatedWave()<=0||p.eliminatedWave()>r.finalWave())))throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);total+=p.kills();}
  if(!slots.equals(Set.of(1,2)))throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);
  for(var authorized:roster.players()){var submitted=r.players().stream().filter(p->p.playerSlot()==authorized.playerSlot()).findFirst().orElseThrow(()->new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH));if(!authorized.playerId().equals(submitted.playerId()))throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);}
  Set<String> monsterIds=new HashSet<>();int monsterTotal=0;int submittedBossKills=0;Map<String,Integer> submittedKills=new HashMap<>();
  try{planetBattles.get(r.mapId());}catch(IllegalArgumentException e){throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);}
  for(var m:r.monsterKills()){if(m==null||blank(m.monsterSpecId())||!monsterIds.add(m.monsterSpecId())||m.totalKills()<0||m.bossKills()<0||m.bossKills()>m.totalKills()||m.totalKillGold()<0)throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);try{var spec=monsters.getById(m.monsterSpecId());if(!spec.enabled())throw new BusinessException(ErrorCode.BATTLE_UNKNOWN_MONSTER);if(m.totalKillGold()!=spec.killGold()*m.totalKills())throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);}catch(IllegalArgumentException e){throw new BusinessException(ErrorCode.BATTLE_UNKNOWN_MONSTER);}monsterTotal+=m.totalKills();submittedBossKills+=m.bossKills();submittedKills.put(m.monsterSpecId(),m.totalKills());}
  int playerBossKills=r.players().stream().mapToInt(BattleSettlementDtos.Player::bossKills).sum();
  Map<String,Integer> expectedKills=expectedKillsThrough(r.finalWave());
  int expectedBossKills=expectedKills.getOrDefault("WAVE_BOSS",0);
  if(total!=monsterTotal||!submittedKills.equals(expectedKills)||submittedBossKills!=expectedBossKills||playerBossKills!=expectedBossKills)throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);
 }
 private Map<String,Integer> expectedKillsThrough(int finalWave){Map<String,Integer> result=new HashMap<>();for(int wave=1;wave<=finalWave;wave++){var spec=waveBalances.getWave("COOP_STANDARD",wave);if(!spec.enabled())throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);for(var spawn:waveBalances.getSpawns(spec.spawnGroupId())){int lanes=switch(spawn.lanePolicy()){case "EACH_FIELD"->2;case "BOSS_SHARED"->1;default->throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);};result.merge(spawn.monsterId(),spawn.spawnCountPerField()*lanes,Integer::sum);}}return result;}
 private boolean blank(String s){return s==null||s.trim().isEmpty();}
 private boolean validResult(String result){try{BattleResult.valueOf(result);return true;}catch(IllegalArgumentException|NullPointerException ex){return false;}}
}
