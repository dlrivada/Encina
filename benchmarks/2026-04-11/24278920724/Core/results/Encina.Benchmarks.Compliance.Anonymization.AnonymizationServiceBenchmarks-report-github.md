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
| &#39;Tokenize: UUID format&#39;                   |  14.628 μs | 0.3384 μs | 0.4853 μs |  14.559 μs |  1.00 |    0.05 |    5 |  0.2136 | 0.0763 |    3632 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.913 μs | 0.4614 μs | 0.6617 μs |  14.948 μs |  1.02 |    0.06 |    5 |  0.2136 | 0.0763 |    3712 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.395 μs | 0.0152 μs | 0.0209 μs |   4.401 μs |  0.30 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.352 μs | 0.0308 μs | 0.0442 μs |   2.387 μs |  0.16 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.004 μs | 0.2395 μs | 0.3278 μs |   8.020 μs |  0.55 |    0.03 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.734 μs | 0.0406 μs | 0.0607 μs |   1.731 μs |  0.12 |    0.01 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 341.958 μs | 2.9992 μs | 4.3014 μs | 340.929 μs | 23.40 |    0.80 |    6 | 17.0898 | 0.4883 |  291296 B |       80.20 |
