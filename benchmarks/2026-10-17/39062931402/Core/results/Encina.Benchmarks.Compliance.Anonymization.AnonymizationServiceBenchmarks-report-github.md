```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.461 μs | 3.6792 μs | 0.2017 μs |  1.00 |    0.02 |    5 |  0.2441 | 0.1221 |    4741 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.247 μs | 2.8603 μs | 0.1568 μs |  0.99 |    0.02 |    5 |  0.2441 | 0.1526 |    4804 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.428 μs | 0.1962 μs | 0.0108 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.311 μs | 0.1018 μs | 0.0056 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.914 μs | 0.2332 μs | 0.0128 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.707 μs | 0.0607 μs | 0.0033 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 340.289 μs | 9.8844 μs | 0.5418 μs | 23.53 |    0.29 |    6 | 17.0898 | 0.4883 |  291296 B |       61.44 |
