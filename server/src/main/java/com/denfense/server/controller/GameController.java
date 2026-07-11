package com.denfense.server.controller;

import com.denfense.server.dto.request.MergeRequestDto;
import com.denfense.server.dto.request.UseInjectorRequestDto;
import com.denfense.server.dto.response.UseInjectorResponseDto;
import com.denfense.server.dto.response.GameResponseDto;
import com.denfense.server.dto.response.WaveSpawnDto;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.object.BoardObject;
import com.denfense.server.game.object.InGameAlien;
import com.denfense.server.game.session.GameSession;
import com.denfense.server.service.InGameService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/game")
@RequiredArgsConstructor
public class GameController {

    private final GameSessionManager sessionManager;
    private final InGameService inGameService;

    /**
     * startGame - 세션생성
     * @param userId
     *
     *
     */
    @PostMapping("/start")
    public String startGame(@RequestParam Long userId) {
        sessionManager.createSession(userId);
        return "게임 시작! (세션 생성됨) - UserId: " + userId;
    }

    /**
     * summon - 소환
     * @param userId
     *
     *
     */
    @PostMapping("/summon")
    public GameResponseDto summon(@RequestParam Long userId) {
        try {
            BoardObject newAlien = inGameService.summonAlien(userId);
            // 헬퍼 메서드(makeResponse)로 통일
            return makeResponse(userId, "소환 성공! (-50 Gold)", newAlien);
        } catch (Exception e) {
            return errorResponse(e);
        }
    }

    /**
     * merge - 머지
     * @param request
     *
     *
     */
    @PostMapping("/merge")
    public GameResponseDto merge(@RequestBody MergeRequestDto request) {
        try {
            InGameAlien result = inGameService.processMerge(
                    request.getUserId(),
                    request.getSourceId(),
                    request.getTargetId()
            );
            return makeResponse(request.getUserId(), "머지 성공!", result);
        } catch (Exception e) {
            return errorResponse(e);
        }
    }

    /**
     * startWave - 웨이브시작
     * @param userId
     *
     *
     */
    @PostMapping("/wave/start")
    public GameResponseDto startWave(@RequestParam Long userId) {
        try {
            List<WaveSpawnDto> plan = inGameService.startNextWave(userId);
            GameSession session = sessionManager.getSession(userId);

            String msg;
            // 이미 죽은 상태면 패배 메시지 전송
            if (session.isGameOver()) {
                msg = "게임 오버!";
            } else {
                // 정상 진행 시 요약 메시지
                StringBuilder sb = new StringBuilder("웨이브 시작! ");
                for (WaveSpawnDto dto : plan) {
                    sb.append(String.format("[%s x%d (HP:%.1fx)] ",
                            dto.getMonsterSpec().getName(),
                            dto.getCount(),
                            dto.getHpMultiplier()));
                }
                msg = sb.toString();
            }

            return new GameResponseDto(msg, null, session.getInGameGold(), session.isGameOver());

        } catch (Exception e) {
            return errorResponse(e);
        }
    }

    /**
     * startMission - 보스미션
     * @param userId
     *
     *
     */
    @PostMapping("/mission/start")
    public GameResponseDto startMission(@RequestParam Long userId) {
        try {
            WaveSpawnDto boss = inGameService.spawnMissionBoss(userId);
            return makeResponse(userId, "미션 보스 등장: " + boss.getMonsterSpec().getName(), null);
        } catch (Exception e) {
            return errorResponse(e);
        }
    }

    /**
     * 킬 - killEnemy
     * @param userId,  monsterSpecId
     *
     *
     */
    @PostMapping("/enemy/kill")
    public GameResponseDto killEnemy(@RequestParam Long userId, @RequestParam Long monsterSpecId) {
        try {
            inGameService.killMonster(userId, monsterSpecId);
            return makeResponse(userId, "처치 완료 (+Gold)", null);
        } catch (Exception e) {
            return errorResponse(e);
        }
    }

    /**
     * reportGameOver - 게임종료
     * @param userId
     *
     *
     */
    @PostMapping("/gameover")
    public GameResponseDto reportGameOver(@RequestParam Long userId) {
        try {
            GameSession session = sessionManager.getSession(userId);

            session.setGameOver(true); // 서버 상태 강제 사망 처리

            return new GameResponseDto("사망 처리됨", null, session.getInGameGold(), true);
        } catch (Exception e) {
            return errorResponse(e);
        }
    }

    // 8. 치트키
    @PostMapping("/cheat/gold")
    public String addGold(@RequestParam Long userId, @RequestParam int amount) {
        try {
            GameSession session = sessionManager.getSession(userId);
            session.earnGold(amount);
            return "치트 성공! 현재 골드: " + session.getInGameGold();
        } catch (Exception e) {
            return "오류: " + e.getMessage();
        }
    }

    // 9. 인젝터 사용 API
    @PostMapping("/use-injector")
    public UseInjectorResponseDto useInjector(@RequestBody UseInjectorRequestDto request) {
        return inGameService.useInjector(
                request.getUserId(),
                request.getInjectorId(),
                request.getAlienId()
        );
    }

    // ==================================================================
    // 🛠️ 내부 헬퍼 메서드
    // ==================================================================

    private GameResponseDto makeResponse(Long userId, String msg, BoardObject alien) {
        GameSession session = sessionManager.getSession(userId);
        return new GameResponseDto(
                msg,
                alien,
                session.getInGameGold(),
                session.isGameOver() // 클라이언트에 생존 여부를 항상 전달
        );
    }

    private GameResponseDto errorResponse(Exception e) {
        e.printStackTrace();
        return new GameResponseDto("오류 발생: " + e.getMessage(), null, 0, false);
    }
}