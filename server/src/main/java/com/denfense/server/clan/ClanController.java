package com.denfense.server.clan;

import com.denfense.server.auth.AccountAccess;
import lombok.RequiredArgsConstructor;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.web.bind.annotation.*;
import static com.denfense.server.clan.ClanDtos.*;

@RestController @RequestMapping("/api/clans") @RequiredArgsConstructor
public class ClanController {
    private final AccountAccess accounts;
    private final ClanService clans;
    @ModelAttribute public void preventCaching(HttpServletResponse response) {response.setHeader("Cache-Control","no-store");}
    @GetMapping public Search search(@RequestParam(defaultValue="") String q,@RequestParam(defaultValue="0") int page,@RequestParam(defaultValue="20") int size) {
        return clans.search(accounts.requirePrincipal().userId(),q,page,size);
    }
    @GetMapping("/me") public Status status(){return clans.status(accounts.requirePrincipal().userId());}
    @GetMapping("/policy") public Policy policy(){return clans.policy(accounts.requirePrincipal().userId());}
    @GetMapping("/{clanId}") public Detail detail(@PathVariable long clanId){return clans.detail(accounts.requirePrincipal().userId(),clanId);}
    @PostMapping("/commands") public Status command(@RequestBody Command request){return clans.command(accounts.requirePrincipal().userId(),request);}
}
