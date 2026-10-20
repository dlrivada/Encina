```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.474 μs | 3.7677 μs | 0.2065 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.1068 |    4218 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.325 μs | 4.8812 μs | 0.2676 μs |  0.99 |    0.02 |    5 |  0.2441 | 0.1068 |    4288 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.362 μs | 0.3044 μs | 0.0167 μs |  0.30 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.362 μs | 0.1477 μs | 0.0081 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.316 μs | 0.3166 μs | 0.0174 μs |  0.57 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.744 μs | 0.0168 μs | 0.0009 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 344.981 μs | 2.1740 μs | 0.1192 μs | 23.84 |    0.29 |    6 | 17.0898 | 0.4883 |  291296 B |       69.06 |
