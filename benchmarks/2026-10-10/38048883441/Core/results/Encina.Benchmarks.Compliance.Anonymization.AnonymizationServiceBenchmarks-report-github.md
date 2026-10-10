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
| &#39;Tokenize: UUID format&#39;                   |  13.957 μs |  3.7138 μs | 0.2036 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0763 |    4226 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.533 μs |  1.0332 μs | 0.0566 μs |  1.04 |    0.01 |    5 |  0.2441 | 0.1068 |    4288 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.485 μs |  0.3010 μs | 0.0165 μs |  0.32 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.457 μs |  0.1765 μs | 0.0097 μs |  0.18 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.816 μs |  0.5032 μs | 0.0276 μs |  0.56 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.752 μs |  0.0637 μs | 0.0035 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 348.050 μs | 41.0760 μs | 2.2515 μs | 24.94 |    0.35 |    6 | 17.0898 | 0.4883 |  291296 B |       68.93 |
