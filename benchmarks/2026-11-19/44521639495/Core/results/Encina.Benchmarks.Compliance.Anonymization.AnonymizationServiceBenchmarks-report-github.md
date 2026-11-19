```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.912 μs |  3.3574 μs | 0.1840 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4098 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.939 μs |  3.7373 μs | 0.2049 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4165 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.651 μs |  0.1608 μs | 0.0088 μs |  0.26 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.111 μs |  0.3947 μs | 0.0216 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.652 μs |  0.2098 μs | 0.0115 μs |  0.48 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.760 μs |  0.0694 μs | 0.0038 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 348.698 μs | 10.7262 μs | 0.5879 μs | 25.07 |    0.29 |    6 | 17.0898 | 0.4883 |  291296 B |       71.08 |
