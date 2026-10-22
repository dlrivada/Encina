```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9.464 μs |  5.0349 μs | 0.2760 μs |  1.00 |    0.04 |    5 | 0.0305 | 0.0153 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   9.717 μs |  7.0251 μs | 0.3851 μs |  1.03 |    0.04 |    5 | 0.0458 | 0.0305 |    4289 B |        1.18 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.332 μs |  0.8348 μs | 0.0458 μs |  0.35 |    0.01 |    3 | 0.0038 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.942 μs |  1.4400 μs | 0.0789 μs |  0.21 |    0.01 |    2 | 0.0038 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.652 μs |  1.9160 μs | 0.1050 μs |  0.60 |    0.02 |    4 | 0.0076 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.455 μs |  1.3461 μs | 0.0738 μs |  0.15 |    0.01 |    1 | 0.0095 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 246.195 μs | 55.5711 μs | 3.0460 μs | 26.03 |    0.71 |    6 | 3.4180 |      - |  291296 B |       80.38 |
