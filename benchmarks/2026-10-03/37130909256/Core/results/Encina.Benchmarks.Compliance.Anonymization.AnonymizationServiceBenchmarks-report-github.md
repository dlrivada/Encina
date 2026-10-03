```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.885 μs |  4.4855 μs | 0.2459 μs |  1.00 |    0.02 |    5 |  0.2441 | 0.1221 |    4760 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.521 μs |  2.4228 μs | 0.1328 μs |  0.98 |    0.02 |    5 |  0.2441 | 0.1221 |    4806 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.414 μs |  0.2531 μs | 0.0139 μs |  0.30 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.317 μs |  0.1020 μs | 0.0056 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.849 μs |  0.3860 μs | 0.0212 μs |  0.53 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.738 μs |  0.0597 μs | 0.0033 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 358.276 μs | 13.8658 μs | 0.7600 μs | 24.07 |    0.35 |    6 | 17.0898 | 0.4883 |  291296 B |       61.20 |
