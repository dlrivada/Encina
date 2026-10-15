```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.867 μs | 5.1829 μs | 0.2841 μs |  1.00 |    0.03 |    5 | 0.0305 | 0.0153 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.015 μs | 2.2956 μs | 0.1258 μs |  1.01 |    0.02 |    5 | 0.0458 | 0.0305 |    4889 B |        1.35 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.438 μs | 2.2643 μs | 0.1241 μs |  0.37 |    0.01 |    3 |      - |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.433 μs | 0.2919 μs | 0.0160 μs |  0.21 |    0.00 |    2 | 0.0038 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.425 μs | 2.1463 μs | 0.1176 μs |  0.63 |    0.02 |    4 |      - |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.903 μs | 0.0813 μs | 0.0045 μs |  0.16 |    0.00 |    1 | 0.0076 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 317.945 μs | 9.2563 μs | 0.5074 μs | 26.80 |    0.55 |    6 | 3.4180 |      - |  291296 B |       80.38 |
