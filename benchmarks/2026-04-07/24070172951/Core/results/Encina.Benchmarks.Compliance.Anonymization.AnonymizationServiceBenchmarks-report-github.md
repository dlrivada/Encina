```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.675 μs |  1.9157 μs | 0.1050 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4224 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.621 μs |  9.0759 μs | 0.4975 μs |  1.00 |    0.03 |    5 |  0.2441 | 0.1068 |    4285 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.377 μs |  0.3674 μs | 0.0201 μs |  0.30 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.315 μs |  0.1052 μs | 0.0058 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.694 μs |  0.1761 μs | 0.0097 μs |  0.52 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.674 μs |  0.0931 μs | 0.0051 μs |  0.11 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 347.046 μs | 25.2323 μs | 1.3831 μs | 23.65 |    0.17 |    6 | 17.0898 | 0.4883 |  291296 B |       68.96 |
