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
import java.util.Collections;
import java.util.Comparator;

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
}
