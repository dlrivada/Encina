```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.021 μs | 3.9768 μs | 0.2180 μs |  1.00 |    0.02 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.504 μs | 6.6713 μs | 0.3657 μs |  1.04 |    0.03 |    5 |  0.2289 | 0.0916 |    4168 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.828 μs | 0.1888 μs | 0.0103 μs |  0.26 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.526 μs | 0.0183 μs | 0.0010 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.902 μs | 0.4311 μs | 0.0236 μs |  0.44 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.112 μs | 0.2351 μs | 0.0129 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 245.393 μs | 3.3513 μs | 0.1837 μs | 22.27 |    0.38 |    6 | 17.3340 | 0.4883 |  291296 B |       83.32 |
