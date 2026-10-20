```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.981 μs |  3.4217 μs | 0.1876 μs |  1.00 |    0.02 |    5 |  0.2441 | 0.1221 |    4735 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.573 μs |  2.6479 μs | 0.1451 μs |  0.97 |    0.01 |    5 |  0.2136 | 0.0763 |    3688 B |        0.78 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.516 μs |  1.3710 μs | 0.0751 μs |  0.30 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.320 μs |  0.0583 μs | 0.0032 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.796 μs |  0.3430 μs | 0.0188 μs |  0.52 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.759 μs |  0.2100 μs | 0.0115 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 347.743 μs | 22.3503 μs | 1.2251 μs | 23.21 |    0.26 |    6 | 17.0898 | 0.4883 |  291296 B |       61.52 |
