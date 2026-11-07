```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.47GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.043 μs | 4.1091 μs | 0.2252 μs |  1.00 |    0.02 |    5 |  0.1526 | 0.0916 |    4091 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.689 μs | 3.1683 μs | 0.1737 μs |  0.97 |    0.02 |    5 |  0.1526 | 0.0763 |    4169 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.389 μs | 0.0938 μs | 0.0051 μs |  0.26 |    0.00 |    3 |  0.0191 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.059 μs | 0.0685 μs | 0.0038 μs |  0.16 |    0.00 |    2 |  0.0153 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.154 μs | 0.2408 μs | 0.0132 μs |  0.47 |    0.01 |    4 |  0.0381 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.438 μs | 0.0585 μs | 0.0032 μs |  0.11 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 358.959 μs | 7.9330 μs | 0.4348 μs | 27.53 |    0.41 |    6 | 11.2305 |      - |  291296 B |       71.20 |
