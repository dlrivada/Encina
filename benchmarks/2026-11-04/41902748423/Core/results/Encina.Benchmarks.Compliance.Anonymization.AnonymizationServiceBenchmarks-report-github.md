```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.056 μs |  5.1208 μs | 0.2807 μs |  1.00 |    0.03 |    5 |  0.2594 | 0.1221 |    4693 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.804 μs |  3.3558 μs | 0.1839 μs |  0.98 |    0.03 |    5 |  0.2289 | 0.0916 |    4167 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.814 μs |  0.1337 μs | 0.0073 μs |  0.25 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.550 μs |  0.0407 μs | 0.0022 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.882 μs |  0.5129 μs | 0.0281 μs |  0.44 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.140 μs |  0.1714 μs | 0.0094 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 246.504 μs | 36.9462 μs | 2.0251 μs | 22.30 |    0.51 |    6 | 17.0898 | 0.4883 |  291296 B |       62.07 |
