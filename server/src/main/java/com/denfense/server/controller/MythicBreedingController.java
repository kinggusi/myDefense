package com.denfense.server.controller;
import com.denfense.server.dto.breeding.MythicBreedingDtos;
import com.denfense.server.service.MythicBreedingService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;
@RestController @RequiredArgsConstructor @RequestMapping("/api/mythic-breeding")
public class MythicBreedingController {
 private final MythicBreedingService service;
 @GetMapping("/slots") public MythicBreedingDtos.SlotsResponse slots(@RequestParam String username){return service.slots(username);}
 @GetMapping("/candidates") public MythicBreedingDtos.CandidatesResponse candidates(@RequestParam String username){return service.candidates(username);}
 @PostMapping("/slots/{slotNo}/unlock") public MythicBreedingDtos.Slot unlock(@RequestParam String username,@PathVariable int slotNo,@RequestBody MythicBreedingDtos.UnlockRequest req){return service.unlock(username,slotNo,req.requestId());}
 @PostMapping("/slots/{slotNo}/start") public MythicBreedingDtos.StartResponse start(@RequestParam String username,@PathVariable int slotNo,@RequestBody MythicBreedingDtos.StartRequest req){return service.start(username,slotNo,req);}
 @PostMapping("/slots/{slotNo}/claim") public MythicBreedingDtos.ClaimResponse claim(@RequestParam String username,@PathVariable int slotNo,@RequestBody MythicBreedingDtos.ClaimRequest req){return service.claim(username,slotNo,req);}
}
