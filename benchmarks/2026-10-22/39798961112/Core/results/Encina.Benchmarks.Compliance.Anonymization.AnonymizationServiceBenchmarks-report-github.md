```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  15.333 μs | 0.3289 μs | 0.4610 μs |  15.220 μs |  1.00 |    0.04 |    5 |  0.2136 | 0.0610 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  15.890 μs | 0.3400 μs | 0.4767 μs |  15.769 μs |  1.04 |    0.04 |    5 |  0.2136 | 0.0610 |    3688 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.463 μs | 0.0087 μs | 0.0119 μs |   4.465 μs |  0.29 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.337 μs | 0.0091 μs | 0.0130 μs |   2.344 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.836 μs | 0.0420 μs | 0.0615 μs |   7.864 μs |  0.51 |    0.02 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.742 μs | 0.0078 μs | 0.0112 μs |   1.746 μs |  0.11 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 341.876 μs | 4.3034 μs | 6.4411 μs | 341.107 μs | 22.32 |    0.77 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
