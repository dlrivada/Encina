```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  12.793 μs |  4.0687 μs | 0.2230 μs |  1.00 |    0.02 |    4 |  0.2594 | 0.1221 |    4697 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.949 μs |  2.5350 μs | 0.1390 μs |  1.01 |    0.02 |    4 |  0.2594 | 0.1221 |    4753 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.699 μs |  0.2806 μs | 0.0154 μs |  0.29 |    0.00 |    2 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.068 μs |  0.5616 μs | 0.0308 μs |  0.16 |    0.00 |    1 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.097 μs |  0.0587 μs | 0.0032 μs |  0.55 |    0.01 |    3 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.762 μs |  0.1266 μs | 0.0069 μs |  0.14 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 330.332 μs | 13.8594 μs | 0.7597 μs | 25.83 |    0.40 |    5 | 17.0898 | 0.4883 |  291296 B |       62.02 |
