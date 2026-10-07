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
| &#39;Tokenize: UUID format&#39;                   |  14.254 μs |  4.3971 μs | 0.2410 μs |  1.00 |    0.02 |    5 |  0.2441 | 0.1221 |    4736 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.446 μs |  3.5367 μs | 0.1939 μs |  1.01 |    0.02 |    5 |  0.2441 | 0.1221 |    4800 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.867 μs |  0.3250 μs | 0.0178 μs |  0.34 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.342 μs |  0.1025 μs | 0.0056 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.530 μs |  0.3969 μs | 0.0218 μs |  0.60 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.730 μs |  0.0327 μs | 0.0018 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 349.355 μs | 52.6414 μs | 2.8855 μs | 24.51 |    0.40 |    6 | 17.0898 | 0.4883 |  291296 B |       61.51 |
