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
| &#39;Tokenize: UUID format&#39;                   |  14.191 μs |  4.1477 μs | 0.2273 μs |  1.00 |    0.02 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.802 μs |  3.9552 μs | 0.2168 μs |  1.04 |    0.02 |    5 |  0.2441 | 0.0916 |    4283 B |        1.18 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.407 μs |  0.4165 μs | 0.0228 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.369 μs |  0.0678 μs | 0.0037 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.867 μs |  1.0385 μs | 0.0569 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.814 μs |  0.0544 μs | 0.0030 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 350.153 μs | 12.1581 μs | 0.6664 μs | 24.68 |    0.34 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
