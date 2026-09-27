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
| &#39;Tokenize: UUID format&#39;                   |  13.809 μs |  5.1878 μs | 0.2844 μs |  1.00 |    0.03 |    5 |  0.2289 | 0.0916 |    4224 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.800 μs |  3.7530 μs | 0.2057 μs |  1.00 |    0.02 |    5 |  0.2441 | 0.0916 |    4289 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.473 μs |  0.1610 μs | 0.0088 μs |  0.32 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.258 μs |  0.1967 μs | 0.0108 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.288 μs |  0.7865 μs | 0.0431 μs |  0.60 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.694 μs |  0.0814 μs | 0.0045 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 347.400 μs | 14.9655 μs | 0.8203 μs | 25.16 |    0.45 |    6 | 17.0898 | 0.4883 |  291296 B |       68.96 |
