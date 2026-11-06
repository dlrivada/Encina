```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  12.935 μs |  2.1006 μs | 0.1151 μs |  1.00 |    0.01 |    4 |  0.2594 | 0.1373 |    4688 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.451 μs |  3.1238 μs | 0.1712 μs |  1.04 |    0.01 |    4 |  0.1984 | 0.0610 |    3568 B |        0.76 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.649 μs |  0.4569 μs | 0.0250 μs |  0.28 |    0.00 |    2 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.052 μs |  0.0809 μs | 0.0044 μs |  0.16 |    0.00 |    1 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.047 μs |  0.3360 μs | 0.0184 μs |  0.54 |    0.00 |    3 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.746 μs |  0.0502 μs | 0.0027 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 343.060 μs | 54.3989 μs | 2.9818 μs | 26.52 |    0.29 |    5 | 17.0898 | 0.4883 |  291296 B |       62.14 |
