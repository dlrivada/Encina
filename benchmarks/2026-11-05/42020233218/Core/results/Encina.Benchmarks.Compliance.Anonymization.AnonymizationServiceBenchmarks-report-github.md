```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.816 μs |  4.6699 μs | 0.2560 μs |  1.00 |    0.03 |    5 |  0.2289 | 0.0916 |    4094 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.122 μs |  0.4464 μs | 0.0245 μs |  1.03 |    0.02 |    5 |  0.2594 | 0.1068 |    4762 B |        1.16 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.771 μs |  0.1052 μs | 0.0058 μs |  0.26 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.531 μs |  0.0146 μs | 0.0008 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.337 μs |  1.1705 μs | 0.0642 μs |  0.49 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.118 μs |  0.1110 μs | 0.0061 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 256.283 μs | 44.5708 μs | 2.4431 μs | 23.70 |    0.52 |    6 | 17.0898 | 0.4883 |  291296 B |       71.15 |
