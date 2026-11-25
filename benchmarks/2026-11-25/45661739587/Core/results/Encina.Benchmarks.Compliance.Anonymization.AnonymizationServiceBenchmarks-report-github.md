```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8.173 μs | 1.4930 μs | 0.0818 μs |  1.00 |    0.01 |    4 | 0.0458 | 0.0305 |    4093 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8.260 μs | 5.2226 μs | 0.2863 μs |  1.01 |    0.03 |    4 | 0.0458 | 0.0305 |    4766 B |        1.16 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.230 μs | 0.2295 μs | 0.0126 μs |  0.27 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.396 μs | 0.1908 μs | 0.0105 μs |  0.17 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3.864 μs | 0.7172 μs | 0.0393 μs |  0.47 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.486 μs | 0.0423 μs | 0.0023 μs |  0.18 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 248.459 μs | 6.2921 μs | 0.3449 μs | 30.40 |    0.27 |    5 | 3.4180 |      - |  291296 B |       71.17 |
