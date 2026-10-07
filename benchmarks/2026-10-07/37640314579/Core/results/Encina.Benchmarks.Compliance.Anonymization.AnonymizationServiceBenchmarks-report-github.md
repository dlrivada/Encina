```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.737 μs |  4.9670 μs | 0.2723 μs |  1.00 |    0.03 |    5 |  0.2289 | 0.0916 |    4223 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.680 μs |  1.0468 μs | 0.0574 μs |  1.00 |    0.02 |    5 |  0.2136 | 0.0763 |    3688 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.362 μs |  0.0889 μs | 0.0049 μs |  0.29 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.726 μs |  0.0601 μs | 0.0033 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.427 μs |  1.2835 μs | 0.0704 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.131 μs |  0.0368 μs | 0.0020 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 255.078 μs | 49.8580 μs | 2.7329 μs | 21.74 |    0.48 |    6 | 17.0898 | 0.4883 |  291296 B |       68.98 |
