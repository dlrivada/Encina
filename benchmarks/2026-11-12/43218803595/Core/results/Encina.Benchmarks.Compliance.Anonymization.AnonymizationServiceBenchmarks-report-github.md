```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.344 μs |  8.1697 μs | 0.4478 μs |  1.00 |    0.04 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.854 μs |  5.3194 μs | 0.2916 μs |  0.96 |    0.03 |    5 |  0.2289 | 0.0916 |    4164 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.667 μs |  0.1790 μs | 0.0098 μs |  0.28 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.103 μs |  0.2493 μs | 0.0137 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.562 μs |  2.2564 μs | 0.1237 μs |  0.49 |    0.02 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.708 μs |  0.0732 μs | 0.0040 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 338.679 μs | 18.6437 μs | 1.0219 μs | 25.40 |    0.73 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
