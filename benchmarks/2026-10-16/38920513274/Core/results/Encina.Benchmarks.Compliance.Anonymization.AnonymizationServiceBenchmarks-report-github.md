```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9,041.8 ns |  5,661.9 ns |   310.35 ns |  1.00 |    0.04 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,973.3 ns |  3,255.8 ns |   178.46 ns |  0.99 |    0.03 |    5 |  0.2441 | 0.0916 |    4290 B |        1.18 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,738.4 ns |  1,660.0 ns |    90.99 ns |  0.30 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,446.4 ns |    728.5 ns |    39.93 ns |  0.16 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,504.3 ns |  1,356.2 ns |    74.34 ns |  0.50 |    0.02 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     816.6 ns |    571.2 ns |    31.31 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 188,257.3 ns | 26,366.4 ns | 1,445.23 ns | 20.84 |    0.64 |    6 | 17.3340 | 0.4883 |  291296 B |       80.38 |
