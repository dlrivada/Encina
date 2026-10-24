```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  15.853 μs | 40.1297 μs | 2.1996 μs |  1.01 |    0.17 |    5 |  0.2136 | 0.0610 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.533 μs |  5.2013 μs | 0.2851 μs |  0.93 |    0.11 |    5 |  0.2594 | 0.1221 |    4887 B |        1.35 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.494 μs |  0.1976 μs | 0.0108 μs |  0.29 |    0.03 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.437 μs |  0.2748 μs | 0.0151 μs |  0.16 |    0.02 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.782 μs |  0.5173 μs | 0.0284 μs |  0.50 |    0.06 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.740 μs |  0.0562 μs | 0.0031 μs |  0.11 |    0.01 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 345.033 μs | 22.7810 μs | 1.2487 μs | 22.03 |    2.47 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
