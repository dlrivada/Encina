```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.245 μs |  2.5432 μs | 0.1394 μs |  1.00 |    0.01 |    5 |  0.1526 | 0.0763 |    4226 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.421 μs |  5.6225 μs | 0.3082 μs |  1.01 |    0.02 |    5 |  0.1526 | 0.0763 |    4288 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.454 μs |  0.1482 μs | 0.0081 μs |  0.31 |    0.00 |    3 |  0.0229 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.561 μs |  0.0431 μs | 0.0024 μs |  0.18 |    0.00 |    2 |  0.0153 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.882 μs |  0.6529 μs | 0.0358 μs |  0.55 |    0.01 |    4 |  0.0305 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.452 μs |  0.2299 μs | 0.0126 μs |  0.10 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 360.216 μs | 26.5123 μs | 1.4532 μs | 25.29 |    0.23 |    6 | 11.2305 |      - |  291296 B |       68.93 |
