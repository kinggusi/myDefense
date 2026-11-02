package com.denfense.server.service.shop;
import java.util.*;
/** Internal versioned persistence format; never deserialized from a client request. */
public class ShopStateData {
    public int schemaVersion=1;
    public String dayKey="",weekKey="";
    public long revision;
    public int rerolls;
    public Map<String,Integer> dailyUsage=new TreeMap<>(),weeklyUsage=new TreeMap<>();
    public List<Slot> slots=new ArrayList<>();
    public static class Slot {
        public String slotKey,offerId;
        public long alienId;
        public boolean purchased;
        public Slot() {}
        public Slot(String key,String offerId,long alienId) { this.slotKey=key;this.offerId=offerId;this.alienId=alienId; }
    }
}
