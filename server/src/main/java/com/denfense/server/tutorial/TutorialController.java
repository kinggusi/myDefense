package com.denfense.server.tutorial;
import com.denfense.server.auth.AccountAccess;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;
import static com.denfense.server.tutorial.TutorialDtos.*;

@RestController @RequestMapping("/api/tutorial") @RequiredArgsConstructor
public class TutorialController {
    private final AccountAccess accounts;
    private final TutorialService tutorial;
    @GetMapping public Status status() { return tutorial.status(accounts.requirePrincipal().userId()); }
    @PostMapping("/commands") public Status command(@RequestBody Command request) { return tutorial.command(accounts.requirePrincipal().userId(),request); }
}
