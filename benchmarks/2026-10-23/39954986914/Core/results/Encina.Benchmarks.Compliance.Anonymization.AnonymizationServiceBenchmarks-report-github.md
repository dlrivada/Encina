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
| &#39;Tokenize: UUID format&#39;                   |  14.634 μs |  0.4144 μs | 0.0227 μs |  1.00 |    0.00 |    5 |  0.2441 | 0.1526 |    4740 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.933 μs |  4.2530 μs | 0.2331 μs |  1.02 |    0.01 |    5 |  0.2441 | 0.1068 |    4288 B |        0.90 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.806 μs |  0.4337 μs | 0.0238 μs |  0.33 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.459 μs |  0.1270 μs | 0.0070 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.385 μs |  0.5248 μs | 0.0288 μs |  0.57 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.749 μs |  0.0873 μs | 0.0048 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 347.859 μs | 24.9156 μs | 1.3657 μs | 23.77 |    0.09 |    6 | 17.0898 | 0.4883 |  291296 B |       61.45 |
