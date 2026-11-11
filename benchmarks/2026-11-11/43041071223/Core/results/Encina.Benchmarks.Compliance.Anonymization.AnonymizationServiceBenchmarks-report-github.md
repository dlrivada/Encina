```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.89GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.098 μs | 3.2832 μs | 0.1800 μs |  1.00 |    0.02 |    5 |  0.1526 | 0.0916 |    4091 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.230 μs | 6.4633 μs | 0.3543 μs |  1.01 |    0.03 |    5 |  0.1526 | 0.0763 |    4157 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.375 μs | 0.1474 μs | 0.0081 μs |  0.26 |    0.00 |    3 |  0.0191 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.055 μs | 0.1122 μs | 0.0062 μs |  0.16 |    0.00 |    2 |  0.0153 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.159 μs | 0.0717 μs | 0.0039 μs |  0.47 |    0.01 |    4 |  0.0381 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.446 μs | 0.1546 μs | 0.0085 μs |  0.11 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 359.599 μs | 9.3897 μs | 0.5147 μs | 27.46 |    0.33 |    6 | 11.2305 |      - |  291296 B |       71.20 |
