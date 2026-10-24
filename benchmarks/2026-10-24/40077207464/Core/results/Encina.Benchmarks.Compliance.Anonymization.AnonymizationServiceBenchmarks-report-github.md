```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.375 μs |  4.2878 μs | 0.2350 μs |  1.00 |    0.03 |    5 |  0.2594 | 0.1221 |    4813 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.884 μs |  1.6544 μs | 0.0907 μs |  1.05 |    0.02 |    5 |  0.2441 | 0.1068 |    4285 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.386 μs |  0.0348 μs | 0.0019 μs |  0.30 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.735 μs |  0.2860 μs | 0.0157 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.729 μs |  0.7394 μs | 0.0405 μs |  0.50 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.110 μs |  0.1887 μs | 0.0103 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.19 |
| &#39;Risk assessment: 100-record dataset&#39;     | 245.611 μs | 23.7564 μs | 1.3022 μs | 21.60 |    0.40 |    6 | 17.0898 | 0.4883 |  291296 B |       60.52 |
