```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.394 μs | 3.1995 μs | 0.1754 μs |  1.00 |    0.02 |    5 |  0.2594 | 0.1068 |    4817 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.512 μs | 1.6353 μs | 0.0896 μs |  1.01 |    0.02 |    5 |  0.2441 | 0.0916 |    4290 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.448 μs | 1.1241 μs | 0.0616 μs |  0.30 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.776 μs | 0.1660 μs | 0.0091 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.845 μs | 0.3806 μs | 0.0209 μs |  0.51 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.156 μs | 0.2514 μs | 0.0138 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.19 |
| &#39;Risk assessment: 100-record dataset&#39;     | 251.439 μs | 7.2748 μs | 0.3988 μs | 22.07 |    0.30 |    6 | 17.0898 | 0.4883 |  291296 B |       60.47 |
