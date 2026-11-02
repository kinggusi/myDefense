package com.denfense.server.tutorial;
import java.util.UUID;
public final class TutorialDtos {
    private TutorialDtos() { }
    public enum Operation { START, DEFER, START_PRACTICE, VERIFY_PRACTICE, COLLECTION_SEEN, UPGRADE_SEEN, CLAIM, BREEDING_SEEN }
    public record Command(UUID requestId,Long expectedRevision,Operation operation,UUID practiceRunId) { }
    public record Status(long userId,String step,long revision,String practiceRunId,boolean practiceAvailable,boolean rewardGranted) { }
}
