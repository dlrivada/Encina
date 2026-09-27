```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.423 μs | 2.4593 μs | 0.1348 μs |  1.00 |    0.01 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.593 μs | 5.1685 μs | 0.2833 μs |  1.01 |    0.02 |    5 |  0.2441 | 0.1221 |    4819 B |        1.33 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.453 μs | 0.4765 μs | 0.0261 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.302 μs | 0.0688 μs | 0.0038 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.775 μs | 0.4975 μs | 0.0273 μs |  0.54 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.746 μs | 0.0711 μs | 0.0039 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 355.278 μs | 9.1530 μs | 0.5017 μs | 24.63 |    0.20 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
