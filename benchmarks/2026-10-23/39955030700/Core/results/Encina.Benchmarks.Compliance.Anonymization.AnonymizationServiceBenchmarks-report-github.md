```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.930 μs |  5.1905 μs | 0.2845 μs |  1.00 |    0.03 |    5 | 0.0458 | 0.0305 |    4226 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.186 μs |  6.0729 μs | 0.3329 μs |  1.02 |    0.03 |    5 | 0.0458 | 0.0305 |    4273 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.323 μs |  0.4380 μs | 0.0240 μs |  0.36 |    0.01 |    3 |      - |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.403 μs |  0.0935 μs | 0.0051 μs |  0.20 |    0.00 |    2 | 0.0038 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.477 μs |  0.4734 μs | 0.0260 μs |  0.63 |    0.01 |    4 |      - |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.899 μs |  0.0770 μs | 0.0042 μs |  0.16 |    0.00 |    1 | 0.0076 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 325.021 μs | 15.9092 μs | 0.8720 μs | 27.25 |    0.56 |    6 | 3.4180 |      - |  291296 B |       68.93 |
