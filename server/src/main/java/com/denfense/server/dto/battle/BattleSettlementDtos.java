package com.denfense.server.dto.battle;
import java.time.LocalDateTime; import java.util.List;
public final class BattleSettlementDtos { private BattleSettlementDtos(){}
 public record Player(String playerId,int playerSlot,boolean eliminated,Integer eliminatedWave,int kills,int supportKills,int bossKills,int initialInGameGold,int inGameGoldEarned,int inGameGoldSpent,int finalInGameGold){}
 public record Monster(String monsterSpecId,int totalKills,int bossKills,int totalKillGold){}
 public record Request(String requestId,String battleSessionId,String balanceVersion,String contentHash,String result,int finalWave,LocalDateTime startedAt,LocalDateTime finishedAt,List<Player> players,List<Monster> monsterKills,String summaryHash){}
 public record Reward(Long userId,int gold){}
 public record Response(String battleSessionId,String status,boolean alreadyProcessed,List<Reward> rewards){}
}
