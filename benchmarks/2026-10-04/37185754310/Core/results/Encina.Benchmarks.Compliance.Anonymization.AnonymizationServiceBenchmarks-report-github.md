```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9.835 μs |  6.5549 μs | 0.3593 μs |  1.00 |    0.04 |    5 | 0.0458 | 0.0305 |    4224 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   9.780 μs |  5.8598 μs | 0.3212 μs |  1.00 |    0.04 |    5 | 0.0458 | 0.0305 |    4289 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.602 μs |  0.4928 μs | 0.0270 μs |  0.37 |    0.01 |    3 | 0.0038 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.131 μs |  1.0383 μs | 0.0569 μs |  0.22 |    0.01 |    2 | 0.0038 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.361 μs |  1.0610 μs | 0.0582 μs |  0.65 |    0.02 |    4 | 0.0076 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.691 μs |  0.0349 μs | 0.0019 μs |  0.17 |    0.01 |    1 | 0.0095 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 281.144 μs | 16.3452 μs | 0.8959 μs | 28.61 |    0.89 |    6 | 3.4180 |      - |  291296 B |       68.96 |
