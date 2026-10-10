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
| &#39;Tokenize: UUID format&#39;                   |  13.970 μs |  2.4242 μs | 0.1329 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4223 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.466 μs |  3.4965 μs | 0.1917 μs |  1.04 |    0.01 |    5 |  0.2441 | 0.1068 |    4282 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.487 μs |  0.8906 μs | 0.0488 μs |  0.32 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.439 μs |  0.1365 μs | 0.0075 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.791 μs |  0.8875 μs | 0.0486 μs |  0.56 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.698 μs |  0.0627 μs | 0.0034 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 357.624 μs | 18.0575 μs | 0.9898 μs | 25.60 |    0.22 |    6 | 17.0898 | 0.4883 |  291296 B |       68.98 |
