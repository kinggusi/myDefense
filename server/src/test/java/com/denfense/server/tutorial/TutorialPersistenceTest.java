package com.denfense.server.tutorial;

import com.denfense.server.DefenseServerApplication;
import com.denfense.server.domain.User;
import com.denfense.server.repository.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import org.springframework.boot.WebApplicationType;
import org.springframework.boot.builder.SpringApplicationBuilder;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.ConfigurableApplicationContext;
import org.springframework.context.annotation.Bean;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.support.TransactionTemplate;
import java.nio.file.Path;
import java.util.*;
import java.util.concurrent.*;
import static com.denfense.server.tutorial.TutorialDtos.*;
import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

class TutorialPersistenceTest {
    @TempDir Path directory;
    @TestConfiguration static class TrustedTestEvidence {
        @Bean TutorialPracticeVerifier testVerifier() { return new TutorialPracticeVerifier(){
            public boolean available(){return true;}
            public Verification verify(long uid,UUID run){return new Verification(uid,run,true);}
        }; }
    }
    private ConfigurableApplicationContext start(String name) {
        return new SpringApplicationBuilder(DefenseServerApplication.class,TrustedTestEvidence.class)
                .web(WebApplicationType.NONE).profiles("local").run("--spring.datasource.url=jdbc:h2:file:"+directory.resolve(name).toAbsolutePath().toString().replace('\\','/')+";MODE=MySQL",
                        "--spring.jpa.hibernate.ddl-auto=update","--spring.jpa.show-sql=false","--spring.sql.init.mode=never",
                        "--mydefense.auth.development-accounts-enabled=false","--mydefense.collection.tutorial-mythic-preview-enabled=false");
    }
    private long user(ConfigurableApplicationContext c) {return c.getBean(UserRepository.class).saveAndFlush(new User("tutorial-"+UUID.randomUUID(),null)).getId();}
    private Command request(Status s,Operation op) {return new Command(UUID.randomUUID(),s.revision(),op,s.practiceRunId().isEmpty()?null:UUID.fromString(s.practiceRunId()));}
    private Status act(TutorialService s,long uid,Operation op) {return s.command(uid,request(s.status(uid),op));}
    private void ready(TutorialService s,long uid) {for(var op:new Operation[]{Operation.START,Operation.START_PRACTICE,Operation.VERIFY_PRACTICE,Operation.COLLECTION_SEEN,Operation.UPGRADE_SEEN})act(s,uid,op);}

    @Test void fileDatabaseRestartPreservesPracticeRewardAndReceipt() {
        long uid;String run;Command claim;
        try(var c=start("restart")) {
            uid=user(c);var s=c.getBean(TutorialService.class);act(s,uid,Operation.START);run=act(s,uid,Operation.START_PRACTICE).practiceRunId();
        }
        try(var c=start("restart")) {
            var s=c.getBean(TutorialService.class);assertThat(s.status(uid).step()).isEqualTo("PRACTICE_PENDING");assertThat(s.status(uid).practiceRunId()).isEqualTo(run);
            act(s,uid,Operation.VERIFY_PRACTICE);act(s,uid,Operation.COLLECTION_SEEN);act(s,uid,Operation.UPGRADE_SEEN);claim=request(s.status(uid),Operation.CLAIM);s.command(uid,claim);
        }
        try(var c=start("restart")) {
            var s=c.getBean(TutorialService.class);assertThat(s.status(uid).step()).isEqualTo("BREEDING_GUIDE");assertThat(s.command(uid,claim).rewardGranted()).isTrue();
            assertThat(c.getBean(JdbcTemplate.class).queryForObject("select count(*) from user_aliens where user_id=?",Integer.class,uid)).isEqualTo(2);
            act(s,uid,Operation.BREEDING_SEEN);assertThat(s.command(uid,claim).step()).isEqualTo("COMPLETED");
        }
    }
    @Test void twoThreadsClaimSameRequestExactlyOnce() throws Exception {
        try(var c=start("concurrent")) {
            long uid=user(c);var s=c.getBean(TutorialService.class);ready(s,uid);var claim=request(s.status(uid),Operation.CLAIM);
            var pool=Executors.newFixedThreadPool(2);var gate=new CountDownLatch(1);
            try {
                Callable<Status> action=()->{gate.await();return s.command(uid,claim);};var a=pool.submit(action);var b=pool.submit(action);gate.countDown();
                assertThat(a.get(15,TimeUnit.SECONDS).rewardGranted()).isTrue();assertThat(b.get(15,TimeUnit.SECONDS).rewardGranted()).isTrue();
                var db=c.getBean(JdbcTemplate.class);assertThat(db.queryForObject("select count(*) from user_aliens where user_id=?",Integer.class,uid)).isEqualTo(2);
                assertThat(db.queryForObject("select count(*) from tutorial_receipt where user_id=? and request_id=?",Integer.class,uid,claim.requestId())).isEqualTo(1);
            } finally {pool.shutdownNow();}
        }
    }
    @Test void secondRewardFailureRollsBackFirstInsertAndStateAndReceipt() {
        try(var c=start("rollback")) {
            long uid=user(c);var real=c.getBean(TutorialService.class);ready(real,uid);var before=real.status(uid);var claim=request(before,Operation.CLAIM);
            var specs=mock(AlienSpecRepository.class);when(specs.findById(29L)).thenReturn(c.getBean(AlienSpecRepository.class).findById(29L));when(specs.findById(30L)).thenReturn(Optional.empty());
            var failing=new TutorialService(c.getBean(UserRepository.class),c.getBean(UserAlienRepository.class),specs,c.getBean(TutorialProgressRepository.class),c.getBean(TutorialReceiptRepository.class),c.getBeanProvider(TutorialPracticeVerifier.class));
            var transaction=new TransactionTemplate(c.getBean(PlatformTransactionManager.class));
            assertThatThrownBy(()->transaction.execute(tx->failing.command(uid,claim))).hasMessageContaining("보상 데이터");
            assertThat(real.status(uid)).isEqualTo(before);
            var db=c.getBean(JdbcTemplate.class);assertThat(db.queryForObject("select count(*) from user_aliens where user_id=?",Integer.class,uid)).isZero();
            assertThat(c.getBean(TutorialReceiptRepository.class).findByUserIdAndRequestId(uid,claim.requestId())).isEmpty();
            assertThat(real.command(uid,claim).rewardGranted()).isTrue();
        }
    }
}
