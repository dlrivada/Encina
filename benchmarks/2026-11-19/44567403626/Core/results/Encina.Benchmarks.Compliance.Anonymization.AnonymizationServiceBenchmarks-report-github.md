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
| &#39;Tokenize: UUID format&#39;                   |  11.756 μs |  8.2897 μs | 0.4544 μs |  1.00 |    0.05 |    5 |  0.2289 | 0.0916 |    4093 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.613 μs |  6.8331 μs | 0.3745 μs |  0.99 |    0.04 |    5 |  0.1984 | 0.0610 |    3568 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.764 μs |  0.1209 μs | 0.0066 μs |  0.24 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.556 μs |  0.0492 μs | 0.0027 μs |  0.13 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.881 μs |  0.5809 μs | 0.0318 μs |  0.42 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.138 μs |  0.1617 μs | 0.0089 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 248.511 μs | 17.4884 μs | 0.9586 μs | 21.16 |    0.70 |    6 | 17.3340 | 0.4883 |  291296 B |       71.17 |
