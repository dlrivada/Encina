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
| &#39;Tokenize: UUID format&#39;                   |  13.892 μs |  2.9851 μs | 0.1636 μs |  1.00 |    0.01 |    5 |  0.2594 | 0.1221 |    4824 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.762 μs |  4.4392 μs | 0.2433 μs |  0.99 |    0.02 |    5 |  0.2136 | 0.0763 |    3688 B |        0.76 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.495 μs |  0.5486 μs | 0.0301 μs |  0.32 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.439 μs |  0.2025 μs | 0.0111 μs |  0.18 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.864 μs |  0.5658 μs | 0.0310 μs |  0.57 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.691 μs |  0.0159 μs | 0.0009 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.19 |
| &#39;Risk assessment: 100-record dataset&#39;     | 340.123 μs | 36.4736 μs | 1.9992 μs | 24.49 |    0.28 |    6 | 17.0898 | 0.4883 |  291296 B |       60.38 |
