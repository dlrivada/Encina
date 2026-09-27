```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.560 μs |  2.3782 μs | 0.1304 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0763 |    4226 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.884 μs |  2.8970 μs | 0.1588 μs |  0.95 |    0.01 |    5 |  0.2136 | 0.0763 |    3688 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.497 μs |  0.5513 μs | 0.0302 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.266 μs |  0.1299 μs | 0.0071 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.695 μs |  1.1409 μs | 0.0625 μs |  0.53 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.702 μs |  0.1161 μs | 0.0064 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 340.641 μs | 38.8919 μs | 2.1318 μs | 23.40 |    0.22 |    6 | 17.0898 | 0.4883 |  291296 B |       68.93 |
