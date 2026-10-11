```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  15.373 μs | 0.4850 μs | 0.6956 μs |  15.335 μs |  1.00 |    0.06 |    5 |  0.2136 | 0.0763 |    3632 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  15.014 μs | 0.3659 μs | 0.5248 μs |  14.845 μs |  0.98 |    0.05 |    5 |  0.2136 | 0.0763 |    3712 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.611 μs | 0.1041 μs | 0.1425 μs |   4.619 μs |  0.30 |    0.02 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.366 μs | 0.0431 μs | 0.0618 μs |   2.370 μs |  0.15 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.141 μs | 0.1622 μs | 0.2273 μs |   7.984 μs |  0.53 |    0.03 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.701 μs | 0.0063 μs | 0.0091 μs |   1.704 μs |  0.11 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 340.946 μs | 2.0309 μs | 2.9127 μs | 340.606 μs | 22.22 |    0.99 |    6 | 17.0898 | 0.4883 |  291296 B |       80.20 |
