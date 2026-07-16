package com.denfense.server.balance.tool;

import com.fasterxml.jackson.core.util.DefaultIndenter;
import com.fasterxml.jackson.core.util.DefaultPrettyPrinter;
import com.fasterxml.jackson.databind.MapperFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;

import java.io.IOException;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.io.Writer;
import java.nio.charset.StandardCharsets;
import java.nio.file.AtomicMoveNotSupportedException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.LinkedHashMap;
import java.util.Map;

public class BalanceJsonWriter {

    private final ObjectMapper mapper;

    public BalanceJsonWriter() {
        this.mapper = new ObjectMapper();
        this.mapper.enable(SerializationFeature.INDENT_OUTPUT);
        this.mapper.enable(MapperFeature.SORT_PROPERTIES_ALPHABETICALLY);
        
        DefaultPrettyPrinter printer = new DefaultPrettyPrinter();
        DefaultIndenter indenter = new DefaultIndenter("  ", "\n");
        printer.indentArraysWith(indenter);
        printer.indentObjectsWith(indenter);
        this.mapper.setDefaultPrettyPrinter(printer);
    }

    public Path writeTempJson(Path targetPath, Object data) {
        Path tempPath = null;
        try {
            if (targetPath.getParent() != null && !Files.exists(targetPath.getParent())) {
                Files.createDirectories(targetPath.getParent());
            }
            tempPath = Files.createTempFile(targetPath.getParent(), "balance_tmp_", ".json");
            
            try (OutputStream os = Files.newOutputStream(tempPath);
                 Writer writer = new OutputStreamWriter(os, StandardCharsets.UTF_8)) {
                
                String json = mapper.writerWithDefaultPrettyPrinter().writeValueAsString(data);
                writer.write(json);
                if (!json.endsWith("\n")) {
                    writer.write("\n");
                }
            }
            return tempPath;
        } catch (IOException e) {
            if (tempPath != null) {
                try {
                    Files.deleteIfExists(tempPath);
                } catch (IOException ignored) {}
            }
            throw new BalanceConversionException("JSON 임시 파일 작성 중 오류 발생: " + targetPath, e);
        }
    }

    public void replaceFile(Path tempPath, Path targetPath) {
        try {
            try {
                Files.move(tempPath, targetPath, StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING);
            } catch (AtomicMoveNotSupportedException e) {
                Files.move(tempPath, targetPath, StandardCopyOption.REPLACE_EXISTING);
            }
        } catch (IOException e) {
            throw new BalanceConversionException("파일 교체 중 오류 발생: " + targetPath, e);
        }
    }

    public void replaceFilesAtomically(Map<Path, Path> stagedToTarget) {
        Map<Path, Path> backups = new LinkedHashMap<>();
        java.util.List<Path> installed = new java.util.ArrayList<>();
        try {
            for (Path target : stagedToTarget.values()) {
                Files.createDirectories(target.getParent());
                if (Files.exists(target)) {
                    Path backup = Files.createTempFile(target.getParent(), target.getFileName() + ".", ".backup");
                    Files.move(target, backup, StandardCopyOption.REPLACE_EXISTING);
                    backups.put(target, backup);
                }
            }
            for (Map.Entry<Path, Path> entry : stagedToTarget.entrySet()) {
                moveReplacing(entry.getKey(), entry.getValue());
                installed.add(entry.getValue());
            }
        } catch (Exception failure) {
            for (Path target : installed) {
                try {
                    Files.deleteIfExists(target);
                } catch (IOException rollbackFailure) {
                    failure.addSuppressed(rollbackFailure);
                }
            }
            for (Map.Entry<Path, Path> backup : backups.entrySet()) {
                try {
                    moveReplacing(backup.getValue(), backup.getKey());
                } catch (IOException rollbackFailure) {
                    failure.addSuppressed(rollbackFailure);
                }
            }
            throw new BalanceConversionException("Generated balance files could not be replaced atomically.", failure);
        } finally {
            for (Path backup : backups.values()) {
                try {
                    Files.deleteIfExists(backup);
                } catch (IOException ignored) {
                }
            }
        }
    }

    private void moveReplacing(Path source, Path target) throws IOException {
        try {
            Files.move(source, target, StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING);
        } catch (AtomicMoveNotSupportedException e) {
            Files.move(source, target, StandardCopyOption.REPLACE_EXISTING);
        }
    }
}
