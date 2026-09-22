```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|------------:|-----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.842 μs |   2.4845 μs |  0.1362 μs |  1.00 |    0.02 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.868 μs |   2.9987 μs |  0.1644 μs |  1.00 |    0.02 |    5 |  0.2441 | 0.1068 |    4277 B |        1.18 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.371 μs |   0.0602 μs |  0.0033 μs |  0.31 |    0.00 |    3 |  0.0343 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.691 μs |   0.0434 μs |  0.0024 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.725 μs |   0.1139 μs |  0.0062 μs |  0.53 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.070 μs |   0.0051 μs |  0.0003 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 249.852 μs | 186.2503 μs | 10.2090 μs | 23.05 |    0.85 |    6 | 17.3340 | 0.4883 |  291296 B |       80.38 |
