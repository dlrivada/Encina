```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.761 μs |  1.7045 μs | 0.0934 μs |  1.00 |    0.01 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.476 μs |  2.6347 μs | 0.1444 μs |  0.97 |    0.01 |    5 |  0.2289 | 0.1068 |    4163 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.051 μs |  0.1549 μs | 0.0085 μs |  0.28 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.499 μs |  0.0424 μs | 0.0023 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.836 μs |  0.2882 μs | 0.0158 μs |  0.45 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.086 μs |  0.0164 μs | 0.0009 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 248.089 μs | 48.7052 μs | 2.6697 μs | 23.06 |    0.28 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
