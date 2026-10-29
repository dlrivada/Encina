```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.218 μs |  5.8696 μs | 0.3217 μs |  1.00 |    0.04 |    5 | 0.0458 | 0.0305 |    4096 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.076 μs |  9.9403 μs | 0.5449 μs |  0.99 |    0.05 |    5 | 0.0458 | 0.0305 |    4769 B |        1.16 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.955 μs |  1.5244 μs | 0.0836 μs |  0.35 |    0.01 |    3 |      - |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.295 μs |  0.4507 μs | 0.0247 μs |  0.20 |    0.01 |    2 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.961 μs |  1.3823 μs | 0.0758 μs |  0.62 |    0.02 |    4 | 0.0076 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.805 μs |  0.3681 μs | 0.0202 μs |  0.16 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 307.350 μs | 73.3084 μs | 4.0183 μs | 27.41 |    0.75 |    6 | 3.4180 |      - |  291296 B |       71.12 |
