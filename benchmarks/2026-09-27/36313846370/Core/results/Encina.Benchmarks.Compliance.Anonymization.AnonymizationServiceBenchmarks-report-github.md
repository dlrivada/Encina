```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.690 μs |  3.9080 μs | 0.2142 μs |  1.00 |    0.02 |    5 |  0.1526 | 0.0916 |    4218 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.451 μs |  2.5345 μs | 0.1389 μs |  0.98 |    0.01 |    5 |  0.1526 | 0.1221 |    4819 B |        1.14 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.612 μs |  1.0924 μs | 0.0599 μs |  0.31 |    0.01 |    3 |  0.0229 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.521 μs |  0.0210 μs | 0.0012 μs |  0.17 |    0.00 |    2 |  0.0153 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.987 μs |  0.5849 μs | 0.0321 μs |  0.54 |    0.01 |    4 |  0.0305 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.460 μs |  0.0267 μs | 0.0015 μs |  0.10 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 367.186 μs | 18.5095 μs | 1.0146 μs | 25.00 |    0.32 |    6 | 11.2305 |      - |  291296 B |       69.06 |
