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
| &#39;Tokenize: UUID format&#39;                   |  14.300 μs | 3.6251 μs | 0.1987 μs |  1.00 |    0.02 |    5 |  0.2594 | 0.1221 |    4816 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.754 μs | 2.0499 μs | 0.1124 μs |  1.03 |    0.01 |    5 |  0.2136 | 0.0763 |    3688 B |        0.77 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.737 μs | 0.2754 μs | 0.0151 μs |  0.33 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.256 μs | 0.0998 μs | 0.0055 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.747 μs | 1.4642 μs | 0.0803 μs |  0.54 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.755 μs | 0.0353 μs | 0.0019 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.19 |
| &#39;Risk assessment: 100-record dataset&#39;     | 348.886 μs | 2.8649 μs | 0.1570 μs | 24.40 |    0.30 |    6 | 17.0898 | 0.4883 |  291296 B |       60.49 |
