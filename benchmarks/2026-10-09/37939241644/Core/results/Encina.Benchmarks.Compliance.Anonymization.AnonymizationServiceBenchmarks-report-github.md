```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.506 μs |  2.6169 μs | 0.1434 μs |  1.00 |    0.02 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.923 μs |  3.2005 μs | 0.1754 μs |  1.04 |    0.02 |    5 |  0.2136 | 0.0763 |    3688 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.445 μs |  0.1542 μs | 0.0085 μs |  0.30 |    0.00 |    3 |  0.0343 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.759 μs |  0.0649 μs | 0.0036 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.826 μs |  0.4426 μs | 0.0243 μs |  0.51 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.111 μs |  0.1897 μs | 0.0104 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 247.023 μs | 60.2067 μs | 3.3001 μs | 21.47 |    0.34 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
