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
| &#39;Tokenize: UUID format&#39;                   |  14.302 μs |  2.6494 μs | 0.1452 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0763 |    4226 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.421 μs |  4.6395 μs | 0.2543 μs |  0.94 |    0.02 |    5 |  0.2594 | 0.1221 |    4884 B |        1.16 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.382 μs |  0.2546 μs | 0.0140 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.339 μs |  0.1069 μs | 0.0059 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.390 μs |  2.4284 μs | 0.1331 μs |  0.59 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.718 μs |  0.1236 μs | 0.0068 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 352.005 μs | 22.7829 μs | 1.2488 μs | 24.61 |    0.23 |    6 | 17.0898 | 0.4883 |  291296 B |       68.93 |
