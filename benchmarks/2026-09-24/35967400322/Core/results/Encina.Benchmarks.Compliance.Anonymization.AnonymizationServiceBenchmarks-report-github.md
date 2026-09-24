```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.269 μs | 3.3250 μs | 0.1823 μs |  1.00 |    0.02 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.441 μs | 4.3335 μs | 0.2375 μs |  1.01 |    0.02 |    5 |  0.2441 | 0.1068 |    4286 B |        1.18 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.538 μs | 1.7750 μs | 0.0973 μs |  0.32 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.402 μs | 0.1063 μs | 0.0058 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.889 μs | 1.0600 μs | 0.0581 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.805 μs | 0.0107 μs | 0.0006 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 352.087 μs | 9.0652 μs | 0.4969 μs | 24.68 |    0.27 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
