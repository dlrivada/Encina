```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  12.909 μs | 3.8508 μs | 0.2111 μs |  1.00 |    0.02 |    4 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.147 μs | 1.9604 μs | 0.1075 μs |  1.02 |    0.02 |    4 |  0.2594 | 0.1373 |    4761 B |        1.36 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.662 μs | 0.1161 μs | 0.0064 μs |  0.28 |    0.00 |    2 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.076 μs | 0.2949 μs | 0.0162 μs |  0.16 |    0.00 |    1 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.059 μs | 0.7808 μs | 0.0428 μs |  0.55 |    0.01 |    3 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.752 μs | 0.0397 μs | 0.0022 μs |  0.14 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 341.499 μs | 3.8348 μs | 0.2102 μs | 26.46 |    0.38 |    5 | 17.0898 | 0.4883 |  291296 B |       83.32 |
