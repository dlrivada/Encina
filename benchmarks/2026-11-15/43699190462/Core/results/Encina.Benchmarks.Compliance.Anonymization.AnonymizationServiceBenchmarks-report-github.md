```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.757 μs | 1.1353 μs | 0.0622 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4095 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.302 μs | 2.1576 μs | 0.1183 μs |  1.05 |    0.01 |    5 |  0.2289 | 0.0916 |    4169 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.051 μs | 0.6736 μs | 0.0369 μs |  0.28 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.530 μs | 0.2810 μs | 0.0154 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.902 μs | 0.7863 μs | 0.0431 μs |  0.46 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.162 μs | 0.0633 μs | 0.0035 μs |  0.11 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 247.377 μs | 5.0628 μs | 0.2775 μs | 23.00 |    0.12 |    6 | 17.0898 | 0.4883 |  291296 B |       71.13 |
