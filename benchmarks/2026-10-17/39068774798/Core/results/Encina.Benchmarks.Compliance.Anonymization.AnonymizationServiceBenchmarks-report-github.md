```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.200 μs | 3.5112 μs | 0.1925 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4220 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.146 μs | 6.7811 μs | 0.3717 μs |  1.00 |    0.03 |    5 |  0.2441 | 0.1068 |    4287 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.920 μs | 0.5768 μs | 0.0316 μs |  0.35 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.354 μs | 0.1131 μs | 0.0062 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.808 μs | 0.2542 μs | 0.0139 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.707 μs | 0.0232 μs | 0.0013 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 334.213 μs | 6.9842 μs | 0.3828 μs | 23.54 |    0.28 |    6 | 17.0898 | 0.4883 |  291296 B |       69.03 |
