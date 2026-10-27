```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.603 μs |  1.6867 μs | 0.0925 μs |  1.00 |    0.01 |    5 |  0.2594 | 0.1068 |    4695 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.911 μs |  2.8140 μs | 0.1542 μs |  0.95 |    0.01 |    5 |  0.1984 | 0.0610 |    3568 B |        0.76 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.395 μs |  0.4741 μs | 0.0260 μs |  0.30 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.359 μs |  0.1563 μs | 0.0086 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.731 μs |  0.3798 μs | 0.0208 μs |  0.53 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.716 μs |  0.0768 μs | 0.0042 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 335.750 μs | 21.0822 μs | 1.1556 μs | 22.99 |    0.14 |    6 | 17.0898 | 0.4883 |  291296 B |       62.04 |
