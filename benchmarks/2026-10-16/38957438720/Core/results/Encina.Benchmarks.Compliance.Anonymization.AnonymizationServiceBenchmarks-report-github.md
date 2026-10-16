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
| &#39;Tokenize: UUID format&#39;                   |  14.506 μs |  1.6142 μs | 0.0885 μs |  1.00 |    0.01 |    5 |  0.2441 | 0.1221 |    4739 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.890 μs |  1.0767 μs | 0.0590 μs |  1.03 |    0.01 |    5 |  0.2594 | 0.1221 |    4886 B |        1.03 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.478 μs |  0.1406 μs | 0.0077 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.422 μs |  0.1572 μs | 0.0086 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.855 μs |  0.3834 μs | 0.0210 μs |  0.54 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.745 μs |  0.1459 μs | 0.0080 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 341.310 μs | 23.4391 μs | 1.2848 μs | 23.53 |    0.15 |    6 | 17.0898 | 0.4883 |  291296 B |       61.47 |
