```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.736 μs |  2.4849 μs | 0.1362 μs |  1.00 |    0.02 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.922 μs |  4.5936 μs | 0.2518 μs |  1.02 |    0.02 |    5 |  0.1984 | 0.0610 |    3568 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.809 μs |  0.3320 μs | 0.0182 μs |  0.26 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.510 μs |  0.0691 μs | 0.0038 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.944 μs |  0.2585 μs | 0.0142 μs |  0.46 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.102 μs |  0.4751 μs | 0.0260 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 244.024 μs | 23.7618 μs | 1.3025 μs | 22.73 |    0.27 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
