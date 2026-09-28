```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.493 μs | 5.9871 μs | 0.3282 μs |  1.00 |    0.04 |    5 | 0.0458 | 0.0305 |    4213 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.381 μs | 4.8942 μs | 0.2683 μs |  0.99 |    0.03 |    5 | 0.0305 | 0.0153 |    3688 B |        0.88 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.566 μs | 0.3264 μs | 0.0179 μs |  0.34 |    0.01 |    3 |      - |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.090 μs | 1.0207 μs | 0.0559 μs |  0.20 |    0.01 |    2 | 0.0038 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.283 μs | 0.6503 μs | 0.0356 μs |  0.60 |    0.02 |    4 | 0.0076 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.630 μs | 0.0321 μs | 0.0018 μs |  0.16 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 281.935 μs | 9.4924 μs | 0.5203 μs | 26.89 |    0.73 |    6 | 3.4180 |      - |  291296 B |       69.14 |
