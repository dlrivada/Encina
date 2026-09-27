```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|------------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.621 μs |   4.6535 μs | 0.2551 μs |  1.00 |    0.02 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.034 μs |   4.4607 μs | 0.2445 μs |  0.96 |    0.02 |    5 |  0.2136 | 0.0763 |    3688 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.483 μs |   0.1638 μs | 0.0090 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.428 μs |   0.1956 μs | 0.0107 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.784 μs |   0.3270 μs | 0.0179 μs |  0.53 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.743 μs |   0.1250 μs | 0.0069 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 364.921 μs | 116.9974 μs | 6.4130 μs | 24.96 |    0.54 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
