```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.998 μs |  3.2467 μs | 0.1780 μs |  1.00 |    0.02 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.022 μs |  1.1289 μs | 0.0619 μs |  1.00 |    0.01 |    5 |  0.2441 | 0.0916 |    4701 B |        1.34 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.497 μs |  0.9394 μs | 0.0515 μs |  0.32 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.367 μs |  0.4462 μs | 0.0245 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.886 μs |  2.6170 μs | 0.1434 μs |  0.56 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.739 μs |  0.2043 μs | 0.0112 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 336.759 μs | 38.6217 μs | 2.1170 μs | 24.06 |    0.30 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
