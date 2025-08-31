-- SigNoz initialization script for ClickHouse
-- Create databases for SigNoz
CREATE DATABASE IF NOT EXISTS signoz_traces;
CREATE DATABASE IF NOT EXISTS signoz_metrics;
CREATE DATABASE IF NOT EXISTS signoz_logs;

-- Set up users and permissions
-- This is handled by the users.xml file but we can add additional setup here

-- Create default tables for SigNoz traces
USE signoz_traces;

-- Basic trace spans table
CREATE TABLE IF NOT EXISTS signoz_spans_local ON CLUSTER cluster_1S_1R (
    timestamp DateTime64(9) CODEC(DoubleDelta, LZ4),
    traceID String CODEC(ZSTD(1)),
    model String CODEC(ZSTD(1))
) ENGINE MergeTree()
PARTITION BY toDate(timestamp)
ORDER BY (timestamp, traceID)
TTL timestamp + INTERVAL 7 DAY DELETE
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

-- Distributed table for traces
CREATE TABLE IF NOT EXISTS signoz_spans ON CLUSTER cluster_1S_1R (
    timestamp DateTime64(9) CODEC(DoubleDelta, LZ4),
    traceID String CODEC(ZSTD(1)),
    model String CODEC(ZSTD(1))
) ENGINE = Distributed('cluster_1S_1R', 'signoz_traces', 'signoz_spans_local', cityHash64(traceID));

-- Create default tables for SigNoz metrics
USE signoz_metrics;

CREATE TABLE IF NOT EXISTS samples_local ON CLUSTER cluster_1S_1R (
    metric_name LowCardinality(String) CODEC(ZSTD(1)),
    fingerprint UInt64 CODEC(Delta, ZSTD(1)),
    timestamp_ms Int64 CODEC(Delta, ZSTD(1)),
    value Float64 CODEC(ZSTD(1))
) ENGINE MergeTree
PARTITION BY toDate(timestamp_ms / 1000)
ORDER BY (metric_name, fingerprint, timestamp_ms)
TTL toDateTime(timestamp_ms / 1000) + INTERVAL 7 DAY DELETE
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

CREATE TABLE IF NOT EXISTS samples ON CLUSTER cluster_1S_1R (
    metric_name LowCardinality(String) CODEC(ZSTD(1)),
    fingerprint UInt64 CODEC(Delta, ZSTD(1)),
    timestamp_ms Int64 CODEC(Delta, ZSTD(1)),
    value Float64 CODEC(ZSTD(1))
) ENGINE = Distributed('cluster_1S_1R', 'signoz_metrics', 'samples_local', fingerprint);

-- Create default tables for SigNoz logs
USE signoz_logs;

CREATE TABLE IF NOT EXISTS logs_local ON CLUSTER cluster_1S_1R (
    timestamp DateTime64(9) CODEC(DoubleDelta, LZ4),
    id String CODEC(ZSTD(1)),
    trace_id String CODEC(ZSTD(1)),
    span_id String CODEC(ZSTD(1)),
    trace_flags UInt32,
    severity_text LowCardinality(String) CODEC(ZSTD(1)),
    severity_number UInt8,
    service_name LowCardinality(String) CODEC(ZSTD(1)),
    body String CODEC(ZSTD(1)),
    attributes_string Map(String, String) CODEC(ZSTD(1)),
    attributes_number Map(String, Float64) CODEC(ZSTD(1)),
    resources_string Map(String, String) CODEC(ZSTD(1))
) ENGINE MergeTree
PARTITION BY toDate(timestamp)
ORDER BY (timestamp, id)
TTL timestamp + INTERVAL 7 DAY DELETE
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

CREATE TABLE IF NOT EXISTS logs ON CLUSTER cluster_1S_1R (
    timestamp DateTime64(9) CODEC(DoubleDelta, LZ4),
    id String CODEC(ZSTD(1)),
    trace_id String CODEC(ZSTD(1)),
    span_id String CODEC(ZSTD(1)),
    trace_flags UInt32,
    severity_text LowCardinality(String) CODEC(ZSTD(1)),
    severity_number UInt8,
    service_name LowCardinality(String) CODEC(ZSTD(1)),
    body String CODEC(ZSTD(1)),
    attributes_string Map(String, String) CODEC(ZSTD(1)),
    attributes_number Map(String, Float64) CODEC(ZSTD(1)),
    resources_string Map(String, String) CODEC(ZSTD(1))
) ENGINE = Distributed('cluster_1S_1R', 'signoz_logs', 'logs_local', cityHash64(id));