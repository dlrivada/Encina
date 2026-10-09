```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.383 μs |  2.4492 μs | 0.1342 μs |  1.00 |    0.01 |    5 |  0.1373 | 0.0610 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.155 μs |  1.4355 μs | 0.0787 μs |  0.98 |    0.01 |    5 |  0.1526 | 0.0610 |    4802 B |        1.33 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.460 μs |  0.3811 μs | 0.0209 μs |  0.31 |    0.00 |    3 |  0.0229 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.487 μs |  0.0460 μs | 0.0025 μs |  0.17 |    0.00 |    2 |  0.0153 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.015 μs |  0.4733 μs | 0.0259 μs |  0.56 |    0.00 |    4 |  0.0305 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.424 μs |  0.0550 μs | 0.0030 μs |  0.10 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 356.625 μs | 13.7155 μs | 0.7518 μs | 24.80 |    0.21 |    6 | 11.2305 |      - |  291296 B |       80.38 |
