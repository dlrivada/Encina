```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.068 μs |  3.2738 μs | 0.1794 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0763 |    4226 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.438 μs |  2.8636 μs | 0.1570 μs |  1.03 |    0.01 |    5 |  0.2441 | 0.1068 |    4286 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.795 μs |  0.2074 μs | 0.0114 μs |  0.34 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.253 μs |  0.0770 μs | 0.0042 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.619 μs |  0.4751 μs | 0.0260 μs |  0.54 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.408 μs |  0.0473 μs | 0.0026 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 322.899 μs | 12.9120 μs | 0.7078 μs | 22.95 |    0.26 |    6 | 17.0898 | 0.4883 |  291296 B |       68.93 |
