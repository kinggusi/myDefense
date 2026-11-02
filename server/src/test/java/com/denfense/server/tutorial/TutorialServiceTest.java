package com.denfense.server.tutorial;

import com.denfense.server.auth.*;
import com.denfense.server.domain.*;
import com.denfense.server.repository.*;
import org.junit.jupiter.api.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.transaction.annotation.Transactional;
import java.util.UUID;
import static com.denfense.server.tutorial.TutorialDtos.*;
import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest @AutoConfigureMockMvc @ActiveProfiles("local") @Transactional
class TutorialServiceTest {
    @Autowired TutorialService service;
    @Autowired UserRepository users;
    @Autowired UserAlienRepository inventory;
    @Autowired AlienSpecRepository specs;
    @Autowired TutorialProgressRepository progress;
    @Autowired MockMvc mvc;
    @MockBean TutorialPracticeVerifier verifier;
    long uid;
    @BeforeEach void init() { uid=users.saveAndFlush(new User("tutorial-"+UUID.randomUUID(),"test")).getId(); }
    Command request(Operation op,long revision,String run) { return new Command(UUID.randomUUID(),revision,op,run==null||run.isEmpty()?null:UUID.fromString(run)); }
    Status act(Operation op) { var s=service.status(uid);return service.command(uid,request(op,s.revision(),s.practiceRunId())); }
    void configureTrustedVerifier() { when(verifier.available()).thenReturn(true);when(verifier.verify(anyLong(),any())).thenAnswer(i->new TutorialPracticeVerifier.Verification(i.getArgument(0),i.getArgument(1),true)); }
    void reachReward() { configureTrustedVerifier();act(Operation.START);act(Operation.START_PRACTICE);act(Operation.VERIFY_PRACTICE);act(Operation.COLLECTION_SEEN);act(Operation.UPGRADE_SEEN); }
    @Test void newAccountStartsWithoutGrant() { var s=service.status(uid);assertThat(s.step()).isEqualTo("NOT_STARTED");assertThat(s.rewardGranted()).isFalse();assertThat(inventory.findAllByUser(users.findById(uid).orElseThrow())).isEmpty(); }
    @Test void unauthenticatedReadAndCommandAreRejectedEvenLocal() throws Exception { mvc.perform(get("/api/tutorial")).andExpect(status().isUnauthorized());mvc.perform(post("/api/tutorial/commands").contentType("application/json").content("{}")).andExpect(status().isUnauthorized()); }
    @Test void principalIsOnlyAccountSource() throws Exception { var user=users.findById(uid).orElseThrow();mvc.perform(get("/api/tutorial?userId=99999").requestAttr(AccountAccess.PRINCIPAL_ATTRIBUTE,new AccountPrincipal(uid,user.getUsername(),"test"))).andExpect(status().isOk()).andExpect(jsonPath("$.userId").value(uid)); }
    @Test void defaultVerifierBlocksPractice() { act(Operation.START);assertThatThrownBy(()->act(Operation.START_PRACTICE)).isInstanceOf(AuthException.class).hasMessageContaining("준비 중");assertThat(service.status(uid).practiceRunId()).isEmpty(); }
    @Test void clientCannotJumpToGuidesOrClaim() { for(var op:new Operation[]{Operation.VERIFY_PRACTICE,Operation.COLLECTION_SEEN,Operation.UPGRADE_SEEN,Operation.CLAIM,Operation.BREEDING_SEEN})assertThatThrownBy(()->act(op)).isInstanceOf(AuthException.class); }
    @Test void repeatStartAndDeferPreserveProgress() { act(Operation.START);var before=act(Operation.DEFER);assertThat(act(Operation.START).step()).isEqualTo(before.step());assertThat(act(Operation.DEFER).revision()).isEqualTo(before.revision()); }
    @Test void repeatPracticeStartKeepsRunBinding() { configureTrustedVerifier();act(Operation.START);var a=act(Operation.START_PRACTICE);assertThat(act(Operation.START_PRACTICE).practiceRunId()).isEqualTo(a.practiceRunId()); }
    @Test void differentRunIsRejected() { configureTrustedVerifier();act(Operation.START);var s=act(Operation.START_PRACTICE);assertThatThrownBy(()->service.command(uid,new Command(UUID.randomUUID(),s.revision(),Operation.VERIFY_PRACTICE,UUID.randomUUID()))).isInstanceOf(AuthException.class); }
    @Test void verifierWrongAccountOrRunIsRejected() { configureTrustedVerifier();act(Operation.START);act(Operation.START_PRACTICE);when(verifier.verify(anyLong(),any())).thenReturn(new TutorialPracticeVerifier.Verification(uid+1,UUID.randomUUID(),true));assertThatThrownBy(()->act(Operation.VERIFY_PRACTICE)).isInstanceOf(AuthException.class);assertThat(service.status(uid).step()).isEqualTo("PRACTICE_PENDING"); }
    @Test void uncompletedEvidenceStaysPending() { configureTrustedVerifier();act(Operation.START);act(Operation.START_PRACTICE);when(verifier.verify(anyLong(),any())).thenAnswer(i->new TutorialPracticeVerifier.Verification(i.getArgument(0),i.getArgument(1),false));assertThatThrownBy(()->act(Operation.VERIFY_PRACTICE)).isInstanceOf(AuthException.class); }
    @Test void staleRevisionAndChangedReplayAreRejected() { var r=request(Operation.START,0,null);service.command(uid,r);assertThatThrownBy(()->service.command(uid,request(Operation.DEFER,0,null))).isInstanceOf(AuthException.class);assertThatThrownBy(()->service.command(uid,new Command(r.requestId(),0L,Operation.CLAIM,null))).isInstanceOf(AuthException.class); }
    @Test void claimReplayIsExactlyOnceAndDoesNotRewindGuidance() { reachReward();var s=service.status(uid);var r=request(Operation.CLAIM,s.revision(),null);assertThat(service.command(uid,r).rewardGranted()).isTrue();act(Operation.BREEDING_SEEN);assertThat(service.command(uid,r).step()).isEqualTo("COMPLETED");assertThat(inventory.findAllByUser(users.findById(uid).orElseThrow())).hasSize(2); }
    @Test void ownedProgressIsPreservedAndMissingUsesNormalConstructor() { var user=users.findById(uid).orElseThrow();var owned=new UserAlien(user,specs.findById(29L).orElseThrow());owned.setLevel(8);owned.setPieces(123);inventory.saveAndFlush(owned);reachReward();act(Operation.CLAIM);assertThat(owned.getLevel()).isEqualTo(8);assertThat(owned.getPieces()).isEqualTo(123);var created=inventory.findByUserAndAlienSpec(user,specs.findById(30L).orElseThrow()).orElseThrow();assertThat(created.getLevel()).isEqualTo(1);assertThat(created.getPieces()).isZero(); }
    @Test void guidanceAndRewardDoNotSpendWallet() { var u=users.findById(uid).orElseThrow();long gold=u.getGold(),diamond=u.getDiamond();reachReward();act(Operation.CLAIM);act(Operation.BREEDING_SEEN);assertThat(u.getGold()).isEqualTo(gold);assertThat(u.getDiamond()).isEqualTo(diamond); }
    @Test void completedStartDoesNotReset() { reachReward();act(Operation.CLAIM);act(Operation.BREEDING_SEEN);assertThat(act(Operation.START).step()).isEqualTo("COMPLETED");assertThat(act(Operation.DEFER).rewardGranted()).isTrue(); }
}
