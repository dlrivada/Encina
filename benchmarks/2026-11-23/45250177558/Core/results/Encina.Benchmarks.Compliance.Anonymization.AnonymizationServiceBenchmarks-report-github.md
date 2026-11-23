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
| &#39;Tokenize: UUID format&#39;                   |  11.807 μs |  3.1444 μs | 0.1724 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4098 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.150 μs | 10.0387 μs | 0.5503 μs |  1.03 |    0.04 |    5 |  0.2289 | 0.0916 |    4166 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.822 μs |  0.5623 μs | 0.0308 μs |  0.24 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.516 μs |  0.0061 μs | 0.0003 μs |  0.13 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.882 μs |  0.5011 μs | 0.0275 μs |  0.41 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.130 μs |  0.0183 μs | 0.0010 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 248.030 μs |  9.6808 μs | 0.5306 μs | 21.01 |    0.27 |    6 | 17.0898 | 0.4883 |  291296 B |       71.08 |
