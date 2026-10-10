```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.793 μs | 4.2790 μs | 0.2345 μs |  1.00 |    0.02 |    5 | 0.0305 | 0.0153 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.942 μs | 5.7565 μs | 0.3155 μs |  1.01 |    0.03 |    5 | 0.0305 | 0.0153 |    3688 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.243 μs | 0.1014 μs | 0.0056 μs |  0.36 |    0.01 |    3 |      - |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.440 μs | 0.1378 μs | 0.0076 μs |  0.21 |    0.00 |    2 | 0.0038 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.505 μs | 2.9155 μs | 0.1598 μs |  0.64 |    0.02 |    4 |      - |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.971 μs | 0.0839 μs | 0.0046 μs |  0.17 |    0.00 |    1 | 0.0076 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 326.629 μs | 5.1858 μs | 0.2842 μs | 27.70 |    0.48 |    6 | 3.4180 |      - |  291296 B |       80.38 |
