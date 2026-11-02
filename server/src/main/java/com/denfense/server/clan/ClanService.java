package com.denfense.server.clan;

import com.denfense.server.auth.AuthException;
import com.denfense.server.auth.AuthService;
import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.persistence.EntityManager;
import jakarta.persistence.LockModeType;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.data.domain.PageRequest;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.transaction.support.TransactionTemplate;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.text.Normalizer;
import java.time.Instant;
import java.util.*;
import static com.denfense.server.clan.ClanDtos.*;

@Service
public class ClanService {
    private final UserRepository users;
    private final ClanRepository clans;
    private final ClanAccountStateRepository states;
    private final ClanCommandReceiptRepository receipts;
    private final ClanPolicy policy;
    private final ClanClock clock;
    private final EntityManager entities;
    private final ObjectMapper json;
    private final AuthService auth;
    private final TransactionTemplate transaction;

    public ClanService(UserRepository users,ClanRepository clans,ClanAccountStateRepository states,
            ClanCommandReceiptRepository receipts,ClanPolicy policy,ClanClock clock,EntityManager entities,
            ObjectMapper json,AuthService auth,PlatformTransactionManager transactionManager) {
        this.users=users;this.clans=clans;this.states=states;this.receipts=receipts;this.policy=policy;
        this.clock=clock;this.entities=entities;this.json=json;this.auth=auth;transaction=new TransactionTemplate(transactionManager);
    }

    public Status command(long actorId,Command command) {
        validate(command);
        try {
            // Catch uniqueness failures OUTSIDE the transaction; never continue in a rollback-only transaction.
            return transaction.execute(tx->execute(actorId,command));
        } catch(DataIntegrityViolationException conflict) {
            throw conflict("CLAN_CONFLICT","클랜 정보가 변경되었거나 이미 사용 중인 이름입니다. 다시 조회해 주세요.");
        }
    }

    @Transactional public Status status(long userId) {
        User user=lockUsers(userId,null).get(userId);
        return snapshot(user,loadState(userId),false);
    }

    @Transactional(readOnly=true) public Policy policy(long userId) {
        if(!users.existsById(userId))throw AuthException.unauthorized();
        return new Policy(policy.minimumLevel(),policy.creationGold(),policy.maximumMembers(),policy.rejoinCooldown().toSeconds(),
                policy.nameMinimum(),policy.nameMaximum(),policy.descriptionMaximum(),policy.noticeMaximum());
    }

    @Transactional public Detail detail(long userId,long clanId) {
        lockUsers(userId,null);
        ClanAccountState actor=loadState(userId);
        Clan clan=lockClan(clanId);
        requireActive(clan);
        return detailFor(clan,actor);
    }

    @Transactional(readOnly=true) public Search search(long userId,String query,int page,int size) {
        if(!users.existsById(userId))throw AuthException.unauthorized();
        if(query==null||query.length()>80||page<0||page>1000||size<1||size>20)throw invalid();
        String normalized=normalize(query).strip();
        if(normalized.codePointCount(0,normalized.length())>policy.nameMaximum()||hasUnsafeText(normalized))throw invalid();
        var result=clans.findByActiveTrueAndNormalizedNameContainingOrderByIdAsc(normalized,PageRequest.of(page,size));
        return new Search(result.getContent().stream().map(this::summary).toList(),page,size,result.hasNext());
    }

