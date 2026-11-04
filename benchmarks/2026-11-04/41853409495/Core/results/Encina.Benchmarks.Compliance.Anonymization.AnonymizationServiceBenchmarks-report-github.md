```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8.996 μs |  3.0381 μs | 0.1665 μs |  1.00 |    0.02 |    4 | 0.0458 | 0.0305 |    4095 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   9.021 μs |  4.4509 μs | 0.2440 μs |  1.00 |    0.03 |    4 | 0.0458 | 0.0305 |    4168 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.357 μs |  0.2522 μs | 0.0138 μs |  0.26 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.529 μs |  0.1004 μs | 0.0055 μs |  0.17 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.303 μs |  0.3954 μs | 0.0217 μs |  0.48 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.562 μs |  0.0699 μs | 0.0038 μs |  0.17 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 263.471 μs | 47.8373 μs | 2.6221 μs | 29.30 |    0.54 |    5 | 3.4180 |      - |  291296 B |       71.13 |
