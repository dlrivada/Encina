```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8.308 μs | 1.5507 μs | 0.0850 μs |  1.00 |    0.01 |    4 | 0.0458 | 0.0305 |    4683 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8.523 μs | 4.2240 μs | 0.2315 μs |  1.03 |    0.03 |    4 | 0.0458 | 0.0305 |    4168 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.218 μs | 0.6336 μs | 0.0347 μs |  0.27 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.388 μs | 0.1281 μs | 0.0070 μs |  0.17 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3.979 μs | 0.9512 μs | 0.0521 μs |  0.48 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.485 μs | 0.0962 μs | 0.0053 μs |  0.18 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 251.124 μs | 9.8909 μs | 0.5422 μs | 30.23 |    0.28 |    5 | 3.4180 |      - |  291296 B |       62.20 |
