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
| &#39;Tokenize: UUID format&#39;                   |  14.026 μs |  3.3544 μs | 0.1839 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4225 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.550 μs |  2.3988 μs | 0.1315 μs |  1.04 |    0.01 |    5 |  0.2441 | 0.1068 |    4285 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.767 μs |  0.2648 μs | 0.0145 μs |  0.34 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.328 μs |  0.1103 μs | 0.0060 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.849 μs |  0.2238 μs | 0.0123 μs |  0.56 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.812 μs |  0.0131 μs | 0.0007 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 346.299 μs | 42.2432 μs | 2.3155 μs | 24.69 |    0.31 |    6 | 17.0898 | 0.4883 |  291296 B |       68.95 |
