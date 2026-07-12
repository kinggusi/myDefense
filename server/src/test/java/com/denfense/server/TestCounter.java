package com.denfense.server;

import java.io.File;
import java.nio.file.Files;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class TestCounter {
    public static void main(String[] args) throws Exception {
        count("server", "build/test-results/test");
        count("balanceToolTest", "build/test-results/balanceToolTest");
    }
    static void count(String name, String path) throws Exception {
        File dir = new File(path);
        int total = 0, fail = 0, skip = 0;
        if (dir.exists() && dir.listFiles() != null) {
            Pattern p = Pattern.compile("tests=\"(\\d+)\" skipped=\"(\\d+)\" failures=\"(\\d+)\" errors=\"(\\d+)\"");
            for (File f : dir.listFiles()) {
                if (f.getName().endsWith(".xml")) {
                    String c = Files.readString(f.toPath());
                    Matcher m = p.matcher(c);
                    if (m.find()) {
                        total += Integer.parseInt(m.group(1));
                        skip += Integer.parseInt(m.group(2));
                        fail += Integer.parseInt(m.group(3)) + Integer.parseInt(m.group(4));
                    }
                }
            }
        }
        System.out.println(name + " tests: " + total + ", failed: " + fail + ", skipped: " + skip);
    }
}
