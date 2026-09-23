```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  12.502 μs |  6.6305 μs | 0.3634 μs |  1.00 |    0.04 |    5 | 0.0305 | 0.0153 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.313 μs |  6.7818 μs | 0.3717 μs |  0.99 |    0.04 |    5 | 0.0458 | 0.0305 |    4883 B |        1.35 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.811 μs |  1.9229 μs | 0.1054 μs |  0.31 |    0.01 |    3 |      - |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.246 μs |  0.6258 μs | 0.0343 μs |  0.18 |    0.01 |    2 | 0.0038 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.582 μs |  0.8829 μs | 0.0484 μs |  0.53 |    0.01 |    4 | 0.0076 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.766 μs |  0.0473 μs | 0.0026 μs |  0.14 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 301.153 μs | 14.4086 μs | 0.7898 μs | 24.10 |    0.60 |    6 | 3.4180 |      - |  291296 B |       80.38 |
