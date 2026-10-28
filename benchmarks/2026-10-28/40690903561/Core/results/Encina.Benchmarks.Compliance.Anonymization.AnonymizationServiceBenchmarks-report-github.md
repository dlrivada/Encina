```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.315 μs |  3.1860 μs | 0.1746 μs |  1.00 |    0.01 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.278 μs |  0.2418 μs | 0.0133 μs |  1.00 |    0.01 |    5 |  0.2441 | 0.0916 |    4679 B |        1.34 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.397 μs |  0.4581 μs | 0.0251 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.271 μs |  0.6353 μs | 0.0348 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.027 μs |  0.4291 μs | 0.0235 μs |  0.56 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.419 μs |  0.0476 μs | 0.0026 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 321.807 μs | 10.1607 μs | 0.5569 μs | 22.48 |    0.24 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
