```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,043.4 ns |  5,277.2 ns |   289.26 ns |  1.00 |    0.04 |    5 |  0.2289 | 0.0916 |    4090 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   7,963.4 ns |    924.3 ns |    50.67 ns |  0.99 |    0.03 |    5 |  0.2594 | 0.1068 |    4766 B |        1.17 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,223.0 ns |    203.0 ns |    11.13 ns |  0.28 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,196.2 ns |    347.9 ns |    19.07 ns |  0.15 |    0.01 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3,718.1 ns |    783.7 ns |    42.95 ns |  0.46 |    0.02 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     773.4 ns |    160.8 ns |     8.81 ns |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 181,725.8 ns | 81,550.0 ns | 4,470.03 ns | 22.61 |    0.85 |    6 | 17.3340 | 0.4883 |  291296 B |       71.22 |