    private Status execute(long actorId,Command request) {
        if(request.targetUserId()!=null&&request.targetUserId()==actorId)throw invalid();
        // Global order: all affected users ascending, THEN one clan. Do not lock any user after this point.
        Map<Long,User> locked=lockUsers(actorId,request.targetUserId());
        User actor=locked.get(actorId);
        ClanAccountState mine=loadState(actorId);
        String fingerprint=fingerprint(request);
        var receipt=receipts.findByUserIdAndRequestId(actorId,request.requestId()).orElse(null);
        if(receipt!=null) {
            if(!receipt.getFingerprint().equals(fingerprint))throw conflict("CLAN_REQUEST_CONFLICT","같은 요청 번호에 다른 내용이 전달되었습니다.");
            // Never replay historical member-only notices/applicants after leave, kick or demotion.
            return snapshot(actor,mine,true);
        }
        Instant now=clock.now();
        if(request.operation()==Operation.CREATE) {
            eligible(actor,mine,now);
            String name=validatedName(request.name());
            String normalized=normalize(name);
            if(clans.existsByNormalizedName(normalized))throw conflict("CLAN_NAME_TAKEN","이미 사용 중인 클랜 이름입니다.");
            if(request.expectedCreationGold()!=null&&request.expectedCreationGold()!=policy.creationGold())
                throw conflict("CLAN_QUOTE_CHANGED","클랜 생성 비용이 변경되었습니다. 다시 확인해 주세요.");
            actor.spendGold(policy.creationGold());
            Clan clan=clans.saveAndFlush(new Clan(name,normalized,text(request.description(),policy.descriptionMaximum()),request.joinMode(),actorId));
            mine.join(clan.getId(),Role.LEADER,now);
        } else {
            ClanAccountState target=request.targetUserId()==null?null:loadState(request.targetUserId());
            Clan clan=lockClan(request.clanId());
            if(request.operation()!=Operation.CANCEL_APPLICATION)requireActive(clan);
            if(request.expectedRevision()!=null&&request.expectedRevision()!=clan.getRevision())
                throw conflict("CLAN_STATE_CHANGED","클랜 정보가 변경되었습니다. 다시 조회해 주세요.");
            switch(request.operation()) {
                case JOIN -> {
                    eligible(actor,mine,now);
                    if(clan.getJoinMode()!=JoinMode.OPEN)throw conflict("CLAN_APPROVAL_REQUIRED","가입 신청이 필요한 클랜입니다.");
                    join(clan,mine,now);
                }
                case APPLY -> {
                    eligible(actor,mine,now);
                    if(clan.getJoinMode()!=JoinMode.APPROVAL)throw conflict("CLAN_OPEN_JOIN","자유 가입 클랜입니다.");
                    capacity(clan);
                    Long pending=effectivePending(mine);
                    if(pending!=null)throw conflict("CLAN_APPLICATION_EXISTS","기존 가입 신청을 먼저 취소해 주세요.");
                    mine.apply(clan.getId());
                }
                case CANCEL_APPLICATION -> {
                    if(!Objects.equals(mine.getPendingClanId(),clan.getId()))throw conflict("CLAN_APPLICATION_MISSING","해당 가입 신청이 없습니다.");
                    mine.apply(null);
                }
                case APPROVE,REJECT -> {
                    officer(mine,clan);
                    if(!Objects.equals(target.getPendingClanId(),clan.getId()))throw conflict("CLAN_APPLICATION_MISSING","해당 가입 신청이 없습니다.");
                    if(request.operation()==Operation.APPROVE) {
                        eligible(locked.get(request.targetUserId()),target,now);
                        join(clan,target,now);
                    } else {target.apply(null);clan.changed();}
                }
                case LEAVE -> {
                    member(mine,clan);
                    if(mine.getRole()==Role.LEADER)throw conflict("CLAN_LEADER_MUST_TRANSFER","다른 멤버에게 위임한 뒤 탈퇴하거나 혼자일 때 해산해 주세요.");
                    leave(clan,mine,now);
                }
                case KICK -> {
                    officer(mine,clan);member(target,clan);
                    if(target.getUserId()==actorId||target.getRole()==Role.LEADER||
                            (mine.getRole()==Role.DEPUTY&&target.getRole()!=Role.MEMBER))throw forbidden();
                    leave(clan,target,now);
                }
                case PROMOTE,DEMOTE -> {
                    leader(mine,clan);member(target,clan);
                    if(target.getRole()==Role.LEADER)throw forbidden();
                    Role expected=request.operation()==Operation.PROMOTE?Role.MEMBER:Role.DEPUTY;
                    if(target.getRole()!=expected)throw conflict("CLAN_ROLE_CHANGED","역할이 변경되었습니다. 다시 조회해 주세요.");
                    target.role(request.operation()==Operation.PROMOTE?Role.DEPUTY:Role.MEMBER);clan.changed();
                }
                case TRANSFER -> {
                    leader(mine,clan);member(target,clan);
                    if(target.getUserId()==actorId)throw invalid();
                    mine.role(Role.MEMBER);target.role(Role.LEADER);clan.transfer(target.getUserId());
                }
                case UPDATE -> {
                    leader(mine,clan);
                    clan.update(text(request.description(),policy.descriptionMaximum()),text(request.notice(),policy.noticeMaximum()),request.joinMode());
                }
                case DISBAND -> {
                    leader(mine,clan);
                    if(clan.getMemberCount()!=1||states.countByClanId(clan.getId())!=1)
                        throw conflict("CLAN_NOT_EMPTY","다른 멤버가 있으면 위임 후 탈퇴해 주세요.");
                    mine.leave(now.plus(policy.rejoinCooldown()));clan.disband(now);
                    // Soft deletion keeps request references valid; pending applications become logically inactive.
                }
                default -> throw invalid();
            }
        }
        receipts.saveAndFlush(new ClanCommandReceipt(actorId,request.requestId(),fingerprint,request.operation().name(),now));
        return snapshot(actor,mine,false);
    }

