```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.670 μs |  1.5602 μs | 0.0855 μs |  1.00 |    0.01 |    4 |  0.2289 | 0.0916 |    4097 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.032 μs |  2.8362 μs | 0.1555 μs |  0.95 |    0.01 |    4 |  0.1984 | 0.0610 |    3568 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.688 μs |  0.1811 μs | 0.0099 μs |  0.27 |    0.00 |    2 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.085 μs |  0.0798 μs | 0.0044 μs |  0.15 |    0.00 |    1 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.629 μs |  0.2890 μs | 0.0158 μs |  0.48 |    0.00 |    3 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.756 μs |  0.1504 μs | 0.0082 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 337.749 μs | 15.4768 μs | 0.8483 μs | 24.71 |    0.14 |    5 | 17.0898 | 0.4883 |  291296 B |       71.10 |
