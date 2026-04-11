```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  15.517 μs | 0.6003 μs | 0.8609 μs |  15.326 μs |  1.00 |    0.08 |    5 |  0.2136 | 0.0763 |    3632 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  15.289 μs | 0.4089 μs | 0.5597 μs |  15.125 μs |  0.99 |    0.06 |    5 |  0.2136 | 0.0763 |    3712 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.522 μs | 0.0293 μs | 0.0411 μs |   4.500 μs |  0.29 |    0.02 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.320 μs | 0.0091 μs | 0.0134 μs |   2.328 μs |  0.15 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.967 μs | 0.1749 μs | 0.2508 μs |   7.978 μs |  0.51 |    0.03 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.467 μs | 0.0048 μs | 0.0069 μs |   1.468 μs |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 320.964 μs | 1.4677 μs | 2.1049 μs | 321.777 μs | 20.74 |    1.09 |    6 | 17.0898 | 0.4883 |  291296 B |       80.20 |