    private Map<Long,User> lockUsers(long actorId,Long targetId) {
        if(actorId<=0||targetId!=null&&targetId<=0)throw invalid();
        var ids=new TreeSet<Long>();ids.add(actorId);if(targetId!=null)ids.add(targetId);
        var result=new HashMap<Long,User>();
        // Separate ordered selects also make the database lock acquisition order explicit.
        for(long id:ids) {
            User user=users.findByIdForUpdate(id).orElseThrow(()->id==actorId?AuthException.unauthorized():error(HttpStatus.NOT_FOUND,"CLAN_USER_NOT_FOUND","계정을 찾을 수 없습니다."));
            entities.refresh(user,LockModeType.PESSIMISTIC_WRITE);
            result.put(id,user);
        }
        return result;
    }
    private ClanAccountState loadState(long id) {
        var state=states.findById(id).orElse(null);
        if(state==null)return states.save(new ClanAccountState(id));
        entities.refresh(state); // Its user mutex has already been acquired; discard any pre-lock first-level-cache copy.
        return state;
    }
    private Clan lockClan(long id) {
        Clan clan=clans.lockById(id).orElseThrow(()->error(HttpStatus.NOT_FOUND,"CLAN_NOT_FOUND","클랜을 찾을 수 없습니다."));
        entities.refresh(clan,LockModeType.PESSIMISTIC_WRITE);return clan;
    }
    private void eligible(User user,ClanAccountState state,Instant now) {
        if(state.getClanId()!=null)throw conflict("CLAN_ALREADY_JOINED","이미 클랜에 소속되어 있습니다.");
        if(user.getAccountLevel()<policy.minimumLevel())throw conflict("CLAN_LEVEL_REQUIRED","클랜 가입 레벨이 부족합니다.");
        if(state.getRejoinAvailableAt()!=null&&now.isBefore(state.getRejoinAvailableAt()))throw conflict("CLAN_REJOIN_COOLDOWN","재가입 대기 시간이 남아 있습니다.");
    }
    private void capacity(Clan clan) {if(clan.getMemberCount()>=policy.maximumMembers())throw conflict("CLAN_FULL","클랜 정원이 가득 찼습니다.");}
    private void join(Clan clan,ClanAccountState state,Instant now) {capacity(clan);state.join(clan.getId(),Role.MEMBER,now);clan.memberAdded();}
    private void leave(Clan clan,ClanAccountState state,Instant now) {state.leave(now.plus(policy.rejoinCooldown()));clan.memberRemoved();}
    private void requireActive(Clan clan) {if(!clan.isActive())throw conflict("CLAN_DISBANDED","해산된 클랜입니다.");}
    private void member(ClanAccountState state,Clan clan) {if(state==null||!Objects.equals(state.getClanId(),clan.getId()))throw forbidden();}
    private void officer(ClanAccountState state,Clan clan) {member(state,clan);if(state.getRole()==Role.MEMBER)throw forbidden();}
    private void leader(ClanAccountState state,Clan clan) {member(state,clan);if(state.getRole()!=Role.LEADER||state.getUserId()!=clan.getLeaderId())throw forbidden();}
    private Long effectivePending(ClanAccountState state) {
        if(state.getPendingClanId()==null)return null;
        return clans.findById(state.getPendingClanId()).filter(Clan::isActive).map(Clan::getId).orElse(null);
    }
    private Status snapshot(User user,ClanAccountState mine,boolean replayed) {
        // One consistent revision/count/member view, following the same User -> Clan lock order.
        // Mutating commands flush the receipt and their state before entering this method.
        Clan current=mine.getClanId()==null?null:lockClan(mine.getClanId());
        if(current!=null&&!current.isActive())current=null;
        return new Status(user.getId(),user.getGold(),current==null?null:mine.getRole(),mine.getRejoinAvailableAt(),effectivePending(mine),
                current==null?null:detailFor(current,mine),clock.now(),replayed,user.getAccountLevel());
    }
    private Summary summary(Clan clan) {return new Summary(clan.getId(),clan.getName(),clan.getDescription(),clan.getJoinMode(),clan.getMemberCount(),policy.maximumMembers());}
    private Detail detailFor(Clan clan,ClanAccountState viewer) {
        boolean member=Objects.equals(viewer.getClanId(),clan.getId());
        boolean officer=member&&viewer.getRole()!=Role.MEMBER;
        var members=states.findByClanIdOrderByUserIdAsc(clan.getId()).stream().map(s->new Member(s.getUserId(),displayName(s.getUserId()),s.getRole(),s.getJoinedAt())).toList();
        var applicants=officer?states.findTop100ByPendingClanIdOrderByUserIdAsc(clan.getId()).stream()
                .map(s->new Applicant(s.getUserId(),displayName(s.getUserId()))).toList():List.<Applicant>of();
        return new Detail(summary(clan),clan.getRevision(),member?clan.getNotice():null,members,applicants);
    }
    private String displayName(long id) {
        var info=auth.userInfo(id); // Reuse the login/profile guest classification; never expose credentials/provider IDs.
        return info.isGuest()?"Guest-"+id:info.username();
    }
    private String normalize(String input) {return Normalizer.normalize(input,Normalizer.Form.NFKC).toLowerCase(Locale.ROOT);}
    private String validatedName(String input) {
        if(input==null||input.length()>160)throw invalid();
        String name=Normalizer.normalize(input,Normalizer.Form.NFKC).strip();
        int length=name.codePointCount(0,name.length());
        if(length<policy.nameMinimum()||length>policy.nameMaximum()||!name.matches("[\\p{L}\\p{N} _-]+"))throw invalid();
        return name;
    }
    private String text(String input,int maximum) {
        if(input==null)return "";
        if(input.length()>maximum*2||input.codePointCount(0,input.length())>maximum||hasUnsafeText(input))throw invalid();
        return input.strip();
    }
    private boolean hasUnsafeText(String input) {
        return input.codePoints().anyMatch(c->Character.isISOControl(c)||Character.getType(c)==Character.FORMAT||c=='<'||c=='>');
    }
    private void validate(Command c) {
        if(c==null||c.requestId()==null||c.operation()==null||c.expectedRevision()!=null&&c.expectedRevision()<0)throw invalid();
        if(c.expectedCreationGold()!=null&&(c.operation()!=Operation.CREATE||c.expectedCreationGold()<0))throw invalid();
        boolean targeted=switch(c.operation()){case APPROVE,REJECT,KICK,PROMOTE,DEMOTE,TRANSFER->true;default->false;};
        boolean settings=c.operation()==Operation.CREATE||c.operation()==Operation.UPDATE;
        if(targeted!=(c.targetUserId()!=null)||!settings&&(c.name()!=null||c.description()!=null||c.notice()!=null||c.joinMode()!=null))throw invalid();
        if(c.operation()==Operation.CREATE) {
            if(c.clanId()!=null||c.expectedRevision()!=null||c.notice()!=null||c.joinMode()==null)throw invalid();
            validatedName(c.name());text(c.description(),policy.descriptionMaximum());
        } else {
            if(c.clanId()==null||c.clanId()<=0||c.name()!=null)throw invalid();
            if((targeted||c.operation()==Operation.UPDATE||c.operation()==Operation.DISBAND)&&c.expectedRevision()==null)throw invalid();
            if(c.operation()==Operation.UPDATE&&(c.joinMode()==null||c.description()==null||c.notice()==null))throw invalid();
            if(c.operation()==Operation.UPDATE){text(c.description(),policy.descriptionMaximum());text(c.notice(),policy.noticeMaximum());}
        }
    }
    private String fingerprint(Command c) {
        try {return HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(json.writeValueAsString(c).getBytes(StandardCharsets.UTF_8)));}
        catch(Exception e){throw new IllegalStateException("Cannot fingerprint clan command",e);}
    }
    private AuthException invalid(){return error(HttpStatus.BAD_REQUEST,"CLAN_INVALID_REQUEST","클랜 요청 정보를 확인해 주세요.");}
    private AuthException forbidden(){return error(HttpStatus.FORBIDDEN,"CLAN_PERMISSION_DENIED","클랜 권한이 없습니다.");}
    private AuthException conflict(String code,String message){return error(HttpStatus.CONFLICT,code,message);}
    private AuthException error(HttpStatus status,String code,String message){return new AuthException(status,code,message);}
}
