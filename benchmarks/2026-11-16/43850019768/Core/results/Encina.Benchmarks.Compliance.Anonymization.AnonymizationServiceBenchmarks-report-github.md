```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.864 μs |  3.9375 μs | 0.2158 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4097 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.722 μs |  2.9440 μs | 0.1614 μs |  0.99 |    0.02 |    5 |  0.1984 | 0.0610 |    3568 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.705 μs |  0.1055 μs | 0.0058 μs |  0.27 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.075 μs |  0.2163 μs | 0.0119 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.639 μs |  0.4048 μs | 0.0222 μs |  0.48 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.709 μs |  0.2374 μs | 0.0130 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 333.887 μs | 23.8945 μs | 1.3097 μs | 24.09 |    0.34 |    6 | 17.0898 | 0.4883 |  291296 B |       71.10 |
