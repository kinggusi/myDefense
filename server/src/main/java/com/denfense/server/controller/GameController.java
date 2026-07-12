package com.denfense.server.controller;

import com.denfense.server.dto.request.MergeRequestDto;
import com.denfense.server.dto.request.UseInjectorRequestDto;
import com.denfense.server.dto.request.MoveObjectRequestDto;
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

import jakarta.validation.Valid;
import java.util.List;

@RestController
@RequestMapping("/api/game")
@RequiredArgsConstructor
public class GameController {

    private final GameSessionManager sessionManager;
    private final InGameService inGameService;
    private final com.denfense.server.service.GameEntryService gameEntryService;
    private final com.denfense.server.service.GameSettlementService gameSettlementService;

    private final com.denfense.server.repository.UserRepository userRepository;

    /**
     * startGame - 기존 게임 시작 (하트 소비 우회 방지 위해 GameEntryService 호출)
     * @param userId
     */
    @PostMapping("/start")
    public String startGame(@RequestParam Long userId) {
        // 기존 /start를 유지하여 Unity 호환성 제공
        // userId 기반으로 username을 조회하여 신규 입장 정책(하트 소비)을 타게 함
        String username = userRepository.findById(userId)
                .map(com.denfense.server.domain.User::getUsername)
                .orElseThrow(() -> new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.USER_NOT_FOUND, "유저를 찾을 수 없습니다."));

        gameEntryService.enterGame(username);
        return "게임 시작! (세션 생성됨) - UserId: " + userId;
    }

    /**
     * 신규 입장 API - 하트 1개 차감
     */
    @PostMapping("/enter")
    public com.denfense.server.dto.response.GameEntryResponseDto enterGame(@RequestBody com.denfense.server.dto.request.GameEntryRequestDto request) {
        return gameEntryService.enterGame(request.getUsername());
    }

    /**
     * summon - 소환
     * @param userId
     */
    @PostMapping("/summon")
    public GameResponseDto summon(@RequestParam Long userId) {
        BoardObject newAlien = inGameService.summonAlien(userId);
        // 헬퍼 메서드(makeResponse)로 통일
        return makeResponse(userId, "납치 성공!", newAlien);
    }

    /**
     * move - 이동 및 Swap
     */
    @PostMapping("/move")
    public GameResponseDto move(@RequestBody MoveObjectRequestDto request) {
        BoardObject movedObject = inGameService.moveBoardObject(
                request.getUserId(),
                request.getObjectId(),
                request.getNewX(),
                request.getNewY()
        );
        return makeResponse(request.getUserId(), "이동 성공!", movedObject);
    }

    /**
     * merge - 머지
     * @param request
     */
    @PostMapping("/merge")
    public GameResponseDto merge(@RequestBody MergeRequestDto request) {
        InGameAlien result = inGameService.processMerge(
                request.getUserId(),
                request.getSourceId(),
                request.getTargetId()
        );
        return makeResponse(request.getUserId(), "머지 성공!", result);
    }

    /**
     * startWave - 웨이브시작
     * @param userId
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
     * @param userId
     * @param monsterSpecId
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

    /**
     * finishGame - 게임 최종 종료 및 보상 정산 API
     */
    @PostMapping("/finish")
    public com.denfense.server.dto.response.GameFinishResponseDto finishGame(@Valid @RequestBody com.denfense.server.dto.request.GameFinishRequestDto request) {
        return gameSettlementService.finishGame(request);
    }

    // 8. 치트키
    @PostMapping("/cheat/gold")
    public String addGold(@RequestParam Long userId, @RequestParam int amount) {
        GameSession session = sessionManager.getSession(userId);
        session.earnGold(amount);
        return "치트 성공! 현재 골드: " + session.getInGameGold();
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

    // 💡 10. 보드 전체 동기화 및 재접속 복구 API
    @PostMapping("/state")
    public com.denfense.server.dto.response.GameSessionStateDto getGameState(@RequestBody com.denfense.server.dto.request.GameStateRequestDto request) {
        return inGameService.getGameState(request.getUserId());
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