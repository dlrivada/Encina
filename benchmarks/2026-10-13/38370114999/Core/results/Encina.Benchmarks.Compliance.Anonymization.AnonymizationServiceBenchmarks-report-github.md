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
| &#39;Tokenize: UUID format&#39;                   |  14.717 μs |  3.8636 μs | 0.2118 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4220 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.576 μs |  5.8598 μs | 0.3212 μs |  0.99 |    0.02 |    5 |  0.2594 | 0.1221 |    4883 B |        1.16 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.482 μs |  0.6619 μs | 0.0363 μs |  0.30 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.344 μs |  0.1439 μs | 0.0079 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.533 μs |  1.6285 μs | 0.0893 μs |  0.58 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.724 μs |  0.1167 μs | 0.0064 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 343.092 μs | 20.5167 μs | 1.1246 μs | 23.32 |    0.30 |    6 | 17.0898 | 0.4883 |  291296 B |       69.03 |
