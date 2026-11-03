```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|------------:|-----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.114 μs |   2.6326 μs |  0.1443 μs |  1.00 |    0.02 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.305 μs |   2.0559 μs |  0.1127 μs |  1.02 |    0.01 |    5 |  0.2289 | 0.0916 |    4165 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.759 μs |   0.1730 μs |  0.0095 μs |  0.25 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.623 μs |   0.4892 μs |  0.0268 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.972 μs |   0.7359 μs |  0.0403 μs |  0.45 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.125 μs |   0.0676 μs |  0.0037 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 263.945 μs | 331.9664 μs | 18.1962 μs | 23.75 |    1.44 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
