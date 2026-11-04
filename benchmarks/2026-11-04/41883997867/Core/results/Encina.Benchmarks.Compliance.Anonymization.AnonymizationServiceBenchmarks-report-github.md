```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   7.918 μs | 2.2450 μs | 0.1231 μs |  1.00 |    0.02 |    4 | 0.0458 | 0.0305 |    4098 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8.025 μs | 2.2226 μs | 0.1218 μs |  1.01 |    0.02 |    4 | 0.0458 | 0.0305 |    4170 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.196 μs | 0.3657 μs | 0.0200 μs |  0.28 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.389 μs | 0.5363 μs | 0.0294 μs |  0.18 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.039 μs | 0.5146 μs | 0.0282 μs |  0.51 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.454 μs | 0.0099 μs | 0.0005 μs |  0.18 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 244.681 μs | 3.8754 μs | 0.2124 μs | 30.91 |    0.42 |    5 | 3.4180 |      - |  291296 B |       71.08 |
