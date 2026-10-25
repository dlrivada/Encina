```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.577 μs | 0.3416 μs | 0.4900 μs |  1.00 |    0.05 |    5 |  0.1373 | 0.0610 |    3632 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.362 μs | 0.1523 μs | 0.2084 μs |  0.99 |    0.03 |    5 |  0.1373 | 0.0610 |    3712 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.421 μs | 0.0208 μs | 0.0305 μs |  0.30 |    0.01 |    3 |  0.0229 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.470 μs | 0.0029 μs | 0.0043 μs |  0.17 |    0.01 |    2 |  0.0153 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.740 μs | 0.0219 μs | 0.0314 μs |  0.53 |    0.02 |    4 |  0.0305 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.457 μs | 0.0058 μs | 0.0086 μs |  0.10 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 358.880 μs | 2.3013 μs | 3.4445 μs | 24.65 |    0.83 |    6 | 11.2305 |      - |  291296 B |       80.20 |
