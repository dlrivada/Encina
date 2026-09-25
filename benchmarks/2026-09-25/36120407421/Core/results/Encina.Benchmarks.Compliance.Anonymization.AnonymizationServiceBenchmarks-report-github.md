```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  15.320 μs | 18.8066 μs | 1.0309 μs |  1.00 |    0.08 |    5 |  0.2136 | 0.0916 |    4188 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.837 μs |  5.9593 μs | 0.3266 μs |  0.97 |    0.06 |    5 |  0.2136 | 0.0763 |    3688 B |        0.88 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.454 μs |  0.0651 μs | 0.0036 μs |  0.29 |    0.02 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.306 μs |  0.0730 μs | 0.0040 μs |  0.15 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.706 μs |  0.2714 μs | 0.0149 μs |  0.50 |    0.03 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.815 μs |  0.1935 μs | 0.0106 μs |  0.12 |    0.01 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 364.991 μs |  3.6389 μs | 0.1995 μs | 23.89 |    1.35 |    6 | 17.0898 | 0.4883 |  291296 B |       69.55 |
