```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.931 μs |  6.6395 μs | 0.3639 μs |  1.00 |    0.03 |    5 |  0.2289 | 0.0916 |    4098 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.234 μs |  2.9595 μs | 0.1622 μs |  1.02 |    0.03 |    5 |  0.2289 | 0.0916 |    4169 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.357 μs |  0.3396 μs | 0.0186 μs |  0.31 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.323 μs |  0.1879 μs | 0.0103 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.628 μs |  1.1434 μs | 0.0627 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.724 μs |  0.0606 μs | 0.0033 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 331.074 μs | 38.1219 μs | 2.0896 μs | 23.78 |    0.55 |    6 | 17.0898 | 0.4883 |  291296 B |       71.08 |
