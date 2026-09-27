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
| &#39;Tokenize: UUID format&#39;                   |  14.043 μs |  2.4478 μs | 0.1342 μs |  1.00 |    0.01 |    5 |  0.2594 | 0.1068 |    4822 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.686 μs |  2.3102 μs | 0.1266 μs |  0.97 |    0.01 |    5 |  0.2441 | 0.0916 |    4290 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.388 μs |  0.1614 μs | 0.0088 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.322 μs |  0.5977 μs | 0.0328 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.537 μs |  0.3900 μs | 0.0214 μs |  0.54 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.753 μs |  0.0969 μs | 0.0053 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.19 |
| &#39;Risk assessment: 100-record dataset&#39;     | 341.241 μs | 15.9930 μs | 0.8766 μs | 24.30 |    0.21 |    6 | 17.0898 | 0.4883 |  291296 B |       60.41 |
