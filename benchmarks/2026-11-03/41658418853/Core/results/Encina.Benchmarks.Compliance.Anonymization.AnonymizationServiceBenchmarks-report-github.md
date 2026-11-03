```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.40GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.068 μs | 2.2707 μs | 0.1245 μs |  1.00 |    0.01 |    5 |  0.1678 | 0.1068 |    4689 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.999 μs | 2.8065 μs | 0.1538 μs |  0.99 |    0.01 |    5 |  0.1526 | 0.0763 |    4165 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.380 μs | 0.0754 μs | 0.0041 μs |  0.26 |    0.00 |    3 |  0.0191 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.079 μs | 0.0619 μs | 0.0034 μs |  0.16 |    0.00 |    2 |  0.0153 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.276 μs | 0.2389 μs | 0.0131 μs |  0.48 |    0.00 |    4 |  0.0381 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.459 μs | 0.0472 μs | 0.0026 μs |  0.11 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 362.864 μs | 5.8918 μs | 0.3229 μs | 27.77 |    0.23 |    6 | 11.2305 |      - |  291296 B |       62.12 |
