```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.899 μs | 0.3119 μs | 0.4270 μs |  14.921 μs |  1.00 |    0.04 |    5 |  0.2136 | 0.0763 |    3632 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.997 μs | 0.3442 μs | 0.4712 μs |  14.898 μs |  1.01 |    0.04 |    5 |  0.2136 | 0.0763 |    3712 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.546 μs | 0.1320 μs | 0.1762 μs |   4.700 μs |  0.31 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.365 μs | 0.0261 μs | 0.0366 μs |   2.334 μs |  0.16 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.974 μs | 0.2195 μs | 0.3004 μs |   7.973 μs |  0.54 |    0.02 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.656 μs | 0.0053 μs | 0.0078 μs |   1.658 μs |  0.11 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 339.113 μs | 1.5153 μs | 2.2681 μs | 338.798 μs | 22.78 |    0.65 |    6 | 17.0898 | 0.4883 |  291296 B |       80.20 |
