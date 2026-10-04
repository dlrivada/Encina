```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.429 μs | 0.2577 μs | 0.3696 μs |  1.00 |    0.05 |    5 | 0.0305 | 0.0153 |    3632 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.499 μs | 0.2179 μs | 0.3125 μs |  1.01 |    0.05 |    5 | 0.0305 | 0.0153 |    3712 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.638 μs | 0.0318 μs | 0.0456 μs |  0.35 |    0.01 |    3 |      - |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.161 μs | 0.0495 μs | 0.0726 μs |  0.21 |    0.01 |    2 | 0.0038 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.416 μs | 0.0884 μs | 0.1324 μs |  0.62 |    0.02 |    4 | 0.0076 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.652 μs | 0.0139 μs | 0.0203 μs |  0.16 |    0.01 |    1 | 0.0095 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 284.334 μs | 4.1196 μs | 6.1661 μs | 27.30 |    1.11 |    6 | 3.4180 |      - |  291296 B |       80.20 |
