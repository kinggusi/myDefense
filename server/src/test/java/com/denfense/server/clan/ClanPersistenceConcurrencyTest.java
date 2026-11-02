package com.denfense.server.clan;

import com.denfense.server.DefenseServerApplication;
import com.denfense.server.auth.AuthException;
import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.persistence.EntityManager;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.io.TempDir;
import org.springframework.boot.WebApplicationType;
import org.springframework.boot.builder.SpringApplicationBuilder;
import org.springframework.context.ConfigurableApplicationContext;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.support.TransactionTemplate;
import java.nio.file.Path;
import java.util.*;
import java.util.concurrent.*;
import java.util.function.Supplier;
import static com.denfense.server.clan.ClanDtos.*;
import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

@TestInstance(TestInstance.Lifecycle.PER_CLASS)
class ClanPersistenceConcurrencyTest {
    @TempDir static Path directory;
    private ConfigurableApplicationContext context;
    private ClanService service;
    private UserRepository users;
    @BeforeAll void open(){context=start("concurrency");service=context.getBean(ClanService.class);users=context.getBean(UserRepository.class);}
    @AfterAll void close(){if(context!=null)context.close();}
    private ConfigurableApplicationContext start(String name) {
        return new SpringApplicationBuilder(DefenseServerApplication.class).web(WebApplicationType.NONE).profiles("local")
                .run("--spring.datasource.url=jdbc:h2:file:"+directory.resolve(name).toAbsolutePath().toString().replace('\\','/')+";MODE=MySQL;LOCK_TIMEOUT=10000",
                        "--spring.jpa.hibernate.ddl-auto=update","--spring.jpa.show-sql=false","--spring.sql.init.mode=never",
                        "--mydefense.auth.local-accounts.bootstrap-enabled=false","--mydefense.collection.tutorial-mythic-preview-enabled=false");
    }
    private long user(ConfigurableApplicationContext c){var u=new User("clan-race-"+UUID.randomUUID(),null);u.setAccountLevel(10);u.setGold(100000);return c.getBean(UserRepository.class).saveAndFlush(u).getId();}
    private long user(){return user(context);}
    private Command create(String name,JoinMode mode){return new Command(UUID.randomUUID(),Operation.CREATE,null,null,null,name,"",null,mode);}
    private Command create(){return create("클랜"+UUID.randomUUID().toString().substring(0,12),JoinMode.OPEN);}
    private long create(long uid){return service.command(uid,create()).clan().clan().clanId();}
    private Command op(Operation op,long clan,Long target,Long revision){return new Command(UUID.randomUUID(),op,clan,target,revision,null,null,null,null);}
    private void join(long uid,long clan){service.command(uid,op(Operation.JOIN,clan,null,null));}
    private Command manage(long uid,long clan,Operation op,Long target){return op(op,clan,target,service.detail(uid,clan).revision());}
    private record Outcome(Status status,String code){}
    private Outcome outcome(Supplier<Status> action){try{return new Outcome(action.get(),null);}catch(AuthException e){return new Outcome(null,e.code());}}
    private List<Outcome> race(Supplier<Status> first,Supplier<Status> second) throws Exception {
        var executor=Executors.newFixedThreadPool(2);var gate=new CountDownLatch(1);
        try {
            var a=executor.submit(()->{gate.await();return outcome(first);});var b=executor.submit(()->{gate.await();return outcome(second);});gate.countDown();
            var outcomes=List.of(a.get(20,TimeUnit.SECONDS),b.get(20,TimeUnit.SECONDS));
            for(var result:outcomes)if(result.status()!=null&&result.status().clan()!=null)
                assertThat(result.status().clan().clan().memberCount()).isEqualTo(result.status().clan().members().size());
            return outcomes;
        } finally {executor.shutdownNow();}
    }
    @Test void concurrentSameCreationRequestChargesExactlyOnce() throws Exception {
        long uid=user();var command=create();var result=race(()->service.command(uid,command),()->service.command(uid,command));
        assertThat(result).allMatch(o->o.code()==null);assertThat(result.stream().map(o->o.status().clan().clan().clanId()).distinct()).hasSize(1);
        assertThat(users.findById(uid).orElseThrow().getGold()).isEqualTo(50000);
        assertThat(context.getBean(ClanCommandReceiptRepository.class).findByUserIdAndRequestId(uid,command.requestId())).isPresent();
    }
    @Test void twoPlayersCompeteForLastPlaceWithoutExceedingThirty() throws Exception {
        long leader=user(),clan=create(leader);for(int i=0;i<28;i++)join(user(),clan);
        long a=user(),b=user();var result=race(()->service.command(a,op(Operation.JOIN,clan,null,null)),()->service.command(b,op(Operation.JOIN,clan,null,null)));
        assertThat(result).filteredOn(o->o.code()==null).hasSize(1);assertThat(result).filteredOn(o->"CLAN_FULL".equals(o.code())).hasSize(1);
        assertThat(service.detail(leader,clan).members()).hasSize(30);assertThat(service.detail(leader,clan).clan().memberCount()).isEqualTo(30);
    }
    @Test void oneAccountCannotJoinTwoClansConcurrently() throws Exception {
        long a=user(),b=user(),candidate=user();long one=create(a),two=create(b);
        var result=race(()->service.command(candidate,op(Operation.JOIN,one,null,null)),()->service.command(candidate,op(Operation.JOIN,two,null,null)));
        assertThat(result).filteredOn(o->o.code()==null).hasSize(1);assertThat(result).filteredOn(o->"CLAN_ALREADY_JOINED".equals(o.code())).hasSize(1);
        assertThat(service.detail(a,one).clan().memberCount()+service.detail(b,two).clan().memberCount()).isEqualTo(3);
    }
    @Test void normalizedNameRaceReturnsConflictAndLoserGoldIsUnchanged() throws Exception {
        long a=user(),b=user();String name="Race"+UUID.randomUUID().toString().substring(0,8);
        var result=race(()->service.command(a,create(name,JoinMode.OPEN)),()->service.command(b,create(name.toUpperCase(),JoinMode.OPEN)));
        assertThat(result).filteredOn(o->o.code()==null).hasSize(1);assertThat(result).filteredOn(o->o.code()!=null&&o.code().startsWith("CLAN_")).hasSize(1);
        assertThat(users.findById(a).orElseThrow().getGold()+users.findById(b).orElseThrow().getGold()).isEqualTo(150000);
        assertThat(context.getBean(ClanAccountStateRepository.class).findById(a).map(ClanAccountState::getClanId).isPresent()^
                context.getBean(ClanAccountStateRepository.class).findById(b).map(ClanAccountState::getClanId).isPresent()).isTrue();
    }
    @Test void disbandVersusJoinNeverDeletesAnOccupiedClan() throws Exception {
        long leader=user(),candidate=user(),clan=create(leader);var disband=manage(leader,clan,Operation.DISBAND,null);
        var result=race(()->service.command(leader,disband),()->service.command(candidate,op(Operation.JOIN,clan,null,null)));
        assertThat(result).filteredOn(o->o.code()==null).hasSize(1);
        Clan stored=context.getBean(ClanRepository.class).findById(clan).orElseThrow();
        long members=context.getBean(ClanAccountStateRepository.class).countByClanId(clan);
        assertThat(members).isEqualTo(stored.getMemberCount());assertThat(members).isEqualTo(stored.isActive()?2:0);
    }
    @Test void oppositeActorTargetOrderDoesNotDeadlock() throws Exception {
        long a=user(),b=user(),clan=create(a);join(b,clan);service.command(a,manage(a,clan,Operation.PROMOTE,b));
        var transfer=manage(a,clan,Operation.TRANSFER,b);var demote=manage(b,clan,Operation.DEMOTE,a);
        var result=race(()->service.command(a,transfer),()->service.command(b,demote));
        assertThat(result.get(0).code()).isNull();assertThat(result.get(1).code()).isIn("CLAN_PERMISSION_DENIED","CLAN_STATE_CHANGED");
        assertThat(service.status(b).role()).isEqualTo(Role.LEADER);
    }
    @Test void queuedOfficerRequestRechecksRoleAndRefreshesPreloadedEntity() throws Exception {
        long leader=user(),deputy=user(),candidate=user(),clan=create(leader);join(deputy,clan);service.command(leader,manage(leader,clan,Operation.PROMOTE,deputy));
        service.command(leader,new Command(UUID.randomUUID(),Operation.UPDATE,clan,null,service.detail(leader,clan).revision(),null,"","",JoinMode.APPROVAL));
        service.command(candidate,op(Operation.APPLY,clan,null,null));
        long afterDemotion=service.detail(leader,clan).revision()+1;
        var ready=new CountDownLatch(1);var demoted=new CountDownLatch(1);var executor=Executors.newFixedThreadPool(2);
        var tx=new TransactionTemplate(context.getBean(PlatformTransactionManager.class));
        try {
            var stale=executor.submit(()->outcome(()->tx.execute(t->{
                context.getBean(ClanAccountStateRepository.class).findById(deputy).orElseThrow(); // Intentionally pre-load stale DEPUTY.
                ready.countDown();try{if(!demoted.await(10,TimeUnit.SECONDS))throw new AssertionError("demotion timeout");}catch(InterruptedException e){throw new RuntimeException(e);}
                return service.command(deputy,op(Operation.APPROVE,clan,candidate,afterDemotion));
            })));
            assertThat(ready.await(10,TimeUnit.SECONDS)).isTrue();service.command(leader,manage(leader,clan,Operation.DEMOTE,deputy));demoted.countDown();
            assertThat(stale.get(15,TimeUnit.SECONDS).code()).isEqualTo("CLAN_PERMISSION_DENIED");
            assertThat(service.status(candidate).clan()).isNull();assertThat(service.status(deputy).role()).isEqualTo(Role.MEMBER);
        } finally {demoted.countDown();executor.shutdownNow();}
    }
    @Test void receiptFailureRollsBackGoldMembershipAndClan() {
        long uid=user();var receiptRepo=mock(ClanCommandReceiptRepository.class);when(receiptRepo.findByUserIdAndRequestId(anyLong(),any())).thenReturn(Optional.empty());
        when(receiptRepo.saveAndFlush(any())).thenThrow(new IllegalStateException("forced receipt failure"));
        var failing=new ClanService(users,context.getBean(ClanRepository.class),context.getBean(ClanAccountStateRepository.class),receiptRepo,
                context.getBean(ClanPolicy.class),context.getBean(ClanClock.class),context.getBean(EntityManager.class),context.getBean(ObjectMapper.class),context.getBean(com.denfense.server.auth.AuthService.class),context.getBean(PlatformTransactionManager.class));
        var command=create();assertThatThrownBy(()->failing.command(uid,command)).hasMessage("forced receipt failure");
        assertThat(service.status(uid).clan()).isNull();assertThat(users.findById(uid).orElseThrow().getGold()).isEqualTo(100000);
        assertThat(context.getBean(ClanRepository.class).existsByNormalizedName(command.name().toLowerCase(Locale.ROOT))).isFalse();
        assertThat(service.command(uid,command).accountGold()).isEqualTo(50000);
    }
    @Test void fileRestartPreservesRolesApplicationsCooldownAndIdempotency() {
        long leader,member,pending,left,clan;Command create,leave;
        try(var c=start("restart")) {
            var s=c.getBean(ClanService.class);leader=user(c);member=user(c);pending=user(c);left=user(c);create=create();
            clan=s.command(leader,create).clan().clan().clanId();s.command(member,op(Operation.JOIN,clan,null,null));s.command(left,op(Operation.JOIN,clan,null,null));
            s.command(leader,op(Operation.PROMOTE,clan,member,s.detail(leader,clan).revision()));leave=op(Operation.LEAVE,clan,null,null);s.command(left,leave);
            s.command(leader,new Command(UUID.randomUUID(),Operation.UPDATE,clan,null,s.detail(leader,clan).revision(),null,"영속 소개","영속 공지",JoinMode.APPROVAL));s.command(pending,op(Operation.APPLY,clan,null,null));
        }
        try(var c=start("restart")) {
            var s=c.getBean(ClanService.class);assertThat(s.command(leader,create).replayed()).isTrue();assertThat(s.status(leader).accountGold()).isEqualTo(50000);
            assertThat(s.status(member).role()).isEqualTo(Role.DEPUTY);assertThat(s.status(member).clan().notice()).isEqualTo("영속 공지");
            assertThat(s.status(pending).pendingClanId()).isEqualTo(clan);assertThat(s.status(left).rejoinAvailableAt()).isNotNull();assertThat(s.command(left,leave).replayed()).isTrue();
            s.command(member,op(Operation.APPROVE,clan,pending,s.detail(member,clan).revision()));assertThat(s.status(pending).role()).isEqualTo(Role.MEMBER);
        }
    }
}
