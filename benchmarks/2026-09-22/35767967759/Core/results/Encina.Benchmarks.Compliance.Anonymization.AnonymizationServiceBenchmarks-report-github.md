```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.539 μs |  1.3765 μs | 0.0755 μs |  1.00 |    0.01 |    5 |  0.2594 | 0.1068 |    4821 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.892 μs |  4.4111 μs | 0.2418 μs |  1.02 |    0.02 |    5 |  0.2136 | 0.0763 |    3688 B |        0.76 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.486 μs |  0.6950 μs | 0.0381 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.356 μs |  0.2508 μs | 0.0137 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.818 μs |  0.7362 μs | 0.0404 μs |  0.54 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.703 μs |  0.0270 μs | 0.0015 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.19 |
| &#39;Risk assessment: 100-record dataset&#39;     | 359.982 μs | 25.6653 μs | 1.4068 μs | 24.76 |    0.14 |    6 | 17.0898 | 0.4883 |  291296 B |       60.42 |
