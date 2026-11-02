package com.denfense.server.tutorial;

import java.util.UUID;

/** User/System injection boundary. Only a trusted server adapter may implement this.
 * No HTTP completion override exists. Battle/Shared integration is a separate task. */
public interface TutorialPracticeVerifier {
    boolean available();
    Verification verify(long userId,UUID runId);
    record Verification(long userId,UUID runId,boolean complete) { }
}
